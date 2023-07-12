using Google.Protobuf.WellKnownTypes;
using ArmODProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Mvc;
using ServerLibrary;
using SharedLibrary;
using SMDataServiceProto.V1;
using static AsoDataProto.V1.AsoData;
using static SMDataServiceProto.V1.SMDataService;
using ServerLibrary.Extensions;

namespace ARMSettings.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[action]")]
    [CheckPermission(new[] { NameBitsPos.General })]
    public class ARMSettingsController : Controller
    {
        private readonly ILogger<ARMSettingsController> logger;

        private readonly SMSSGso.SMSSGsoClient _SMGso;

        private readonly AsoDataClient _ASOData;

        private readonly ArmOD.ArmODClient _ArmClient;

        private readonly SMDataServiceProto.V1.SMDataService.SMDataServiceClient _SMData;

        public ARMSettingsController(ILogger<ARMSettingsController> logger, SMSSGso.SMSSGsoClient data, SMDataServiceProto.V1.SMDataService.SMDataServiceClient sMData, ArmOD.ArmODClient armClient, AsoDataClient asoData)
        {
            this.logger = logger;
            _SMGso = data;
            _ArmClient = armClient;
            _SMData = sMData;
            _ASOData = asoData;
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddGroup(P16xGroup request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                IntID result;
                result = await _SMData.AddGroupAsync(request);

                //IntID
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> EditGroup(P16xGroup request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _SMData.EditGroupAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> RemoveGroup(IntID request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _SMData.RemoveGroupAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_ToolStripButtonRemoveGroup_Click_DeleteCount(FillNodeGroupItems request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.P16xObjectList_ToolStripButtonRemoveGroup_Click_DeleteCountAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_ToolStripButtonAddObjectInToGroup_Click_Insert(FillNodeGroupItems request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.P16xObjectList_ToolStripButtonAddObjectInToGroup_Click_InsertAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_ToolStripButtonRemoveObject_Click(IntID request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.P16xObjectList_ToolStripButtonRemoveObject_ClickAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddGroupCommand(GroupCommand request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.AddGroupCommandAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> P16xObjectList_FillNode_GroupItems(IntID request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                FillNodeGroupItemsList result;
                result = await _ArmClient.P16xObjectList_FillNode_GroupItemsAsync(request);

                //List<FillNodeGroupItems>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetGroupCommandList(IntID request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                GroupCommandList result;
                result = await _ArmClient.GetGroupCommandListAsync(request);

                //List<GroupCommand>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_DoObjectManage_Insert(P16xGateObjectControl request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.P16xObjectList_DoObjectManage_InsertAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_DoObjectManage_Delete(IntID request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                BoolValue result;
                result = await _ArmClient.P16xObjectList_DoObjectManage_DeleteAsync(request);

                //BoolValue
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> P16xObjectList_DoObjectManage_ObjectID(ReDrawList request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                IntID result;
                result = await _ArmClient.P16xObjectList_DoObjectManage_ObjectIDAsync(request);

                //IntID
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> P16xObjectManage_ReDraw_UZSlist()
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                PRDList result;
                result = await _ArmClient.P16xObjectManage_ReDraw_UZSlistAsync(new Empty());

                //List<PRD>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> P16xObjectManage_ReDraw_StaffList()
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                StaffControlUnitList result;
                result = await _ArmClient.P16xObjectManage_ReDraw_StaffListAsync(new Empty());

                //List<StaffControlUnit>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> P16xObjectManage_ReDraw_PRDlist()
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                PRDList result;
                result = await _ArmClient.P16xObjectManage_ReDraw_PRDlistAsync(new Empty());

                //List<PRD>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }

        [HttpPost]
        public async Task<IActionResult> P16xObjectList_ReDraw_GroupList()
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                P16xGroupList result;
                result = await _ArmClient.P16xObjectList_ReDraw_GroupListAsync(new Empty());

                //List<P16xGroup>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> P16xObjectList_ReDraw_ListControl(IntID request)
        //{
        //    logger.LogInformation(Request.RouteValues["action"]?.ToString());
        //    try
        //    {
        //        P16xGateObjectControlList result;
        //        result = await _ArmClient.P16xObjectList_ReDraw_ListControlAsync(request);

        //        //List<P16xGateObjectControl>
        //        return Ok(result.Array);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex.Message);
        //        return ex.GetResultStatusCode();
        //    }
        //}


        [HttpPost]
        public async Task<IActionResult> P16xObjectList_ReDraw_List(GetItemRequest request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                ReDrawListL result;
                result = await _ArmClient.P16xObjectList_ReDraw_ListAsync(request);

                //List<ReDrawList>
                return Ok(result.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetGsoUserSecurity(GsoUserSecurity request)
        {
            // ReSharper disable once RedundantAssignment
            GsoUserSecurityList response = new();
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                response = await _SMGso.GetGsoUserSecurityAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(response!.Array);
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> SetGsoUserSecurity(GsoUserSecurity request)
        {
            BoolValue result;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.SetGsoUserSecurityAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetUserSecurityGroup([FromBody] int userId)
        {
            // ReSharper disable once RedundantAssignment
            SecurityGroupList response = new();
            var request = new IntID() { ID = userId };

            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                response = await _SMGso.GetUserSecurityGroupAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> RemoveSecurityUserGroup(CChangeSecurityUserGroup request)
        {
            BoolValue result;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.RemoveSecurityUserGroupAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetSecurityGroup()
        {
            // ReSharper disable once RedundantAssignment
            SecurityGroupList response = new();

            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                response = await _SMGso.GetSecurityGroupAsync(new Empty());
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(response.Array);
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddSecurityUserGroup(CChangeSecurityUserGroup request)
        {
            BoolValue result;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.AddSecurityUserGroupAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetUserSecurityParams([FromBody] int? userId)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                var response = await _SMGso.GetUserSecurityParamsAsync(new IntID() { ID = userId ?? 0 });
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }

        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> AddUserSecurityParam(CChangeUserSecurityParam request)
        {
            BoolValue result;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.AddUserSecurityParamAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result);
        }

        [HttpPost]
        [CheckPermission(new[] { NameBitsPos.Create })]
        public async Task<IActionResult> RemoveUserSecurityParam(CChangeUserSecurityParam request)
        {
            BoolValue result;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.RemoveUserSecurityParamAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetItems_ISituation_WithObjId(GetItemRequest request)
        {
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                var response = await _ASOData.GetItems_ISituationAsync(request);
                //List<SituationItem>
                return Ok(response.Array);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }

        }
        [HttpPost]
        public async Task<IActionResult> GetSitGroupList()
        {
            // ReSharper disable once RedundantAssignment
            SitGroupInfoList result = new();
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                result = await _SMGso.GetSitGroupListAsync(new OBJ_ID());
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(result.Array);
        }
        [HttpPost]
        public async Task<IActionResult> AddSitGroup(SitGroupInfo request)
        {
            // ReSharper disable once RedundantAssignment
            var responce = new SitGroupIDResponse();
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                responce = await _SMGso.AddSitGroupAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSitGroup(SitGroupInfo request)
        {
            BoolValue responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                responce = await _SMGso.UpdateSitGroupAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSitGroup([FromBody] int sitId)
        {
            var id = new IntID()
            {
                ID = sitId
               ,
            };
            BoolValue responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                responce = await _SMGso.RemoveSitGroupAsync(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce);
        }

        [HttpPost]
        public async Task<IActionResult> GetSitGroupListLink([FromBody] int sitId)
        {
            var id = new IntID()
            {
                ID = sitId
               ,
            };
            SitGroupLinkInfoList responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                responce = await _SMGso.GetSitGroupListLinkAsync(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce.Array);
        }

        [HttpPost]
        public async Task<IActionResult> AddSitGroupLink([FromBody] string json)
        {
            BoolValue responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                var request = SitGroupLinkRequest.Parser.ParseJson(json);
                responce = await _SMGso.AddSitGroupLinkAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveSitGroupLink([FromBody] string json)
        {
            BoolValue responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                var request = SitGroupLinkRequest.Parser.ParseJson(json);
                responce = await _SMGso.RemoveSitGroupLinkAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce);
        }

        [HttpPost]
        public async Task<IActionResult> GetSituationState(OBJ_Key request)
        {
            SituationStateList responce;
            logger.LogInformation(Request.RouteValues["action"]?.ToString());
            try
            {
                responce = await _SMGso.GetSituationStateAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return ex.GetResultStatusCode();
            }
            return Ok(responce.Array);
        }
    }

}
