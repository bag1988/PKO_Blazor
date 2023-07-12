using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;

namespace ServerLibrary
{
    public class CheckPermissionFilter : IAuthorizationFilter
    {
        readonly int[] _BitsPos;
        readonly int? SubSysteID;

        public CheckPermissionFilter(int[] BitsPos, int subSysteID = 0)
        {
            _BitsPos = BitsPos;
            SubSysteID = subSysteID == 0 ? null : SubSysteID;
        }


        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userContext = context.HttpContext.User;

            if (context.ActionDescriptor.EndpointMetadata.Any(em => em.GetType() == typeof(AllowAnonymousAttribute)))
                return;

            if (!userContext.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            bool hasClaim = false;
            AuthorizUser? user = null;
            try
            {
                int SubsystemID = SubsystemType.SUBSYST_ASO;

                if (context.HttpContext?.Request.Headers.ContainsKey(CookieName.SubsystemID) ?? false)
                    int.TryParse(context.HttpContext?.Request.Headers[CookieName.SubsystemID], out SubsystemID);

                var c = userContext.Claims;

                if (c != null)
                {
                    user = new(c);
                    user.SubSystemID = SubsystemID;

                    byte[]? PerSub = null;

                    switch (SubSysteID ?? SubsystemID)
                    {
                        case SubsystemType.SUBSYST_ASO: PerSub = user.Permisions.PerAccAso; break;
                        case SubsystemType.SUBSYST_SZS: PerSub = user.Permisions.PerAccSzs; break;
                        case SubsystemType.SUBSYST_GSO_STAFF: PerSub = user.Permisions.PerAccCu; break;
                        case SubsystemType.SUBSYST_P16x: PerSub = user.Permisions.PerAccP16; break;
                        case SubsystemType.SUBSYST_Security: PerSub = user.Permisions.PerAccSec; break;
                        case SubsystemType.SUBSYST_Setting: PerSub = user.Permisions.PerAccFn; break;
                        case SubsystemType.SUBSYST_RDM: PerSub = user.Permisions.PerAccRdm; break;
                    }

                    foreach (var bits in _BitsPos)
                    {
                        hasClaim = CheckPermission.CheckBitPos(PerSub, bits);
                        if (hasClaim)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (!hasClaim)
            {
                context.Result = new ForbidResult();
            }
        }
    }


    public class CheckPermissionAttribute : TypeFilterAttribute
    {
        public CheckPermissionAttribute(int[] BitsPos, int SubsystemID = 0) : base(typeof(CheckPermissionFilter))
        {
            Arguments = new object[] { BitsPos, SubsystemID };
        }
    }
}
