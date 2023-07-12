using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using ServerLibrary;

namespace DeviceConsole.Server.Controllers.ASO
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public class DepartmentController : Controller
    {
        private readonly AsoDataClient _ASOData;
        private readonly ILogger<DepartmentController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;

        public DepartmentController(ILogger<DepartmentController> logger, AsoDataClient ASOData, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
        }

        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteDepartment(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _ASOData.DeleteDepartmentAsync(request);
                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: 343/*IDS_REG_DEP_DELETE*/, SubsystemID: 1/*_userInfo.GetInfo?.SubSystemID*/, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Получить детальный список подразделений
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> IDepartment_Aso_GetItems(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            DepartmentAsoList s = new();

            try
            {
                s = await _ASOData.IDepartment_Aso_GetItemsAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<DepartmentAso>
            return Ok(s.Array.ToList());
        }

        /// <summary>
        /// Список объектов в которых значиться подразделение
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> IDepartment_Aso_GetLinkObjects(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            StringArray s = new();

            try
            {
                s = await _ASOData.IDepartment_Aso_GetLinkObjectsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<string>
            return Ok(s.Array.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> GetDepartmentInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            DepartmentAso s = new();

            try
            {
                s = await _ASOData.GetDepartmentInfoAsync(request);
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
        public async Task<IActionResult> SetDepartmentInfo(DepartmentAso request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();

            try
            {
                s = await _ASOData.SetDepartmentInfoAsync(request);

                int EventCode = 344;/*IDS_REG_DEP_INSERT*/
                if (request.IDDep != 0)
                    EventCode = 345;/*IDS_REG_DEP_UPDATE*/


                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: EventCode, SubsystemID: 1/*_userInfo.GetInfo?.SubSystemID*/, UserID: _userInfo.GetInfo?.UserID);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

    }
}
