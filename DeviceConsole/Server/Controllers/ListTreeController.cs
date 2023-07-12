using System.Text;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using static UUZSDataProto.V1.UUZSData;
using ServerLibrary;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class ListTreeController : Controller
    {
        private readonly SMSSGsoClient _SMGso;
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<ListTreeController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public ListTreeController(ILogger<ListTreeController> logger, AsoDataClient ASOData, SMSSGsoClient SMGso, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
            _SMGso = SMGso;
        }

        [HttpPost]
        public async Task<IActionResult> GetLinkObjects_IList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray s = new();
            try
            {
                s = await _SMGso.GetLinkObjects_IListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<string>
            return Ok(s.Array);
        }

        /// <summary>
        /// Удаление списка абонентов
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                s = await _SMGso.DeleteListAsync(request);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LIST_DELETE, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(s);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteAndExportAbon(List<OBJ_ID> childAbon)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            List<string> ForExportXml = new();
            ForExportXml.Add("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<XYZ xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            try
            {

                if (childAbon != null && childAbon.Any())
                {
                    foreach (var item in childAbon)
                    {
                        ForExportXml.Add((await _ASOData.GetExportAbInfoAsync(new OBJ_ID(item) { SubsystemID = SubsystemType.SUBSYST_ASO })).Value);
                        await _ASOData.DeleteAbonentAsync(new OBJ_ID(item) { SubsystemID = SubsystemType.SUBSYST_ASO });
                        await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: 336/*IDS_REG_AB_DELETE*/, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
                    }
                    ForExportXml.Add("</XYZ>");
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            string s = string.Join("", ForExportXml);

            //string
            return Ok(Convert.ToBase64String(Encoding.UTF8.GetBytes(s)));
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_IList(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            ListList s = new();

            try
            {
                s = await _SMGso.GetItems_IListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<ListItem>
            return Ok(s.Array);
        }

    }
}
