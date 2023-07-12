using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Mvc;
using ReplaceLibrary;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
//using Dapr.Client;
using SMDataServiceProto.V1;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using UserInfo = SharedLibrary.Models.UserInfo;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SecurityController : Controller
    {
        private readonly SMSSGsoClient _SMGso;
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly AuthorizedInfo _userInfo;
        private readonly ILogger<SecurityController> _logger;
        private readonly WriteLog _Log;
        public SecurityController(ILogger<SecurityController> logger, SMSSGsoClient data, WriteLog log, AuthorizedInfo userInfo, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData)
        {
            _logger = logger;
            _SMGso = data;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteGsoUserEx(UserInfo x)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            UserInfoExList requestProto = new();
            BoolValue response = new() { Value = false };

            requestProto.Array.Add(new UserInfoEx()
            {
                Login = x.Login,
                Passw = x.Passw,
                OBJID = x.OBJID,
                Status = x.Status,
                Permisions = null
            });

            try
            {
                response = await _SMGso.SetGsoUserEx2Async(requestProto);

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_SEC_DELETE, SubsystemID: 5, UserID: _userInfo.GetInfo?.UserID);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(response);
        }

        [HttpPost]
        public IActionResult CheckPassword(ChangePassword request)
        {
            try
            {
                if (AesEncrypt.EncryptString(request.OldPassword ?? "") == request.EncryptPassword)
                {
                    request.EncryptPassword = AesEncrypt.EncryptString(request.NewPassword ?? "");
                }
                else
                    request.EncryptPassword = null;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(request);
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetGsoUserEx(List<UserInfo> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            UserInfoExList requestProto = new();
            BoolValue response = new() { Value = false };

            requestProto.Array.AddRange(request.Select(x => new UserInfoEx()
            {
                Login = x.Login,
                Passw = x.Passw ?? "",
                OBJID = x.OBJID,
                Status = x.Status,
                SuperVision = x.SuperVision,
                Permisions = x.Permisions != null ? new PermisionUser()
                {
                    PerAccFn = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccFn),
                    PerAccAso = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccAso),
                    PerAccSzs = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccSzs),
                    PerAccCu = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccCu),
                    PerAccP16 = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccP16),
                    PerAccRdm = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccRdm),
                    PerAccSec = UnsafeByteOperations.UnsafeWrap(x.Permisions.PerAccSec)
                } : null
            }));

            try
            {
                response = await _SMGso.SetGsoUserEx2Async(requestProto);

                int? UserId = _userInfo.GetInfo?.UserID;
                foreach (var item in request)
                {
                    int EventCode = (int)GsoEnum.IDS_REG_SEC_INSERT;
                    if (item.OBJID?.ObjID != 0)
                        EventCode = (int)GsoEnum.IDS_REG_SEC_UPDATE;

                    await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: 5, UserID: UserId);
                }



            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(response);
        }

        /// <summary>
        /// Получаем параметры входа
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetParamLogin()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ParamList s = new();
            LoginMode param = new();
            try
            {
                s = await _SMData.GetParamsListAsync(new Null());
                if (s != null)
                {
                    var p = param.GetType().GetProperties();
                    if (p != null)
                    {
                        foreach (var prop in p)
                        {
                            var v = s.Array.FirstOrDefault(x => x.Name == prop.Name)?.Value;
                            prop.SetValue(param, Convert.ChangeType(v, prop.PropertyType));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(param);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetParamLogin(LoginMode request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ParamList r = new();
            BoolValue b = new() { Value = false };
            List<ParamSystem> paramList = new();
            try
            {
                var p = request.GetType().GetProperties();


                if (p != null)
                {
                    foreach (var prop in p)
                    {
                        var v = prop.GetValue(request);
                        r.Array.Add(new ParamSystem() { Name = prop.Name, Value = v?.ToString() ?? "" });
                    }
                }


                b = await _SMData.SetParamsListAsync(r);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(b);
        }

    }
}
