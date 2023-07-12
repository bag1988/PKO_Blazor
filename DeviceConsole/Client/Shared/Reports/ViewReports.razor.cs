using System.Net.Http.Json;
using BlazorLibrary;
using BlazorLibrary.Shared;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using GsoReporterProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeviceConsole.Client.Shared.Reports
{
    partial class ViewReports
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 1;

        private List<GetReportListItem>? Model = null;

        private List<GetReportColumnListItem>? Columns = null;

        private GetReportListItem? SelectItem = null;

        private ReporterFont? ReportFont = null;

        private bool IsUpdate = false;

        private string TitleName = "";

        protected override async Task OnInitializedAsync()
        {
            switch (SubsystemID)
            {
                case SubsystemType.SUBSYST_ASO: TitleName = RepoterRep["IDS_STRING_REPORT_SETTINGS_ASO"]; break;
                case SubsystemType.SUBSYST_GSO_STAFF: TitleName = RepoterRep["IDS_STRING_REPORT_SETTINGS_CU"]; break;
            }
            await GetList();
        }

        private async Task GetList()
        {
            Model = null;
            var result = await Http.PostAsJsonAsync("api/v1/GetReportList", new IntID() { ID = SubsystemID });
            if (result.IsSuccessStatusCode)
            {
                Model = await result.Content.ReadFromJsonAsync<List<GetReportListItem>>();

                SelectItem = Model?.FirstOrDefault();

                if (SelectItem != null)
                {
                    await GetReportColumnList(new List<GetReportListItem>() { SelectItem });
                }
            }

            if (Model == null)
                Model = new();
        }
        private void ChangeColumn(OBJ_ID? obj)
        {
            if (Columns != null)
            {
                var elem = Columns.FirstOrDefault(x => x.ObjId.Equals(obj));

                if (elem != null)
                {
                    elem.BNode = elem.BNode == 1 ? 0 : 1;
                }
            }
        }

        private async Task GetReportColumnList(List<GetReportListItem>? obj)
        {
            SelectItem = obj?.FirstOrDefault();
            if (SelectItem == null)
                return;
            Columns = null;
            IsUpdate = false;
            await GetReportFont(new OBJ_ID(SelectItem.ObjId));

            var result = await Http.PostAsJsonAsync("api/v1/GetReportColumnList", new OBJ_ID(SelectItem.ObjId));
            if (result.IsSuccessStatusCode)
            {
                Columns = await result.Content.ReadFromJsonAsync<List<GetReportColumnListItem>>() ?? new();
            }
            if (Columns == null)
            {
                Columns = new();
            }
            StateHasChanged();

        }

        private async Task CGsoReport()
        {
            IsUpdate = false;
            if (Columns != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/CGsoReport", Columns.Select(x => new CGsoReportItem() { BNode = x.BNode, LType = x.LType, ObjId = x.ObjId }).ToList());
                if (result.IsSuccessStatusCode)
                {
                    await SetFonts();
                }
                IsUpdate = true;
                _ = Task.Delay(2000).ContinueWith(x =>
                {
                    IsUpdate = false; StateHasChanged();
                });
            }
        }

        private async Task ViewReport()
        {
            if (SelectItem != null && ReportFont != null && Columns != null)
            {
                ReportInfo RepInfo = new ReportInfo() { Name = SelectItem.MName, Font = ReportFont.GetBase64() };


                List<GetColumnsExItem>? ColumnList = new(Columns.Select(x => new GetColumnsExItem() { NColumnId = x.ObjId.ObjID, NStatus = x.BNode, TContrName = x.MComment, TName = x.MName }));

                byte[]? html = null;
                switch (SelectItem.ObjId.ObjID)
                {
                    case 1:
                    {
                        List<SitReport> Model = new() { new SitReport() { SitName = "name", CodeName = "code", SitPrior = 1, MsgName = "name", CountObj = 0, TypeName = "type", Comm = "comm" } };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model);
                    }; break;
                    case 2:
                    {
                        List<AbonReport> Model = new() { new AbonReport() { AbName = "name", DepName = "dep", Position = "position", AbPrior = 1, StatusName = "status", TypeName = "type", LocName = "loc", ConnParam = "conn", Address = "address", AbComm = "comm" } };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model, _OtherForReport.AbonOther(0));
                    }; break;
                    case 3:
                    {
                        List<string> bodyContent = new();
                        if (ColumnList.FirstOrDefault(x => x.NColumnId == 3)?.NStatus == 1)
                        {
                            bodyContent.Add(ReportGenerate.CreateHtmlTable(new string[] { GsoRep["IDS_STRING_NAME"], AsoRep["Value"] }, _OtherForReport.AsoOther(DateTime.Now, DateTime.Now, 0, 1), GsoRep["IDS_SHARED_INFO"]));
                        }

                        List<AsoReportTime> Model = new() { new AsoReportTime() { SitName = StartUIRep["IDS_SITUATIONCOLUMN"], AbName = StartUIRep["IDS_ABONENTNAME"], DepName = StartUIRep["IDS_DEPARTMENT"], Position = "", AbPrior = 0, ResultName = "", Time = DateTime.Now.ToString("T"), ConnParam = "", CountCall = 0, MsgName = "" } };

                        List<string> sectionContent = new();
                        foreach (var groupItem in Model.GroupBy(x => x.SitName))
                        {
                            sectionContent.Add(ReportGenerate.CreateHtmlTableForProto(ColumnList, 3, groupItem.ToList(), groupItem.Key));
                        }

                        bodyContent.Add(ReportGenerate.CreateHtmlSection(string.Join("", sectionContent), GsoRep["IDS_SIT_INFO"]));
                        bool Center = ColumnList.FirstOrDefault(x => x.NColumnId == 201)?.NStatus == 0 ? false : true;
                        html = ReportGenerate.GetHtml(bodyContent, RepInfo, Center);
                        //html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model, _OtherForReport.AsoOther(DateTime.Now, DateTime.Now, 0, 0));
                    }; break;
                    case 4:
                    {
                        List<ChannelInfo> Model = new() { new ChannelInfo() /*{ ChName = 0, ChState = 0, ChNotReadyLine = 0, ChNotController = 0, ChAnswer = 0, ChNoAnswer = "", ChAbBusy = "", ChAnswerDtmf = "", ChAnswerTicker = "", ChErrorAts = "", ChAnswerFax = "", ChInterError = "", ChAnswerSetup = "", ChUndefinedAnswer = "", ChInfo = "" }*/ };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model, _OtherForReport.ChannelsOther());
                    }; break;
                    case 5:
                    {
                        List<CUResultView> Model = new() { new CUResultView() };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model, _OtherForReport.CUResultView("", DateTime.Now, DateTime.Now, 0, 0));
                    }; break;
                    case 6:
                    {
                        List<CSessions> Model = new() { new CSessions() { TSessBeg = DateTime.Now.ToUniversalTime().ToTimestamp(), TSessEnd = DateTime.Now.ToUniversalTime().ToTimestamp(), TSitName = "SitName" } };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model);
                    }; break;
                    case 7:
                    {
                        List<CUDetaliResult> Model = new() { new CUDetaliResult() { SitName = "", ObjName = "", ObjType = "", ObjDefine = "", StatusName = "", UnitName = "" } };
                        html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, SelectItem.ObjId.ObjID, Model, _OtherForReport.CUDetaliOther("", DateTime.Now, DateTime.Now));
                    }; break;
                }

                if (html != null)
                {
                    using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));
                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "report.html", streamRef);
                    streamRef.Dispose();
                }

            }
        }

        private async Task SetFonts()
        {
            if (SelectItem != null && ReportFont != null)
            {
                await Http.PostAsJsonAsync("api/v1/SetFonts", new SetFontModel() { Obj = SelectItem.ObjId, Font = ReportFont.GetBytes() });
            }
        }

        private async Task GetReportFont(OBJ_ID obj)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetReportFont", obj);
            if (result.IsSuccessStatusCode)
            {
                ReportFont = await result.Content.ReadFromJsonAsync<ReporterFont>();
            }

        }

    }
}
