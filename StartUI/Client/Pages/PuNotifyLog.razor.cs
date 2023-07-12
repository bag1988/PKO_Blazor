
using System.Net.Http.Json;
using BlazorLibrary;
using BlazorLibrary.Shared.NotifyLog;
using Google.Protobuf.WellKnownTypes;
using GsoReporterProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using BlazorLibrary.GlobalEnums;

namespace StartUI.Client.Pages
{
    partial class PuNotifyLog
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public int SessId { get; set; }

        private List<Tuple<bool, CSessions>>? SelectedList = null;

        private bool IsViewDelete = false;

        private bool IsDeleteProcessing = false;

        private DataGridViewCommonInfo? dataGridViewCommn;

        private DataGridViewCommonInfoASO? dataGridViewAso;

        private DataGridViewCommonInfoSMP? dataGridViewSMP;

        private DataGridViewCommonInfoUUZS? dataGridViewUUZS;

        private DataGridViewDetailInfoResult? dataGridViewDetailInfoResult;

        TableVirtualize<Tuple<bool, CSessions>>? table;

        public string GetTitle
        {
            get
            {
                switch (request.ObjID.SubsystemID)
                {
                    case 1: return StartUIRep["IDS_ASOTITLE"];
                    case 2: return StartUIRep["IDS_UUZSTITLE"];
                    case 3: return StartUIRep["IDS_STAFFTITLE"];
                    case 4: return StartUIRep["IDS_P16xTITLE"];
                    default: return "";
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.SubsystemID = SubsystemID;
            request.ObjID.StaffID = await _User.GetLocalStaff();
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_START_TIME"] },
                { 1, StartUIRep["IDS_END_TIME"] },
                { 2, StartUIRep[SubsystemID==SubsystemType.SUBSYST_P16x ?"IDS_COMMANDCOLUMN": "IDS_SITUATIONCOLUMN"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Situation), StartUIRep["IDS_SITUATIONCOLUMN"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpSitName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateStartRange), StartUIRep["IDS_START_TIME"], TypeHint.Date));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateEndRange), StartUIRep["IDS_END_TIME"], TypeHint.Date));

            HintItems.Add(new HintItem(nameof(FiltrModel.Session), StartUIRep["SESSION_NAME"], TypeHint.Number));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrNotifyLog);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (request.ObjID.SubsystemID != SubsystemID && SessId > 0)
            {
                MyNavigationManager.NavigateTo("/PuNotifyLog", false, true);
                request.ObjID.SubsystemID = SubsystemID;
                FiltrModel = new();
                await AddItemFiltr(new List<FiltrItem>() { new FiltrItem(nameof(FiltrModel.Session), new(SessId.ToString()), FiltrOperationType.Equal) });
                StateHasChanged();
            }
        }

        ItemsProvider<Tuple<bool, CSessions>> GetProvider => new ItemsProvider<Tuple<bool, CSessions>>(ThList, LoadChildList, request, new List<int>() { 20, 20 });

        private async ValueTask<IEnumerable<Tuple<bool, CSessions>>> LoadChildList(GetItemRequest req)
        {
            List<Tuple<bool, CSessions>> response = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSessionList", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var newData = await result.Content.ReadFromJsonAsync<List<CSessions>>() ?? new();

                foreach (var item in newData.GroupBy(x => x.ObjID.ObjID))
                {
                    foreach (var t in item)
                    {
                        response.Add(new Tuple<bool, CSessions>((item.Count() == 0 || t.Equals(item.First()) ? true : false), t));
                    }
                }
            }
            else
            {
                MessageView?.AddError("", StartUIRep["IDS_EFAILGETSESSIONINFO"]);
            }
            return response;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForSessionLog", new IntAndString() { Number = request.ObjID.SubsystemID, Str = req.BstrFilter });
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


        private async Task DeleteSession()
        {
            Tuple<bool, CSessions>? newSelect = null;
            if (SelectedList?.Any() ?? false)
            {
                IsDeleteProcessing = true;
                var removeList = SelectedList.Select(x => x.Item2).GroupBy(x => x.ObjID.ObjID);
                foreach (var item in removeList)
                {
                    var result = await Http.PostAsJsonAsync("api/v1/DeleteSession", item.FirstOrDefault()?.ObjID, ComponentDetached);
                    if (result.IsSuccessStatusCode)
                    {
                        var str = await result.Content.ReadFromJsonAsync<BoolValue>();

                        if (str?.Value == true)
                            MessageView?.AddMessage("", $"{StartUIRep["Record"]} № {item.FirstOrDefault()?.ObjID.ObjID} - {StartUIRep["deleted"]}!");
                        else
                            MessageView?.AddError("", $"{StartUIRep["Record"]} № {item.FirstOrDefault()?.ObjID.ObjID} - {AsoRep["IDS_ERRORCAPTION"]}!");

                        StateHasChanged();
                    }
                    else
                    {
                        MessageView?.AddError("", StartUIRep["IDS_EFAILGETSESSIONINFO"]);
                    }
                }
                newSelect = table?.GetNextItemMatch(x => !SelectedList.Any(s => s.Item2.ObjID.ObjID == x.Item2.ObjID.ObjID));
                table?.RemoveAllItem(x => SelectedList.Any(s => s.Item2.ObjID.ObjID == x.Item2.ObjID.ObjID));
            }
            IsDeleteProcessing = false;
            if (newSelect != null)
            {
                SelectedList = new() { newSelect };
            }
            else
                SelectedList = null;
            CloseDelete();
        }

        private void GetItemInfo(List<Tuple<bool, CSessions>>? id)
        {
            if (SelectedList?.LastOrDefault()?.Item2.ObjID.ObjID != id?.LastOrDefault()?.Item2.ObjID.ObjID)
            {
                if (id?.Any() ?? false)
                {
                    if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_GSO_STAFF)
                    {
                        dataGridViewCommn?.Refresh(id.Last().Item2);
                        dataGridViewDetailInfoResult?.Refresh(id.Last().Item2);
                    }
                    else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_ASO)
                    {
                        dataGridViewAso?.Refresh(id.Last().Item2);
                    }
                    else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_SZS)
                    {
                        dataGridViewUUZS?.Refresh(id.Last().Item2);
                    }
                    else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_P16x)
                    {
                        dataGridViewSMP?.Refresh(id.Last().Item2);
                    }
                }
            }
            SelectedList = id;

        }

        private void CloseDelete()
        {
            if (!IsDeleteProcessing)
                IsViewDelete = false;
        }

        private async Task RefreshTable()
        {
            SelectedList = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task GetReport()
        {
            List<CSessions> reportData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSessionList", new GetItemRequest(request) { CountData = 0 }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                reportData = await result.Content.ReadFromJsonAsync<List<CSessions>>() ?? new();
            }

            if (reportData.Count == 0)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            OBJ_ID ReportId = new OBJ_ID() { ObjID = 6, SubsystemID = 0 };

            ReportInfo? RepInfo = null;
            //инфо отчета
            result = await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                RepInfo = await result.Content.ReadFromJsonAsync<ReportInfo>();
            }


            if (RepInfo == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ReportInfo " + Rep["NoData"]);
                return;
            }

            List<GetColumnsExItem>? ColumnList = null;
            //столбцы и др инфо
            result = await Http.PostAsJsonAsync("api/v1/GetColumnsEx", ReportId, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                ColumnList = await result.Content.ReadFromJsonAsync<List<GetColumnsExItem>>();
            }

            if (ColumnList == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], "ColumnsEx " + Rep["NoData"]);
                return;
            }

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 6, reportData, null, _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "SessionsInfo.html", streamRef);

        }

    }
}
