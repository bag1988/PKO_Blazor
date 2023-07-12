using BlazorLibrary.ServiceColection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Features;
using StartUI.Client;
using StartUI.Client.Injects;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//HttpClient, InfoUser, ReplaceDictionary
builder.Services.AddServiceBlazor();

builder.Services.AddSingleton<GenerateChannelsReport>();


var host = builder.Build();

await BuilderCulture.Set(host);

await host.RunAsync();