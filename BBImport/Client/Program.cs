using BBImport.Client;
using BlazorLibrary.ServiceColection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Features;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddServiceBlazor();


var host = builder.Build();

await BuilderCulture.Set(host);

await host.RunAsync();
