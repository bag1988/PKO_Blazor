using BlazorLibrary.ServiceColection;
using Microsoft.AspNetCore.Components.Web;
//using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ViewState.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//HttpClient, InfoUser, ReplaceDictionary
builder.Services.AddServiceBlazor();

var host = builder.Build();
await BuilderCulture.Set(host);
await host.RunAsync();
