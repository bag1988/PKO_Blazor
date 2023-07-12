using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.ServiceColection
{
    public static class ServiceCollection
    {
        public static IServiceCollection AddServiceBlazor(this IServiceCollection services)
        {
            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Error));
            services.AddOptions();
            services.AddLocalization();

            services.AddHttpClient("WebAPI", (service, client) =>
            {
                client.BaseAddress = new Uri(service.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress);
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddSingleton<LocalStorage>();

            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddAuthorizationCore(x =>
            {
                x.AddPolicy("Bearer", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
                    policy.RequireClaim(ClaimTypes.Name);
                });
            });

            services.AddSingleton<AuthenticationStateProvider, AuthStateProvider>();

            services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebAPI"));

            services.AddSingleton<AddHeadersHandler>();
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<AddHeadersHandler>());
                });
            });

            services.AddSingleton<GetUserInfo>();
            services.AddTransient<HubContextCreate>();
            services.AddSingleton<OtherInfoForReport>();
            return services;
        }


        public class AddHeadersHandler : DelegatingHandler
        {
            private readonly IStringLocalizer<DeviceReplace> DeviceRep;
            private readonly IStringLocalizer<SMDataReplace> SMDataRep;
            private readonly AuthenticationStateProvider _authStateProvider;
            private readonly LocalStorage _localStorage;

            public AddHeadersHandler(IStringLocalizer<DeviceReplace> _DeviceRep, IStringLocalizer<SMDataReplace> _SMDataRep, AuthenticationStateProvider authStateProvider, LocalStorage localStorage)
            {
                DeviceRep = _DeviceRep;
                SMDataRep = _SMDataRep;
                _authStateProvider = authStateProvider;
                _localStorage = localStorage;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
            {
                var r = await base.SendAsync(request, cancellationToken);

                if (r.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    MessageView?.Clear();
                    MessageView?.AddError(SMDataRep["IDS_ACCESS_DENIDE"], DeviceRep["IDS_STRING_SUBSYSTEM_NOT_PERMISSIONS"]);
                }
                if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ((AuthStateProvider)_authStateProvider).NotifyUserLogout();
                    _ = _localStorage.RemoveAllAsync();
                    MessageView?.Clear();
                }
                return r;
            }
        }

    }
}
