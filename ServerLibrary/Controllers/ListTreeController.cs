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

namespace ServerLibrary.Controllers
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
        private readonly UUZSDataClient _UUZSData;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public ListTreeController(ILogger<ListTreeController> logger, AsoDataClient ASOData, SMSSGsoClient SMGso, UUZSDataClient UUZSData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
            _SMGso = SMGso;
            _UUZSData = UUZSData;
        }

        /// <summary>
        /// Удаление абонента из списка
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> DeleteListItem(List<CListItemInfo> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new();
            DeleteListItemRequest r = new DeleteListItemRequest();
            r.Array.AddRange(request);
            try
            {
                response = await _SMGso.DeleteListItemAsync(r);
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
        public async Task<IActionResult> GetListInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            GetListInfoResponse response = new();
            try
            {
                response = await _SMGso.GetListInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //GetListInfoResponse
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> GetABCAbonent(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray response = new();
            try
            {
                response = await _ASOData.GetABCAbonentAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        public async Task<IActionResult> IDepartment_Aso_GetObjects(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ObjectsList response = new();
            try
            {
                response = await _ASOData.IDepartment_Aso_GetObjectsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        public async Task<IActionResult> GetObjects_IList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ObjectsList response = new();
            try
            {
                response = await _SMGso.GetObjects_IListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> EditList(UpdateList request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            AddEditList r = new();
            r.List = request.Info;
            r.ListItem = new();
            r.ListItem.Array.AddRange(request.Lists);

            BoolValue response = new();
            try
            {
                response = await _SMGso.EditListAsync(r);
                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LIST_UPDATE, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
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
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> AddList(UpdateList request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            AddEditList r = new();
            r.List = request.Info;
            r.ListItem = new();
            r.ListItem.Array.AddRange(request.Lists);


            BoolValue response = new();
            try
            {
                response = await _SMGso.AddListAsync(r);

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_LIST_INSERT, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);


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
        public async Task<IActionResult> GetAbonentByABC(IntAndString request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AbonentByABCList response = new();
            try
            {
                response = await _ASOData.GetAbonentByABCAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Получить список абонентов подразделения
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDepAbonList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            AbonentByABCList response = new();
            try
            {
                response = await _ASOData.GetDepAbonListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Получаем элементы списка оповещения
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetListItems(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ItemsList response = new();
            try
            {
                if (request.SubsystemID == SubsystemType.SUBSYST_ASO)
                    response = await _ASOData.GetListItemsAsync(request);
                else if (request.SubsystemID == SubsystemType.SUBSYST_SZS)
                    response = await _UUZSData.GetListItemsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<Items>
            return Ok(response.Array);
        }

        /// <summary>
        /// Список объектов в которых значится абонент(при удалении абонента из списка)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLinkObjectsListItem(CListItemInfo request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray s = new();
            try
            {
                s = await _SMGso.GetLinkObjectsListItemAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<string>
            return Ok(s.Array);
        }

    }
}
