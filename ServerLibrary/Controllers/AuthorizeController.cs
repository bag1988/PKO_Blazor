using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
//using Dapr.Client;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/allow/[action]")]
    [AllowAnonymous]
    public class AuthorizeController : Controller
    {
        private readonly SMSSGsoClient _SMGso;
        private readonly AuthorizedInfo _userInfo;
        private readonly ILogger<AuthorizeController> _logger;
        private readonly WriteLog _Log;
        private readonly IConfiguration _conf;

        public AuthorizeController(ILogger<AuthorizeController> logger, SMSSGsoClient data, WriteLog log, AuthorizedInfo userInfo, IConfiguration conf)
        {
            _logger = logger;
            _SMGso = data;
            _Log = log;
            _userInfo = userInfo;
            _conf = conf;
        }

        [HttpPost]
        public async Task<IActionResult> AuthorizeUser(RequestLogin request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            activity?.AddTag("Пользователь", request.User);

            AuthResponse? user = null;
            try
            {
                request.Password = AesEncrypt.EncryptString(request.Password);
                user = await _SMGso.BeginUserSessAsync(request, cancellationToken: HttpContext.RequestAborted);

                await _Log.Write(Source: (int)GSOModules.Security_Module, EventCode: 129, Info: request.User, SubsystemID: 5);

                if (user?.IsAuthSuccessful == true)
                {
                    UserCookie newUser = new()
                    {
                        UserToken = user.Token,
                        RefreshToken = request.Password
                    };

                    Response.SetUserCookie(newUser);
                    return Ok(user.Token);
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshUser([FromBody] string NameUser)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            activity?.AddTag("Пользователь", NameUser);
            AuthResponse? user = null;
            try
            {
                UserCookie? userCookie = Request.GetUserCookie();

                if (userCookie != null)
                {
                    user = await _SMGso.BeginUserSessAsync(new RequestLogin()
                    {
                        User = NameUser,
                        Password = userCookie.RefreshToken ?? ""
                    }, cancellationToken: HttpContext.RequestAborted);

                    if (user?.IsAuthSuccessful == true)
                    {
                        userCookie.UserToken = user.Token;

                        Response.SetUserCookie(userCookie);

                        return Ok(user.Token);
                    }
                }
                Response.Cookies.Delete(Request.Host.GetTokenName());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckUser([FromBody] string token)
        {
            try
            {
                var IsAuth = JwtParser.IsValidToken(token);
                UserCookie? userCookie = Request.GetUserCookie();
                if (!IsAuth || userCookie == null || !token.Equals(userCookie.UserToken))
                {
                    Logout();
                    return Ok(false);
                }
                var statusSess = await _SMGso.GetUserSessStatusAsync(new Int32Value() { Value = _userInfo.GetInfo?.UserSessID ?? 0 });
                var IsLogoutAll = statusSess.Value == 1 ? true : false;

                return Ok(IsLogoutAll ? IsAuth : false);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            if (Request.Cookies.ContainsKey(Request.Host.GetTokenName()))
            {
                Response.Cookies.Delete(Request.Host.GetTokenName());
            }
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            return Ok();
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberMajor()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_MAJOR"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberMinor()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_MINOR"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetProductVersionNumberPatch()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_VERSION_NUMBER_PATCH"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetBuildNumber()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                return Ok(_conf["PRODUCT_BUILD_NUMBER"]);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult PVersionFull()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var productVersion = new ProductVersion(
                CompanyName: _conf["COMPANY_NAME"],
                ProductName: _conf["PRODUCT_NAME"],
                ProductVersionNumberMajor: _conf["PRODUCT_VERSION_NUMBER_MAJOR"],
                ProductVersionNumberMinor: _conf["PRODUCT_VERSION_NUMBER_MINOR"],
                ProductVersionNumberPatch: _conf["PRODUCT_VERSION_NUMBER_PATCH"],
                BuildNumber: _conf["PRODUCT_BUILD_NUMBER"]);
                return Ok(productVersion);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public IActionResult GetConfStart()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                ConfigStart response = new();
                _conf.GetSection("ConfStart").Bind(response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }


        }

        [HttpPost]
        public async Task<IActionResult> WriteLog(WriteLog2Request request)
        {
            if (!await _Log.Write(request))
                return BadRequest();
            return Ok();
        }

    }
}
