using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;

namespace DeviceConsole.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.ViewTask })]
    public class TaskController : Controller
    {
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<TaskController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public TaskController(ILogger<TaskController> logger, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _ASOData = ASOData;
            _Log = log;
            _userInfo = userInfo;
        }


        [HttpPost]
        public async Task<IActionResult> GetItems_ITask(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                CGetTaskInfoList response = new();
                response = await _ASOData.GetItems_ITaskAsync(request);

                //List<CGetTaskInfo>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetTaskResults(IGetTaskResults request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OGetTaskResults response = new();
                response = await _ASOData.GetTaskResultsAsync(request);

                //OGetTaskResults
                return Ok(JsonFormatter.Default.Format(response));
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetTaskInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                CTaskInfo response = new();
                response = await _ASOData.GetTaskInfoAsync(request);

                //CTaskInfo
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.EditTask })]
        public async Task<IActionResult> SetTaskInfo(CTaskInfo request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OBJ_ID response = new();
                response = await _ASOData.SetTaskInfoAsync(request);

                int EventCode = 155;/*IDS_REG_TASK_INSERT*/
                if (request.TaskID?.ObjID > 0)
                    EventCode = 156;/*IDS_REG_TASK_UPDATE*/

                await _Log.Write(Source: (int)GSOModules.AutoTasks_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);


                //OBJ_ID
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.EditTask })]
        public async Task<IActionResult> DeleteTask(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue response = new();
                response = await _ASOData.DeleteTaskAsync(request);

                await _Log.Write(Source: (int)GSOModules.AutoTasks_Module, EventCode: 157/*IDS_REG_TASK_DELETE*/, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);

                if (response?.Value != true)
                    return BadRequest();

                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.EditTask })]
        public async Task<IActionResult> SetTaskStatus(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue response = new();
                response = await _ASOData.SetTaskStatusAsync(request);

                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.EditTime })]
        public async Task<IActionResult> GetTimeList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                TaskSheduleList response = new();
                response = await _ASOData.GetTimeListAsync(request);

                //List<TaskShedule>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetWeekDayTasks(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                TaskSheduleList response = new();
                response = await _ASOData.GetWeekDayTasksAsync(request);

                //List<TaskShedule>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetTaskControlUnit()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                OBJ_ID response = new();
                response = await _ASOData.GetTaskControlUnitAsync(new Empty());

                //OBJ_ID
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckTaskSit(CCheckTaskSit request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                IntID response = new();
                response = await _ASOData.CheckTaskSitAsync(request);

                //IntID
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetSession(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                BoolValue response = new();
                response = await _ASOData.SetSessionAsync(request);

                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetTaskLastStart(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                SMDataServiceProto.V1.String response = new();
                response = await _ASOData.GetTaskLastStartAsync(request);

                //CGetTaskLastStart
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEndTaskInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                CGetEndTaskInfo response = new();
                response = await _ASOData.GetEndTaskInfoAsync(request);

                //CGetEndTaskInfo
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.EditTime })]
        public async Task<IActionResult> SetTimeList([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                var request = CSetTimeList.Parser.ParseJson(json);
                BoolValue response = new();
                response = await _ASOData.SetTimeListAsync(request);

                //BoolValue
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }
    }
}
