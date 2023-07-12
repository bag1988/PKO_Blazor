using System.Text;
using Microsoft.AspNetCore.Http;

namespace ServerLibrary.Extensions
{
    public static class HostStringExtension
    {
        public static string GetTokenName(this HostString host) => new string(Convert.ToBase64String(Encoding.UTF8.GetBytes(host.Value)).Where(x => char.IsLetter(x)).ToArray());

    }
}
