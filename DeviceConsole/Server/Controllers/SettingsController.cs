using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using static StaffDataProto.V1.StaffData;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class SettingsController : Controller
    {
        private readonly SMSSGsoClient _SMSGso;
        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly StaffDataClient _StaffData;
        private readonly ILogger<SettingsController> _logger;
        private readonly AuthorizedInfo _userInfo;
        private readonly WriteLog _Log;

        private readonly string BackupFolder;

        public SettingsController(ILogger<SettingsController> logger, SMSSGsoClient SMSGso, SMDataServiceProto.V1.SMDataService.SMDataServiceClient SMData, AsoDataClient ASOData, StaffDataClient StaffData, AuthorizedInfo userInfo, WriteLog Log, IConfiguration conf)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _SMData = SMData;
            _StaffData = StaffData;
            _userInfo = userInfo;
            _Log = Log;
            _ASOData = ASOData;
            BackupFolder = conf["BackupFolder"] ?? "";
        }

        [HttpPost]
        public async Task<IActionResult> GetAppPortInfo(BoolValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var response = await _SMSGso.GetAppPortsAsync(request, cancellationToken: HttpContext.RequestAborted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Генерируем новый StafID
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> GenerateStaffId()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new() { Value = false };
            try
            {
                response = await _SMData.GenerateStaffIdAsync(new Empty());

                await _SMSGso.LogoutAllUserAsync(new Empty());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Удаляем резервную копию базы данных
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public IActionResult DeleteDBPostgreeSql(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new() { Value = false };
            try
            {
                string backupFile = request.Value;

                if (System.IO.File.Exists(Path.Combine(BackupFolder, backupFile)))
                {
                    System.IO.File.Delete(Path.Combine(BackupFolder, backupFile));
                    response.Value = true;
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
        /// Скачать файл
        /// </summary>
        /// <param name=MetaDataName.FileName></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetFileServer([FromQuery] string FileName)
        {
            string FullPath = Path.Combine(BackupFolder, FileName);
            if (!System.IO.File.Exists(FullPath))
                return BadRequest();
            return PhysicalFile(FullPath, "application/octet-stream");
        }

        /// <summary>
        /// Получаем список файлов для восстановления базы данных
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetFileBackupGSO()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            List<BackupInfo> response = new();
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(BackupFolder))
                    {
                        response.AddRange(new DirectoryInfo(BackupFolder).EnumerateFiles("*.bak", SearchOption.AllDirectories).Select(x => new BackupInfo() { Name = x.Name, Created = x.CreationTime, SizeFile = x.Length, Url = Path.Combine(BackupFolder, x.Name) }));
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Сохранение базы данных в файл
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> BackupGSO(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntID response = new() { ID = -1 };
            try
            {
                response = await _SMData.BackupDBPostgreeSqlAsync(request);
                if (response == null || response.ID > 0)
                    await _Log.Write(Source: (int)GSOModules.DeviceConsole_Module, EventCode: 136/*IDS_REG_ERR_BACKUP*/, SubsystemID: 0, UserID: _userInfo.GetInfo?.UserID, Info: $"код: {response?.ID}", Type: 1);
                else
                    await _Log.Write(Source: (int)GSOModules.DeviceConsole_Module, EventCode: 135/*IDS_REG_BACKUP*/, SubsystemID: 0, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Восстановление базы данных из файла
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RestoreGSO(StringValue request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            IntID response = new() { ID = -1 };
            try
            {
                response = await _SMData.RestoreDBPostgreeSqlAsync(request, deadline: DateTime.UtcNow.AddSeconds(600));
                await _SMSGso.LogoutAllUserAsync(new Empty());
                if (response == null || response.ID > 0)
                    await _Log.Write(Source: (int)GSOModules.DeviceConsole_Module, EventCode: 137/*IDS_REG_ERR_RELOAD*/, SubsystemID: 0, UserID: _userInfo.GetInfo?.UserID, Info: $"код: {response?.ID}", Type: 1);
                else
                    await _Log.Write(Source: (int)GSOModules.DeviceConsole_Module, EventCode: 134/*IDS_REG_RESORE*/, SubsystemID: 0, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Просмотр каталога
        /// </summary>
        /// <param name="NameParent"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetChildDirectories([FromBody] string[]? NameParent = null)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            List<List<string>> ChildDirectories = new();
            StringArray request = new();
            try
            {
                request.Array.AddRange(NameParent);
                var r = await _SMData.GetChildDirectoriesAsync(request);
                ChildDirectories.AddRange(r.Array.Select(x => x.Split("/").ToList()));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(ChildDirectories);
        }


        /// <summary>
        /// Получаем весь список настроек
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetParamsList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ParamList s = new();
            ParamsSystem param = new();
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
                            if (v != null)
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


        /// <summary>
        /// Получаем геоданные
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGeolocation(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SMDataServiceProto.V1.String s = new();
            try
            {
                s = await _StaffData.GetGeolocationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Сохраняем данные геоданные
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetGeolocation(CSetGeolocation request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                s = await _StaffData.SetGeolocationAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetParamsList(ParamsSystem request)
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
