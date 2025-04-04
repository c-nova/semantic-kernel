﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace ImportDocument;

/// <summary>
/// This console app imports a file to the CopilotChat WebAPI document memory store.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var config = Config.GetConfig();
        if (!Config.Validate(config))
        {
            Console.WriteLine("Error: Failed to read appsettings.json.");
            return;
        }

        var fileOption = new Option<FileInfo>(name: "--file", description: "The file to import to document memory store.")
        {
            IsRequired = true
        };

        var userCollectionOption = new Option<bool>(
            name: "--user-collection",
            description: "Save the extracted context to an isolated user collection.",
            getDefaultValue: () => false
        );

        var rootCommand = new RootCommand(
            "This console app imports a file to the CopilotChat WebAPI's document memory store."
        )
        {
            fileOption, userCollectionOption
        };

        rootCommand.SetHandler(async (file, userCollection) =>
            {
                await UploadFileAsync(file, config!, userCollection);
            },
            fileOption, userCollectionOption
        );

        rootCommand.Invoke(args);
    }

    /// <summary>
    /// Acquires a user unique ID from Azure AD.
    /// </summary>
    private static async Task<string?> AcquireUserIdAsync(Config config)
    {
        Console.WriteLine("Requesting User Account ID...");

        string[] scopes = { "User.Read" };
        try
        {
            var app = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithRedirectUri(config.RedirectUri)
                .Build();
            var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            var accounts = await app.GetAccountsAsync();
            if (!accounts.Any())
            {
                Console.WriteLine("Error: No accounts found");
                return null;
            }

            return accounts.First().HomeAccountId.Identifier;
        }
        catch (Exception ex) when (ex is MsalServiceException || ex is MsalClientException)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Conditionally uploads a file to the Document Store for parsing.
    /// </summary>
    /// <param name="file">The file to upload for injection.</param>
    /// <param name="config">Configuration.</param>
    /// <param name="toUserCollection">Save the extracted context to an isolated user collection.</param>
    private static async Task UploadFileAsync(FileInfo file, Config config, bool toUserCollection)
    {
        if (!file.Exists)
        {
            Console.WriteLine($"File {file.FullName} does not exist.");
            return;
        }

        using var fileContent = new StreamContent(file.OpenRead());
        using var formContent = new MultipartFormDataContent
        {
            { fileContent, "formFile", file.Name }
        };
        if (toUserCollection)
        {
            Console.WriteLine("Uploading and parsing file to user collection...");
            var userId = await AcquireUserIdAsync(config);

            if (userId != null)
            {
                using var userScopeContent = new StringContent("User");
                using var userIdContent = new StringContent(userId);
                formContent.Add(userScopeContent, "documentScope");
                formContent.Add(userIdContent, "userId");

                // Calling UploadAsync here to make sure disposable objects are still in scope.
                await UploadAsync(formContent, config);
            }
        }
        else
        {
            Console.WriteLine("Uploading and parsing file to global collection...");
            using var globalScopeContent = new StringContent("Global");
            formContent.Add(globalScopeContent, "documentScope");

            // Calling UploadAsync here to make sure disposable objects are still in scope.
            await UploadAsync(formContent, config);
        }
    }

    /// <summary>
    /// Sends a POST request to the Document Store to upload a file for parsing.
    /// </summary>
    /// <param name="multipartFormDataContent">The multipart form data content to send.</param>
    /// <param name="config">Configuration.</param>
    private static async Task UploadAsync(MultipartFormDataContent multipartFormDataContent, Config config)
    {
        // Create a HttpClient instance and set the timeout to infinite since
        // large documents will take a while to parse.
        using HttpClientHandler clientHandler = new()
        {
            CheckCertificateRevocationList = true
        };
        using HttpClient httpClient = new(clientHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                new Uri(new Uri(config.ServiceUri), "importDocument"),
                multipartFormDataContent
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return;
            }

            Console.WriteLine("Uploading and parsing successful.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
