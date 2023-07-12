using Grpc.Core;
using Microsoft.AspNetCore.Http;
using SharedLibrary;

namespace ServerLibrary.Extensions
{
    public static class MetaDataExtensions
    {
        public static Metadata AddRequestToMetadata(this Metadata metaData, HttpRequest request)
        {
            try
            {
                string? token = request.Headers.Authorization.FirstOrDefault();

                if (!metaData.Any(x => x.Key == MetaDataName.Authorization) && !string.IsNullOrEmpty(token))
                {
                    metaData.Add(MetaDataName.Authorization, token);
                }
                if (!metaData.Any(x => x.Key == MetaDataName.Language) && !string.IsNullOrEmpty(request.Headers.AcceptLanguage))
                    metaData.Add(MetaDataName.Language, request.Headers.AcceptLanguage.ToString());
                if (!metaData.Any(x => x.Key == MetaDataName.TimeZone) && !string.IsNullOrEmpty(request.Headers[MetaDataName.TimeZone].FirstOrDefault()))
                    metaData.Add(MetaDataName.TimeZone, request.Headers[MetaDataName.TimeZone].FirstOrDefault() ?? "");
                return metaData;
            }
            catch
            {
                throw new InvalidOperationException();
            }
        }
    }
}
