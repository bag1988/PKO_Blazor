using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class SRSController : Controller
    {
        private readonly ILogger<SRSController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        private readonly string PathSRSConfig;
        private readonly string PathSRSLptConfig;
        public SRSController(ILogger<SRSController> logger, WriteLog log, AuthorizedInfo userInfo, IConfiguration conf)
        {
            _logger = logger;
            _Log = log;
            _userInfo = userInfo;
            string RootFolder = conf["RootFolder"] ?? "";
            PathSRSConfig = Path.Combine(RootFolder, "SMSrsNL", "SMSrsNL.Cfg.txt");
            PathSRSLptConfig = Path.Combine(RootFolder, "SMSrsNL", "FSCtrl.txt");
        }

        [HttpPost]
        public async Task<IActionResult> LoadSRSConfig()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                List<SRSLine> response = new();

                if (System.IO.File.Exists(PathSRSLptConfig))
                {
                    var result = await System.IO.File.ReadAllLinesAsync(PathSRSLptConfig, HttpContext.RequestAborted);
                    foreach (var line in result)
                    {
                        SRSLine elem = new();

                        if (line.Split(' ').Length > 2)
                        {
                            var arr = line.Split(' ');
                            elem.Id = response.Count + 1;
                            elem.Port = 0;
                            elem.Version = (uint)SRSVersion.SRS_LPT;
                            elem.Line = Convert.ToUInt32(arr[0].Split('-')[0]);
                            elem.SitID = Convert.ToUInt32(arr[0].Split('-').Length > 1 ? arr[0].Split('-')[1] : 0);
                            elem.StaffID = Convert.ToUInt32(arr[1]);
                            elem.SubSystID = Convert.ToUInt32(arr[2]);
                            response.Add(elem);
                        }
                    }
                }

                if (System.IO.File.Exists(PathSRSConfig))
                {
                    var result = await System.IO.File.ReadAllLinesAsync(PathSRSConfig, HttpContext.RequestAborted);
                    foreach (var line in result)
                    {
                        SRSLine elem = new();

                        if (line.Split(' ').Length > 3)
                        {
                            var arr = line.Split(' ');
                            elem.Id = response.Count + 1;
                            elem.Port = Convert.ToUInt32(arr[0]);
                            elem.Line = Convert.ToUInt32(arr[1]);
                            elem.StaffID = Convert.ToUInt32(arr[2]);
                            elem.SubSystID = Convert.ToUInt32(arr[3]);
                            elem.SitID = Convert.ToUInt32(arr[4]);

                            if (elem.Port > 0xFFFF)
                                elem.Version = (uint)SRSVersion.SRS_TCP_UUZS;
                            else
                                elem.Version = (uint)SRSVersion.SRS_HSCOM_UUZS;
                            response.Add(elem);
                        }
                    }
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SaveSRSConfig(List<SRSLine> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntResponse s = new();
            try
            {
                var dir = Path.GetDirectoryName(PathSRSConfig);
                if (string.IsNullOrEmpty(dir))
                    return BadRequest();

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);


                if (request.Any(x => x.Version == (uint)SRSVersion.SRS_LPT))
                {
                    await System.IO.File.WriteAllLinesAsync(PathSRSLptConfig, request.Where(x => x.Version == (uint)SRSVersion.SRS_LPT).Select(x => $"{x.Line}-{x.SitID} {x.StaffID} {x.SubSystID}"), HttpContext.RequestAborted);
                }
                else
                    await System.IO.File.WriteAllLinesAsync(PathSRSLptConfig, Array.Empty<string>(), HttpContext.RequestAborted);

                if (request.Any(x => x.Version != (uint)SRSVersion.SRS_LPT))
                {
                    await System.IO.File.WriteAllLinesAsync(PathSRSConfig, request.Where(x => x.Version != (uint)SRSVersion.SRS_LPT).Select(x => $"{x.Port} {x.Line} {x.StaffID} {x.SubSystID} {x.SitID}"), HttpContext.RequestAborted);
                }
                else
                    await System.IO.File.WriteAllLinesAsync(PathSRSConfig, Array.Empty<string>(), HttpContext.RequestAborted);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

    }
}
