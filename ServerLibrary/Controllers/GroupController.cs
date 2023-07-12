using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class GroupController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly UUZSDataClient _UUZSData;
        private readonly SMSSGsoClient _SMSSGso;

        private readonly ILogger<GroupController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public GroupController(ILogger<GroupController> logger, SMDataServiceClient SMData, AsoDataClient ASOData, UUZSDataClient UUZSData, SMSSGsoClient SMSSGso, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
            _UUZSData = UUZSData;
            _SMSSGso = SMSSGso;
        }

        /// <summary>
        /// Получаем список объектов в группе
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGroupItemList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGroupDevIDList response = new();
            try
            {
                response = await _UUZSData.GetGroupItemListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CGroupDevID
            return Ok(response.Array);
        }


        /// <summary>
        /// Получить список групп(УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_IGroup(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGroupInfoListOutList response = new();
            try
            {
                response = await _UUZSData.GetItems_IGroupAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CGroupInfoListOut
            return Ok(response.Array);
        }

        /// <summary>
        /// Получить список объектов в которых есть группа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLinkObjects_IGroup(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray response = new();
            try
            {
                response = await _UUZSData.GetLinkObjects_IGroupAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Удаление группы (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteGroup(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new() { Value = false };
            try
            {
                response = await _UUZSData.DeleteGroupAsync(request);

                await _Log.Write(Source: (int)GSOModules.SzsForms_Module, EventCode: 6/*IDS_REG_GROUP_DELETE*/, SubsystemID: request.SubsystemID, UserID: _userInfo.GetInfo?.UserID);
                if (response.Value != true)
                {
                    return BadRequest();
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
        /// Получить параметры группы
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGroupInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGetGroupInfo response = new();
            try
            {
                response = await _UUZSData.GetGroupInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(JsonFormatter.Default.Format(response));
        }

        /// <summary>
        /// Получение информации о линиях
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGroupLineList_Uuzs()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CLineGroupTypeList response = new();
            try
            {
                response = await _UUZSData.GetGroupLineList_UuzsAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }


        /// <summary>
        /// Получение информации о типе оконечных устройств
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetGroupLineDevList_Uuzs(CLineGroupType request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CLineGroupDevList response = new();
            try
            {
                response = await _UUZSData.GetGroupLineDevList_UuzsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        /// <summary>
        /// Удаление элементов группы
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> DeleteGroupItem(List<CGroupItemInfo> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue response = new() { Value = false };
            CGroupItemInfoList listGroup = new();
            listGroup.Array.AddRange(request);
            try
            {
                response = await _UUZSData.DeleteGroupItemAsync(listGroup);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        /// <summary>
        /// Проверка группового номера на уникальность
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> CheckExistGroupID(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CountResponse response = new();
            try
            {
                response = await _UUZSData.CheckExistGroupIDAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }


        /// <summary>
        /// Обновляем информацию о группе (УУЗС)
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateGroup([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            var request = CUpdateGroup.Parser.ParseJson(json);

            BoolValue response = new() { Value = false };
            try
            {
                response = await _UUZSData.UpdateGroupAsync(request);

                int EventCode = 4;//IDS_REG_GROUP_INSERT
                if (request.GroupInfo.GroupID > 0)
                    EventCode = 5;//IDS_REG_GROUP_UPDATE

                await _Log.Write(Source: (int)GSOModules.SzsForms_Module, EventCode: EventCode, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

    }
}
