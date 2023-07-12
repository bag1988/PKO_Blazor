using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServerLibrary.Extensions;
using SharedLibrary.Interfaces;

namespace ServerLibrary
{
    public class RGrpcCallHelper : IGrpcCallHelper
    {
        public void AddCallMetadata(AuthInterceptorContext context, Metadata metadata, IServiceProvider serviceProvider)
        {
            var provider = serviceProvider.GetService<IHttpContextAccessor>();
            if (provider?.HttpContext != null)
            {
                metadata.AddRequestToMetadata(provider.HttpContext.Request);
            }
        }
    }
}
