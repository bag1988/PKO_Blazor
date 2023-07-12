using System.Net.Http.Headers;

namespace BlazorLibrary.Helpers
{
    public static class HttpRequestHeaderExtinsions
    {
        public static void AddHeader(this HttpRequestHeaders headers, string headerName, string newValue)
        {
            if (headers.Contains(headerName))
            {
                headers.Remove(headerName);
            }
            if (!string.IsNullOrEmpty(newValue))
                headers.Add(headerName, newValue);
        }

        public static string GetHeader(this HttpRequestHeaders headers, string headerName)
        {
            string result;
            headers.TryGetValues(headerName, out IEnumerable<string>? list);

            result = list?.FirstOrDefault() ?? "";

            return result;
        }
    }
}
