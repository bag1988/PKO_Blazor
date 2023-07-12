using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceLibrary.Diagnostic;
using ServiceLibrary.Extensions;
using SynthesisService.Lib;
using SynthesisService.Services;
using Serilog;
using ServiceLibrary.Logging.SeriLog;

LogExtensions.Initialize();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSharedServiceConfiguration();

    builder.Services.AddOptions();

    builder.TryAddTracing<Program>(Tracing.Sources);

    builder.TryAddMetrics<Program>(Tracing.Sources);

    builder.Services.AddSingleton<Synthesizer>();
    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

    // Add services to the container.
    builder.Services.AddGrpc();

    builder.Services.AddGrpcHealthChecks()
                    .AddCheck("Common", () => HealthCheckResult.Healthy());

    builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader()
             .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    }));

    builder.Host.UseSerilog();

    var app = builder.Build();

    app.UseCors("AllowAll");
    app.UseGrpcWeb();

    app.UseStaticFiles();


    // Configure the HTTP request pipeline.
    app.MapGrpcService<GenerateSoundV1>();
    app.MapGrpcHealthChecksService();

    app.MapGet("/", async context =>
    {
        await context.Response.WriteAsync(
            "Sensor-M, GRPC Service");
    });

    app.TryUseOpenTelemetryPrometheusScrapingEndpoint(
            context => context.Request.Path == "/metrics"
                && context.Connection.LocalPort == 9464);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
