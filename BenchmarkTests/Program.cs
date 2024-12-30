using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers; 
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkTests;
using Perfolizer.Horology;

BenchmarkRunner.Run<ConnectionParametersTests>();

//BenchmarkRunner
//    .Run<HttpClientTests>(
//        DefaultConfig.Instance.AddJob(
//            Job.Default.WithToolchain(new InProcessEmitToolchain(timeout: TimeSpan.FromSeconds(9), logOutput: false))
//            .WithLaunchCount(1)
//            .WithWarmupCount(5)
//            .WithIterationCount(100)
//            .WithIterationTime(TimeInterval.FromMilliseconds(80)))
//        .AddLogger(new ConsoleLogger(unicodeSupport: true, ConsoleLogger.CreateGrayScheme()))
//        .WithOptions(ConfigOptions.DisableLogFile));
