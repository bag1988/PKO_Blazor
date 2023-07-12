using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Linq;
using BlazorLibrary;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LibraryProto.Helpers;
using LibraryProto.Helpers.V1;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Interfaces;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using ViewState.Client.Shared;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ViewState.Client.Pages
{
    public partial class ViewStates : IAsyncDisposable, IPubSubMethod
    {
        private List<ViewHardware>? CacheItems;

        private System.Timers.Timer timer = new();

        private HBits Bits = new();

        private bool IsReturnForeach = false;

        public DateTime StartTime { get; set; } = DateTime.Now.Date;
        public DateTime EndTime { get; set; } = DateTime.Now;

        public List<ViewHardware>? SelectedList { get; set; } = null;

        private bool IsViewInfo = false;

        private string NORMCMD = " bgColor=\"#ffEEff\"";
        private string NORMANSW = " bgColor=\"#ffffEE\"";
        private string NORM = " bgColor=\"#ffffff\"";
        private string WARN = " bgColor=\"#ffff99\"";
        private string ERRR = " bgColor=\"#ffAAAA\"";
        private string CALIGN = " align=\"center\"";
        private string WIDTH(int x) => " width=\"" + x + "\"";
        private string CSIZE(int x) => " size=\"" + x + "\"";
        private string COLUMN(string x, string y, string z) => "<TD" + x + "><FONT" + z + ">" + y + "</FONT></TD>\n";

        private int StaffID { get; set; } = 0;

        public class ViewHardware
        {
            public string StateName { get; set; } = "";
            public string ObjTypeName { get; set; } = "";
            public string SerialNumberName { get; set; } = "";
            public DateTime? TimeAccessName { get; set; }
            public string? ConnectionPointSucces { get; set; }
            public bool BAvaria { get; set; }
            public bool BTesting { get; set; }
            public HardwareMonitorEx? hm { get; set; }
            public bool IsConnect { get; set; } = true;
            public string DevName { get; set; } = "";
            public string CuName { get; set; } = "";
            public string[] Info { get; set; } = new string[0];
            public string[] InfoTitle { get; set; } = new string[0];
        }

        TableVirtualize<ViewHardware>? table;

        protected override async Task OnInitializedAsync()
        {
            StaffID = await _User.GetLocalStaff();
            ThList = new Dictionary<int, string>
            {
                { 0, ViewRep["ThState"] },
                { 1, ViewRep["ThType"] },
                { 2, ViewRep["ThNumber"] },
                { 3, ViewRep["ThName"] },
                { 4, ViewRep["ThCheckTime"] },
                { 5, ViewRep["ThAffiliation"] },
                { 6, ViewRep["ConnectionPointSucces"] },
                { -7, ViewRep["ThDetalis"] }
            };
            await GetAllStates();

            await StartGetInfoChild();
            timer.Elapsed += async (sender, eventArgs) =>
            {
                IsReturnForeach = false;
                await StartGetInfoChild();
            };
            timer.Start();

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), ViewRep["ThName"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpDevName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.State), ViewRep["ThState"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpState)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Type), ViewRep["ThType"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpType)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Number), ViewRep["ThNumber"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpNumber)));
            HintItems.Add(new HintItem(nameof(FiltrModel.CheckTimeRange), ViewRep["ThCheckTime"], TypeHint.Date));
            HintItems.Add(new HintItem(nameof(FiltrModel.Affiliation), ViewRep["ThAffiliation"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpCuName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.IsUpdated), ViewRep["ConnectionPointSucces"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpUpdated)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Details), ViewRep["ThDetalis"], TypeHint.ContainsOnly));
            HintItems.Add(new HintItem(nameof(FiltrModel.IsAvaria), ViewRep["tAvar"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            HintItems.Add(new HintItem(nameof(FiltrModel.IsConnected), ViewRep["CONNECTED"], TypeHint.Bool, null, FiltrOperationType.BoolEqual));
            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewState);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_AddErrorState(byte[] Value)
        {
            try
            {
                var elem = HardwareMonitorEx.Parser.ParseFrom(Value);
                if (elem != null && elem.OBJKey != null)
                {
                    if (CacheItems == null)
                        CacheItems = new();
                    lock (CacheItems)
                    {
                        if (elem.OBJKey.ObjType == -1)
                        {
                            CacheItems?.RemoveAll(x => x.hm?.OBJKey?.ObjID.Equals(elem.OBJKey.ObjID) ?? false);
                            SelectedList?.RemoveAll(x => x.hm?.OBJKey?.ObjID.Equals(elem.OBJKey.ObjID) ?? false);
                        }
                        else
                        {                            
                            if (CacheItems.Any(x => x.hm != null && x.hm.OBJKey.Equals(elem.OBJKey)))
                            {
                                var firstElem = CacheItems.FirstOrDefault(x => x.hm?.OBJKey.Equals(elem.OBJKey) ?? false);
                                if (firstElem?.hm != null)
                                {
                                    elem = new(firstElem.hm) { AnswerType = elem.AnswerType, TimeAccess = elem.TimeAccess };
                                    var newElem = GenerateItem(elem, true);
                                    var f = CacheItems.FindIndex(x => x.hm?.OBJKey.Equals(elem.OBJKey) ?? false);
                                    CacheItems.RemoveAt(f);
                                    CacheItems.Insert(f, newElem);

                                    if (SelectedList?.Any(x => x.hm?.OBJKey?.ObjID.Equals(elem.OBJKey.ObjID) ?? false) ?? false)
                                    {
                                        SelectedList.RemoveAll(x => x.hm?.OBJKey?.ObjID.Equals(elem.OBJKey.ObjID) ?? false);
                                        SelectedList.Add(newElem);
                                    }
                                }
                            }
                            else
                                CacheItems.Add(GenerateItem(elem, true));
                        }
                        _ = table?.ResetData();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fire_AddErrorState {ex.Message}");
            }


            return Task.CompletedTask;
        }

        ItemsProvider<ViewHardware> GetProvider => new ItemsProvider<ViewHardware>(ThList, LoadChildList, request);

        private ValueTask<IEnumerable<ViewHardware>> LoadChildList(GetItemRequest req)
        {
            List<ViewHardware> newData = new();
            if (CacheItems != null)
            {
                try
                {
                    SetSort(req.LSortOrder, req.BFlagDirection);

                    ViewStateFiltr FiltrModel = new();

                    if (!request.BstrFilter.TryBase64ToProto(out FiltrModel))
                    {
                        FiltrModel = new();
                    }
                    var modelType = Expression.Parameter(typeof(ViewHardware));

                    BinaryExpression? filter = null;

                    if (FiltrModel.State?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.StateName));
                        filter = FiltrModel.State.CreateExpressionFromRepeatedString(member, filter);
                    }
                    if (FiltrModel.Type?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.ObjTypeName));
                        filter = FiltrModel.Type.CreateExpressionFromRepeatedString(member, filter);
                    }
                    if (FiltrModel.Number?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.SerialNumberName));
                        filter = FiltrModel.Number.CreateExpressionFromRepeatedString(member, filter);
                    }

                    if (FiltrModel.Name?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.DevName));
                        filter = FiltrModel.Name.CreateExpressionFromRepeatedString(member, filter);
                    }

                    if (FiltrModel.CheckTimeRange?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.TimeAccessName));
                        var uintToStringExp = Expression.Call(typeof(ViewStates), "DataTimeToTimeStamp", null, member);
                        filter = FiltrModel.CheckTimeRange.CreateExpressionFromRepeatedDataTime(uintToStringExp, filter);
                    }
                    if (FiltrModel.Affiliation?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.CuName));
                        filter = FiltrModel.Affiliation.CreateExpressionFromRepeatedString(member, filter);
                    }
                    if (FiltrModel.IsUpdated?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.ConnectionPointSucces));
                        filter = FiltrModel.IsUpdated.CreateExpressionFromRepeatedString(member, filter);
                    }
                    if (FiltrModel.Details?.Count > 0)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.Info));
                        var uintToStringExp = Expression.Call(typeof(ViewStates), "HardwareMonitorExToString", null, member);
                        filter = FiltrModel.Details.CreateExpressionFromRepeatedString(uintToStringExp, filter);
                    }
                    if (FiltrModel.IsAvaria != null)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.BAvaria));
                        filter = filter.AddBinaryExpression(Expression.Equal(member, Expression.Constant(FiltrModel.IsAvaria.Value)));
                    }
                    if (FiltrModel.IsConnected != null)
                    {
                        var member = Expression.PropertyOrField(modelType, nameof(ViewHardware.IsConnect));
                        filter = filter.AddBinaryExpression(Expression.Equal(member, Expression.Constant(FiltrModel.IsConnected.Value)));
                    }

                    if (filter != null)
                    {
                        Expression<Func<ViewHardware, bool>>? filtrExp = Expression.Lambda<Func<ViewHardware, bool>>(filter, modelType);
                        newData.AddRange(CacheItems.Where(filtrExp.Compile()));
                    }
                    else
                    {
                        newData.AddRange(CacheItems);
                    }

                    if (req.CountData != 0)
                    {
                        newData = newData.Skip(req.SkipItems).Take(request.CountData).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error get items {ex.Message}");
                }
            }
            return new(newData);
        }

        static Timestamp DataTimeToTimeStamp(DateTime? strData)
        {
            if (strData == null) return new();
            return Timestamp.FromDateTime(strData.Value);
        }

        static string HardwareMonitorExToString(string[] items)
        {
            var ss = string.Join(" ", items);
            return ss;
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpUpdated(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.ConnectionPointSucces?.Contains(req.BstrFilter) ?? false).Select(x => new Hint(x.ConnectionPointSucces)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpDevName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.DevName.Contains(req.BstrFilter)).Select(x => new Hint(x.DevName)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpCuName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.CuName.Contains(req.BstrFilter)).Select(x => new Hint(x.CuName)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }
        private ValueTask<IEnumerable<Hint>> LoadHelpState(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.StateName.Contains(req.BstrFilter)).Select(x => new Hint(x.StateName)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpType(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.ObjTypeName.Contains(req.BstrFilter)).Select(x => new Hint(x.ObjTypeName)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }
        private ValueTask<IEnumerable<Hint>> LoadHelpNumber(GetItemRequest req)
        {
            List<Hint>? newData = new();
            if (CacheItems != null)
            {
                newData.AddRange(CacheItems.Where(x => x.SerialNumberName.Contains(req.BstrFilter)).Select(x => new Hint(x.SerialNumberName)).Distinct().Take(20));
            }
            return new(newData ?? new());
        }



        private async Task RefreshTable()
        {
            SelectedList = null;
            if (table != null)
                await table.ResetData();
        }

        private string GetBg(ViewHardware item)
        {
            return (item.BAvaria || !item.IsConnect) ? "bg-avaria" : item.BTesting ? "bg-testing" : "";
        }

        private void ChangeRadio(int e)
        {
            switch (e)
            {
                case 1: StartTime = DateTime.Now.AddMonths(-1); break;
                case 2: StartTime = DateTime.Now.AddDays(-1); break;
                case 3: StartTime = DateTime.Now.Date; break;
            }
        }

        private void SetSort(int column, int flag)
        {
            bool bFlag = flag == 1;
            if (column == 3)
            {
                if (bFlag)
                    CacheItems = CacheItems?.OrderBy(x => x.hm?.DevName).ToList();
                else
                    CacheItems = CacheItems?.OrderByDescending(x => x.hm?.DevName).ToList();
            }
            else if (column == 5)
            {
                if (bFlag)
                    CacheItems = CacheItems?.OrderBy(x => x.hm?.CUName).ToList();
                else
                    CacheItems = CacheItems?.OrderByDescending(x => x.hm?.CUName).ToList();
            }
            else
                SortList.Sort(ref CacheItems, column, flag);
        }

        private void AddSelectItem(List<ViewHardware>? items)
        {
            SelectedList = items;
        }

        private async Task GetAllStates()
        {
            var result = await Http.PostAsync("api/v1/GetResultList2", null);
            try
            {
                if (result.IsSuccessStatusCode)
                {
                    if (CacheItems == null)
                        CacheItems = new List<ViewHardware>();

                    var json = await result.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(json))
                        return;
                    var Models = JsonParser.Default.Parse<HardwareMonitorExList>(json) ?? new();
                    if (Models != null)
                    {
                        foreach (var item in Models.Array)
                        {
                            if (CacheItems.Any(x => x.hm != null && x.hm.OBJKey.Equals(item.OBJKey)))
                            {
                                var f = CacheItems.FindIndex(x => x.hm?.OBJKey.Equals(item.OBJKey) ?? false);
                                CacheItems.RemoveAt(f);
                                CacheItems.Insert(f, GenerateItem(item, true));
                            }
                            else
                                CacheItems.Add(GenerateItem(item, true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task StartGetInfoChild()
        {
            try
            {
                timer.Stop();
                if (CacheItems != null)
                {
                    if (!MainLayout.param.IsCheckCu)
                    {
                        CacheItems.RemoveAll(x => x.hm?.OBJKey?.ObjID?.StaffID != StaffID);
                        SelectedList?.RemoveAll(x => x.hm?.OBJKey?.ObjID?.StaffID != StaffID);
                    }
                    else
                    {
                        foreach (var item in CacheItems.ToList().Where(x => x.hm?.OBJKey?.ObjType == (int)HMT.Staff))
                        {
                            if (!MainLayout.param.IsCheckCu)
                                break;
                            if (IsReturnForeach)
                                return;
                            if (item.hm?.OBJKey?.ObjID?.ObjID > 0)
                            {
                                await GetChildInfo(item.hm.OBJKey.ObjID.ObjID);
                            }
                        }
                    }
                    _ = table?.ResetData();
                }
                timer.Interval = TimeSpan.FromSeconds(MainLayout.param.IntervalUpdateState).TotalMilliseconds;
                if (!IsReturnForeach)
                    timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ViewDialogHistory()
        {
            if (SelectedList?.Any() ?? false)
            {
                EndTime = DateTime.Now;
                StartTime = DateTime.Now.Date;
                IsViewInfo = true;
            }
        }

        private async Task GetChildInfo(int staffid)
        {
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetChildInfo", staffid);
                if (result.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    CacheItems?.RemoveAll(x => x.hm?.OBJKey?.ObjID?.StaffID == staffid);
                    SelectedList?.RemoveAll(x => x.hm?.OBJKey?.ObjID?.StaffID == staffid);

                    var elem = CacheItems?.FirstOrDefault(x => staffid == x.hm?.OBJKey?.ObjID?.ObjID);
                    if (elem != null)
                    {
                        elem.IsConnect = false;
                        elem.StateName = ViewRep["String28"];
                    }
                }
                else if (result.IsSuccessStatusCode)
                {
                    if (CacheItems == null)
                        CacheItems = new List<ViewHardware>();

                    var json = await result.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(json))
                        return;

                    var Models = JsonParser.Default.Parse<HardwareMonitorExList>(json) ?? new();
                    if (Models != null)
                    {
                        CacheItems.RemoveAll(x => !Models.Array.Any(m => m.OBJKey?.Equals(x.hm?.OBJKey) ?? false) && x.hm?.OBJKey?.ObjID?.StaffID == staffid);

                        foreach (var item in Models.Array)
                        {
                            var newElem = GenerateItem(item, false);

                            if (CacheItems.Any(x => x.hm != null && x.hm.OBJKey.Equals(item.OBJKey)))
                            {
                                var f = CacheItems.FindIndex(x => x.hm.OBJKey.Equals(item.OBJKey));
                                CacheItems.RemoveAt(f);
                                CacheItems.Insert(f, newElem);

                                if (SelectedList?.Any(x => x.hm != null && x.hm.OBJKey.Equals(item.OBJKey)) ?? false)
                                {
                                    SelectedList.RemoveAll(x => x.hm != null && x.hm.OBJKey.Equals(item.OBJKey));
                                    SelectedList.Add(newElem);
                                }
                            }
                            else
                            {
                                CacheItems.Add(newElem);
                            }
                        }
                        var elem = CacheItems?.FirstOrDefault(x => staffid == x.hm?.OBJKey?.ObjID?.ObjID);
                        if (elem != null)
                        {
                            elem.IsConnect = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private ViewHardware GenerateItem(HardwareMonitorEx item, bool? cp_state = false)
        {
            ViewHardware itemModel = new() { hm = item };
            try
            {
                string pState = "";
                itemModel.ConnectionPointSucces = cp_state == true ? ViewRep["True"] : ViewRep["False"];
                item.Operator = StaffID < item.OBJKey.ObjID.StaffID || (StaffID == item.OBJKey.ObjID.StaffID && (int)HMT.None < item.OBJKey.ObjType) || (StaffID == item.OBJKey.ObjID.StaffID && (int)HMT.None == item.OBJKey.ObjType && 0 < item.OBJKey.ObjID.ObjID);

                var h = item.CheckPeriod.ToDateTime().Hour;
                var m = item.CheckPeriod.ToDateTime().Minute;
                var s = item.CheckPeriod.ToDateTime().Second;
                Int64 llTimeout = (2 * (h * 3600 + m * 60 + s) * ((Int64)1000 * 1000 * 10));

                var pflt = DateTime.UtcNow.ToFileTime();
                var pfdt = item.TimeAccess?.ToDateTime().ToFileTime();

                if (item.CheckPeriod.ToDateTime().Year == 0 || pflt > (pfdt + llTimeout))
                {
                    pState = ViewRep["String32"]; //Таймаут
                    itemModel.BAvaria = true;
                }
                else
                {
                    itemModel.BAvaria = false;

                    switch (item.AnswerType)
                    {
                        case 0x10/*LOG_DEVICE_OK*/: pState = ViewRep["String27"]; break;//Работает
                        case 0x20/*LOG_NO_ANSWER*/:
                        {
                            pState = ViewRep["String28"];//Нет связи
                            itemModel.BAvaria = true;
                        }
                        break;
                        case 0x05/*SCS_LOG_ANSWER*/:
                        {
                            pState = ViewRep["String27"];//Работает

                            SCS_DEV_ANSWER pAnswer = new(item.State.Memory.ToArray());

                            if ((pAnswer.InfoType == 0x01))
                            {
                                if (pAnswer.Param[0] == 0x3C)
                                {
                                    byte firstByte = pAnswer.Param[1];

                                    /** Отображение аварии ПОС.*/
                                    if (MainLayout.param.IsPOSIgnore)
                                        firstByte &= 0x1F;


                                    if ((firstByte & 0x80) > 0 || (firstByte & 0x40) > 0 || (firstByte & 0x20) > 0 ||
                                        ((firstByte & 0x08) > 0 && ((firstByte & 0x12) != 0x12)) ||
                                        ((firstByte & 0x04) == 0) ||
                                        ((firstByte & 0x10) > 0 && (pAnswer.Param[2] & 0x0E) > 0) ||
                                        ((firstByte & 0x10) == 0 && (pAnswer.Param[2] & 0xE0) > 0))
                                    {
                                        itemModel.BAvaria = true;
                                    }
                                }
                                else if (pAnswer.Param[0] == 0x3D)
                                {
                                    if ((pAnswer.Param[1] & 0x08) > 0 || (pAnswer.Param[1] & 0x04) > 0 || (pAnswer.Param[1] & 0x02) > 0 || pAnswer.Param[2] != 0)
                                    {
                                        itemModel.BAvaria = true;
                                    }
                                }
                            }
                            break;
                        }
                        case 0x0D/*SCS_LOG_NO_ANSWER*/:
                        {
                            pState = ViewRep["String31"];//Нет ответа
                            itemModel.BAvaria = true;
                            break;
                        }
                        case 0x00/*SCS_LOG_CMD_DEV*/:
                        {
                            pState = ViewRep["String33"];//Проверка...
                            itemModel.BAvaria = false;
                            itemModel.BTesting = true;
                            break;
                        }
                        default: pState = ViewRep["String34"]; break;//Неизвестный ответ
                    }

                }

                itemModel.StateName = pState;

                switch (item.OBJKey.ObjType)
                {
                    case (int)HMT.Uuzs:
                    {
                        itemModel.SerialNumberName = IpAddressUtilities.UintToString(item.ConnParam);
                        itemModel.ObjTypeName = SMDataRep["SUBSYST_SZS"];
                    }
                    break;

                    case (int)HMT.Aso:
                    {
                        itemModel.SerialNumberName = $"{(item.State.Length > 2 && item.State[3] == 4 ? /*LPT*/" " : /*COM*/" ")}{item.ConnParam}";
                        itemModel.ObjTypeName = SMDataRep["SUBSYST_ASO"];
                    }
                    break;

                    case (int)HMT.Uzs:
                    {
                        itemModel.SerialNumberName = $"S/N: {item.OBJKey.ObjID.ObjID}";
                        itemModel.ObjTypeName = SMDataRep["SUBSYST_SZ"];
                    }
                    break;

                    case (int)HMT.Staff:
                    {
                        itemModel.SerialNumberName = $"ID: {item.OBJKey.ObjID.ObjID}";
                        itemModel.ObjTypeName = SMDataRep["SUBSYST_GSO_STAFF"];
                    }
                    break;

                    default:
                    {
                        itemModel.SerialNumberName = $"{ViewRep["String43"]}: {item.OBJKey.ObjType}";//Неизвестный тип
                        itemModel.ObjTypeName = ViewRep["String35"];//Неизвестно
                    }
                    break;
                }

                if (item.TimeAccess?.ToDateTime().Year > 0)
                    itemModel.TimeAccessName = item.TimeAccess?.ToDateTime();
                itemModel.DevName = item.DevName;
                itemModel.CuName = item.CUName;
                itemModel.Info = GetInfo(item, true).ToArray();
                itemModel.InfoTitle = GetInfo(item, false).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error GenerateItem {ex.Message}");
            }
            return itemModel;

        }

        private IEnumerable<string> GetInfo(HardwareMonitorEx? item, bool? bErrorOnly = true)
        {
            if (item == null)
                return new string[0];

            SCS_DEV_ANSWER pAnswer = new(item.State.Memory.ToArray());

            string tStr = "";

            if (pAnswer.Param[0] == 0x3C)
            {
                // Напряжение АКБ
                if (item.AKB != 0)
                {

                    byte bAKB = Bits.LOBYTE(Bits.LOWORD(item.AKB));
                    float AKB = (((float)bAKB) * 2) / 33;
                    string tBuff = "";
                    tBuff = $"АКБ: {item.AKB} {ViewRep["String24"]}\n"; //В на один АКБ
                    tStr += tBuff;
                    //str.Add(IH.Group8(item.AKB));
                }


                if ((pAnswer.Param[1] & 0x12) == 0x12) //Тип УЗСЗ
                {
                    tStr += "\n" + ViewRep["String42"];//Тип УЗСЗ
                }
                else if ((pAnswer.Param[1] & 0x10) > 0) // УКБ-200у
                {
                    if ((pAnswer.Param[2] & 0x01) > 0) tStr += "\n" + ViewRep["String1"];//Перегрев усилителя мощности
                    if ((pAnswer.Param[2] & 0x04) > 0) tStr += "\n" + ViewRep["String2"];//Неисправность усилителя мощности
                    if ((pAnswer.Param[2] & 0x04) > 0) tStr += "\n" + ViewRep["String3"];//Отсутствие питания 220В
                    if ((pAnswer.Param[2] & 0x08) > 0) tStr += "\n" + ViewRep["String4"];//Разряд аккумуляторной батареи


                    string tt = "";

                    if ((pAnswer.Param[1] & 0x80) > 0) tt += "\n" + ViewRep["String5"];//Сработала ПС
                    if ((pAnswer.Param[1] & 0x40) > 0) tt += "\n" + ViewRep["String6"];//Сработала ОС
                    if ((pAnswer.Param[1] & 0x20) > 0) tt += "\n" + ViewRep["String7"];//Сработал датчик вскрытия
                    if ((pAnswer.Param[1] & 0x08) > 0 && (pAnswer.Param[1] & 0x12) != 0x12)
                        tt += "\n" + ViewRep["String8"];//Нет ответа ОУ
                    if ((pAnswer.Param[1] & 0x04) == 0) tt += "\n" + ViewRep["String9"];//Ошибка ЭПУ

                    if (tt.Length > 0)
                        tStr += $"\n{ViewRep["String37"]}: " + tt;

                    uint dwZone = BitConverter.ToUInt32(pAnswer.Param, 2);
                    dwZone = pAnswer.Param[2];
                    dwZone >>= 4;
                    dwZone |= (uint)(pAnswer.Param[6] << 28);

                    for (int i = 0; i < 15; i++)
                    {
                        byte bZone = (byte)((dwZone >> i * 2) & 0x3);

                        switch (bZone)
                        {
                            case 1:
                            {
                                tStr += "\n" + ViewRep["String10"] + $" {(i + 1)} ";//Линия
                                tStr += "\n" + ViewRep["String12"];/*Авария - оборвана*/
                            }
                            break;
                            case 2:
                            {
                                tStr += "\n" + ViewRep["String10"] + $" {(i + 1)} ";//Линия
                                tStr += "\n" + ViewRep["String13"];/*Авария - перегружена или закорочена*/
                            }
                            break;
                        }
                    }


                }
                else// УКБ-500+
                {
                    if ((pAnswer.Param[6] & 0x01) > 0)
                        tStr += "\n" + ViewRep["String38"];//Тип УЗС3 (Видеоперехват) Запрет оповещения
                    else
                        tStr += "\n" + ViewRep["String40"];//Тип УКБ-500+


                    if ((pAnswer.Param[2] & 0x80) > 0) tStr += "\n" + ViewRep["String15"];// Авария линии
                    if ((pAnswer.Param[2] & 0x40) > 0) tStr += "\n" + ViewRep["String16"];//Авария радиофидера
                    if ((pAnswer.Param[2] & 0x20) > 0) tStr += "\n" + ViewRep["String17"];//Авария усилителя мощности

                    ushort wZone = Bits.MAKEWORD(pAnswer.Param[5], pAnswer.Param[4]);
                    for (int i = 0; i < 16; i++)
                    {
                        string tStr1 = "\n" + ViewRep["String25"] + (i + 1) + " ";//Зона
                        if (((byte)(wZone >> i) & 0x1) > 0)
                            tStr1 += ViewRep["tAvar"];//"Авария";
                        else tStr1 += ViewRep["String18"];// "Работает";

                        if (bErrorOnly == false || ((byte)(wZone >> i) & 0x1) > 0)
                            tStr += tStr1;
                    }
                }
            }
            else if (pAnswer.Param[0] == 0x3D)
            {
                tStr += "\n" + ViewRep["String39"];//Тип УЗС3 (Видеоперехват)


                if ((pAnswer.Param[1] & 0x02) > 0) tStr += "\n" + ViewRep["String19"];//Нет связи с COM модема аналоговой линии (Ошибка ОУ)
                if ((pAnswer.Param[1] & 0x04) > 0) tStr += "\n" + ViewRep["String20"];// Ошибки в конфигурировании ПО УЗС(Ошибка ОУ)
                if ((pAnswer.Param[1] & 0x08) > 0) tStr += "\n" + ViewRep["String21"];//Ошибка связи с коммутатором (Нет ответа ОУ)


                tStr += "\n" + ViewRep["String41"];//Тип Видеоперехват. Характеристики:

                ushort wZone = BitConverter.ToUInt16(pAnswer.Param, 2);

                for (int i = 0; i < 16; i++)
                {
                    tStr += "\n" + ViewRep["String10"] + " " + (i + 1) + " ";//Линия
                    if ((wZone & (1 << i)) > 0)
                        tStr += ViewRep["String22"];//Ошибка при коммутации
                    else
                        tStr += ViewRep["String23"];//Норма
                }
            }


            if (string.IsNullOrEmpty(tStr) && bErrorOnly == false)
                tStr = ViewRep["NoInfo"];

            return tStr.Split("\n").Where(x => !string.IsNullOrEmpty(x));
        }

        private async Task<HistoryList> GetHistoryDevice(HistoryRequest request)
        {
            HistoryList Models = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetHistoryDevice", request);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                Models = JsonParser.Default.Parse<HistoryList>(json);
            }
            return Models;
        }

        private async Task OnHistory()
        {
            try
            {
                if (SelectedList?.Count > 0)
                {
                    string iof = "";
                    iof += $"<HTML><head><title>{ViewRep["TitleHistory"]}</title><META http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">"
                                + "</head><BODY leftMargin=\"20\" style=\"background-color:white\">"
                                  + $"<P align=\"center\"><B>{ViewRep["TitleHistory"]}</B></P><br>";

                    iof += $"  <P>{ViewRep["PK_NAME"]}: <B>{MyNavigationManager.BaseUri}</B><br>\n";

                    iof += $"  {ViewRep["PERIOD"]}: <B>{StartTime.ToString("d")} - {EndTime.ToString("d")}</B><br>\n";

                    iof += $"  {ViewRep["DATA_REPORT"]}: <B>{DateTime.Now}</B>\n";

                    foreach (var item in SelectedList)
                    {
                        iof += "  <br>\n  <P>Статистика работы устройства ";
                        iof += item.hm?.DevName;

                        if (item.hm?.OBJKey.ObjType == (int)HMT.Uuzs)
                        {
                            iof += $" ({item.hm?.ConnParam})";
                        }
                        iof += "<P>Принадлежность: ";
                        iof += item.hm?.CUName;

                        iof += ":\n  <TABLE cellSpacing=\"0\" cellPadding=\"2\" border=\"1\">\n"; //  width=\"98%\"
                        HistoryRequest request = new();
                        request.OBJKey = item.hm?.OBJKey ?? new(); //new tagOBJ_Key() { NObjType = (int)HMT.Staff, ObjID = new OBJ_ID() { SubsystemID = SubsystemType.SUBSYST_GSO_STAFF, StaffID = 0, ObjID = 0 } };
                        request.BeginTime = StartTime.ToUniversalTime().ToTimestamp();
                        request.EndTime = EndTime.ToUniversalTime().ToTimestamp();

                        var result = await GetHistoryDevice(request);
                        if (result != null && result.Array.Any())
                        {
                            string OneStr = "";
                            string tAvar = "";
                            string tZones = "";
                            string tSensors = "";
                            uint dwZone = 0;

                            long dwStartTime = DateTime.Now.Ticks;

                            foreach (var itemHistory in result.Array)
                            {
                                OneStr = "";
                                if ((itemHistory.AnswerType == 0x00/*SCS_LOG_CMD_DEV*/ || (itemHistory.AnswerType == 0x05/*SCS_LOG_ANSWER*/ && request!.OBJKey!.ObjType == (int)HMT.Uzs && request.OBJKey.ObjID.SubsystemID == SubsystemType.SUBSYST_SZS
                                && itemHistory.State[2] != 0x3C)) && !MainLayout.param.IsHistoryCommand)
                                    continue;

                                if (request.OBJKey?.ObjID?.SubsystemID == SubsystemType.SUBSYST_SZS && request.OBJKey?.ObjType == (int)HMT.Uuzs)
                                {
                                    OneStr += COLUMN(WIDTH(200) + CALIGN + NORM, itemHistory.TimeAccess.ToDateTime().ToString("G"), CSIZE(2)); // Время события

                                    if (item.hm?.ConnParam > 0)
                                        OneStr += COLUMN(WIDTH(30) + CALIGN + NORM, IpAddressUtilities.UintToString(item.hm.ConnParam), CSIZE(2));     // адрес
                                }
                                else if (request.OBJKey?.ObjID?.SubsystemID != SubsystemType.SUBSYST_ASO)
                                {
                                    OneStr += COLUMN(WIDTH(200) + CALIGN + NORM, itemHistory.TimeAccess.ToDateTime().ToString("G"), CSIZE(2));            // Время события
                                    OneStr += COLUMN(WIDTH(30) + CALIGN + NORM, item.hm?.OBJKey?.ObjID?.ObjID.ToString() ?? "", CSIZE(2));     // S/N

                                    if (!string.IsNullOrEmpty(item.hm?.DevName))
                                        OneStr += COLUMN(NORM, item.hm.DevName, CSIZE(2));    // Имя устройства
                                }
                                else
                                {
                                    OneStr += COLUMN(WIDTH(200) + CALIGN + NORM, itemHistory.TimeAccess.ToDateTime().ToString("G"), CSIZE(2));            // Время события
                                }

                                switch (itemHistory.AnswerType)
                                {
                                    case 0x10/*LOG_DEVICE_OK*/: OneStr += COLUMN(WIDTH(250) + NORM, ViewRep["String27"], CSIZE(2)); break;// Работает
                                    case 0x20/*LOG_NO_ANSWER*/: OneStr += COLUMN(WIDTH(250) + WARN, ViewRep["String28"], CSIZE(2)); break;// Нет связи
                                    case 0x00/*SCS_LOG_CMD_DEV*/: OneStr += COLUMN(NORMCMD, ViewRep["String29"], CSIZE(2)); break;//Команда
                                    case 0x05/*SCS_LOG_ANSWER*/: OneStr += COLUMN(NORMANSW, ViewRep["String30"], CSIZE(2)); break;//Устройство ответило // По просьбе Чеснокова от 29.03.2016
                                    case 0x0D/*SCS_LOG_NO_ANSWER*/: OneStr += COLUMN(WARN, ViewRep["String31"], CSIZE(2)); break;//Нет ответа
                                }

                                if (request.OBJKey?.ObjID?.SubsystemID == SubsystemType.SUBSYST_SZS && request.OBJKey?.ObjType == (int)HMT.Uzs)
                                {
                                    SCS_DEV_ANSWER pAnswer = new(itemHistory.State.Memory.ToArray());

                                    OneStr += COLUMN(NORM, $"{pAnswer.InfoType.ToString("X2")} {pAnswer.ChannelNumber.ToString("X2")} {pAnswer.Param[0].ToString("X2")} {pAnswer.Param[1].ToString("X2")} {pAnswer.Param[2].ToString("X2")} {pAnswer.Param[3].ToString("X2")} {pAnswer.Param[4].ToString("X2")} {pAnswer.Param[5].ToString("X2")} {pAnswer.Param[6].ToString("X2")} {pAnswer.Param[7].ToString("X2")}", CSIZE(2));

                                    // Разобрать квитанцию о состоянии
                                    if (pAnswer.InfoType == 0x01)
                                    {

                                        tAvar = "";
                                        tZones = "";
                                        tSensors = "";

                                        string pColor = NORM;
                                        if (pAnswer.Param[0] == 0x3C)
                                        {

                                            if ((pAnswer.Param[1] & 0x80) > 0) tSensors += ViewRep["String5"] + "<br/>";//Сработала ПС
                                            if ((pAnswer.Param[1] & 0x40) > 0) tSensors += ViewRep["String6"] + "<br/>";//Сработала ОС
                                            if ((pAnswer.Param[1] & 0x20) > 0) tSensors += ViewRep["String7"] + "<br/>";//Сработал датчик вскрытия
                                            if ((pAnswer.Param[1] & 0x08) > 0 && (pAnswer.Param[1] & 0x12) != 0x12) tSensors += ViewRep["String8"] + "<br/>";//Нет ответа ОУ                                            
                                            if ((pAnswer.Param[1] & 0x04) == 0) tSensors += ViewRep["String9"] + "<br/>";//Ошибка ЭПУ

                                            if ((pAnswer.Param[1] & 0x12) == 0x12)
                                            {
                                                // УЗС3
                                            }
                                            else if ((pAnswer.Param[1] & 0x10) > 0)// УКБ-200у
                                            {

                                                if ((pAnswer.Param[2] & 0x01) > 0) { tAvar += ViewRep["String1"] + "<br/>"; pColor = ERRR; }//Перегрев усилителя мощности
                                                if ((pAnswer.Param[2] & 0x04) > 0) { tAvar += ViewRep["String2"] + "<br/>"; pColor = ERRR; }//Неисправность усилителя мощности
                                                if ((pAnswer.Param[2] & 0x04) > 0) { tAvar += ViewRep["String3"] + "<br/>"; pColor = ERRR; }//Отсутствие питания 220В
                                                if ((pAnswer.Param[2] & 0x08) > 0) { tAvar += ViewRep["String4"] + "<br/>"; pColor = ERRR; }//Разряд аккумуляторной батареи


                                                // Для линии
                                                // 00 - Все ОК - линия выключена
                                                // 11 - Все ОК - линия включена
                                                // 10 - линия перегружена или закорочена
                                                // 01 - линия оборвана

                                                dwZone = BitConverter.ToUInt32(pAnswer.Param, 2);
                                                dwZone >>= 4;
                                                dwZone |= ((uint)pAnswer.Param[6] << 28);

                                                for (int i = 0; i < 16; i++)
                                                {
                                                    tZones += $"<br>{ViewRep["String10"]} " + (i + 1) + " ";//Линия
                                                    byte bZone = (byte)((dwZone >> i * 2) & 0x3);
                                                    switch (bZone)
                                                    {
                                                        case 0: tZones += ViewRep["String11"]; break;//Выкл
                                                        case 1: { tZones += ViewRep["String44"]; pColor = ERRR; } break;//Обрыв
                                                        case 2: { tZones += ViewRep["String45"]; pColor = ERRR; } break;//КЗ
                                                        case 3: tZones += ViewRep["String14"]; break;//Вкл
                                                    }
                                                }
                                            }
                                            else// УКБ-500+
                                            {
                                                if ((pAnswer.Param[2] & 0x80) > 0) tAvar += "\n" + ViewRep["String15"];//Авария линии
                                                if ((pAnswer.Param[2] & 0x40) > 0) tAvar += "\n" + ViewRep["String16"];//Авария радиофидера
                                                if ((pAnswer.Param[2] & 0x20) > 0) tAvar += "\n" + ViewRep["String46"];//Авария УМ

                                                var wZone = Bits.MAKEWORD(pAnswer.Param[5], pAnswer.Param[4]);
                                                for (int i = 0; i < 16; i++)
                                                {
                                                    tZones += "<br> " + ViewRep["String25"] + (i + 1) + " ";//Зона
                                                    if ((byte)((wZone >> i) & 0x1) > 0) { tZones += ViewRep["tAvar"]; pColor = ERRR; }//Авария
                                                    else tZones += ViewRep["String18"];//Работает
                                                }
                                            }

                                            // Колонка зон
                                            if (!string.IsNullOrEmpty(tZones)) OneStr += COLUMN(pColor, tZones, CSIZE(2));

                                            // Колонка аварий
                                            if (!string.IsNullOrEmpty(tSensors)) OneStr += COLUMN(ERRR, tSensors, CSIZE(2));

                                            // Колонка аварий
                                            if (!string.IsNullOrEmpty(tAvar)) OneStr += COLUMN(ERRR, tAvar, CSIZE(2));

                                        }
                                        else if (pAnswer.Param[0] == 0x3D)
                                        {

                                            if ((pAnswer.Param[1] & 0x02) > 0) tSensors += ViewRep["String19"] + "<br/>";//Нет связи с COM модема аналоговой линии (Ошибка ОУ)
                                            if ((pAnswer.Param[1] & 0x04) > 0) tSensors += ViewRep["String20"] + "<br/>";// Ошибки в конфигурировании ПО УЗС(Ошибка ОУ)
                                            if ((pAnswer.Param[1] & 0x08) > 0) tSensors += ViewRep["String21"] + "<br/>";//Ошибка связи с коммутатором (Нет ответа ОУ)


                                            ushort wZone = BitConverter.ToUInt16(pAnswer.Param, 2);

                                            for (int i = 0; i < 16; i++)
                                            {
                                                tZones += "<br/>" + ViewRep["String10"] + " " + (i + 1) + " ";//Линия
                                                if ((wZone & (1 << i)) > 0)
                                                    tZones += ViewRep["String22"];//Ошибка при коммутации
                                                else
                                                    tZones += ViewRep["String23"];//Норма
                                            }

                                            // Колонка зон
                                            if (!string.IsNullOrEmpty(tZones)) OneStr += COLUMN(pColor, tZones, CSIZE(2));

                                            // Колонка аварий
                                            if (!string.IsNullOrEmpty(tSensors)) OneStr += COLUMN(ERRR, tSensors, CSIZE(2));

                                            // Колонка аварий
                                            if (!string.IsNullOrEmpty(tAvar)) OneStr += COLUMN(ERRR, tAvar, CSIZE(2));

                                        }// Вывод напряжения АКБ
                                        else if (pAnswer.Param[0] == 0x3F && pAnswer.Param[3] == 0x56)
                                        {
                                            float AKB = (((float)pAnswer.Param[5]) * 2) / 33;

                                            tZones += $"АКБ: {AKB} {ViewRep["String26"]} {pAnswer.Param[4]}";//В на один АКБ. Усилитель

                                            // Колонка зон
                                            OneStr += COLUMN(NORM, "", CSIZE(2));
                                            OneStr += COLUMN(NORM, tZones, CSIZE(2));
                                        }
                                        else
                                        {
                                            OneStr += COLUMN(NORM, "", CSIZE(2));
                                        }
                                    }
                                    else
                                    {
                                        OneStr += COLUMN(NORM, "", CSIZE(2));
                                    }

                                }

                                // Очередное событие
                                iof += "    <TR>\n" + OneStr + "    </TR>\n";
                            }
                        }

                        iof += "  </TABLE>\n  </P>\n";
                    }

                    iof += "</BODY></HTML>\n";

                    using var streamRef = new DotNetStreamReference(stream: new MemoryStream(Encoding.UTF8.GetBytes(iof)));

                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "History.html", streamRef);
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public async ValueTask DisposeAsync()
        {
            IsReturnForeach = true;
            timer.Stop();
            await timer.DisposeAsync();
            Http.CancelPendingRequests();
            DisposeToken();
            await _HubContext.DisposeAsync();
        }
    }
}
