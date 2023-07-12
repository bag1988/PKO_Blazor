using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using AsoDataProto.V1;
using BlazorLibrary;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FiltersGSOProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using Label.V1;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.VisualBasic.FileIO;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.ASO.Abonent
{
    partial class ViewAbonent : IAsyncDisposable, IPubSubMethod
    {
        private int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<Tuple<bool, AbonentItem>>? SelectedList = null;

        private CountResponse? CountAb;

        private int AllCount = 0;

        private bool? IsViewEdit = false;

        private bool? IsEditList = false;

        private bool? IsDelete = false;
        private bool? OnDelete = false;

        private bool IsViewImport = false;

        private bool IsProcessingImport = false;
        private bool IsProcessingExport = false;

        readonly List<string> Process = new();

        private bool ChangeImportLoc = false;
        private bool ChangeImportInfo = false;
        private TimeSpan TimerCancel = TimeSpan.Zero;
        private SetNewInfoGlobal _setNewInfoGlobal = SetNewInfoGlobal.none;
        private SetNewInfoGlobal _setNewLocGlobal = SetNewInfoGlobal.none;
        private int? LocIdImport = null;
        private int? LocIdImportGlobal = null;
        private string? ImportName = "";
        private string? ImportPhone = "";
        private string? ImportLoc = "";

        enum SetNewInfoGlobal
        {
            none = 0,
            skip = 1,
            replace = 2,
            replaceGlobal = 3,
            skipGlobal = 4,
            abort
        }

        enum ExportType
        {
            XML = 1,
            CSV,
            XLSX
        }

        private int CountImport = 0;

        private IEnumerable<AsoDataProto.V1.Department> DepartmentList = new List<AsoDataProto.V1.Department>();

        private IEnumerable<Objects> LocationList = new List<Objects>();

        private List<string>? ListSit;

        List<LabelNameValueField> labelsList = new();

        readonly Dictionary<int, List<Restrict>> RestrictList = new();

        List<IntAndString> LineTypes = new();

        bool IsViewLabels = false;

        bool ChangeViewLabels
        {
            get
            {
                return IsViewLabels;
            }
            set
            {

                if (!value)
                {
                    if (ThList.ContainsKey(-15))
                    {
                        ThList.Remove(-15);
                    }
                }
                else
                {
                    if (!ThList.ContainsKey(-15))
                    {
                        ThList.Add(-15, DeviceRep["SPECIFICATIONS"]);
                    }
                }
                IsViewLabels = value;
            }
        }

        List<ASOAbonent>? importListAbon = null;

        ASOAbonent? replaceItem = null;

        TableVirtualize<Tuple<bool, AbonentItem>>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;
            request.BFlagDirection = 0;
            ThList = new Dictionary<int, string>
            {
                { 0, AsoRep["IDS_ABNAME"] },//ФИО
                { 1, AsoRep["IDS_ABDEPARTMENT"] },//Принадлежность
                { 2, AsoRep["IDS_ABPOSITION"] },//Должность
                { 3, AsoRep["IDS_ABPRIORITY"] },//Приоритет
                { 4, AsoRep["IDS_ABSTATUS"] },//Состояние
                { 10, AsoRep["IDS_ABROLE"] },//Категория
                { 5, AsoRep["IDS_CONNTYPE"] },//Тип связи
                { 6, AsoRep["IDS_ABLOCATION"] },//Местоположение
                { 7, AsoRep["IDS_ABPHONE"] },//Телефон
                { 8, AsoRep["IDS_ABADDRESS"] },//Адрес
                { 9, AsoRep["IDS_ABCONFIRMATION"] }//Код подтверждения
            };

            await GetDepartmentList();
            await GetLocationInfo();
            await GetAbCount();

            HintItems.Add(new HintItem(nameof(FiltrModel.Fio), AsoRep["IDS_ABNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Department), AsoRep["IDS_ABDEPARTMENT"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpDepName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Position), AsoRep["IDS_ABPOSITION"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Prior), AsoRep["IDS_ABPRIORITY"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.State), AsoRep["IDS_ABSTATUS"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpStatusName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Role), AsoRep["IDS_ABROLE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpRoleName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.ConnType), AsoRep["IDS_CONNTYPE"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpConnType)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Location), AsoRep["IDS_ABLOCATION"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpLocationName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Phone), AsoRep["IDS_ABPHONE"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Address), AsoRep["IDS_ABADDRESS"], TypeHint.Input));

            HintItems.Add(new HintItem(nameof(FiltrModel.Confirm), AsoRep["IDS_ABCONFIRMATION"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpConfirm)));

            await GetLabelsForFiltr();

            await OnInitFiltr(RefreshTable, FiltrName.FiltrAbonent);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteDepartment(uint Value)
        {
            await GetDepartmentList();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateDepartment(uint Value)
        {
            await GetDepartmentList();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateLocation(ulong Value)
        {
            await GetLocationInfo();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteLocation(ulong Value)
        {
            await GetLocationInfo();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateAbonent(byte[] AbonentItemByte)
        {
            try
            {
                if (table != null)
                {
                    var newItem = AbonentItem.Parser.ParseFrom(AbonentItemByte);

                    SelectedList?.RemoveAll(x => x.Item2.ASOShedulID == newItem.ASOShedulID && x.Item2.IDAb == newItem.IDAb);

                    if (newItem.ASOShedulID > 0 && newItem.SStaffID == 0)//удаляем расписание
                    {
                        if (table.AnyItemMatch(x => x.Item2.ASOShedulID == newItem.ASOShedulID))
                        {
                            var item = table.FindItemMatch(x => x.Item2.ASOShedulID == newItem.ASOShedulID);
                            if (item != null)
                            {
                                if (table.CountItemMatch(x => x.Item2.IDAb == newItem.IDAb) == 1)
                                {
                                    item.Item2.ASOShedulID = 0;
                                    item.Item2.ConnType = 0;
                                    item.Item2.ConnParam = "";
                                    item.Item2.LocationID = 0;
                                    item.Item2.Beeper = 0;
                                    item.Item2.Address = "";
                                }
                                else
                                {
                                    await table.RemoveItem(item);
                                    if (item.Item1)
                                    {
                                        var nextItem = table.FindItemMatch(x => x.Item2.IDAb == newItem.IDAb && !x.Item1);
                                        if (nextItem != null)
                                        {
                                            var insertItem = new Tuple<bool, AbonentItem>(true, nextItem.Item2);
                                            var index = table.GetIndexItem(nextItem);
                                            await table.RemoveItem(nextItem);
                                            await table.InsertItem(index, insertItem);
                                        }
                                    }
                                    AllCount -= 1;
                                }

                            }
                        }


                    }
                    else if (table.AnyItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.SStaffID == newItem.SStaffID && x.Item2.ASOShedulID == newItem.ASOShedulID))//обновляем расписание
                    {
                        var indexAb = table.FindItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.SStaffID == newItem.SStaffID && x.Item2.ASOShedulID == newItem.ASOShedulID);
                        if (indexAb != null)
                        {
                            indexAb.Item2.Address = newItem.Address;
                            indexAb.Item2.Beeper = newItem.Beeper;
                            indexAb.Item2.ConnParam = newItem.ConnParam;
                            indexAb.Item2.ConnType = newItem.ConnType;
                            indexAb.Item2.LocationID = newItem.LocationID;
                            indexAb.Item2.LocationStaffID = newItem.LocationStaffID;
                        }
                    }
                    else if (table.AnyItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.StaffID == newItem.StaffID && newItem.ASOShedulID > 0))//добавляем расписание
                    {
                        var indexAb = table.FindItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.StaffID == newItem.StaffID);

                        if (indexAb != null)
                        {
                            if (indexAb.Item2.ASOShedulID == 0)
                            {
                                indexAb.Item2.Address = newItem.Address;
                                indexAb.Item2.Beeper = newItem.Beeper;
                                indexAb.Item2.ConnParam = newItem.ConnParam;
                                indexAb.Item2.ConnType = newItem.ConnType;
                                indexAb.Item2.LocationID = newItem.LocationID;
                                indexAb.Item2.LocationStaffID = newItem.LocationStaffID;
                                indexAb.Item2.ASOShedulID = newItem.ASOShedulID;
                                indexAb.Item2.SStaffID = newItem.SStaffID;
                            }
                            else
                            {
                                var newAb = new AbonentItem(indexAb.Item2)
                                {
                                    Address = newItem.Address,
                                    Beeper = newItem.Beeper,
                                    ConnParam = newItem.ConnParam,
                                    ConnType = newItem.ConnType,
                                    LocationID = newItem.LocationID,
                                    LocationStaffID = newItem.LocationStaffID,
                                    ASOShedulID = newItem.ASOShedulID,
                                    SStaffID = newItem.SStaffID
                                };

                                int indexElem = table.GetIndexItem(indexAb);
                                if (indexElem == -1)
                                {
                                    await table.AddItem(new Tuple<bool, AbonentItem>(false, newAb));
                                }
                                else
                                {
                                    await table.InsertItem(indexElem + 1, new Tuple<bool, AbonentItem>(false, newAb));
                                }
                                AllCount += 1;
                            }
                        }
                    }
                    else if (table.AnyItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.StaffID == newItem.StaffID && !string.IsNullOrEmpty(newItem.AbName)))//обновляем абонента
                    {
                        await table.ForEachItems(x =>
                        {
                            if (x.Item2.IDAb == newItem.IDAb && x.Item2.StaffID == newItem.StaffID)
                            {
                                x.Item2.IDDep = newItem.IDDep;
                                x.Item2.DepStaffID = newItem.DepStaffID;
                                x.Item2.AbStatus = newItem.AbStatus;
                                x.Item2.AbName = newItem.AbName;
                                x.Item2.AbPrior = newItem.AbPrior;
                                x.Item2.Position = newItem.Position;
                                x.Item2.Role = newItem.Role;
                                x.Item2.LabelNameValueFieldList = newItem.LabelNameValueFieldList;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteAbonent(byte[] AbonentItemByte)
        {
            try
            {
                if (table != null)
                {
                    var newItem = AbonentItem.Parser.ParseFrom(AbonentItemByte);

                    SelectedList?.RemoveAll(x => x.Item2.IDAb == newItem.IDAb);

                    if (string.IsNullOrEmpty(newItem.AbName))//удаляем абонента
                    {
                        var countRemove = await table.RemoveAllItem(x => x.Item2.IDAb == newItem.IDAb);
                        if (CountAb != null)
                        {
                            CountAb.Count -= 1;
                        }
                        AllCount -= countRemove;
                    }
                    else if (newItem.IDAb > 0 && newItem.StaffID > 0 && !string.IsNullOrEmpty(newItem.AbName))//добавляем абонента
                    {
                        if (!table.AnyItemMatch(x => x.Item2.IDAb == newItem.IDAb && x.Item2.StaffID == newItem.StaffID))
                        {
                            await table.AddItem(new Tuple<bool, AbonentItem>(true, newItem));

                            if (CountAb == null)
                            {
                                CountAb = new() { Count = 1 };
                            }
                            else
                                CountAb.Count += 1;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateLabels(byte[] AbonentItemByte)
        {
            try
            {
                if (table != null)
                {
                    var newItem = LabelFieldAndOBJKey.Parser.ParseFrom(AbonentItemByte);

                    SelectedList?.RemoveAll(x => x.Item2.IDAb == newItem?.ObjKey?.ObjID?.ObjID && x.Item2.StaffID == newItem.ObjKey.ObjID.StaffID);

                    LabelNameValueFieldList newLabels = new();
                    newLabels.List.AddRange(newItem.FieldList?.List.Select(x => new LabelNameValueField() { NameField = x.NameField, ValueField = x.ValueField }));

                    await table.ForEachItems(x =>
                    {
                        if (x.Item2.IDAb == newItem?.ObjKey?.ObjID?.ObjID && x.Item2.StaffID == newItem.ObjKey.ObjID.StaffID)
                        {
                            x.Item2.LabelNameValueFieldList = newLabels;
                        }
                    });
                }

                await GetLabelsForFiltr();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private Task CallBackEvent(bool? UpdateList = false)
        {
            IsViewEdit = false;
            return Task.CompletedTask;
        }

        private Task CallBackEditList(bool? UpdateList = false)
        {
            IsEditList = false;
            SelectedList = null;
            return Task.CompletedTask;
        }

        ItemsProvider<Tuple<bool, AbonentItem>> GetProvider => new ItemsProvider<Tuple<bool, AbonentItem>>(ThList, LoadChildList, request, IsViewLabels ? null : new List<int>() { 18, 10, 10, 3, 6, 6, 7, 10, 10, 10, 10 });

        private async ValueTask<IEnumerable<Tuple<bool, AbonentItem>>> LoadChildList(GetItemRequest req)
        {
            List<Tuple<bool, AbonentItem>> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IAbonent_3", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                result.Headers.TryGetValues(MetaDataName.TotalCount, out IEnumerable<string>? AllCountList);

                if (AllCountList != null)
                {
                    int.TryParse(AllCountList.First(), out AllCount);
                    StateHasChanged();
                }

                var json = await result.Content.ReadAsStringAsync();

                var newData = JsonParser.Default.Parse<AbonentList>(json);
                if (newData != null)
                {
                    foreach (var item in newData.Array.GroupBy(x => x.IDAb))
                    {
                        foreach (var t in item)
                        {
                            response.Add(new Tuple<bool, AbonentItem>((item.Count() == 0 || t.Equals(item.First()) ? true : false), t));
                        }
                    }
                }
            }
            return response;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetAbonNameForAbonList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            return newData ?? new();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpDynamicName(GetItemRequest req)
        {
            List<Hint>? newData = new();

            string? nameFiled = labelsList.FirstOrDefault(x => x.IdNameField == req.LFrom)?.NameField;

            if (!string.IsNullOrEmpty(nameFiled))
            {

                var result = await Http.PostAsJsonAsync("api/v1/GetLabelValueForNameList", new ValueByNameAndEntity() { Name = nameFiled, Value = req.BstrFilter }, ComponentDetached);
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

        private ValueTask<IEnumerable<Hint>> LoadHelpDepName(GetItemRequest req)
        {
            List<Hint> newData = new(DepartmentList.Select(x => new Hint(x.Name, x.Dep.ObjID.ToString())));
            return new(newData);
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpStatusName(GetItemRequest req)
        {
            List<Hint> newData = new() { new Hint(SqlRep["101"], "1"), new Hint(SqlRep["102"], "2") };

            return new(newData);
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpRoleName(GetItemRequest req)
        {
            List<Hint> newData = new() { new Hint("VIP", "1"), new Hint(AsoRep["IDS_STRING_REGULAR"], "0") };
            return new(newData);
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpConnType(GetItemRequest req)
        {
            List<Hint> newData = new() {
                new Hint(SqlRep["207"], ((int)BaseLineType.LINE_TYPE_GSM_TERMINAL).ToString()),
                new Hint(SqlRep["202"], ((int)BaseLineType.LINE_TYPE_DEDICATED).ToString()),
                new Hint(SqlRep["210"], ((int)BaseLineType.LINE_TYPE_DCOM).ToString()),
                new Hint(SqlRep["203"], ((int)BaseLineType.LINE_TYPE_PAGE).ToString()),
                new Hint(SqlRep["201"], ((int)BaseLineType.LINE_TYPE_DIAL_UP).ToString()),
                new Hint(SqlRep["206"], ((int)BaseLineType.LINE_TYPE_SMTP).ToString())
            };
            return new(newData);
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpConfirm(GetItemRequest req)
        {
            List<Hint> newData = new() {
                new Hint(AsoRep["IDS_STRING_NO_CONFIRM"], "0"),
                new Hint(AsoRep["IDS_STRING_TICKER_CONFIRM"], "1"),
                new Hint(AsoRep["IDS_STRING_PASSWORD_CONFIRM"], "2"),
                new Hint(AsoRep["IDS_STRING_WITHOUT_CONFIRM"], "3"),
            };
            return new(newData);
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpLocationName(GetItemRequest req)
        {
            List<Hint> newData = new(LocationList.Select(x => new Hint(x.Name, x.OBJID.ObjID.ToString())));
            return new(newData);
        }

        private async Task GetLabelsForFiltr()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLabelAllFiedlForAbonent", new OBJ_Key());
            if (result.IsSuccessStatusCode)
            {
                labelsList = await result.Content.ReadFromJsonAsync<List<LabelNameValueField>>() ?? new();

                if (labelsList.Count > 0)
                {
                    foreach (var item in labelsList)
                    {
                        if (!HintItems.Any(x => x.Key == item.NameField))
                        {
                            HintItems.Add(
                                new HintItem(
                                    item.NameField,
                                    "* " + item.NameField,
                                    TypeHint.Select,
                                    null,
                                    FiltrOperationType.None,
                                    new VirtualizeProvider<Hint>(
                                        new GetItemRequest()
                                        {
                                            LFrom = item.IdNameField,
                                            CountData = 20
                                        },
                                        LoadHelpDynamicName)));
                        }
                    }
                }
            }
        }

        string GetConfirmName(int id)
        {
            return id switch
            {               
                1 => AsoRep["IDS_STRING_TICKER_CONFIRM"],
                2 => AsoRep["IDS_STRING_PASSWORD_CONFIRM"],
                _ => AsoRep["IDS_STRING_WITHOUT_CONFIRM"]
            };
        }

        private void SetSelectList(List<Tuple<bool, AbonentItem>>? newItems = null)
        {
            SelectedList = newItems;
        }

        private async Task GetDepartmentList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetDepartmentList", new IntID() { ID = request.ObjID.StaffID });
            if (result.IsSuccessStatusCode)
            {
                DepartmentList = await result.Content.ReadFromJsonAsync<IEnumerable<AsoDataProto.V1.Department>>() ?? new List<AsoDataProto.V1.Department>();
            }
        }

        private async Task GetAbCount()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetAbCount", new IntID() { ID = request.ObjID.StaffID });
            if (result.IsSuccessStatusCode)
            {
                CountAb = await result.Content.ReadFromJsonAsync<CountResponse>();
            }
        }

        private async Task GetLocationInfo()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = request.ObjID.StaffID });
            if (result.IsSuccessStatusCode)
            {
                LocationList = await result.Content.ReadFromJsonAsync<IEnumerable<Objects>>() ?? new List<Objects>();
            }
        }

        private void ViewDelete()
        {
            if (SelectedList?.Count > 0)
                IsDelete = true;
        }

        private void CancelDelete()
        {
            OnDelete = false;
            ListSit = null;
            IsDelete = false;
        }

        private async Task DeleteAbonent()
        {
            try
            {
                if (SelectedList?.Count > 0)
                {
                    OBJ_ID obj = new OBJ_ID() { SubsystemID = SubsystemID };

                    var deleteList = SelectedList.Select(x => new AbonentItem(x.Item2)).ToList();

                    foreach (var item in deleteList)
                    {
                        if (ComponentDetached.IsCancellationRequested)
                            throw new OperationCanceledException();
                        obj.ObjID = item.IDAb;
                        obj.StaffID = item.StaffID;
                        if (OnDelete == false && deleteList.Count == 1)
                        {
                            var result = await Http.PostAsJsonAsync("api/v1/IAbonent_Aso_GetLinkObjects", obj);
                            if (result.IsSuccessStatusCode)
                            {
                                ListSit = await result.Content.ReadFromJsonAsync<List<string>>();
                            }
                            OnDelete = true;
                            if (ListSit != null && ListSit.Count > 0)
                            {
                                return;
                            }
                        }

                        if (OnDelete == true || (OnDelete == false && deleteList.Count > 1))
                        {
                            ListSit = null;
                            OnDelete = false;

                            var result = await Http.PostAsJsonAsync("api/v1/DeleteAbonent", obj);
                            if (!result.IsSuccessStatusCode)
                            {
                                MessageView?.AddError(AsoRep["IDS_REG_AB_DELETE"], item.AbName + " " + AsoRep["IDS_EFAIL_DELABONENT"]);
                            }
                            else
                            {
                                MessageView?.AddMessage(AsoRep["IDS_REG_AB_DELETE"], item.AbName + "-" + AsoRep["IDS_OK_DELETE"]);
                            }

                        }
                    }
                    SelectedList = null;
                    IsDelete = false;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void EditAbon()
        {
            if (SelectedList?.Count == 1)
            {
                IsViewEdit = true;
            }
        }

        private async Task ExportAbonList(ExportType exportType)
        {
            IsProcessingExport = true;
            if (SelectedList?.Count > 0)
            {
                string url = exportType switch
                {
                    ExportType.XLSX => "CreateXLSXAbon",
                    ExportType.CSV => "CreateCsvAbon",
                    _ => "CreateXmlAbon"
                };

                var result = await Http.PostAsJsonAsync($"api/v1/{url}", SelectedList.Select(x => new OBJ_ID() { ObjID = x.Item2.IDAb, StaffID = x.Item2.StaffID, SubsystemID = SubsystemID }).Distinct().ToList(), ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var r = Convert.FromBase64String(await result.Content.ReadAsStringAsync());
                    if (r != null)
                    {
                        using var streamRef = new DotNetStreamReference(stream: new MemoryStream(r));

                        string exe = exportType switch
                        {
                            ExportType.XLSX => "xlsx",
                            ExportType.CSV => "csv",
                            _ => "xml"
                        };

                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", $"Список абонентов АСО.{exe}", streamRef);

                        streamRef.Dispose();
                    }
                }
            }
            IsProcessingExport = false;
        }

        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            try
            {
                importListAbon = null;
                Process.Clear();
                Process.Add(AsoRep["SearchRecords"]);

                var file = e.File;
                var contentType = file.ContentType.ToLower();//text/xml

                if (contentType == "text/xml")
                    await LoadXml(file);
                else if (contentType == "text/csv")
                    await LoadCSV(file);
                else if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    await LoadXLSX(file);
                else
                {
                    Process.Add(GsoRep["ERROR_READ_FILE"]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async Task LoadXLSX(IBrowserFile file)
        {
            try
            {
                Process.Clear();
                Process.Add(AsoRep["SearchRecords"]);
                using var msXLSX = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXLSX);
                msXLSX.Position = 0;

                using var spreadsheetDocument = SpreadsheetDocument.Open(msXLSX, false);

                var workbookPart = spreadsheetDocument.WorkbookPart;

                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();

                Dictionary<string, string> headers = new();

                if (worksheetPart != null && workbookPart != null)
                {
                    using var reader = OpenXmlReader.Create(worksheetPart);

                    int rowIndex = 1;

                    while (reader.Read())
                    {
                        ASOAbonent newItem = new();

                        if (reader.ElementType == typeof(Row))
                        {
                            try
                            {
                                var row = reader.LoadCurrentElement() as Row;

                                if (row != null)
                                {
                                    var cells = row.Elements<Cell>().ToList();

                                    if (headers.Count == 0)
                                    {
                                        headers = cells.ToDictionary(x => GetValueCell(x, workbookPart), x => GetColumnName(x.CellReference?.Value ?? "") ?? "");
                                    }
                                    else if (headers.Count > 0)
                                    {
                                        newItem.Name = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Name))}{rowIndex}"), workbookPart);
                                        newItem.Comment = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Comment))}{rowIndex}"), workbookPart);
                                        newItem.Prior = Convert.ToInt32(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Prior))}{rowIndex}"), workbookPart));
                                        newItem.Dep = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Dep))}{rowIndex}"), workbookPart);
                                        newItem.Loc = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Loc))}{rowIndex}"), workbookPart);
                                        newItem.Pos = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Pos))}{rowIndex}"), workbookPart);
                                        newItem.Role = GetRoleImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Role))}{rowIndex}"), workbookPart));
                                        newItem.Stat = GetStateImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Stat))}{rowIndex}"), workbookPart));
                                        newItem.Passw = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Passw))}{rowIndex}"), workbookPart);

                                        newItem.Phone = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Phone))}{rowIndex}"), workbookPart);
                                        newItem.Confirm = GetConfirmImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Confirm))}{rowIndex}"), workbookPart));
                                        newItem.BaseType = await GetBaseTypeImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.BaseType))}{rowIndex}"), workbookPart));

                                        newItem.GlobType = await GetGlobalTypeImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.GlobType))}{rowIndex}"), workbookPart), newItem.BaseType);
                                        newItem.UserType = await GetGlobalTypeImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.UserType))}{rowIndex}"), workbookPart), newItem.BaseType);
                                        newItem.Addr = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.Addr))}{rowIndex}"), workbookPart);

                                        newItem.BTime = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.BTime))}{rowIndex}"), workbookPart);
                                        newItem.ETime = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.ETime))}{rowIndex}"), workbookPart);
                                        newItem.DayType = GetDayTypeImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.DayType))}{rowIndex}"), workbookPart));

                                        newItem.WeekDay = GetWeekDayImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.WeekDay))}{rowIndex}"), workbookPart));

                                        newItem.ConnType = GetConnTypeImport(GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{GetCellName(headers, nameof(ASOAbonent.ConnType))}{rowIndex}"), workbookPart));

                                        if (headers.Count > 20)
                                        {
                                            newItem.Labels = new();
                                            foreach (var h in headers.Skip(20))
                                            {
                                                var valueLabel = GetValueCell(cells.FirstOrDefault(x => x.CellReference == $"{h.Value}{rowIndex}"), workbookPart);
                                                if (!string.IsNullOrEmpty(valueLabel))
                                                    newItem.Labels.Add(new Labels() { Key = h.Key, Value = valueLabel });
                                            }
                                        }
                                        if (importListAbon == null)
                                        {
                                            importListAbon = new();
                                        }
                                        if (!string.IsNullOrEmpty(newItem.Name) && !string.IsNullOrEmpty(newItem.Phone))
                                            importListAbon.Add(newItem);
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                Process.Add($"{GsoRep["STR_NUMBER"]}: {rowIndex}. {GsoRep["ERROR_PARSE_ABON"]}: {AsoRep["IDS_ABONENT"]} {(string.IsNullOrEmpty(newItem.Name) ? GsoRep["ERROR"] : newItem.Name)}, {AsoRep["IDS_ABPHONE"]} {(string.IsNullOrEmpty(newItem.Phone) ? GsoRep["ERROR"] : newItem.Phone)}");
                            }
                            rowIndex++;
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (importListAbon != null)
            {
                Process.Add(AsoRep["FindRecords"] + ". " + AsoDataRep["IDS_STRING_ABONENT_COMMENT"] + ". " + AsoRep["IDS_STRING_COUNT"] + ": " + importListAbon.Count);
            }
            else
                Process.Add(AsoRep["FindRecords"] + " " + 0);
        }

        string GetWeekDayImport(string value)
        {
            if (Regex.IsMatch(value, @"^\d{7}$"))
                return value;
            else
            {
                var valueArray = value.Split('&');

                char[] dayArray = "0000000".ToCharArray();

                foreach (var item in valueArray)
                {
                    if (item == AsoRep[Days.Mo.ToString()])
                        dayArray[1] = '1';
                    else if (item == AsoRep[Days.Tu.ToString()])
                        dayArray[2] = '1';
                    else if (item == AsoRep[Days.We.ToString()])
                        dayArray[3] = '1';
                    else if (item == AsoRep[Days.Th.ToString()])
                        dayArray[4] = '1';
                    else if (item == AsoRep[Days.Fr.ToString()])
                        dayArray[5] = '1';
                    else if (item == AsoRep[Days.Sa.ToString()])
                        dayArray[6] = '1';
                    else if (item == AsoRep[Days.Su.ToString()])
                        dayArray[0] = '1';
                }
                return string.Join("", dayArray);
            }
        }

        int GetDayTypeImport(string value)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                if (value == AsoRep["ALLWAYS"])
                    return 0;
                else if (value == AsoRep["WORKDAY"])
                    return 1;
                else if (value == AsoRep["WEEKEND"])
                    return 2;
                else if (value == AsoRep["HOLYDAY"])
                    return 3;
                else if (value == AsoRep["SELECTED"])
                    return 4;
                else
                    throw new ArgumentException();
            }
        }

        int GetRoleImport(string value)
        {
            if (int.TryParse(value, out int role))
                return role;
            else
            {
                return value == "VIP" ? 1 : 0;
            }
        }

        int GetStateImport(string value)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                return value == SqlRep["101"] ? 1 : 2;
            }
        }
        int GetConfirmImport(string value)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                return value == AsoRep["IDS_STRING_TICKER_CONFIRM"] ? 1 : value == AsoRep["IDS_STRING_PASSWORD_CONFIRM"] ? 2 : value == AsoRep["IDS_STRING_WITHOUT_CONFIRM"] ? 3 : 0;
            }
        }

        int GetConnTypeImport(string value)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                if (value == SqlRep["202"])
                    return 2;
                else if (value == SqlRep["210"])
                    return 10;
                else if (value == SqlRep["203"])
                    return 3;
                else if (value == SqlRep["201"])
                    return 1;
                else if (value == SqlRep["206"])
                    return 6;
                else
                    throw new ArgumentException();
            }
        }

        async Task<int> GetGlobalTypeImport(string value, int baseType)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                var valueArray = value.Split('&');

                await GetRestrictList(baseType);
                int result = 0;
                if (RestrictList.ContainsKey(baseType))
                {
                    foreach (var item in valueArray)
                    {
                        var resItem = RestrictList[baseType].FirstOrDefault(x => x.RestrictName == item);
                        if (resItem != null)
                        {
                            SetRestrictBitStatus(resItem, ref result);
                        }
                    }
                }
                return result;
            }
        }

        private void SetRestrictBitStatus(Restrict item, ref int result)
        {
            int BitNumber = item.BitNumber;

            int i = 0;
            i |= (1 << BitNumber);
            if (item.RestrictType == 0)
            {
                if (((result >> BitNumber) & 0x01) > 0)
                {
                    result -= i;
                }
                else
                {
                    result += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((result >> BitNumber) & 0x01) > 0)
                {
                    result -= i;
                }
                else
                {
                    result += i;
                }
            }


        }

        private async Task GetRestrictList(int baseType)
        {
            if (!RestrictList.ContainsKey(baseType))
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = baseType });
                List<Restrict> newData = new();
                if (result.IsSuccessStatusCode)
                {

                    newData = await result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
                }
                RestrictList.Add(baseType, newData.Where(x => x.RestrictType != 2).ToList());
            }
        }

        async Task<int> GetBaseTypeImport(string value)
        {
            if (int.TryParse(value, out int state))
                return state;
            else
            {
                await GetLineTypeList();
                int result = 0;
                if (LineTypes.Any(x => x.Str == value))
                {
                    result = LineTypes.First(x => x.Str == value).Number;

                    if (result > 0)
                        return result;
                }
                throw new ArgumentException();
            }
        }

        private async Task GetLineTypeList()
        {
            if (LineTypes.Count == 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemType.SUBSYST_ASO });
                if (result.IsSuccessStatusCode)
                {
                    LineTypes = await result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
                }
                if (LineTypes == null)
                    LineTypes = new();
            }
        }

        string GetValueCell(Cell? cell, WorkbookPart workbookPart)
        {
            string cellValue = string.Empty;
            if (cell != null && cell.DataType != null && cell.CellValue != null && cell.DataType == CellValues.SharedString && workbookPart.SharedStringTablePart != null)
            {
                SharedStringItem ssi = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(cell.CellValue.InnerText));

                cellValue = ssi.Text?.Text ?? "";
            }
            else
            {
                cellValue = cell?.CellValue?.InnerText ?? "";
            }

            return cellValue;
        }

        string GetCellName(Dictionary<string, string> headers, string keyName)
        {
            if (headers.ContainsKey(keyName))
                return headers[keyName];
            return string.Empty;

        }

        string? GetColumnName(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
            {
                return null;
            }
            return Regex.Replace(cellReference.ToUpper(), @"[\d]", string.Empty);
        }

        async Task LoadCSV(IBrowserFile file)
        {
            try
            {
                Process.Clear();
                Process.Add(AsoRep["SearchRecords"]);
                using var msXml = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);
                msXml.Position = 0;
                using (var parser = new TextFieldParser(msXml))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(";");
                    int line = 1;
                    while (!parser.EndOfData)
                    {
                        ASOAbonent newItem = new();
                        string[]? fields = parser.ReadFields();
                        if (fields?.Length >= 20)//length field
                        {
                            try
                            {
                                newItem.Name = fields[0];
                                newItem.Comment = fields[1];
                                newItem.Prior = Convert.ToInt32(fields[2]);
                                newItem.Dep = fields[3];
                                newItem.Loc = fields[4];
                                newItem.Pos = fields[5];
                                newItem.Role = Convert.ToInt32(fields[6]);
                                newItem.Stat = Convert.ToInt32(fields[7]);
                                newItem.Passw = fields[8];

                                newItem.Phone = fields[9];
                                newItem.Confirm = Convert.ToInt32(fields[10]);
                                newItem.GlobType = Convert.ToInt32(fields[11]);
                                newItem.UserType = Convert.ToInt32(fields[12]);
                                newItem.Addr = fields[13];

                                newItem.BTime = fields[14];
                                newItem.ETime = fields[15];
                                newItem.DayType = Convert.ToInt32(fields[16]);
                                newItem.WeekDay = string.IsNullOrEmpty(fields[17]) ? "0000000" : fields[17];

                                newItem.BaseType = Convert.ToInt32(fields[18]);
                                newItem.ConnType = Convert.ToInt32(fields[19]);

                                if (fields.Length > 20)
                                {
                                    var labels = fields[20].Split('&');
                                    if (labels?.Length > 0)
                                    {
                                        newItem.Labels = new();
                                        foreach (var label in labels)
                                        {
                                            if (label.Split('=').Length > 1)
                                                newItem.Labels.Add(new Labels() { Key = label.Split('=')[0], Value = label.Split("=")[1] });
                                        }
                                    }
                                }

                                if (importListAbon == null)
                                {
                                    importListAbon = new();
                                }
                                importListAbon.Add(newItem);
                                line++;
                            }
                            catch
                            {
                                Process.Add($"{GsoRep["STR_NUMBER"]}: {line}. {GsoRep["ERROR_PARSE_ABON"]}: {AsoRep["IDS_ABONENT"]} {(string.IsNullOrEmpty(newItem.Name) ? GsoRep["ERROR"] : newItem.Name)}, {AsoRep["IDS_ABPHONE"]} {(string.IsNullOrEmpty(newItem.Phone) ? GsoRep["ERROR"] : newItem.Phone)}");
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (importListAbon != null)
            {
                Process.Add(AsoRep["FindRecords"] + ". " + AsoDataRep["IDS_STRING_ABONENT_COMMENT"] + ". " + AsoRep["IDS_STRING_COUNT"] + ": " + importListAbon.Count);
            }
            else
                Process.Add(AsoRep["FindRecords"] + " " + 0);
        }

        async Task LoadXml(IBrowserFile file)
        {
            try
            {
                Process.Clear();
                Process.Add(AsoRep["SearchRecords"]);

                XmlSerializer formatter = new XmlSerializer(typeof(List<ASOAbonent>), new XmlRootAttribute { ElementName = "XYZ" });

                using var msXml = new MemoryStream();

                await file.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);

                using var sr = new StringReader(Encoding.Default.GetString(msXml.ToArray()));

                importListAbon = formatter.Deserialize(sr) as List<ASOAbonent>;

                await msXml.DisposeAsync();
                sr.Dispose();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (importListAbon != null)
            {
                Process.Add(AsoRep["FindRecords"] + ". " + AsoDataRep["IDS_STRING_ABONENT_COMMENT"] + ". " + AsoRep["IDS_STRING_COUNT"] + ": " + importListAbon.Count);
            }
            else
                Process.Add(AsoRep["FindRecords"] + " " + 0);
        }

        async Task WriteAbonToBase()
        {
            _setNewInfoGlobal = SetNewInfoGlobal.none;
            _setNewLocGlobal = SetNewInfoGlobal.none;
            IsProcessingImport = true;
            CountImport = 0;
            try
            {
                if (importListAbon != null)
                {
                    foreach (var abonInfo in importListAbon.GroupBy(x => x.Name))
                    {
                        if (ComponentDetached.IsCancellationRequested || !IsProcessingImport)
                        {
                            Process.Add(GsoRep["CANCELLATION"]);
                            break;
                        }

                        ASOAbonent item = abonInfo.First();
                        AbonInfo ImportAbon = new();
                        OBJ_ID AbonId = new();

                        ImportAbon.AbName = item.Name;
                        ImportAbon.Password = item.Passw ?? string.Empty;
                        ImportAbon.Position = item.Pos ?? string.Empty;
                        ImportAbon.AbComm = item.Comment ?? string.Empty;
                        ImportAbon.AbPrior = item.Prior;
                        ImportAbon.AbStatus = item.Stat;
                        ImportAbon.Role = item.Role;

                        ImportName = item.Name;

                        OBJ_ID? DepId = await CheckDep(item);
                        ImportAbon.Dep = DepId;

                        List<Shedule> shedules = new List<Shedule>();

                        foreach (var newShedule in abonInfo)
                        {
                            if (ComponentDetached.IsCancellationRequested || !IsProcessingImport)
                            {
                                Process.Add(GsoRep["CANCELLATION"]);
                                break;
                            }
                            StateHasChanged();
                            await Task.Delay(10);

                            var locId = await CheckLoc(newShedule);

                            if (locId == null)
                            {
                                Process.Add(newShedule.Name + " " + newShedule.Loc + " " + AsoRep["IDS_E_CREATESHEDULEREC"]);
                                continue;
                            }

                            newShedule.Loc = LocationList.FirstOrDefault(x => x.OBJID.ObjID == LocIdImport)?.Name;

                            var oldAbonId = await CheckConnParam(newShedule, locId);

                            if (oldAbonId.AbonId > 0 && _setNewInfoGlobal != SetNewInfoGlobal.replaceGlobal)
                            {
                                if (_setNewInfoGlobal == SetNewInfoGlobal.skipGlobal)
                                {
                                    continue;
                                }
                                _setNewInfoGlobal = SetNewInfoGlobal.none;
                                await CheckInfoAbon(newShedule);
                                if (_setNewInfoGlobal == SetNewInfoGlobal.skip || _setNewInfoGlobal == SetNewInfoGlobal.skipGlobal || _setNewInfoGlobal == SetNewInfoGlobal.none)
                                {
                                    Process.Add(newShedule.Name + " " + item.Phone + " " + AsoRep["IDS_E__EXIST_CONNPARAM"]);
                                    continue;
                                }
                                else if (_setNewInfoGlobal == SetNewInfoGlobal.abort)
                                    break;
                            }

                            AbonId.ObjID = oldAbonId.AbonId;
                            AbonId.StaffID = request.ObjID.StaffID;
                            AbonId.SubsystemID = SubsystemID;

                            shedules.Add(new Shedule()
                            {
                                Abon = AbonId,
                                Address = newShedule.Addr ?? string.Empty,
                                ASOShedule = new OBJ_ID() { ObjID = oldAbonId.SheduleId },
                                BaseType = newShedule.BaseType,
                                Beeper = newShedule.Confirm,
                                Begtime = GetDurationImport(newShedule.BTime) ?? Duration.FromTimeSpan(TimeSpan.Parse("00:00:00")),
                                ConnParam = newShedule.Phone,
                                ConnType = newShedule.ConnType,
                                DayType = newShedule.DayType,
                                DayWeek = newShedule.WeekDay ?? string.Empty,
                                Endtime = GetDurationImport(newShedule.ETime) ?? Duration.FromTimeSpan(TimeSpan.Parse("23:59:59")),
                                GlobalType = newShedule.GlobType,
                                Loc = locId,
                                UserType = newShedule.UserType,
                                TimeType = ((newShedule.BTime != "00:00:00" || newShedule.ETime != "23:59:59") ? 1 : 0)
                            });
                        }

                        if (ComponentDetached.IsCancellationRequested || !IsProcessingImport)
                        {
                            Process.Add(GsoRep["CANCELLATION"]);
                            break;
                        }

                        ImportAbon.Abon = AbonId;
                        if (!shedules.Any())
                        {
                            Process.Add(item.Name + " " + AsoRep["IDS_E_ABONENT"]);
                            continue;
                        }
                        else
                        {
                            var result = await Http.PostAsJsonAsync("api/v1/SetAbInfo", ImportAbon, ComponentDetached);

                            if (result.IsSuccessStatusCode)
                            {
                                var id = await result.Content.ReadFromJsonAsync<IntID>();
                                if (id != null && id.ID != 0)
                                {
                                    shedules.ForEach(x => x.Abon.ObjID = id.ID);
                                    await SaveShulde(shedules);

                                    var labelsField = abonInfo.First().Labels;
                                    LabelFieldAndOBJKey requestLabels = new();
                                    requestLabels.ObjKey = new() { ObjID = new() { ObjID = id.ID, StaffID = request.ObjID.StaffID } };
                                    requestLabels.FieldList = new();
                                    if (labelsField?.Count > 0)
                                    {
                                        requestLabels.FieldList.List.AddRange(labelsField.Select(x => new Label.V1.Field()
                                        {
                                            NameField = x.Key,
                                            ValueField = x.Value
                                        }));
                                    }
                                    await SaveSpecifications(requestLabels);
                                    Process.Add(item.Name + " " + AsoRep["IDS_OK_SAVE"]);
                                }
                                else
                                {
                                    Process.Add(item.Name + " " + AsoRep["IDS_E_FAILCREATEABONENT"]);
                                }
                                CountImport += shedules.Count;
                            }
                            else
                            {
                                Process.Add(item.Name + " " + AsoRep["IDS_E_CREATEABONENT"]);
                            }
                        }
                    }
                    LocIdImport = null;
                    LocIdImportGlobal = null;
                    SelectedList = null;
                    await Task.Delay(10);
                    Process.Add("-----");
                    Process.Add(AsoRep["ImportResult"].ToString().Replace("{all}", CountImport.ToString()).Replace("{skipped}", (importListAbon.Count - CountImport).ToString()));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            importListAbon = null;
            IsProcessingImport = false;
        }


        Duration? GetDurationImport(string? value)
        {
            if (TimeSpan.TryParse(value, out var duration))
            {
                return Duration.FromTimeSpan(duration);
            }
            else if (double.TryParse(value?.Replace(".", ","), out double seconds))
            {
                return Duration.FromTimeSpan(TimeSpan.FromSeconds(seconds));
            }
            else
                return null;
        }

        private async Task SaveSpecifications(LabelFieldAndOBJKey requestItem)
        {
            try
            {
                string json = JsonFormatter.Default.Format(requestItem);

                await Http.PostAsJsonAsync("api/v1/UpdateLabelFieldAsoAbonent", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task LoadFilesV2(InputFileChangeEventArgs e)
        {
            try
            {
                MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["SearchRecords"]);

                var file = e.File;
                var ex = file.ContentType;//text/xml

                if (file.ContentType.ToLower() != "text/xml")
                    return;

                var formatter = new XmlSerializer(typeof(List<ASOAbonent>), new XmlRootAttribute { ElementName = "XYZ" });

                using var msXml = new MemoryStream();

                await file.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);

                msXml.Position = 0;

                XmlReader xmlReader = XmlReader.Create(msXml);

                if (!formatter.CanDeserialize(xmlReader))
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_IMPORT_AB"], GsoRep["ERROR_READ_FILE"]);
                    return;
                }
                List<ASOAbonent>? list = formatter.Deserialize(xmlReader) as List<ASOAbonent>;

                await msXml.DisposeAsync();

                AbonInfoImportList importList = new();

                if (list != null)
                {
                    MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["FindRecords"] + ". " + AsoDataRep["IDS_STRING_ABONENT_COMMENT"] + ". " + AsoRep["IDS_STRING_COUNT"] + ": " + list.GroupBy(x => x.Name).Count());

                    OBJ_ID AbonId = new()
                    {
                        StaffID = request.ObjID.StaffID,
                        SubsystemID = SubsystemID
                    };
                    foreach (var item in list)
                    {

                        if (IsViewImport == false || ComponentDetached.IsCancellationRequested)
                        {
                            MessageView?.AddError(AsoRep["IDS_STRING_IMPORT_AB"], GsoRep["CANCELLATION"]);
                            return;
                        }
                        AbonInfoImport ImportAbon = new();
                        ImportAbon.Abon = AbonId;
                        ImportAbon.AbName = item.Name;
                        ImportAbon.Password = item.Passw;
                        ImportAbon.Position = item.Pos;
                        ImportAbon.AbComm = item.Comment;
                        ImportAbon.AbPrior = item.Prior;
                        ImportAbon.AbStatus = item.Stat;
                        ImportAbon.Role = item.Role;

                        ImportName = item.Name;
                        var locId = await CheckLoc(item);

                        if (locId == null)
                        {
                            MessageView?.AddError(AsoRep["IDS_STRING_IMPORT_AB"], item.Name + " " + item.Loc + " " + AsoRep["IDS_E_CREATESHEDULEREC"]);
                            continue;
                        }
                        ImportAbon.SheduleInfo = new Shedule()
                        {
                            Address = item.Addr,
                            ASOShedule = new OBJ_ID(),
                            BaseType = item.BaseType,
                            Beeper = item.Confirm,
                            Begtime = Duration.FromTimeSpan(TimeSpan.Parse(item.BTime ?? "00:00:00")),
                            ConnParam = item.Phone,
                            ConnType = item.ConnType,
                            DayType = item.DayType,
                            DayWeek = item.WeekDay,
                            Endtime = Duration.FromTimeSpan(TimeSpan.Parse(item.ETime ?? "23:59:59")),
                            GlobalType = item.GlobType,
                            Loc = locId,
                            UserType = item.UserType,
                        };

                        if (item.Labels?.Count > 0)
                        {
                            ImportAbon.Labels = new();
                            ImportAbon.Labels.List.AddRange(item.Labels.Select(x => new LabelNameValueField() { NameField = x.Key, ValueField = x.Value }));
                        }

                        importList.Array.Add(ImportAbon);
                    }
                    MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], GsoRep["SAVING"]);

                    var result = await Http.PostAsJsonAsync("api/v1/ImportAbonent", JsonFormatter.Default.Format(importList), ComponentDetached);

                    AbonInfoImportList noImportList = new();

                    if (result.IsSuccessStatusCode)
                    {
                        var json = await result.Content.ReadAsStringAsync();
                        noImportList = JsonParser.Default.Parse<AbonInfoImportList>(json);
                    }

                    MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["IDS_OK_SAVE"]);


                    LocIdImport = null;
                    LocIdImportGlobal = null;
                    SelectedList = null;

                    MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["ImportResult"].ToString().Replace("{all}", (importList.Array.Count - noImportList.Array.Count).ToString()).Replace("{skipped}", (list.Count - importList.Array.Count + noImportList.Array.Count).ToString()));
                }
                else
                    MessageView?.AddMessage(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["FindRecords"] + " " + 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<ExistConnParam> CheckConnParam(ASOAbonent item, OBJ_ID locId)
        {
            CIsExistConnParam request = new CIsExistConnParam();
            request.ConnParam = item.Phone;
            request.ConnType = item.ConnType;
            request.Loc = new OBJ_ID(locId) { SubsystemID = 0 };

            var result = await Http.PostAsJsonAsync("api/v1/IsExistConnParam", request);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<ExistConnParam>() ?? new();
                return response;
            }
            return new();
        }

        private async Task SaveShulde(List<Shedule> shedules)
        {
            if (shedules != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/SetSheduleInfo", shedules, ComponentDetached);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["IDS_E_CREATESHEDULEREC"]);
                }
            }
        }

        private async Task<OBJ_ID?> CheckDep(ASOAbonent XmlAbon)
        {
            OBJ_ID? DepId = null;
            // Проверка подразделения
            if (string.IsNullOrEmpty(XmlAbon.Dep))
            {
                DepId = new OBJ_ID();
            }
            else if (DepartmentList.Any(x => x.Name.Equals(XmlAbon.Dep)))
            {
                DepId = DepartmentList.First(x => x.Name.Equals(XmlAbon.Dep)).Dep;
            }
            else
            {
                // Создать подразделение
                var result = await Http.PostAsJsonAsync("api/v1/SetDepartmentInfo", new DepartmentAso() { StaffID = request.ObjID.StaffID, DepName = XmlAbon.Dep });
                if (result.IsSuccessStatusCode)
                {
                    await GetDepartmentList();
                    DepId = DepartmentList.FirstOrDefault(x => x.Name.Equals(XmlAbon.Dep))?.Dep;
                }
            }

            return DepId;
        }

        private async Task<OBJ_ID?> CheckLoc(ASOAbonent XmlAbon)
        {
            // Проверка местоположения
            if (string.IsNullOrEmpty(XmlAbon.Loc) || !LocationList.Any(x => x.Name == XmlAbon.Loc))
            {
                if (_setNewLocGlobal == SetNewInfoGlobal.skipGlobal)
                {
                    return null;
                }
                else if (_setNewLocGlobal == SetNewInfoGlobal.replaceGlobal && LocIdImportGlobal > 0)
                {
                    LocIdImport = LocIdImportGlobal;
                }
                else //if (_setNewLocGlobal == SetNewInfoGlobal.replace || _setNewLocGlobal == SetNewInfoGlobal.none || _setNewLocGlobal == SetNewInfoGlobal.skip)
                {
                    LocIdImport = null;
                    ImportPhone = XmlAbon.Phone;
                    //TimerCancel = new TimeSpan(0, 0, 10);
                    ImportLoc = XmlAbon.Loc;
                    ChangeImportLoc = true;
                    StateHasChanged();
                    while (ChangeImportLoc)
                    {
                        await Task.Delay(100);
                        //TimerCancel = TimerCancel.Add(new TimeSpan(0, 0, 0, 0, -100));
                        //StateHasChanged();
                    }

                    if (_setNewLocGlobal == SetNewInfoGlobal.abort || LocIdImport == null)
                    {
                        return null;
                    }
                }
            }
            else
            {
                LocIdImport = LocationList.First(x => x.Name == XmlAbon.Loc).OBJID.ObjID;
            }
            return LocationList.FirstOrDefault(x => x.OBJID.ObjID == LocIdImport)?.OBJID;
        }

        private async Task CheckInfoAbon(ASOAbonent XmlAbon)
        {
            replaceItem = XmlAbon;
            //TimerCancel = new TimeSpan(0, 0, 10);
            ChangeImportInfo = true;
            StateHasChanged();
            while (ChangeImportInfo)
            {
                await Task.Delay(100);
                //TimerCancel = TimerCancel.Add(new TimeSpan(0, 0, 0, 0, -100));
                //StateHasChanged();
            }
        }

        private void ToAcceptInfo(SetNewInfoGlobal global)
        {
            ChangeImportInfo = false;
            if (global == SetNewInfoGlobal.abort)
            {
                IsProcessingImport = false;
                return;
            }
            _setNewInfoGlobal = global;

        }

        private void ToAccept(SetNewInfoGlobal global)
        {
            _setNewLocGlobal = global;
            if (global == SetNewInfoGlobal.abort)
            {
                IsProcessingImport = false;
                ChangeImportLoc = false;
                return;
            }
            else
            {
                LocIdImportGlobal = null;
                if (global == SetNewInfoGlobal.replace || global == SetNewInfoGlobal.replaceGlobal)
                {
                    if (LocIdImport != null)
                    {
                        if (global == SetNewInfoGlobal.replaceGlobal)
                            LocIdImportGlobal = LocIdImport;
                    }
                    else
                    {
                        MessageView?.AddError(AsoRep["IDS_STRING_IMPORT_AB"], AsoRep["IDS_EABLOC"]);
                        return;
                    }
                }
                else if (global == SetNewInfoGlobal.skip)
                {
                    LocIdImport = null;
                }
            }
            ChangeImportLoc = false;
        }

        private async Task RefreshTable()
        {
            SelectedList = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task GetReport()
        {
            OBJ_ID ReportId = new OBJ_ID() { ObjID = 2, SubsystemID = SubsystemType.SUBSYST_ASO };

            ReportInfo? RepInfo = null;

            var result = await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID });
            if (result.IsSuccessStatusCode)
            {
                RepInfo = await result.Content.ReadFromJsonAsync<ReportInfo>();
            };

            if (RepInfo == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ReportInfo " + Rep["NoData"]);
                return;
            }

            List<GetColumnsExItem>? ColumnList = null;

            result = await Http.PostAsJsonAsync("api/v1/GetColumnsEx", ReportId);
            if (result.IsSuccessStatusCode)
            {
                ColumnList = await result.Content.ReadFromJsonAsync<List<GetColumnsExItem>>();
            }

            if (ColumnList == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ColumnsEx " + Rep["NoData"]);
                return;
            }

            List<AbonReport>? abonReports = null;

            result = await Http.PostAsJsonAsync("api/v1/GetAbonReportAso", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();

                abonReports = JsonParser.Default.Parse<AbonReportList>(json)?.Array.ToList();
            }

            if (abonReports == null || abonReports.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 2, abonReports, _OtherForReport.AbonOther(abonReports.Count), _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "AbonReportAso.html", streamRef);

        }

        string GetCallName
        {
            get
            {
                return replaceItem?.BaseType switch
                {
                    (int)BaseLineType.LINE_TYPE_WSDL => AsoRep["Region"],
                    (int)BaseLineType.LINE_TYPE_DCOM => AsoRep["Server"],
                    (int)BaseLineType.LINE_TYPE_SMTP => AsoRep["Email"],
                    _ => AsoRep["PhoneNumber"]
                };
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }

    }
}
