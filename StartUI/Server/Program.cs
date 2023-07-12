using ServerLibrary;
using ServerLibrary.HubsProvider;
using ServiceLibrary;
using ServiceLibrary.Diagnostic;
using ServiceLibrary.Extensions;
using Serilog;
using ServiceLibrary.Logging.SeriLog;
using Microsoft.AspNetCore.Http.Features;

LogExtensions.Initialize();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSharedAppConfiguration();

    builder.Services.AddHostedService<LifeTimeService>();

    builder.Services.AddOptions();

    builder.TryAddTracing<Program>(Tracing.Sources);

    builder.TryAddMetrics<Program>(Tracing.Sources);

    builder.Services.AddAntiforgery();

    //Subscribe
    builder.Services.AddSubscribeNotify();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    //SignalR
    builder.Services.AddSignalRNotify();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddServerCollection();
    builder.Services.AddSMDataServices();

    builder.Services.AddCors(o => o.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    }));

    builder.Services.Configure<FormOptions>(options =>
    {
        // Установите ограничение в 256 МБ
        options.MultipartBodyLengthLimit = int.MaxValue / 2;
        options.BufferBodyLengthLimit = int.MaxValue / 2;
    });

    builder.Host.UseSerilog();

    var app = builder.Build();

    app.UseCors("AllowAll");


    app.UseResponseCompression();


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        //app.UseHsts();
    }

    //app.UseHttpsRedirection();

    app.UseRequestLocalization();

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
    app.UseStaticFiles(new StaticFileOptions()
    {
        ServeUnknownFileTypes = true
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    //Dapr PubSub
    app.UseCloudEvents();



    app.MapRazorPages();
    app.MapControllers();

    //Dapr PubSub
    app.MapSubscribeHandler();

    //SignalR
    app.MapHub<SharedHub>("/CommunicationHub");

    app.MapFallbackToFile("_content/BlazorLibrary/index.html");

    app.TryUseOpenTelemetryPrometheusScrapingEndpoint();

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

