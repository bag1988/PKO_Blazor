using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using Google.Protobuf;
using AsoDataProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;
using SMSSGsoProto.V1;
using BlazorLibrary.GlobalEnums;
using FiltersGSOProto.V1;
using Label.V1;
using LibraryProto.Helpers;

namespace BlazorLibrary.Shared.Situation
{
    partial class CreateSituation : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public int? SitId { get; set; }

        [Parameter]
        public bool IsNoStandart { get; set; } = false;

        [Parameter]
        public EventCallback CallbackEvent { get; set; }

        [Parameter]
        public bool IsReadOnly { get; set; } = false;

        private string TitleName { get; set; } = "";

        private int? Abon = null;

        private SituationInfo NewSit = new() { SysSet = 1, Status = 1, SitTypeID = 1, SitPriority = 1 };

        private List<Objects>? Folders = null;

        private List<ItemTree>? SelectFolders = null;

        private bool IsCreateDevice = false;
        private bool IsCreateGroup = false;

        private SubsystemParam? SubParam;

        private bool EditSubParam = false;
        private int? IsIndividualMode { get; set; } = 0;

        private bool ViewMessageList = false;

        private int? MsgID = null;

        private SitTimeout? OldTimeOut = null;

        private List<CGetSitItemInfo>? OldList = null;

        private string? OldName = null;
        private int StaffId = 0;

        private bool IsAddListAbon = false;

        readonly HBits Bits = new();

        List<LabelNameValueField> labelsList = new();

        bool IsViewSpecification = false;

        IntID ConfirmPassword = new();

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            await LoadList();

            if (SitId == null)
            {
                NewSit.Sit = new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID };
                NewSit.SitTypeID = 0;//0-для нестандартного сценария
                NewSit.SitName = "# " + AsoRep["ScenarioFrom"] + " " + DateTime.Now.ToString("dd.MM") + "_" + DateTime.Now.ToString("T");

