using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using SMP16XProto.V1;
using SMSSGsoProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReplaceLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using static SMP16XProto.V1.SMP16x;
using static SMSSGsoProto.V1.SMSSGso;
using static UUZSDataProto.V1.UUZSData;

namespace ServerLibrary.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new int[] { NameBitsPos.General })]
    public partial class DeviceController : Controller
    {
        private readonly SMDataServiceClient _SMData;
        private readonly AsoDataClient _ASOData;
        private readonly UUZSDataClient _UUZSData;
        private readonly SMSSGsoClient _SMSSGso;
        private readonly SMP16xClient _SMP16x;

        private readonly ILogger<DeviceController> _logger;
        private readonly WriteLog _Log;
        private readonly AuthorizedInfo _userInfo;



        public DeviceController(ILogger<DeviceController> logger, SMDataServiceClient SMData, AsoDataClient ASOData, UUZSDataClient UUZSData, SMSSGsoClient SMSSGso, SMP16xClient SMP16x, WriteLog log, AuthorizedInfo userInfo)
        {
            _logger = logger;
            _SMData = SMData;
            _Log = log;
            _userInfo = userInfo;
            _ASOData = ASOData;
            _UUZSData = UUZSData;
            _SMSSGso = SMSSGso;
            _SMP16x = SMP16x;
        }


        /// <summary>
        /// Сохранить зоны устройства
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetSubDevice(List<CSubDevice> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                foreach (var subDevice in request)
                {
                    s = await _UUZSData.SetSubDeviceAsync(subDevice);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(s);
        }


        /// <summary>
        /// Сохраняем устройство (УУЗС)
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> UpdateDeviceEx([FromBody] string json)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                var request = DeviceInfoEx.Parser.ParseJson(json);
                s = await _UUZSData.UpdateDeviceExAsync(request);
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
        public async Task<IActionResult> EditCommand(List<CmdCode> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue response = new() { Value = false };
            CmdCodeList s = new();
            s.Array.AddRange(request);
            try
            {
                response = await _SMP16x.EditCommandAsync(s);

                await _Log.Write(Source: (int)GSOModules.P16Forms_Module, EventCode: 103/*IDS_REG_BINDING_UPDATE*/, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCommand(List<CmdInfo> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue response = new() { Value = false };
            CmdInfoList s = new();
            s.Array.AddRange(request);
            try
            {
                response = await _SMP16x.RemoveCommandAsync(s);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }


        [HttpPost]
        public async Task<IActionResult> AddCommand(List<CmdInfo> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue response = new() { Value = false };
            CmdInfoList s = new();
            s.Array.AddRange(request);
            try
            {
                response = await _SMP16x.AddCommandAsync(s);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(response);
        }


        [HttpPost]
        public async Task<IActionResult> GetUnitCommandList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CmdInfoList s = new();
            try
            {
                s = await _SMP16x.GetUnitCommandListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<CmdInfo>
            return Ok(s.Array);
        }

        /// <summary>
        /// Проверка серийного номера на уникальность (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CheckDeviceID(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();
            try
            {
                s = await _UUZSData.IsSZSDeviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(s);
        }


        /// <summary>
        /// Получить список команд
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetCmdList(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CCmdInfoList s = new();
            try
            {
                s = await _UUZSData.GetCmdListAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CCmdInfo
            return Ok(s.Array);
        }

        /// <summary>
        /// Получить количество зон УЗС2
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetZoneCount(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CZoneInfoList s = new();
            try
            {
                s = await _UUZSData.GetZoneCountAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CZoneInfo
            return Ok(s.Array);
        }


        /// <summary>
        /// Определить имена типов оконечных устройств
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetObjects_ITerminalDevice()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SubsystemObjectList response = new();
            try
            {
                response = await _UUZSData.GetObjects_ITerminalDeviceAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //SubsystemObject
            return Ok(response.Array);
        }


        /// <summary>
        /// Получаем зоны (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDeviceSubDevice(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CSubDeviceList response = new();
            try
            {
                response = await _UUZSData.GetDeviceSubDeviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CSubDevice
            return Ok(response.Array);
        }


        /// <summary>
        /// Получение информации о типах оконечных устройств (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDeviceClassList_Uuzs(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            DeviceClassList response = new();
            try
            {
                response = await _UUZSData.GetDeviceClassList_UuzsAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //DeviceClass
            return Ok(response.Array);
        }


        /// <summary>
        /// Получаем информацию об устройстве(УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDeviceInfoEx(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            DeviceInfoEx response = new();
            try
            {
                response = await _UUZSData.GetDeviceInfoExAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CDeviceInfoEx
            return Ok(JsonFormatter.Default.Format(response));
        }

        /// <summary>
        /// Удаляем устройство
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteDevice(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SIntResponse response = new();
            try
            {
                response = await _UUZSData.DeleteDeviceAsync(request);

                await _Log.Write(Source: (int)GSOModules.SzsForms_Module, EventCode: 3/*IDS_REG_DEV_DELETE*/, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //SIntResponse
            return Ok(response);
        }


        /// <summary>
        /// Получаем список где используется данное устройство
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetLinkObjects_ITerminalDevice(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            StringArray response = new();
            try
            {
                response = await _UUZSData.GetLinkObjects_ITerminalDeviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //String
            return Ok(response.Array);
        }


        /// <summary>
        /// Получить список УЗС из таблицы для заданного типа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_ITerminalDevice(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGetDeviceInfoList response = new();
            try
            {
                response = await _UUZSData.GetItems_ITerminalDeviceAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CGetDeviceInfo
            return Ok(response.Array);
        }


        /// <summary>
        /// получаем список устройств из базы (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_InOrder(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CLineGroupDevList response = new();
            try
            {
                response = await _UUZSData.GetItems_InOrderAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CLineGroupDev
            return Ok(response.Array);
        }


        /// <summary>
        /// получаем названия типов устройств из базы (УУЗС)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDeviceTypeList_Uuzs()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            CGetTermDevTypeList response = new();
            try
            {
                response = await _UUZSData.GetDeviceTypeList_UuzsAsync(new Null());
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //CGetTermDevType
            return Ok(response.Array);
        }

        /// <summary>
        /// Поиск блока на порту 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetBlockInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BlockList s = new();
            try
            {
                s = await _SMData.GetBlockInfoAsync(request);

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //Block
            return Ok(s.Array);
        }

        /// <summary>
        /// Сохранить информацию об устройстве
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetControllingDeviceInfo(SetControllingDevice request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();
            try
            {

                CSetControllingDeviceInfo r = new();
                r.AsoControllingDeviceInfo = request.ControllingDevice;
                r.UpdatingControllingDeviceFlags = request.flags;
                r.ControllDescriptList = new ControllDescriptList();
                r.ControllDescriptList.Array.Add(request.ControllDesc);

                s = await _ASOData.SetControllingDeviceInfoAsync(r);

                int EventCode = 339;/*IDS_REG_BLOCK_INSERT*/
                if (request.ControllingDevice.DeviceID != 0)
                    EventCode = 340;/*IDS_REG_BLOCK_UPDATE*/


                await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: EventCode, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(s);
        }

        /// <summary>
        /// Сохранить информацию об устройстве (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetControllingDeviceInfoUUZS(List<SMSSGsoProto.V1.ControllingDevice> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            SMSSGsoProto.V1.ControllingDeviceList requestList = new();
            requestList.Array.AddRange(request);
            BoolValue s = new();
            try
            {
                s = await _SMSSGso.SetControllingDeviceInfoAsync(requestList);
                int EventCode = (int)GsoEnum.IDS_REG_DEV_INSERT;
                if (request.FirstOrDefault()?.DeviceID != 0)
                    EventCode = (int)GsoEnum.IDS_REG_DEV_UPDATE;

                await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: EventCode, SubsystemID: _userInfo.GetInfo?.SubSystemID, UserID: _userInfo.GetInfo?.UserID);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

            return Ok(s);
        }

        /// <summary>
        /// Список интервалов времени для контроля состояний
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSheduleList()
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
               var  response = await _SMData.GetSheduleListAsync(new GetSheduleListRequest());               
                //GetSheduleListItem
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }            
        }

        /// <summary>
        /// Контроль состояния устройства
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetDeviceShedule(OBJ_Key request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            GetShedule s = new();
            try
            {
                s = await _SMData.GetDeviceSheduleAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //GetShedule
            return Ok(s);
        }
        /// <summary>
        /// Установить расписание контроля состояния устройства
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> SetDeviceShedule(SetShedule request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            BoolValue s = new();
            try
            {
                s = await _SMData.SetDeviceSheduleAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //BoolValue
            return Ok(s);
        }

        /// <summary>
        /// Получить список линий
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GGetLineInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            CGGetLineInfo s = new();
            try
            {
                s = await _ASOData.GGetLineInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<GGetLineInfoItem>
            return Ok(s.Array);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GGetDeviceInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            AsoControllingDeviceInfo s = new();
            try
            {
                s = await _ASOData.GGetDeviceInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //GGetDeviceInfoResponse
            return Ok(s);
        }

        /// <summary>
        /// Список контроллеров для устройств
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GGetControllerInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            ControllDescriptList s = new();
            try
            {
                s = await _ASOData.GGetControllerInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<GGetControllerInfoItem>
            return Ok(s.Array);
        }

        [HttpPost]
        public async Task<IActionResult> GetControllerInfo(IntID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            try
            {
                var response = await _ASOData.GetControllerInfoAsync(request);
                //List<ControllerDescription>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetDeviceDescript(OBJ_ID request)
        {
            try
            {
                var response = await _ASOData.GetControllingDeviceInfoAsync(request);
                return Ok(response?.DeviceDescriptList?.Array);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Получаем информацию о управляющем устройстве
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetControllingDeviceInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            List<SMSSGsoProto.V1.ControllingDevice> s = new();
            try
            {
                s = await GetDeviceInfo(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<GGetControllerInfoItem>
            return Ok(s);
        }

        /// <summary>
        /// Получить параметры УЗС из таблиц
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        private async Task<List<SMSSGsoProto.V1.ControllingDevice>> GetDeviceInfo(OBJ_ID request)
        {
            SMSSGsoProto.V1.ControllingDeviceList s = new();
            try
            {
                s = await _SMSSGso.GetControllingDeviceInfoAsync(request);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, ex.Message));
            }
            return s.Array.ToList();
        }

        /// <summary>
        /// Сохраняем информацию о пу
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SetControlUnit(List<CControlUnit> request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            BoolValue s = new();

            CControlUnitList r = new();
            r.Array.AddRange(request);
            try
            {
                s = await _SMSSGso.SetControlUnitAsync(r);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Получаем название пункта управления
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetControlUnitInfo(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            ControlUnitInfo s = new();
            try
            {
                s = await _SMSSGso.GetControlUnitInfoAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }

        /// <summary>
        /// Удалить управляющие устройство
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create })]
        public async Task<IActionResult> DeleteControllingDevice(OBJ_ID request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    IntResponse s = new();
                    s = await _ASOData.DeleteControllingDeviceAsync(new IntID() { ID = request.ObjID });

                    await _Log.Write(Source: (int)GSOModules.AsoForms_Module, EventCode: 338/*IDS_REG_BLOCK_DELETE*/, SubsystemID: SubsystemType.SUBSYST_ASO, UserID: _userInfo.GetInfo?.UserID);

                    if (s.Int != 0)
                        return BadRequest();

                    return Ok(s);
                }
                else if (request.SubsystemID == SubsystemType.SUBSYST_SZS || request.SubsystemID == SubsystemType.SUBSYST_P16x)
                {
                    UInt32Value s = new() { Value = 0 };

                    var info = await GetDeviceInfo(request);

                    var b = await _SMSSGso.DeleteControllingDeviceAsync(request);

                    if (b.Value != true)
                        return BadRequest();

                    if (info?.Any() ?? false)
                    {
                        var first = info.First();

                        if (first.PortNo > 0 && first.Type != 0x0010/*SZS*/ && first.Type != 0x0008/*P16x*/)
                        {
                            s.Value = (uint)first.PortNo;
                        }
                    }
                    await _Log.Write(Source: (int)GSOModules.GsoForms_Module, EventCode: (int)GsoEnum.IDS_REG_DEV_DELETE, SubsystemID: SubsystemType.SUBSYST_SZS, UserID: _userInfo.GetInfo?.UserID);
                    return Ok(s);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
        }


        /// <summary>
        /// Добавить устройство (УУЗС)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [CheckPermission(new int[] { NameBitsPos.Create, NameBitsPos.CreateNoStandart })]
        public async Task<IActionResult> AddBlock(Block request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();

            IntID s = new();
            try
            {
                s = await _SMData.AddBlockAsync(request);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            return Ok(s);
        }


        /// <summary>
        /// Получить детальный список устройств
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetItems_IControllingDevice(GetItemRequest request)
        {
            using var activity = this.ActivitySourceForController()?.StartActivity();
            try
            {
                if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    AsoDataProto.V1.ControllingDeviceList s = new();
                    s = await _ASOData.GetItems_IControllingDeviceAsync(request);
                    return Ok(s.Array);
                }
                else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_SZS)
                {
                    CContrDeviceList s = new();
                    s = await _UUZSData.GetItems_IControllingDeviceAsync(request);
                    return Ok(s.Array);
                }
                else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_P16x)
                {
                    CContrDeviceList s = new();
                    s = await _SMP16x.GetItems_IControllingDeviceAsync(request);
                    return Ok(s.Array);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, Request.RouteValues["action"]?.ToString());
                return ex.GetResultStatusCode();
            }
            //List<ControllingDeviceItem>
            return NoContent();
        }

    }
}
