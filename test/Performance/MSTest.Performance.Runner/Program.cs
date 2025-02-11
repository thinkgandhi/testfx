﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;

using Microsoft.Testing.TestInfrastructure;

using MSTest.Performance.Runner.Steps;

using DotnetMuxer = MSTest.Performance.Runner.Steps.DotnetMuxer;
using ExecutionScope = MSTest.Performance.Runner.Steps.ExecutionScope;

namespace MSTest.Performance.Runner;

internal class EntryPoint
{
    public static Task<int> Main(string[] args)
    {
        // Opt out telemetry for clean stacks, AppInsight is allocating strings and polluting the results.
        Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        Console.WriteLine("Microsoft (R) MSTest Performance Profiler Command Line Tool");

        var rootCommand = new RootCommand("MSTest Performance Profiler Command Line Tool");
        var pipelineNameFilter = new Option<string>(name: "--pipelineNameFilter", description: "Globing filter for the pipeline name to execute.", getDefaultValue: () => string.Empty);
        var executeTests = new Command("execute", "Execute the performance scenarios.")
        {
            pipelineNameFilter,
        };
        executeTests.SetHandler(
            pipelineNameFilter => _ = Pipelines(pipelineNameFilter), pipelineNameFilter);

        rootCommand.AddCommand(executeTests);

        return rootCommand.InvokeAsync(args);
    }

    private static int Pipelines(string pipelineNameFilter)
    {
        var pipelineRunner = new PipelinesRunner();

        pipelineRunner.AddPipeline("Default", "Scenario1_PerfView", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new PerfviewRunner(" /BufferSizeMB:1024 ", "Scenario1_PerfView.zip", includeScenario: true))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));

        pipelineRunner.AddPipeline("Default", "Scenario1_DotnetTrace", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new DotnetTrace("--profile cpu-sampling", "DotnetTrace_CPU_Sampling.zip"))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));

        // C:\Program Files\Microsoft Visual Studio\2022\Preview\Team Tools\DiagnosticsHub\Collector\AgentConfigs
        pipelineRunner.AddPipeline("Default", "Scenario1_DotNetObjectAllocBase", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new VSDiagnostics("DotNetObjectAllocLow.json", "Scenario1_DotNetObjectAllocBase.zip"))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));
        pipelineRunner.AddPipeline("Default", "Scenario1_CpuUsageLow", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new VSDiagnostics("CpuUsageHigh.json", "Scenario1_CpuUsageLow.zip"))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));

        pipelineRunner.AddPipeline("Default", "Scenario1_ConcurrencyVisualizer", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new ConcurrencyVisualizer("Scenario1_ConcurrencyVisualizer.zip"))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));

        pipelineRunner.AddPipeline("Default", "Scenario1_PlainProcess", [OSPlatform.Windows], parametersBag =>
        Pipeline
            .FirstStep(() => new Scenario1(numberOfClass: 100, methodsPerClass: 100, tfm: "net9.0", executionScope: ExecutionScope.MethodLevel), parametersBag)
            .NextStep(() => new DotnetMuxer(BuildConfiguration.Debug))
            .NextStep(() => new PlainProcess("Scenario1_PlainProcess.zip"))
            .NextStep(() => new MoveFiles("*.zip", Path.Combine(Directory.GetCurrentDirectory(), "Results")))
            .NextStep(() => new CleanupDisposable()));

        return pipelineRunner.Run(pipelineNameFilter);
    }
}
