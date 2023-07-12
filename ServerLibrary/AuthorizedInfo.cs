using Microsoft.AspNetCore.Http;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;

namespace ServerLibrary
{

    public class AuthorizedInfo
    {
        private readonly IHttpContextAccessor _http;

        public AuthorizedInfo(IHttpContextAccessor http)
        {
            _http = http;
        }

        public AuthorizUser? GetInfo
        {
            get
            {
                AuthorizUser? user = null;
                try
                {
                    if (!_http.HttpContext?.User.Identity?.IsAuthenticated ?? true)
                        return null;

                    var SubsystemID = SubsystemType.SUBSYST_ASO;

                    if (_http.HttpContext?.Request.Headers.ContainsKey(CookieName.SubsystemID) ?? false)
                        int.TryParse(_http.HttpContext?.Request.Headers[CookieName.SubsystemID], out SubsystemID);


                    var c = _http.HttpContext?.User.Claims;

                    if (c != null)
                    {
                        user = new(c);
                        user.SubSystemID = SubsystemID;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return user;
            }

        }
    }
}
