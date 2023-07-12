using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary;
using AsoDataProto.V1;
using GsoReporterProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeviceConsole.Client.Shared.Situation
{
    partial class ViewSituation : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private SituationItem? SelectItem = null;

        private bool? IsDelete = false;

        private bool IsViewNotify = false;

        private bool IsCreateSit = false;

        private string TitleName = "";

        TableVirtualize<SituationItem>? table;

        protected override async Task OnInitializedAsync()
        {
            TitleName = AsoDataRep["IDS_STRING_SITUATION"];

            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;
            request.NObjType = await _User.GetUserSessId();

            switch (request.ObjID.SubsystemID)
            {
                case SubsystemType.SUBSYST_ASO: TitleName = GsoRep["IDS_STRING_SIT_FOR_ASO"]; break;
                case SubsystemType.SUBSYST_GSO_STAFF: TitleName = GsoRep["IDS_STRING_SIT_FOR_CU"]; break;
                case SubsystemType.SUBSYST_SZS: TitleName = GsoRep["IDS_STRING_SIT_FOR_SZS"]; break;
            }

            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_NAME"] },
                { 1, GsoRep["IDS_STRING_SIT_NUMBER"] },
                { 2, GsoRep["IDS_STRING_SIT_PRIOR"] },
                { 3, GsoRep["IDS_STRING_SIT_COMMENT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep["IDS_STRING_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Number), GsoRep["IDS_STRING_SIT_NUMBER"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Prior), GsoRep["IDS_STRING_SIT_PRIOR"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), GsoRep["IDS_STRING_SIT_COMMENT"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrSituation);

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteSituation(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = SituationItemForFire.Parser.ParseFrom(value);

                    if (newItem != null && newItem.SitID?.SubsystemID == SubsystemID)
                    {
                        if (string.IsNullOrEmpty(newItem.SitName) && string.IsNullOrEmpty(newItem.Comm) && newItem.SitPrior == 0)
                        {
                            if (SelectItem != null && SelectItem.SitID == newItem.SitID.ObjID)
                                SelectItem = null;
                            await table.RemoveAllItem(x => x.SitID == newItem.SitID.ObjID);
                        }
                        else
                        {
                            if (!table.AnyItemMatch(x => x.SitID == newItem.SitID.ObjID))
                            {
                                await table.AddItem(new SituationItem()
                                {
                                    SitID = newItem.SitID.ObjID,
                                    Comm = newItem.Comm,
                                    SitPrior = newItem.SitPrior,
                                    CodeName = newItem.CodeName,
                                    SitName = newItem.SitName
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
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateSituation(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = SituationItemForFire.Parser.ParseFrom(value);
                    if (newItem != null && newItem.SitID?.SubsystemID == SubsystemID)
                    {
                        if (SelectItem != null && SelectItem.SitID == newItem.SitID?.ObjID)
                        {
                            SelectItem.SitName = newItem.SitName;
                            SelectItem.SitPrior = newItem.SitPrior;
                            SelectItem.Comm = newItem.Comm;
                            SelectItem.CodeName = newItem.CodeName;
                        }
                        await table.ForEachItems(x =>
                        {
                            if (x.SitID == newItem.SitID?.ObjID)
                            {
                                x.SitName = newItem.SitName;
                                x.SitPrior = newItem.SitPrior;
                                x.Comm = newItem.Comm;
                                x.CodeName = newItem.CodeName;

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
        }

        ItemsProvider<SituationItem> GetProvider => new ItemsProvider<SituationItem>(ThList, LoadChildList, request, new List<int>() { 40, 10, 10, 40 });

        private async ValueTask<IEnumerable<SituationItem>> LoadChildList(GetItemRequest req)
        {
            List<SituationItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ISituation", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<SituationItem>>() ?? new();
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSitNameForList", new OBJIDAndStr() { OBJID = new(request.ObjID) { ObjID = request.NObjType }, Str = req.BstrFilter }, ComponentDetached);
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


        void ViewCreateDialog(bool isCreate)
        {
            if (isCreate && SelectItem != null)
            {
                SelectItem = null;
            }
            IsCreateSit = true;
        }

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task DeleteSituation()
        {
            OBJ_ID obj = new OBJ_ID() { ObjID = SelectItem?.SitID ?? new(), StaffID = request.ObjID.StaffID, SubsystemID = request.ObjID.SubsystemID };
            var r = new OBJIDAndStr() { OBJID = obj, Str = SelectItem?.SitName };

            string? sName = SelectItem?.SitName;
            if (SelectItem != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteSituation", r);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", StartUIRep["IDS_STRING_SIT_DEL_ERR"] + " " + sName);
                }
                else
                {
                    MessageView?.AddMessage(GsoRep["IDS_REG_SIT_DELETE"], AsoRep["IDS_OK_DELETE"] + " " + sName);
                }

            }
            SelectItem = null;
            IsDelete = false;
        }

        private async Task GetReport()
        {
            OBJ_ID ReportId = new OBJ_ID() { ObjID = 1, SubsystemID = 0 };

            ReportInfo? RepInfo = null;

            var result = await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID });
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
            List<SitReport>? sitReports = null;

            result = await Http.PostAsJsonAsync("api/v1/GetSitReportAso", new GetItemRequest(request) { CountData = 0 });
            if (result.IsSuccessStatusCode)
            {
                sitReports = await result.Content.ReadFromJsonAsync<List<SitReport>>();
            }

            if (sitReports == null)
            {
                MessageView?.AddError(StartUIRep["IDS_PRINT"], Rep["NoData"]);
                return;
            }

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 1, sitReports, null, _OtherForReport.FiltrOther(FiltrItemsToString));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", $"SitReport{(SubsystemID == SubsystemType.SUBSYST_ASO ? "Aso" : SubsystemID == SubsystemType.SUBSYST_SZS ? "Szs" : "Cu")}.html", streamRef);
        }

        private void DbClick()
        {
            if (SelectItem == null) return;
            ViewCreateDialog(false);
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