                //для обычного сценария
                if (IsNoStandart)
                {
                    NewSit.SitName = "";
                    NewSit.SitTypeID = 1;
                }
                SelectFolders = new();
            }
            else
            {
                await GetInfoModel();

            }

            if (SubsystemID == SubsystemType.SUBSYST_ASO)
            {
                await GetLabelsForFiltr();
                await GetSituationLabelField();
                await GetLabelFieldConfirmBySituation();
                PlaceHolder = FiltrName.FiltrSitStart;
            }
            IsPageLoad = false;
            _ = _HubContext.SubscribeAsync(this);
        }

        private async Task GetLabelsForFiltr()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLabelAllFiedlForForm", new OBJ_Key());
            if (result.IsSuccessStatusCode)
            {
                labelsList = await result.Content.ReadFromJsonAsync<List<LabelNameValueField>>() ?? new();

                if (labelsList.Count > 0)
                {
                    foreach (var item in labelsList)
                    {
                        if (!HintItems.Any(x => x.Key == item.NameField))
                            HintItems.Add(new HintItem(item.NameField, item.NameField, TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { LFrom = item.IdNameField, CountData = 20 }, LoadHelpDynamicName)));
                    }
                }
            }
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpDynamicName(GetItemRequest req)
        {
            List<Hint>? newData = new();

            string? nameFiled = labelsList.FirstOrDefault(x => x.IdNameField == req.LFrom)?.NameField;

            if (!string.IsNullOrEmpty(nameFiled))
            {

                var result = await Http.PostAsJsonAsync("api/v1/GetAllLabelValueForNameList", new ValueByNameAndEntity() { Name = nameFiled, Value = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            return newData ?? new();
        }

        private async Task LoadList()
        {
            await GetListFolder();
            if (SubsystemID == SubsystemType.SUBSYST_ASO)
                await GetListABC();
            else if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                await GetDeviceTypes();
            }
            await GetSubsystemParam();
        }

        //[Description(DaprMessage.PubSubName)]
        //public async Task Fire_UpdateAbonent(byte[] AbonentItemByte)
        //{
        //    if (SubsystemID != SubsystemType.SUBSYST_ASO)
        //        return;
        //    await GetListABC();
        //    StateHasChanged();
        //}


        [Description(DaprMessage.PubSubName)]
        public Task Fire_InsertDeleteList(byte[] value)
        {
            try
            {
                if (Folders == null)
                    Folders = new();

                if (Folders.Any(x => x.Type == ListType.LIST))
                {
                    var newItem = ListItem.Parser.ParseFrom(value);

                    if (newItem != null && newItem.List.SubsystemID == SubsystemID)
                    {
                        if (string.IsNullOrEmpty(newItem.Name) && string.IsNullOrEmpty(newItem.Comm) && newItem.Priority == 0)
                        {
                            SelectFolders?.RemoveAll(x => x.Key.OBJID.Equals(newItem.List) && x.Key.Type == ListType.LIST);
                            Folders?.RemoveAll(x => x.OBJID.Equals(newItem.List) && x.Type == ListType.LIST);
                        }
                        else
                        {
                            if (!Folders.Any(x => x.OBJID.Equals(newItem.List) && x.Type == ListType.LIST))
                            {
                                Folders.Insert(0, new Objects()
                                {
                                    Name = newItem.Name,
                                    OBJID = new OBJ_ID(newItem.List),
                                    Type = ListType.LIST
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            StateHasChanged();
            return Task.CompletedTask;
        }

        //[Description(DaprMessage.PubSubName)]
        //public async Task Fire_UpdateTermDevice(long Value)
        //{
        //    if (SubsystemID != SubsystemType.SUBSYST_SZS)
        //        return;
        //    await LoadList();
        //    StateHasChanged();
        //}
        //[Description(DaprMessage.PubSubName)]
        //public async Task Fire_InsertDeleteTermDevice(long Value)
        //{
        //    if (SubsystemID != SubsystemType.SUBSYST_SZS)
        //        return;
        //    await LoadList();
        //    StateHasChanged();
        //}


        //[Description(DaprMessage.PubSubName)]
        //public async Task Fire_UpdateTermDevicesGroup(long Value)
        //{
        //    if (SubsystemID != SubsystemType.SUBSYST_SZS)
        //        return;
        //    await LoadList();
        //    StateHasChanged();
        //}
        //[Description(DaprMessage.PubSubName)]
        //public async Task Fire_InsertDeleteTermDevicesGroup(long Value)
        //{
        //    if (SubsystemID != SubsystemType.SUBSYST_SZS)
        //        return;
        //    await LoadList();
        //    StateHasChanged();
        //}

        /// <summary>
        /// Получаем информацию о ситуации
        /// </summary>
        /// <returns></returns>
        private async Task GetInfoModel()
        {
            if (SitId != null)
            {
                // !!! возможно нужно вызвать S_GetSituationList - была ошибка в имени
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationInfo", new OBJ_ID() { ObjID = SitId.Value, StaffID = StaffId, SubsystemID = SubsystemID });
                if (result.IsSuccessStatusCode)
                {
                    NewSit = await result.Content.ReadFromJsonAsync<SituationInfo>() ?? new();

                    if (NewSit != null)
                    {
                        if (IsReadOnly && NewSit.SitTypeID == 0)
                            IsReadOnly = false;

                        OldName = TitleName = NewSit.SitName;

                        if (NewSit.SysSet == 0)
                            await GetSitTimeout(NewSit.Sit);
                        else
                            NewSit.Param = null;

                        if (SubsystemID == SubsystemType.SUBSYST_ASO || SubsystemID == SubsystemType.SUBSYST_SZS)
                            await GetSituationItems(NewSit.Sit);
                    }

                }
                else
                {
                    MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
                }
            }
        }

        /// <summary>
        /// Получаем таймауты
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private async Task GetSitTimeout(OBJ_ID req)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetSitTimeout", req);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<SitTimeout>();

                if (r != null)
                {
                    OldTimeOut = new(r);
                    NewSit.Param.TimeoutAbBu = r.NTioutBu;
                    NewSit.Param.VipTioutBu = r.NVipTioutBu;
                    NewSit.Param.VipTioutNo = r.NVipTioutNo;
                }
            }
        }

        /// <summary>
        /// Получаем список абонентов для редактирования ситуации(АСО)
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private async Task GetSituationItems(OBJ_ID req)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationItems", req);
            if (result.IsSuccessStatusCode)
            {
                OldList = await result.Content.ReadFromJsonAsync<List<CGetSitItemInfo>>();

                if (OldList != null && OldList.Any())
                {
                    if (SubsystemID == SubsystemType.SUBSYST_SZS)
                    {
                        var f = OldList.First();

                        if ((f.Param1 & 0xFFFF) != 0)
                        {
                            IsIndividualMode = 1;
                        }

                        if (OldList.Any(x => x.CmdID != f.CmdID
                        || x.CmdSubsystemID != f.CmdSubsystemID
                        || x.CmdParam != f.CmdParam
                        || x.Param1 != f.Param1
                        || x.MsgID != f.MsgID
                        || x.MsgStaffID != f.MsgStaffID
                        || Bits.HIWORD(x.Param1) != Bits.HIWORD(f.Param1)))
                        {
                            IsIndividualMode = 1;
                        }
                    }

                    MsgID = OldList.FirstOrDefault(x => x.MsgID > 0)?.MsgID;

                    ItemTree child = new();

                    foreach (var GroupList in OldList.GroupBy(x => x.ListID))
                    {
                        child = new();
                        var key = Folders?.FirstOrDefault(x => x.OBJID.SubsystemID == SubsystemID && x.OBJID.Equals(GroupList.Key));

                        if (key != null)
                        {
                            child.Key = new(key);
                            child.Child = new();
                            child.Child.AddRange(GroupList.Select(x => new CGetSitItemInfo(x)));
                            AddSelectFolder(child);
                        }
                        else
                        {
                            if (SubsystemID == SubsystemType.SUBSYST_ASO)
                            {
                                foreach (var GroupType in GroupList.GroupBy(x => x.Name.Substring(0, 1)))
                                {
                                    child = new();
                                    child.Key = Folders?.FirstOrDefault(x => x.Type == ListType.MAN && x.Name == GroupType.Key) ?? new() { OBJID = req, Type = ListType.MAN };
                                    child.Child = new();
                                    child.Child.AddRange(GroupType.Select(x => new CGetSitItemInfo(x)));
                                    AddSelectFolder(child);
                                }
                            }
                            else
                            {
                                foreach (var GroupType in GroupList.GroupBy(x => x.DevType))
                                {
                                    child = new();
                                    child.Key = Folders?.FirstOrDefault(x => x.Type == ListType.MEN && x.OBJID.ObjID == GroupType.Key) ?? new() { OBJID = req, Type = ListType.MEN };
                                    child.Child = new();
                                    child.Child.AddRange(GroupType.Select(x => new CGetSitItemInfo(x)));
                                    AddSelectFolder(child);
                                }
                            }
                        }
                    }

                }
                else
                    SelectFolders = new();
                StateHasChanged();
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
            }
        }

        private void AddSelectFolder(ItemTree child)
        {
            if (SelectFolders == null)
                SelectFolders = new();

            if (child.Child.Any())
            {
                if (SelectFolders.Any(x => x.Key.Equals(child.Key)))
                    SelectFolders.First(x => x.Key.Equals(child.Key)).Child.AddRange(child.Child);
                else
                    SelectFolders.Add(child);
            }
        }

        private async Task GetDeviceTypes()
        {
            var result = await Http.PostAsync("api/v1/GetDeviceTypeList_Uuzs", null);
            if (result.IsSuccessStatusCode)
            {
                var re = await result.Content.ReadFromJsonAsync<List<CGetTermDevType>>();
                if (re != null)
                {
                    if (Folders == null)
                        Folders = new();
                    Folders.AddRange(re.Select(x => new Objects()
                    {
                        Name = x.ObjectParam?.ObjectInfo?.Name,
                        OBJID = new OBJ_ID()
                        {
                            ObjID = x.ObjectParam?.ObjectInfo?.ObjID ?? 0,
                            StaffID = StaffId,
                            SubsystemID = SubsystemID
                        },
                        Type = ListType.MEN
                    }));
                }
            }
        }
        /// <summary>
        /// Получаем список папок
        /// </summary>
        /// <returns></returns>
        private async Task GetListFolder()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_IList", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                var re = await result.Content.ReadFromJsonAsync<List<Objects>>();
                if (re != null)
                {
                    if (Folders == null)
                        Folders = new();
                    Folders.AddRange(re.Select(x => new Objects(x) { Type = ListType.LIST }));
                }
            }
        }

        /// <summary>
        /// Получаем настройки по умолчанию
        /// </summary>
        /// <returns></returns>
        private async Task GetSubsystemParam()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetSubsystemParam", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                SubParam = await result.Content.ReadFromJsonAsync<SubsystemParam>() ?? new();
                SubParam.TimeoutAbBu = SubParam.TimeoutAb;
            }
        }

        private async Task GetLabelFieldConfirmBySituation()
        {
            if (SitId > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetLabelFieldConfirmBySituation", new OBJ_Key() { ObjID = new() { ObjID = SitId.Value, StaffID = StaffId, SubsystemID = SubsystemID }, ObjType = 1 });
                if (result.IsSuccessStatusCode)
                {
                    ConfirmPassword = await result.Content.ReadFromJsonAsync<IntID>() ?? new();
                }
            }
        }

        private async Task UpdateLabelFieldConfirmSituation(OBJ_KeyInt request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/UpdateLabelFieldConfirmSituation", request);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", GsoRep["IDS_E_SAVE_CONFIRM_PASSWORD"]);
            }
        }


        /// <summary>
        /// Установка пользовательских настроек
        /// </summary>
        /// <param name="param"></param>
        private void CallBackSubParam(SubsystemParam? param)
        {
            EditSubParam = false;
            if (param != null)
            {
                if (SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    if (param.TimeoutAb < 60)
                    {
                        MessageView?.AddMessage("", GsoRep["IDS_STRING_TIMEOUT_BETWEEN_CALL"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                    }
                    if (param.TimeoutAbBu < 60)
                    {
                        MessageView?.AddMessage("", GsoRep["IDS_STRING_TIMEOUT_BETWEEN_CALL_BUSY"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                    }


                    if (param.VipTioutNo < 60)
                    {
                        MessageView?.AddMessage("", AsoRep["TimoutNoAnswerVip"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                    }
                    if (param.VipTioutBu < 60)
                    {
                        MessageView?.AddMessage("", AsoRep["TimoutBusyVip"] + " " + AsoRep["ERR_TIMOUT_MSG"]);
                    }
                }
                NewSit.Param = param;
            }
        }

        /// <summary>
        /// Сохраняем сценарий АСО
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        private async Task SaveSit(OBJ_ID? Message)
        {
            if (Message != null)
            {
                if (SelectFolders != null)
                {
                    SelectFolders.ForEach(x =>
                    {
                        x.Child.ForEach(c =>
                        {
                            c.MsgID = Message.ObjID;
                            c.MsgStaffID = Message.StaffID;
                        });

                    });
                    await SaveSituation();
                }
            }
            ViewMessageList = false;
        }

        private async Task SaveSituation()
        {
            if (IsReadOnly)
            {
                ViewMessageList = false;
                await GoCallBack();
                return;
            }

            if (NewSit.SysSet == 1 || NewSit.Param == null)
                NewSit.Param = SubParam;

            bool IsName = false;
            if (string.IsNullOrEmpty(NewSit.SitName))
            {
                IsName = true;
            }
            if (!IsName && !NewSit.SitName.Equals(OldName))
            {
                var result = await Http.PostAsJsonAsync("api/v1/CheckMatchSitName", new OBJIDAndStr() { OBJID = NewSit.Sit, Str = NewSit.SitName });
                if (result.IsSuccessStatusCode)
                {
                    var re = await result.Content.ReadFromJsonAsync<IntID>() ?? new();
                    if (re.ID > 0)
                    {
                        IsName = true;
                    }
                }
            }
            if (IsName)
            {
                MessageView?.AddError(AsoRep["NameSit"], AsoRep["IDS_MISMATCHERROR"]);
                return;
            }

            if (NewSit.SitPriority < 0 && NewSit.SitPriority > 50)
            {
                MessageView?.AddError("", AsoRep["ErrorPriorList"]);
                return;
            }
            UpdateSituation Sit = new();
            Sit.Info = NewSit;

            if (SelectFolders?.Any() ?? false)
            {
                Sit.Items = SelectFolders.SelectMany(x => x.Child).ToList();
            }

            if (Sit.Items == null)
            {
                MessageView?.AddError("", AsoRep["IDS_CHOOSEERROR"]);
                return;
            }

            bool ErrorSave = false;
            int sitId = 0;
            if (SitId != null)
            {
                var deletelist = OldList?.Where(x => !Sit.Items.Any(c => c.SZSDevID == x.SZSDevID && c.SZSGroupID == x.SZSGroupID && c.AsoAbonID == x.AsoAbonID)).ToList() ?? new();

                if (deletelist.Any())
                {
                    CSitItemInfoList DeleteRequest = new();
                    DeleteRequest.Array.AddRange(deletelist.Distinct().Select(x => new SitItem()
                    {
                        Abon = new()
                        {
                            ObjID = x.AsoAbonID,
                            StaffID = x.AsoAbonStaffID
                        },
                        Cmd = new()
                        {
                            ObjID = x.CmdID,
                            SubsystemID = x.CmdSubsystemID
                        },
                        List = x.ListID,
                        Msg = new()
                        {
                            ObjID = x.MsgID,
                            StaffID = x.MsgStaffID
                        },
                        Szs = new()
                        {
                            ObjID = x.SZSDevID,
                            StaffID = x.SZSDevStaffID
                        },
                        SzsGroup = new()
                        {
                            ObjID = x.SZSGroupID,
                            StaffID = x.SZSGroupStaffID
                        },
                        CmdParam = x.CmdParam,
                        Param1 = x.Param1,
                        Param2 = x.Param2,
                        Sit = NewSit.Sit
                    }));

                    await Http.PostAsJsonAsync("api/v1/DeleteSitItem", JsonFormatter.Default.Format(DeleteRequest));

                }

                var groupList = Sit.Items.Where(x => x.SZSGroupID > 0 && (!OldList?.Any(c => c.SZSDevID == x.SZSDevID && c.SZSGroupID == x.SZSGroupID && c.AsoAbonID == x.AsoAbonID) ?? true)).ToList() ?? new();

                if (groupList.Any())
                {
                    foreach (var item in groupList)
                    {
                        OBJ_ID requestGroupInfo = new();
                        requestGroupInfo.ObjID = item.SZSGroupID;
                        requestGroupInfo.StaffID = item.SZSGroupStaffID;

                        CGetSitItemInfo newGroup = new();
                        newGroup.CmdSubsystemID = Sit.Info?.Sit?.SubsystemID ?? SubsystemType.SUBSYST_SZS;
                        newGroup.CmdID = 7;
                        newGroup.SZSGroupID = item.SZSGroupID;
                        newGroup.SZSGroupStaffID = item.SZSGroupStaffID;
                        var l = await GetGroupItemList(requestGroupInfo);

                        if (l.Any())
                            Sit.Items.AddRange(l.Select(x => new CGetSitItemInfo(newGroup) { SZSDevID = x.DevID, SZSDevStaffID = x.DevStaffID }));
                    }
                }

                Sit.Items = Sit.Items.Except(OldList ?? new()).ToList();

                UpdateSituationRequest req = new();
                req.Info = Sit.Info;
                req.Items.AddRange(Sit.Items);

                var result = await Http.PostAsJsonAsync("api/v1/UpdateSituation", JsonFormatter.Default.Format(req));
                if (!result.IsSuccessStatusCode)
                {
                    ErrorSave = true;
                    MessageView?.AddError("", GsoRep["IDS_ERRORCAPTION"]);
                }
                sitId = SitId.Value;

                if (NewSit.SysSet == 0 && NewSit.Param != null && (OldTimeOut?.NTioutBu != NewSit.Param.TimeoutAbBu || OldTimeOut?.NVipTioutBu != NewSit.Param.VipTioutBu || OldTimeOut?.NVipTioutNo != NewSit.Param.VipTioutNo))
                {
                    await UpdateSitTimeout(new SitTimeout(OldTimeOut ?? new()) { NTioutBu = NewSit.Param.TimeoutAbBu, NVipTioutBu = NewSit.Param.VipTioutBu, NVipTioutNo = NewSit.Param.VipTioutNo });
                }
            }
            else
            {
                var result = await Http.PostAsJsonAsync("api/v1/AddSituation", Sit);
                if (!result.IsSuccessStatusCode)
                {
                    ErrorSave = true;
                    MessageView?.AddError("", GsoRep["IDS_ERRORCAPTION"]);
                }
                else
                {
                    var response = await result.Content.ReadFromJsonAsync<OBJ_ID>() ?? new();
                    sitId = response.ObjID;
                }
            }

            if (!ErrorSave)
            {
                await GoCallBack();
                if (SubsystemID == SubsystemType.SUBSYST_ASO)
                {
                    _ = UpdateSituationLabelField(sitId);
                    _ = UpdateLabelFieldConfirmSituation(new OBJ_KeyInt() { ID = ConfirmPassword.ID, OBJKey = new() { ObjType = 1, ObjID = new() { ObjID = sitId, StaffID = StaffId, SubsystemID = SubsystemID } } });
                }
            }

        }

        private async Task SaveSitSzs(List<CGetSitItemInfo>? items)
        {
            if (items != null && SelectFolders != null)
            {
                SelectFolders.ForEach(x =>
                {
                    x.Child.ForEach(c =>
                    {
                        if (items.Any(i => i.SZSDevID == c.SZSDevID && i.SZSGroupID == c.SZSGroupID))
                            c = items.First(i => i.SZSDevID == c.SZSDevID && i.SZSGroupID == c.SZSGroupID);
                    });
                });
                await SaveSituation();
            }
            ViewMessageList = false;
        }


        private async Task<List<CGroupDevID>> GetGroupItemList(OBJ_ID req)
        {
            List<CGroupDevID> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetGroupItemList", req);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<List<CGroupDevID>>() ?? new();
            }
            else
                MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
            return response;
        }

        private async Task UpdateSituationLabelField(int sitId)
        {
            if (sitId > 0)
            {
                SituationLabelField request = new();
                request.SubsystemId = SubsystemID;
                request.StaffId = StaffId;
                request.SitId = sitId;
                request.FiltrDynamic = FiltrModel.ToByteString();
                var result = await Http.PostAsJsonAsync("api/v1/UpdateSituationLabelField", JsonFormatter.Default.Format(request));
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", GsoRep["ERROR_SAVE_LABELS"]);
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_SAVE_LABELS_NO_SIT"]);
            }
        }

        private async Task GetSituationLabelField()
        {
            try
            {
                if (SitId > 0)
                {
                    var result = await Http.PostAsJsonAsync("api/v1/GetSituationLabelField", new SitLabelField() { StaffId = StaffId, SubsystemId = SubsystemID, SitId = SitId.Value });
                    if (result.IsSuccessStatusCode)
                    {
                        var re = await result.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(re))
                        {
                            var labels = JsonParser.Default.Parse<SituationLabelField>(re);

                            if (labels != null)
                            {
                                FiltrModel = SitStartFiltr.Parser.ParseFrom(labels.FiltrDynamic);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error get filtr for situation, {ex.Message}");
            }
        }

        private void NextPage()
        {
            ViewMessageList = true;
        }

        private async Task GoCallBack()
        {
            if (CallbackEvent.HasDelegate)
                await CallbackEvent.InvokeAsync();
        }

        /// <summary>
        /// Обнавляем таймауты
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private async Task UpdateSitTimeout(SitTimeout req)
        {
            await Http.PostAsJsonAsync("api/v1/UpdateSitTimeout", req);
        }

        /// <summary>
        /// Получаем список абонентов сортированных по алфавиту
        /// </summary>
        /// <returns></returns>
        private async Task GetListABC()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetABCAbonent", new IntID() { ID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                var re = await result.Content.ReadFromJsonAsync<List<string>>();
                if (re != null)
                {
                    if (Folders == null)
                        Folders = new();
                    Folders.AddRange(re.Select(x => new Objects()
                    {
                        Name = x,
                        OBJID = new OBJ_ID()
                        {
                            ObjID = re.IndexOf(x),
                            StaffID = StaffId,
                            SubsystemID = SubsystemID
                        },
                        Type = ListType.MAN
                    }));

                }
            }
        }

        private string GetTextTitle
        {
            get
            {
                if (SitId == null)
                {
                    if (IsNoStandart)
                    {
                        return SubsystemID switch
                        {
                            SubsystemType.SUBSYST_SZS => GsoRep["IDS_STRING_CREATE_SIT_SZS"],
                            SubsystemType.SUBSYST_GSO_STAFF => GsoRep["IDS_STRING_CREATE_SIT_CU"],
                            _ => GsoRep["IDS_STRING_CREATE_SIT_ASO"]
                        };
                    }
                    else
                    {
                        return SubsystemID switch
                        {
                            SubsystemType.SUBSYST_SZS => GsoRep["IDS_PR_SZS_3"],
                            SubsystemType.SUBSYST_GSO_STAFF => GsoRep["IDS_PR_CU_3"],
                            _ => GsoRep["IDS_PR_ASO_3"]
                        };
                    }
                }
                else
                {
                    return NewSit.SitName;
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
