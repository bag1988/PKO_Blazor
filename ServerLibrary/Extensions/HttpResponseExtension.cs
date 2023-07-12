using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SharedLibrary;
using SharedLibrary.Models;

namespace ServerLibrary.Extensions
{
    public static class HttpResponseExtension
    {
        public static byte[] SetResponseHeaderSound(this HttpResponse httpResponse, Tuple<uint, uint> rangeHeade, string? formatSound)
        {
            httpResponse.Headers.AcceptRanges = $"{"bytes"}";
            if (rangeHeade.Item1 > 0)
                httpResponse.StatusCode = (int)HttpStatusCode.PartialContent;
            httpResponse.Headers.Add(MetaDataName.FormatSound, formatSound);
            httpResponse.ContentType = "audio/wav";
            if (!string.IsNullOrEmpty(formatSound))
            {
                var bf = Convert.FromBase64String(formatSound);
                WavHeaderModel w = new(bf);
                uint endIndex = w.ChunkHeaderSize == uint.MaxValue ? 0 : w.ChunkHeaderSize + 8;
                uint newEndIndex = rangeHeade.Item2 > 0 ? rangeHeade.Item2 : endIndex;

                if (endIndex > 0)
                    httpResponse.ContentLength = newEndIndex - rangeHeade.Item1;
                httpResponse.Headers.ContentRange = $"{"bytes"} {rangeHeade.Item1}-{newEndIndex - 1}/{(endIndex > 0 ? endIndex : "*")}";
                if (rangeHeade.Item1 == 0)
                    return bf;
            }
            return Array.Empty<byte>();
        }

        public static bool SetUserCookie(this HttpResponse httpResponse, UserCookie userCookie)
        {
            try
            {
                var json = JsonSerializer.Serialize(userCookie);

                var cookieValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                httpResponse.Cookies.Append(httpResponse.HttpContext.Request.Host.GetTokenName(), cookieValue);
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
