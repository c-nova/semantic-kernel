﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernel.Planning;

/// <summary>
/// Standard Semantic Kernel callable plan.
/// Plan is used to create trees of <see cref="ISKFunction"/>s.
/// </summary>
public sealed class Plan : ISKFunction
{
    /// <summary>
    /// State of the plan
    /// </summary>
    [JsonPropertyName("state")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables State { get; } = new();

    /// <summary>
    /// Steps of the plan
    /// </summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<Plan> Steps => this._steps.AsReadOnly();

    /// <summary>
    /// Named parameters for the function
    /// </summary>
    [JsonPropertyName("named_parameters")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables NamedParameters { get; set; } = new();

    /// <summary>
    /// Named outputs for the function
    /// </summary>
    [JsonPropertyName("named_outputs")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables NamedOutputs { get; set; } = new();

    /// <summary>
    /// Gets whether the plan has a next step.
    /// </summary>
    [JsonIgnore]
    public bool HasNextStep => this.NextStepIndex < this.Steps.Count;

    /// <summary>
    /// Gets the next step index.
    /// </summary>
    [JsonPropertyName("next_step_index")]
    public int NextStepIndex { get; private set; }

    #region ISKFunction implementation

    /// <inheritdoc/>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("skill_name")]
    public string SkillName { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsSemantic { get; private set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public CompleteRequestSettings RequestSettings { get; private set; } = new();

    #endregion ISKFunction implementation

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    public Plan(string goal)
    {
        this.Description = goal;
        this.SkillName = this.GetType().FullName;
        this.Name = goal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params ISKFunction[] steps) : this(goal)
    {
        this.AddSteps(steps);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params Plan[] steps) : this(goal)
    {
        this.AddSteps(steps);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    public Plan(ISKFunction function)
    {
        this.SetFunction(function);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function and steps.
    /// </summary>
    /// <param name="name">The name of the plan.</param>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="description">The description of the plan.</param>
    /// <param name="nextStepIndex">The index of the next step.</param>
    /// <param name="state">The state of the plan.</param>
    /// <param name="namedParameters">The named parameters of the plan.</param>
    /// <param name="namedOutputs">The named outputs of the plan.</param>
    /// <param name="steps">The steps of the plan.</param>
    [JsonConstructor]
    public Plan(string name, string skillName, string description, int nextStepIndex, ContextVariables state, ContextVariables namedParameters,
        ContextVariables namedOutputs,
        IReadOnlyList<Plan> steps)
    {
        this.Name = name;
        this.SkillName = skillName;
        this.Description = description;
        this.NextStepIndex = nextStepIndex;
        this.State = state;
        this.NamedParameters = namedParameters;
        this.NamedOutputs = namedOutputs;
        this._steps.Clear();
        this.AddSteps(steps.ToArray());
    }

    /// <summary>
    /// Deserialize a JSON string into a Plan object.
    /// </summary>
    /// <param name="json">JSON string representation of a Plan</param>
    /// <param name="context">The context to use for function registrations.</param>
    /// <returns>An instance of a Plan object.</returns>
    /// <remarks>If Context is not supplied, plan will not be able to execute.</remarks>
    public static Plan FromJson(string json, SKContext? context = null)
    {
        var plan = JsonSerializer.Deserialize<Plan>(json, new JsonSerializerOptions() { IncludeFields = true }) ?? new Plan(string.Empty);

        if (context != null)
        {
            plan = SetRegisteredFunctions(plan, context);
        }

        return plan;
    }

    /// <summary>
    /// Get JSON representation of the plan.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Adds one or more existing plans to the end of the current plan as steps.
    /// </summary>
    /// <param name="steps">The plans to add as steps to the current plan.</param>
    /// <remarks>
    /// When you add a plan as a step to the current plan, the steps of the added plan are executed after the steps of the current plan have completed.
    /// </remarks>
    public void AddSteps(params Plan[] steps)
    {
        this._steps.AddRange(steps);
    }

    /// <summary>
    /// Adds one or more new steps to the end of the current plan.
    /// </summary>
    /// <param name="steps">The steps to add to the current plan.</param>
    /// <remarks>
    /// When you add a new step to the current plan, it is executed after the previous step in the plan has completed. Each step can be a function call or another plan.
    /// </remarks>
    public void AddSteps(params ISKFunction[] steps)
    {
        this._steps.AddRange(steps.Select(step => new Plan(step)));
    }

    /// <summary>
    /// Runs the next step in the plan using the provided kernel instance and variables.
    /// </summary>
    /// <param name="kernel">The kernel instance to use for executing the plan.</param>
    /// <param name="variables">The variables to use for the execution of the plan.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the execution of the plan.</param>
    /// <returns>A task representing the asynchronous execution of the plan's next step.</returns>
    /// <remarks>
    /// This method executes the next step in the plan using the specified kernel instance and context variables. The context variables contain the necessary information for executing the plan, such as the memory, skills, and logger. The method returns a task representing the asynchronous execution of the plan's next step.
    /// </remarks>
    public Task<Plan> RunNextStepAsync(IKernel kernel, ContextVariables variables, CancellationToken cancellationToken = default)
    {
        var context = new SKContext(
            variables,
            kernel.Memory,
            kernel.Skills,
            kernel.Log,
            cancellationToken);
        return this.InvokeNextStepAsync(context);
    }

    /// <summary>
    /// Invoke the next step of the plan
    /// </summary>
    /// <param name="context">Context to use</param>
    /// <returns>The updated plan</returns>
    /// <exception cref="KernelException">If an error occurs while running the plan</exception>
    public async Task<Plan> InvokeNextStepAsync(SKContext context)
    {
        if (this.HasNextStep)
        {
            var step = this.Steps[this.NextStepIndex];

            // Merge the state with the current context variables for step execution
            var functionVariables = this.GetNextStepVariables(context.Variables, step);

            // Execute the step
            var functionContext = new SKContext(functionVariables, context.Memory, context.Skills, context.Log, context.CancellationToken);
            var result = await step.InvokeAsync(functionContext).ConfigureAwait(false);

            if (result.ErrorOccurred)
            {
                throw new KernelException(KernelException.ErrorCodes.FunctionInvokeError,
                    $"Error occurred while running plan step: {context.LastErrorDescription}", context.LastException);
            }

            #region Update State

            // Update state with result
            this.State.Update(result.Result.Trim());

            // Update state with named outputs (if any)
            foreach (var item in step.NamedOutputs)
            {
                // ignore the input key
                if (item.Key.ToUpperInvariant() == "INPUT")
                {
                    continue;
                }

                this.State.Set(item.Key, result.Result.Trim());
            }

            #endregion Update State

            this.NextStepIndex++;
        }

        return this;
    }

    #region ISKFunction implementation

    /// <inheritdoc/>
    public FunctionView Describe()
    {
        // TODO - Eventually, we should be able to describe a plan and it's expected inputs/outputs
        return this.Function?.Describe() ?? new();
    }

    /// <inheritdoc/>
    public Task<SKContext> InvokeAsync(string input, SKContext? context = null, CompleteRequestSettings? settings = null, ILogger? log = null,
        CancellationToken? cancel = null)
    {
        context ??= new SKContext(new ContextVariables(), null!, null, log ?? NullLogger.Instance, cancel ?? CancellationToken.None);

        context.Variables.Update(input);

        return this.InvokeAsync(context, settings, log, cancel);
    }

    /// <inheritdoc/>
    public async Task<SKContext> InvokeAsync(SKContext? context = null, CompleteRequestSettings? settings = null, ILogger? log = null,
        CancellationToken? cancel = null)
    {
        context ??= new SKContext(this.State, null!, null, log ?? NullLogger.Instance, cancel ?? CancellationToken.None);

        if (this.Function is not null)
        {
            var result = await this.Function.InvokeAsync(context, settings, log, cancel).ConfigureAwait(false);

            if (result.ErrorOccurred)
            {
                result.Log.LogError(
                    result.LastException,
                    "Something went wrong in plan step {0}.{1}:'{2}'", this.SkillName, this.Name, context.LastErrorDescription);
                return result;
            }

            context.Variables.Update(result.Result.ToString());
        }
        else
        {
            // loop through steps and execute until completion
            while (this.HasNextStep)
            {
                var functionContext = context;

                AddVariablesToContext(this.State, functionContext);

                await this.InvokeNextStepAsync(functionContext).ConfigureAwait(false);

                context.Variables.Update(this.State.ToString());
            }
        }

        return context;
    }

    /// <inheritdoc/>
    public ISKFunction SetDefaultSkillCollection(IReadOnlySkillCollection skills)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetDefaultSkillCollection(skills);
    }

    /// <inheritdoc/>
    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetAIService(serviceFactory);
    }

    /// <inheritdoc/>
    public ISKFunction SetAIConfiguration(CompleteRequestSettings settings)
    {
        return this.Function is null
            ? throw new NotImplementedException()
            : this.Function.SetAIConfiguration(settings);
    }

    #endregion ISKFunction implementation

    /// <summary>
    /// Expand variables in the input string.
    /// </summary>
    /// <param name="variables">Variables to use for expansion.</param>
    /// <param name="input">Input string to expand.</param>
    /// <returns>Expanded string.</returns>
    internal string ExpandFromVariables(ContextVariables variables, string input)
    {
        var result = input;
        var matches = Regex.Matches(input, @"\$(?<var>\w+)");
        var orderedMatches = matches.Cast<Match>().Select(m => m.Groups["var"].Value).OrderByDescending(m => m.Length);

        foreach (var varName in orderedMatches)
        {
            result = variables.Get(varName, out var value)
                ? result.Replace($"${varName}", value)
                : this.State.Get(varName, out value)
                    ? result.Replace($"${varName}", value)
                    : result.Replace($"${varName}", string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Set functions for a plan and its steps.
    /// </summary>
    /// <param name="plan">Plan to set functions for.</param>
    /// <param name="context">Context to use.</param>
    /// <returns>The plan with functions set.</returns>
    private static Plan SetRegisteredFunctions(Plan plan, SKContext context)
    {
        if (plan.Steps.Count == 0)
        {
            if (context.IsFunctionRegistered(plan.SkillName, plan.Name, out var skillFunction))
            {
                Verify.NotNull(skillFunction, nameof(skillFunction));
                plan.SetFunction(skillFunction);
            }
        }
        else
        {
            foreach (var step in plan.Steps)
            {
                SetRegisteredFunctions(step, context);
            }
        }

        return plan;
    }

    /// <summary>
    /// Add any missing variables from a plan state variables to the context.
    /// </summary>
    private static void AddVariablesToContext(ContextVariables vars, SKContext context)
    {
        // Loop through vars and add anything missing to context
        foreach (var item in vars)
        {
            if (!context.Variables.ContainsKey(item.Key))
            {
                context.Variables.Set(item.Key, item.Value);
            }
        }
    }

    /// <summary>
    /// Get the variables for the next step in the plan.
    /// </summary>
    /// <param name="variables">The current context variables.</param>
    /// <param name="step">The next step in the plan.</param>
    /// <returns>The context variables for the next step in the plan.</returns>
    private ContextVariables GetNextStepVariables(ContextVariables variables, Plan step)
    {
        // If the current step is passing to another plan, we set the default input to an empty string.
        // Otherwise, we use the description from the current plan as the default input.
        // We then set the input to the value from the SKContext, or the input from the Plan.State, or the default input.
        var defaultInput = step.Steps.Count > 0 ? string.Empty : this.Description ?? string.Empty;
        var planInput = string.IsNullOrEmpty(variables.Input) ? this.State.Input : variables.Input;
        var stepInput = string.IsNullOrEmpty(planInput) ? defaultInput : planInput;
        var stepVariables = new ContextVariables(stepInput);

        // Priority for remaining stepVariables is:
        // - NamedParameters (pull from State by a key value)
        // - Parameters (from context)
        // - Parameters (from State)
        var functionParameters = step.Describe();
        foreach (var param in functionParameters.Parameters)
        {
            if (variables.Get(param.Name, out var value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(param.Name, value);
            }
            else if (this.State.Get(param.Name, out value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(param.Name, value);
            }
        }

        foreach (var item in step.NamedParameters)
        {
            if (!string.IsNullOrEmpty(item.Value))
            {
                var value = this.ExpandFromVariables(variables, item.Value);
                stepVariables.Set(item.Key, value);
            }
            else if (variables.Get(item.Key, out var value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(item.Key, value);
            }
            else if (this.State.Get(item.Key, out value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(item.Key, value);
            }
        }

        return stepVariables;
    }

    private void SetFunction(ISKFunction function)
    {
        this.Function = function;
        this.Name = function.Name;
        this.SkillName = function.SkillName;
        this.Description = function.Description;
        this.IsSemantic = function.IsSemantic;
        this.RequestSettings = function.RequestSettings;
    }

    private ISKFunction? Function { get; set; } = null;

    private readonly List<Plan> _steps = new();
}
