using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Models;
using Google.Protobuf;
using SMControlSysProto.V1;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SharedLibrary.Models;
using static BlazorLibrary.Shared.Main;
using SituationItem = SMControlSysProto.V1.SituationItem;
using SharedLibrary.Interfaces;

namespace BlazorLibrary.Shared.Situation
{
    partial class CreateSitStaff : IAsyncDisposable, IPubSubMethod
    {
        int SubsystemID { get; set; } = SubsystemType.SUBSYST_GSO_STAFF;

        [Parameter]
        public int? SitId { get; set; }

        [Parameter]
        public bool IsNoStandart { get; set; } = false;

        [Parameter]
        public EventCallback CallbackEvent { get; set; }

        [Parameter]
        public bool IsReadOnly { get; set; } = false;

        int SelectSubsystemId = 0;
        //private string TitleName { get; set; } = "";

        private SituationInfo NewSit = new() { SysSet = 1, Status = 1, SitTypeID = 1, SitPriority = 1 };
        private SituationInfo OldSit = new() { SysSet = 1, Status = 1, SitTypeID = 1, SitPriority = 1 };

        private List<SituationItem>? Folders = null;

        private List<SituationItem>? SelectFolders = null;

        private List<SituationItem>? SelectData = null;

        private List<SituationItem>? SelectItem = null;

        private bool ViewMessageList = false;

        private List<CcommSubSystem>? SubSystemList = null;

        private List<CUListsItem>? CULists = null;

        //private string? OldName = null;
        private int StaffId = 0;

        private int GlobalNum = 1;

        private List<SituationItem>? OldStaffList = null;

        bool IsPageLoad = true;

        bool IsProcessing = false;

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
            IsPageLoad = false;
            _ = _HubContext.SubscribeAsync(this);
        }

        private async Task LoadList()
        {
            await GetCULists();
            await GetSubSystemList();
            //await GetObjects_ISituation(SubSystemList?.FirstOrDefault()?.SubSystID ?? 0);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateAbonent(byte[] AbonentItemByte)
        {
            await LoadList();

            StateHasChanged();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteList(byte[] value)
        {
            await GetCULists();
            await GetSubSystemList();
            await GetObjects_ISituation(SubSystemList?.FirstOrDefault()?.SubSystID ?? 0);
            StateHasChanged();
        }

        /// <summary>
        /// получаем список подсистем
        /// </summary>
        /// <returns></returns>
        private async Task GetSubSystemList()
        {
            await Http.PostAsync("api/v1/GetSubSystemList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    SubSystemList = await x.Result.Content.ReadFromJsonAsync<List<CcommSubSystem>>();

                    if (SubSystemList != null)
                    {
                        SubSystemList.RemoveAll(x => (!CULists?.Where(cu => cu.OBJID.StaffID == StaffId).Any(cu => cu.OBJID.SubsystemID == x.SubSystID) ?? false));
                    }

                }
            });
        }

