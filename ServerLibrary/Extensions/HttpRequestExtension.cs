using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SharedLibrary.Models;

namespace ServerLibrary.Extensions
{
    public static class HttpRequestExtension
    {
        public static Tuple<uint, uint> GetRangeHeaderRequest(this HttpRequest httpRequest)
        {
            var range = httpRequest.Headers.Range.FirstOrDefault();
            RangeHeaderValue.TryParse(range, out var rangeValue);
            uint startIndex = (uint?)rangeValue?.Ranges?.FirstOrDefault()?.From ?? 0;
            uint endIndex = (uint?)rangeValue?.Ranges?.FirstOrDefault()?.To ?? 0;
            return new Tuple<uint, uint>(startIndex, endIndex);
        }

        public static UserCookie? GetUserCookie(this HttpRequest httpRequest)
        {
            UserCookie? userCookie = null;
            try
            {
                if (httpRequest.Cookies.ContainsKey(httpRequest.Host.GetTokenName()))
                {
                    var cookieValue = httpRequest.Cookies[httpRequest.Host.GetTokenName()];

                    if (!string.IsNullOrEmpty(cookieValue))
                    {
                        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));

                        if (!string.IsNullOrEmpty(json))
                        {
                            userCookie = JsonSerializer.Deserialize<UserCookie>(json);
                        }
                    }
                }
            }
            catch
            {
                httpRequest.HttpContext.Response.Cookies.Delete(httpRequest.Host.GetTokenName());
            }
            return userCookie;
        }
    }
}
