﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning.Action;
using Microsoft.SemanticKernel.SkillDefinition;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using NS of Plan
namespace Microsoft.SemanticKernel.Planning;
#pragma warning restore IDE0130

/// <summary>
/// Action Planner allows to select one function out of many, to achieve a given goal.
/// The planner implement the Intent Detection pattern, uses the functions registered
/// in the kernel to see if there's a relevant one, providing instructions to call the
/// function and the rationale used to select it. The planner can also return
/// "no function" is nothing relevant is available.
/// The rationale is currently available only in the prompt, we might include it in
/// the Plan object in future.
/// </summary>
public sealed class ActionPlanner
{
    private const string StopSequence = "#END-OF-PLAN";
    private const string SkillName = "this";

    // Planner semantic function
    private readonly ISKFunction _plannerFunction;

    // Context used to access the list of functions in the kernel
    private readonly SKContext _context;
    private readonly IKernel _kernel;

    // TODO: allow to inject skill store
    /// <summary>
    /// Initialize a new instance of the <see cref="ActionPlanner"/> class.
    /// </summary>
    /// <param name="kernel">The semantic kernel instance.</param>
    /// <param name="prompt">Optional prompt override</param>
    public ActionPlanner(
        IKernel kernel,
        string? prompt = null)
    {
        Verify.NotNull(kernel, "The planner requires a non-null kernel instance");

        string promptTemplate = prompt ?? EmbeddedResource.Read("skprompt.txt");

        this._plannerFunction = kernel.CreateSemanticFunction(
            skillName: SkillName,
            promptTemplate: promptTemplate,
            maxTokens: 1024,
            stopSequences: new[] { StopSequence });

        kernel.ImportSkill(this, skillName: SkillName);

        this._kernel = kernel;
        this._context = kernel.CreateNewContext();
    }

    public async Task<Plan> CreatePlanAsync(string goal)
    {
        if (string.IsNullOrEmpty(goal))
        {
            throw new PlanningException(PlanningException.ErrorCodes.InvalidGoal, "The goal specified is empty");
        }

        SKContext result = await this._plannerFunction.InvokeAsync(goal, this._context).ConfigureAwait(false);

        var json = """{"plan":{ "rationale":""" + result;

        // extract and parse JSON
        ActionPlanResponse? planData;
        try
        {
            planData = JsonSerializer.Deserialize<ActionPlanResponse?>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DictionaryKeyPolicy = null,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception e)
        {
            throw new PlanningException(PlanningException.ErrorCodes.InvalidPlan,
                "Plan parsing error, invalid JSON", e);
        }

        if (planData == null)
        {
            throw new PlanningException(PlanningException.ErrorCodes.InvalidPlan, "The plan deserialized to a null object");
        }

        // Build and return plan
        ISKFunction function;
        if (planData.Plan.Function.Contains("."))
        {
            var parts = planData.Plan.Function.Split('.');
            function = this._context.Skills!.GetFunction(parts[0], parts[1]);
        }
        else
        {
            function = this._context.Skills!.GetFunction(planData.Plan.Function);
        }

        var plan = new Plan(goal);
        plan.AddSteps(function);

        // Create a plan using the function and the parameters suggested by the planner
        var variables = new ContextVariables();
        foreach (KeyValuePair<string, string> p in planData.Plan.Parameters)
        {
            plan.State[p.Key] = p.Value;
        }

        //Console.WriteLine(JsonSerializer.Serialize(planData, new JsonSerializerOptions { WriteIndented = true }));

        var context = this._kernel.CreateNewContext();
        context.Variables.Update(variables);

        return plan;
    }

    // TODO: use goal to find relevant functions in a skill store
    /// <summary>
    /// Native function returning a list of all the functions in the current context,
    /// excluding functions in the planner itself.
    /// </summary>
    /// <param name="goal">Currently unused. Will be used to handle long lists of functions.</param>
    /// <param name="context">Function execution context</param>
    /// <returns>List of functions, formatted accordingly to the prompt</returns>
    [SKFunction("List all functions available in the kernel")]
    [SKFunctionName("ListOfFunctions")]
    [SKFunctionInput(Description = "The current goal processed by the planner", DefaultValue = "")]
    public string ListOfFunctions(string goal, SKContext context)
    {
        Verify.NotNull(context.Skills, "The planner requires a non-null skill collection");
        var functionsAvailable = context.Skills.GetFunctionsView();

        // Prepare list using the format used by skprompt.txt
        var list = new StringBuilder();
        this.PopulateList(list, functionsAvailable.NativeFunctions);
        this.PopulateList(list, functionsAvailable.SemanticFunctions);

        return list.ToString();
    }

    // TODO: generate string programmatically
    // TODO: use goal to find relevant examples
    [SKFunction("List a few good examples of plans to generate")]
    [SKFunctionName("GoodExamples")]
    [SKFunctionInput(Description = "The current goal processed by the planner", DefaultValue = "")]
    public string GoodExamples(string goal, SKContext context)
    {
        return """
[EXAMPLE]
- List of functions:
// Read a file.
FileIOSkill.ReadAsync
Parameter "path": Source file.
// Write a file.
FileIOSkill.WriteAsync
Parameter "path": Destination file.
Parameter "content": File content.
// Get the current time.
TimeSkill.Time
No parameters.
// Makes a POST request to a uri.
HttpSkill.PostAsync
Parameter "body": The body of the request.
- End list of functions.
Goal: create a file called "something.txt".
{"plan":{
"rationale": "the list contains a function that allows to create files",
"function": "FileIOSkill.WriteAsync",
"parameters": {
"path": "something.txt",
"content": null
}}}
#END-OF-PLAN
""";
    }

    // TODO: generate string programmatically
    [SKFunction("List a few edge case examples of plans to handle")]
    [SKFunctionName("EdgeCaseExamples")]
    [SKFunctionInput(Description = "The current goal processed by the planner", DefaultValue = "")]
    public string EdgeCaseExamples(string goal, SKContext context)
    {
        return """
[EXAMPLE]
- List of functions:
// Get the current time.
TimeSkill.Time
No parameters.
// Write a file.
FileIOSkill.WriteAsync
Parameter "path": Destination file.
Parameter "content": File content.
// Makes a POST request to a uri.
HttpSkill.PostAsync
Parameter "body": The body of the request.
// Read a file.
FileIOSkill.ReadAsync
Parameter "path": Source file.
- End list of functions.
Goal: tell me a joke.
{"plan":{
"rationale": "the list does not contain functions to tell jokes or something funny",
"function": "",
"parameters": {}
}}}
#END-OF-PLAN
""";
    }

    private void PopulateList(StringBuilder list, IDictionary<string, List<FunctionView>> functions)
    {
        foreach (KeyValuePair<string, List<FunctionView>> skill in functions)
        {
            // Skip this planner skills
            if (string.Equals(skill.Key, SkillName, StringComparison.OrdinalIgnoreCase)) { continue; }

            foreach (FunctionView func in skill.Value)
            {
                // Function description
                list.AppendLine($"// {AddPeriod(func.Description)}");

                // Function name
                list.AppendLine($"{func.SkillName}.{func.Name}");

                // Function parameters
                foreach (var p in func.Parameters)
                {
                    var description = string.IsNullOrEmpty(p.Description) ? p.Name : p.Description;
                    list.AppendLine($"Parameter \"{p.Name}\": {AddPeriod(description)}");
                }
            }
        }
    }

    private static string AddPeriod(string x)
    {
        return x.EndsWith(".", StringComparison.Ordinal) ? x : $"{x}.";
    }
}