        /// <summary>
        /// получаем список пунктов управления
        /// </summary>
        /// <returns></returns>
        private async Task GetCULists()
        {
            await Http.PostAsync("api/v1/GetCULists", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    CULists = await x.Result.Content.ReadFromJsonAsync<List<CUListsItem>>();
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_EGETREGLISTINFO"]);
                }
            });
        }

        private async Task ChangeSubSystem(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out int SubId);
            await ChangeSelectSubsystem(SubId);
        }

        async Task ChangeSelectSubsystem(int SubId)
        {
            Folders = null;
            SelectData = null;
            SelectSubsystemId = SubId;

            if (SubId > 0)
            {
                if (SubId != SubsystemType.SUBSYST_GSO_STAFF)
                    await GetObjects_ISituation(SubId);
                else
                {
                    Folders = new();
                    if (CULists != null)
                    {
                        Folders.AddRange(CULists.Where(x => x.OBJID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF && x.OBJID.StaffID != StaffId).Select(x => new SituationItem()
                        {
                            SitID = x.OBJID,
                            SitName = x.CUName,
                            CustMsg = new()
                        }));
                    }
                }
            }
        }

        private async Task GetObjects_ISituation(int SubId)
        {
            await Http.PostAsJsonAsync("api/v1/GetObjects_ISituation", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var re = await x.Result.Content.ReadFromJsonAsync<List<Objects>>();
                    if (re != null)
                    {
                        if (Folders == null)
                            Folders = new();

                        Folders.AddRange(re.Select(x => new SituationItem()
                        {
                            SitID = x.OBJID,
                            SitName = x.Name,
                            CustMsg = new()
                        }));
                    }
                    else
                        Folders = new();
                }
                else
                {
                    MessageView?.AddError("", GsoRep["ERROR_GET_SIT_INFO"]);
                }
            });
        }

        /// <summary>
        /// Получаем информацию о ситуации
        /// </summary>
        /// <returns></returns>
        private async Task GetInfoModel()
        {
            if (SitId != null)
            {
                await Http.PostAsJsonAsync("api/v1/GetSituationInfo", new OBJ_ID() { ObjID = SitId.Value, StaffID = StaffId, SubsystemID = SubsystemID }).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        NewSit = await x.Result.Content.ReadFromJsonAsync<SituationInfo>() ?? new();

                        OldSit = new(NewSit);

                        if (NewSit != null)
                        {
                            if (IsReadOnly && NewSit.SitTypeID == 0)
                                IsReadOnly = false;


                            int.TryParse(NewSit.CodeName, out GlobalNum);
                            await GetSituationItemsStaff(NewSit.Sit);
                        }
                    }
                    else
                    {
                        MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
                    }
                });
            }
        }

        /// <summary>
        /// Получаем список абонентов для редактирования ситуации(Система управления)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task GetSituationItemsStaff(OBJ_ID request)
        {
            await Http.PostAsJsonAsync("api/v1/GetSituationItems", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    OldStaffList = await x.Result.Content.ReadFromJsonAsync<List<SituationItem>>();

                    if (OldStaffList != null)
                    {
                        if (SelectFolders == null)
                            SelectFolders = new();

                        OldStaffList.ForEach(x =>
                        {
                            var n = CULists?.FirstOrDefault(cu => cu.OBJID.StaffID == x.SitID.StaffID && cu.OBJID.SubsystemID == x.SitID.SubsystemID)?.CUName;

                            if (x.SitID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                            {
                                x.SitName = n;
                            }
                            else
                            {
                                x.SitName = $"{x.SitName} ({n})";
                            }
                            if (x.CustMsg == null)
                                x.CustMsg = new();

                        });

                        SelectFolders = new(OldStaffList.Select(x => new SituationItem(x)));

                        await ChangeSelectSubsystem(SelectFolders.FirstOrDefault()?.SitID?.SubsystemID ?? 0);

                        StateHasChanged();
                    }

                }
                else
                {
                    MessageView?.AddError("", GsoRep["IDS_EMESSAGEINFO"]);
                }
            });
        }

        /// <summary>
        /// сохраняем сценарий Staff
        /// </summary>
        /// <param name="isSave"></param>
        /// <returns></returns>
        private async Task SaveSitStaff(List<SituationItem>? NewStaffList = null)
        {

            if (IsReadOnly && NewStaffList == null)
            {
                ViewMessageList = false;
                await GoCallBack();
                return;
            }

            if (NewStaffList == null)
            {
                ViewMessageList = false;
                return;
            }

            if ((!NewStaffList.Any()))
            {
                MessageView?.AddError("", AsoRep["IDS_CHOOSEERROR"]);
                return;
            }

            if ((OldStaffList?.SequenceEqual(NewStaffList) ?? false) && OldSit.Equals(NewSit))
            {
                MessageView?.AddError("", AsoRep["NotChangeSave"]);
                return;
            }
            NewSit.Param = new SubsystemParam() { CountRepeat = 1 };

            var DeleteArray = OldStaffList?.Except(NewStaffList);
            //var DeleteArray = OldStaffList?.Where(x => !NewStaffList.Any(n => (x.SitID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF && n.SitID.StaffID == x.SitID.StaffID) || (x.SitID.SubsystemID != SubsystemType.SUBSYST_GSO_STAFF && n.SitID.ObjID == x.SitID.ObjID)));

            if (DeleteArray?.Any() ?? false)
            {
                DeleteStaffSitItems DeleteRequest = new();
                DeleteRequest.Array.AddRange(DeleteArray.Select(x => new DeleteStaffSitItem() { CmdID = NewSit.Sit, SitID = x.SitID }));
                await Http.PostAsJsonAsync("api/v1/DeleteSitItem", JsonFormatter.Default.Format(DeleteRequest)).ContinueWith(x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", StartUIRep["IDS_STRING_SIT_DEL_ERR"]);
                    }
                });
            }

            var addSitItem = NewStaffList.Except(OldStaffList ?? new()).ToList();
            bool ErrorSave = false;
            if (!OldSit.Equals(NewSit) || addSitItem.Count > 0)
            {
                UpdateSituationStaff request = new();
                request.Info = NewSit;
                request.Items = addSitItem;
                request.UserSessId = await _User.GetUserSessId();


                await Http.PostAsJsonAsync("api/v1/AddSituationStaff", request).ContinueWith(x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        ErrorSave = true;
                        MessageView?.AddError("", GsoRep["IDS_ERRORCAPTION"]);
                    }
                });
            }

            //if (!ErrorSave)
            ViewMessageList = false;

            if (!ErrorSave)
                await GoCallBack();
        }

        private List<SituationItem> GetListTree
        {
            get
            {
                var l = Folders?.Where(x => !SelectFolders?.Any(s => (x.SitID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF ? s.SitID.StaffID.Equals(x.SitID.StaffID) : s.SitID.Equals(x.SitID))) ?? false).ToList();
                return l ?? new List<SituationItem>();
            }

        }

        private void RemoveSelect()
        {
            if (SelectItem == null)
                return;

            RemoveToSelectFolders(new List<SituationItem>(SelectItem));
        }

        private void RemoveAll()
        {
            if (SelectFolders == null)
                return;

            RemoveToSelectFolders(SelectFolders.Select(x => new SituationItem(x)).ToList());
        }

        private void RemoveToSelectFolders(List<SituationItem> items)
        {
            if (SelectFolders == null)
                return;

            var newSelect = SelectFolders.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x));

            foreach (var item in items)
            {
                SelectFolders.Remove(item);
            }
            SelectItem = null;
            if (SelectFolders.Count > 0)
            {
                if (newSelect == null)
                {
                    SelectItem = new() { SelectFolders.Last() };
                }
                else
                    SelectItem = new() { newSelect };
            }
        }

        private void AddSelect()
        {
            if (SelectData == null)
                return;
            AddToSelectFolders(new List<SituationItem>(SelectData));
        }

        private void AddAll()
        {
            if (Folders == null)
                return;
            AddToSelectFolders(GetListTree);
        }

        private void AddToSelectFolders(List<SituationItem> items)
        {
            if (SelectFolders == null)
                SelectFolders = new();

            var newSelect = GetListTree.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x));

            foreach (var item in items)
            {
                if (!SelectFolders.Any(x => x.SitID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF
                                            ? (x.SitID.StaffID == item.SitID.StaffID && x.SitID.SubsystemID == item.SitID.SubsystemID)
                                            : x.SitID.Equals(item.SitID)))
                {
                    item.ExInfoType = 1;
                    SelectFolders.Add(item);
                    StateHasChanged();
                }
                else
                {
                    MessageView?.AddError("", item.SitName + " - " + StartUIRep["IDS_AB_BEEN_SELECTED"]);
                }
            }
            SelectData = null;
            if (GetListTree.Count > 0)
            {
                if (newSelect == null)
                {
                    SelectData = new() { GetListTree.Last() };
                }
                else
                    SelectData = new() { newSelect };
            }
        }

        private async Task NextPage()
        {
            if (SelectFolders == null || !SelectFolders.Any())
            {
                MessageView?.AddError("", AsoRep["IDS_CHOOSEERROR"]);
                return;
            }

            IsProcessing = true;

            bool IsName = false;
            if (string.IsNullOrEmpty(NewSit.SitName))
            {
                MessageView?.AddError("", $"{DeviceRep["ErrorNull"]} {AsoRep["NameSit"].ToString().ToLower()}");
            }
            else if (!NewSit.SitName.Equals(OldSit.SitName))
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
            }
            else
            {
                CountResponse? r = null;
                if (GlobalNum < 0)
                {
                    MessageView?.AddError("", GsoRep["IDS_NOCODENAME"]);
                }
                else
                {
                    if (NewSit.Sit != null)
                    {
                        int.TryParse(NewSit.CodeName, out int OldGlobalNum);

                        if (OldGlobalNum != GlobalNum)
                        {
                            var result = await Http.PostAsJsonAsync("api/v1/CheckMatchCodeName", new OBJIDAndStr() { OBJID = NewSit.Sit, Str = GlobalNum.ToString() });
                            if (result.IsSuccessStatusCode)
                            {
                                r = await result.Content.ReadFromJsonAsync<CountResponse>();
                            }
                        }
                        else
                            r = new CountResponse() { Count = 0 };
                    }

                    if (r == null || r.Count > 0)
                    {
                        MessageView?.AddError("", GsoRep["IDS_CODENAMEEXIST"]);
                    }
                    else
                    {
                        NewSit.CodeName = GlobalNum.ToString();
                        SelectFolders.ForEach(x =>
                        {
                            if (x.SitID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                                x.SitID.ObjID = GlobalNum;
                        });
                        ViewMessageList = true;
                    }
                }
                

            }
            IsProcessing = false;

        }

        private async Task GoCallBack()
        {
            if (CallbackEvent.HasDelegate)
                await CallbackEvent.InvokeAsync();
        }

        private string GetIconName(int? id)
        {
            string response = "folder text-warning";
            switch (id)
            {
                case ListType.MAN:
                case ListType.Aso: response = "people text-warning"; break;
                case ListType.Szs: response = "bullhorn"; break;
                case ListType.LIST: response = "list"; break;
                case ListType.Staff: response = "monitor"; break;
            }

            return response;
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
            return _HubContext.DisposeAsync();
        }
    }
}
