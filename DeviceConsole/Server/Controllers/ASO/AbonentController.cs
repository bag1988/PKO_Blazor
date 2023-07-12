using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;
using Int32Value = Google.Protobuf.WellKnownTypes.Int32Value;
using ServerLibrary;

namespace DeviceConsole.Server.Controllers.ASO
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class AbonentController : Controller
    {
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<AbonentController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public AbonentController(ILogger<AbonentController> logger, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _ASOData = ASOData;
            _Log = log;
            _userInfo = userInfo;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ImportListAbon(List<ListModelXml> list)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                ListModelXmlList response = new();
                ListModelXmlList request = new();
                request.Array.AddRange(list);
                response = await _ASOData.ImportListAbonAsync(request);

                //List<ListModelXml>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ExportListAbonent(List<OBJ_ID> list)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                ListModelXmlList response = new();
                OBJ_IDList request = new();
                request.Array.AddRange(list);
                response = await _ASOData.ExportListAbonentAsync(request);


                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> ImportMsgParam(List<AbonMsgParam> list)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                AbonMsgParamList response = new();
                AbonMsgParamList request = new();
                request.Array.AddRange(list);
                response = await _ASOData.ImportMsgParamAsync(request);

                //Int32Value
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> GetFiltrAbonForName(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                IntAndStrList response = new();
                response = await _ASOData.GetFiltrAbonForNameAsync(request);

                //List<IntAndString>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteMsgParam(List<AbonMsgParam> list)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                Int32Value response = new();
                AbonMsgParamList request = new();
                request.Array.AddRange(list);
                response = await _ASOData.DeleteMsgParamAsync(request);

                //Int32Value
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
        public async Task<IActionResult> GetMsgParamList(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AbonMsgParamList s = new();
            try
            {
                s = await _ASOData.GetMsgParamListAsync(request);

                //List<AbonMsgParam>
                return Ok(s.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> CreateXLSXAbon(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OBJ_IDList r = new OBJ_IDList();
                r.Array.AddRange(request);
                var response = await _ASOData.CreateXLSXAbonAsync(r);
                //ArrayByte
                return Ok(response.Value.ToBase64());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> CreateXmlAbon(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OBJ_IDList r = new OBJ_IDList();
                r.Array.AddRange(request);
                var response = await _ASOData.CreateXmlAbonAsync(r);
                //ArrayByte
                return Ok(response.Value.ToBase64());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> CreateCsvAbon(List<OBJ_ID> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OBJ_IDList r = new OBJ_IDList();
                r.Array.AddRange(request);
                var response = await _ASOData.CreateCsvAbonAsync(r);
                //ArrayByte
                return Ok(response.Value.ToBase64());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        /// <summary>
        /// Получить список объектов в которых есть абонент
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> IAbonent_Aso_GetLinkObjects(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray response = new();
            try
            {
                response = await _ASOData.IAbonent_Aso_GetLinkObjectsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<string>
            return Ok(response.Array);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteAbonent(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new();
            try
            {
                response = await _ASOData.DeleteAbonentAsync(request);

                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: 336/*IDS_REG_AB_DELETE*/, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> GetAbCount(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CountResponse response = new();
            try
            {
                response = await _ASOData.GetAbCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CountResponse
            return Ok(response);
        }

        /// <summary>
        /// Получить список абонентов
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_IAbonent_3(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var call = _ASOData.GetItems_IAbonent_3Async(request);
                var headers = await call.ResponseHeadersAsync;
                string? FormatSound = headers.GetValue(MetaDataName.TotalCount);
                Response.Headers.Add(MetaDataName.TotalCount, FormatSound);
                var response = await call.ResponseAsync;

                //List<AbonentItem>
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
