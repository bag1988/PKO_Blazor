using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary.Models;
using SMSSGsoProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;

namespace BlazorLibrary.Shared.ListObject
{
    partial class CreateList : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public EventCallback CallbackEvent { get; set; }

        [Parameter]
        public int? ListId { get; set; }

        private string? TitleName { get; set; }

        //Основная модель
        private Objects ListModel = new() { Type = 1 };

        private List<Objects>? Folders { get; set; } = null;

        private List<ItemTree>? SelectFolders = null;

        private List<CListItemInfo>? OldListItem = null;

        private int StaffId = 0;

        bool IsPageLoad = true;

        bool IsProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            if (SubsystemID == SubsystemType.SUBSYST_ASO)
                TitleName = GsoRep["IDS_STRING_CREATE_AB_LIST"];
            else if (SubsystemID == SubsystemType.SUBSYST_SZS)
                TitleName = GsoRep["IDS_STRING_CREATE_DEVICE_LIST"];

            await LoadList();

            if (ListId != null)
            {
                await GetListInfo();
            }
            else
                SelectFolders = new();

            IsPageLoad = false;
            _ = _HubContext.SubscribeAsync(this);
        }

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

        [Description(DaprMessage.PubSubName)]
        public Task Fire_UpdateList(byte[] value)
        {
            try
            {
                if (Folders != null && Folders.Any(x => x.Type == ListType.LIST))
                {
                    var newItem = ListItem.Parser.ParseFrom(value);

                    if (newItem != null && newItem.List.SubsystemID == SubsystemID)
                    {
                        SelectFolders?.ForEach(x =>
                        {
                            if (x.Key.Type == ListType.LIST && x.Key.OBJID.Equals(newItem.List))
                            {
                                x.Key.Name = newItem.Name;
                                x.Key.Comm = newItem.Comm;
                                return;
                            }
                        });

                        Folders.ForEach(x =>
                        {
                            if (x.Type == ListType.LIST && x.OBJID.Equals(newItem.List))
                            {
                                x.Name = newItem.Name;
                                x.Comm = newItem.Comm;
                                return;
                            }
                        });
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

        private async Task LoadList()
        {
            await GetListFolder();

            if (SubsystemID == SubsystemType.SUBSYST_ASO)
            {
                await GetListDepFolder();
                await GetListABC();
            }
            else if (SubsystemID == SubsystemType.SUBSYST_SZS)
            {
                await GetDeviceTypes();
            }
        }

        private async Task GetListInfo()
        {
            if (ListId != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetListInfo", new OBJ_ID() { ObjID = ListId.Value, StaffID = StaffId, SubsystemID = SubsystemID });
                if (result.IsSuccessStatusCode)
                {
                    var re = await result.Content.ReadFromJsonAsync<GetListInfoResponse>();
                    if (re != null)
                    {
                        ListModel = new() { Comm = re.SzComm, Name = re.SzName, OBJID = new() { ObjID = re.DwListID, StaffID = re.DwListStaffID, SubsystemID = re.DwListSubsystemID }, Type = re.DwPriority };
                        TitleName = ListModel.Name;
                        await GetChildList(ListModel.OBJID);
                    }
                }
            }
        }

        private async Task GetChildList(OBJ_ID obj)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetListItems", obj);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<List<Items>>() ?? new();

                if (r != null)
                {
                    ItemTree child = new();

                    foreach (var GroupType in r.GroupBy(x => x.DevType))
                    {
                        if (GroupType.Key == 0)
                        {
                            if (SubsystemID == SubsystemType.SUBSYST_ASO)
                            {
                                foreach (var item in GroupType.GroupBy(x => x.Name.Substring(0, 1)))
                                {
                                    child = new();
                                    child.Key = Folders?.FirstOrDefault(x => x.Type == ListType.MAN && x.Name == item.Key) ?? new() { OBJID = obj, Type = ListType.MAN };
                                    child.Child = new();
                                    child.Child.AddRange(item.Select(x => new CGetSitItemInfo()
                                    {
                                        Name = x.Name,
                                        AsoAbonID = x.AsoAbon?.ObjID ?? 0,
                                        AsoAbonStaffID = x.AsoAbon?.StaffID ?? 0,
                                        SZSDevID = x.SZSDev?.ObjID ?? 0,
                                        SZSDevStaffID = x.SZSDev?.StaffID ?? 0
                                    }));
                                    AddSelectFolder(child);
                                }
                            }
                        }
                        else
                        {
                            child = new();
                            var key = Folders?.FirstOrDefault(x => x.Type == ListType.MEN && x.OBJID.ObjID == GroupType.Key);
                            if (key != null)
                            {
                                child.Key = new(key);
                                child.Child = new();
                                child.Child.AddRange(GroupType.Select(x => new CGetSitItemInfo()
                                {
                                    Name = x.Name,
                                    AsoAbonID = x.AsoAbon?.ObjID ?? 0,
                                    AsoAbonStaffID = x.AsoAbon?.StaffID ?? 0,
                                    SZSDevID = x.SZSDev?.ObjID ?? 0,
                                    SZSDevStaffID = x.SZSDev?.StaffID ?? 0
                                }));
                                AddSelectFolder(child);
                            }
                        }
                    }
                }
            }

            if (SelectFolders == null)
                SelectFolders = new();
            StateHasChanged();
        }

        private void AddSelectFolder(ItemTree child)
        {
            if (SelectFolders == null)
                SelectFolders = new();
            if (OldListItem == null)
                OldListItem = new();
            if (child.Child.Any())
            {
                if (SelectFolders.Any(x => x.Key.Equals(child.Key)))
                    SelectFolders.First(x => x.Key.Equals(child.Key)).Child.AddRange(child.Child);
                else
                    SelectFolders.Add(child);

                OldListItem.AddRange(child.Child.Select(x => new CListItemInfo()
                {
                    ListID = ListId ?? 0,
                    ListStaffID = StaffId,
                    ListSubsystemID = SubsystemID,
                    AsoAbonID = x.AsoAbonID,
                    AsoAbonStaffID = x.AsoAbonStaffID,
                    SZSDevID = x.SZSDevID,
                    SZSDevStaffID = x.SZSDevStaffID
                }));
            }
        }

        private async Task AddList()
        {
            IsProcessing = true;
            if (string.IsNullOrEmpty(ListModel.Name))
            {
                MessageView?.AddError(GsoRep["IDS_CREATENOLIST"], AsoRep["IDS_E_NOLISTNAME"]);                
            }
            else  if (ListModel.Type < 1 || ListModel.Type > 50)
            {
                MessageView?.AddError(GsoRep["IDS_CREATENOLIST"], AsoRep["ErrorPriorList"]);
            }
            else if (SelectFolders?.Count > 0)
            {
                if (ListModel.OBJID == null)
                    ListModel.OBJID = new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID };

                UpdateList request = new();
                request.Info = new SMSSGsoProto.V1.CListInfo() { Name = ListModel.Name, ListID = ListModel.OBJID.ObjID, ListStaffID = ListModel.OBJID.StaffID, ListSubsystemID = ListModel.OBJID.SubsystemID, Comm = ListModel.Comm, Priority = ListModel.Type };
                request.Lists = new();

                SelectFolders.ForEach(x =>
                {
                    x.Child.ForEach(c =>
                    {
                        c.ListID = new(x.Key.OBJID);
                    });
                });

                var l = SelectFolders.Where(x => (x.Child != null)).SelectMany(x => x.Child).Select(x => new CListItemInfo()
                {
                    ListID = ListId ?? 0,
                    ListStaffID = StaffId,
                    ListSubsystemID = SubsystemID,
                    AsoAbonID = x.AsoAbonID,
                    AsoAbonStaffID = x.AsoAbonStaffID,
                    SZSDevID = x.SZSDevID,
                    SZSDevStaffID = x.SZSDevStaffID
                }).ToList();

                if (l?.Any() ?? false)
                    request.Lists.AddRange(l);

                if (ListId != null)
                {
                    await UpdateList(request);
                }
                else
                {
                    await InsertList(request);
                }
                await GoCallBack();
            }
            else
            {
                MessageView?.AddError(GsoRep["IDS_CREATENOLIST"], GsoRep["IDS_STRING_ABS"] + " " + GsoRep["IDS_STRING_FOR_LIST_NOT_SELECTED"]);
            }

            IsProcessing = false;

        }

        private async Task GoCallBack()
        {
            if (CallbackEvent.HasDelegate)
                await CallbackEvent.InvokeAsync();
        }

        private async Task UpdateList(UpdateList request)
        {
            var d = OldListItem?.Where(x => !request.Lists?.Any(a => a.AsoAbonID == x.AsoAbonID && a.AsoAbonStaffID == x.AsoAbonStaffID && a.SZSDevID == x.SZSDevID && a.SZSDevStaffID == x.SZSDevStaffID) ?? true).ToList();

            if (d?.Any() ?? false)
            {
                await DeleteListItem(d);
            }
            await Http.PostAsJsonAsync("api/v1/EditList", request);
        }

        private async Task DeleteListItem(List<CListItemInfo> obj)
        {
            await Http.PostAsJsonAsync("api/v1/DeleteListItem", obj);
        }

        private async Task InsertList(UpdateList request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/AddList", request);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError(GsoRep["IDS_CREATENOLIST"], AsoRep["IDS_ESAVECHANGES"]);
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

        private async Task GetListDepFolder()
        {
            var result = await Http.PostAsJsonAsync("api/v1/IDepartment_Aso_GetObjects", new IntID() { ID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                var re = await result.Content.ReadFromJsonAsync<List<Objects>>();
                if (re != null)
                {
                    if (Folders == null)
                        Folders = new();
                    Folders.AddRange(re.Select(x => new Objects(x) { Type = ListType.DEPARTMENT }));
                }
            }
        }

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
                    Folders.AddRange(re.Select(x => new Objects() { Name = x, OBJID = new OBJ_ID() { ObjID = re.IndexOf(x), StaffID = StaffId, SubsystemID = SubsystemID }, Type = ListType.MAN }));

                }
            }
        }


        private string GetTextTitle
        {
            get
            {
                if (ListId == null)
                {
                    return SubsystemID switch
                    {
                        SubsystemType.SUBSYST_SZS => GsoRep["IDS_STRING_CREATE_DEVICE_LIST"],
                        _ => GsoRep["IDS_STRING_CREATE_AB_LIST"]
                    };
                }
                else
                {
                    return ListModel.Name;
                }
            }
        }


        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
