using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using RemoteConnectLibrary;
using ServerLibrary.HubsProvider;
using SharedLibrary.Interfaces;

namespace ServerLibrary
{
    public static class ServerCollections
    {
        public static IServiceCollection AddSignalRNotify(this IServiceCollection services)
        {
            //SignalR
            services.AddSignalR();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });            
            services.AddSingleton<SharedHub>();
            return services;
        }

        public static IServiceCollection AddSignalRNotifyChat(this IServiceCollection services)
        {
            //SignalR
            services.AddSignalR();
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
            services.AddSingleton<ChatHub>();
            return services;
        }

        public static IServiceCollection AddServerCollection(this IServiceCollection services)
        {
            services.AddSingleton<IJwtBearerEvents<HttpRequest>, RJwtBearerEvents>();
            services.AddSingleton<IGrpcCallHelper, RGrpcCallHelper>();
            services.AddSingleton<WriteLog>();
            services.AddSingleton<RemoteGateProvider>();
            services.AddSingleton<AuthorizedInfo>();
            services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
            });
           
            return services;
        }


        /*public static void MapSubscribeEventsNotifyService(this IEndpointRouteBuilder builder)
        {
            var serverAddressesFeature = builder.DataSources.ElementAt(1).Endpoints.Where(x => x.Metadata.Any(m => m.GetType().Equals(typeof(Dapr.TopicAttribute)))).ToList();
            var daprpoint = serverAddressesFeature.Select(x => new SubscribeNotifyModel { UrlMethod = (x as RouteEndpoint)?.RoutePattern.RawText, NameTopic = x.Metadata.GetMetadata<Dapr.TopicAttribute>()?.Name }).ToList();


            var b = builder.CreateApplicationBuilder();


            b.ApplicationServices.GetService<IHostApplicationLifetime>()?.ApplicationStarted.Register(
              () =>
              {
                  var server = b.ApplicationServices.GetService<IServer>();
                  var addressFeature = server?.Features.Get<IServerAddressesFeature>();


                  if (addressFeature != null && addressFeature.Addresses.Any())
                  {
                      var port = addressFeature.Addresses.FirstOrDefault(x => x.Contains("https"))?.Split(':').Last();


                      if (port == null)
                          return;
                      b.ApplicationServices.GetService<SubscribeNotifications>()?.SendSubscribe(new KeyValuePair<string, List<SubscribeNotifyModel>>(port, daprpoint));
                  }
              });
        }*/
    }
}
