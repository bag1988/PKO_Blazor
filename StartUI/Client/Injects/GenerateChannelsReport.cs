using System.Net.Http.Json;
using BlazorLibrary;
using AsoDataProto.V1;
using GsoReporterProto.V1;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using ReplaceLibrary;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace StartUI.Client.Injects
{

    public class GenerateChannelsReport
    {
        private readonly HttpClient Http;
        private readonly IStringLocalizer<StartUIReplace> _StartUIRep;
        private readonly IStringLocalizer<ReplaceDictionary> _Rep;
        private readonly IStringLocalizer<ASODataReplace> _AsoDataRep;
        private readonly OtherInfoForReport _OtherForReport;
        private readonly IJSRuntime JSRuntime;
        public GenerateChannelsReport(HttpClient httpClient, IStringLocalizer<StartUIReplace> StartUIRep, IStringLocalizer<ReplaceDictionary> Rep, IStringLocalizer<ASODataReplace> AsoDataRep, OtherInfoForReport OtherForReport, IJSRuntime _JSRuntime)
        {
            Http = httpClient;
            _StartUIRep = StartUIRep;
            _Rep = Rep;
            _AsoDataRep = AsoDataRep;
            _OtherForReport = OtherForReport;
            JSRuntime = _JSRuntime;
        }

        public async Task CreateReport()
        {

            SettingApp Settings = new();
            await Http.PostAsync("api/v1/GetSetting", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    Settings = await x.Result.Content.ReadFromJsonAsync<SettingApp>() ?? new();
                }
                else
                    MessageView?.AddError("", _StartUIRep["IDS_ERRORCAPTION"]);
            });



            await Http.PostAsJsonAsync("api/v1/GetChannelInfoList", new OBJ_ID()).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var contrInfo = await x.Result.Content.ReadFromJsonAsync<List<ChannelContainer>>();

                    if (contrInfo == null || !contrInfo.Any())
                        return;

                    //Отображаем только подключенные каналы
                    if (Settings.ChannelConn == true)
                    {
                        contrInfo = contrInfo.Where(x => x.ContrInfo.LChannelStatus > 0).ToList();
                    }


                    var ChannelInfoList = new List<ChannelInfo>(contrInfo.Select(x => x.Info));

                    ChannelInfoList.ForEach(x =>
                    {
                        x.ChInfo = !string.IsNullOrEmpty(x.ChInfo) ? _StartUIRep[x.ChInfo] : "";
                    });

                    await GetReport(ChannelInfoList);


                }
                else
                    MessageView?.AddError("", _StartUIRep["IDS_ERRCHANNELINFO"]);
            });
        }


        public async Task GetReport(List<ChannelInfo>? ChannelInfoList, ChannelGroup? SelectedChild = null)
        {
            if (ChannelInfoList == null)
            {
                MessageView?.AddError(_StartUIRep["IDS_PRINT"], _Rep["NoData"]);
                return;
            }

            List<ChannelInfo> valueList = new(ChannelInfoList.Select(x => new ChannelInfo(x)));

            OBJ_ID ReportId = new OBJ_ID() { ObjID = 4, SubsystemID = SubsystemType.SUBSYST_ASO };

            ReportInfo? RepInfo = null;

            await Http.PostAsJsonAsync("api/v1/GetReportInfo", new IntID() { ID = ReportId.ObjID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    RepInfo = await x.Result.Content.ReadFromJsonAsync<ReportInfo>();
                }
            });


            if (RepInfo == null)
            {
                MessageView?.AddError(_StartUIRep["IDS_PRINT"], "ReportInfo " + _Rep["NoData"]);
                return;
            }

            List<GetColumnsExItem>? ColumnList = null;

            await Http.PostAsJsonAsync("api/v1/GetColumnsEx", ReportId).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ColumnList = await x.Result.Content.ReadFromJsonAsync<List<GetColumnsExItem>>();
                }
            });

            if (ColumnList == null)
            {
                MessageView?.AddError(_StartUIRep["IDS_PRINT"], "ColumnsEx " + _Rep["NoData"]);
                return;
            }


            if (ColumnList.FirstOrDefault(x => x.NColumnId == 100)?.NStatus == 1)
            {
                valueList = valueList.Where(x => x.ChState == 1).ToList();
            }

            string NameBloc = "";
            int CountChannel = 0;
            string State = "";
            string InfoStr = "";

            if (SelectedChild != null)
            {
                NameBloc = SelectedChild.Name.Split("/")?[0] ?? "";
                CountChannel = SelectedChild.CountCh;
                if (SelectedChild.Temp == -1)
                {

                    State = (SelectedChild.Status == 0 ? _AsoDataRep["IDS_STRING_OFFED"] : SelectedChild.Status == 1 ? _AsoDataRep["IDS_STRING_ACTIVED"] : _AsoDataRep["IDS_STRING_UNKNOWN_STATE"]);
                }
                else
                {
                    State = (SelectedChild.Status == 0 ? _AsoDataRep["IDS_STRING_NOT_USED"] : SelectedChild.Status == 1 ? _AsoDataRep["IDS_STRING_USED"] : "");
                }
            }


            valueList.ForEach(x =>
            {
                string stateName = "";
                switch (x.ChState)
                {
                    case 1:
                        stateName = _StartUIRep["ASO_RC_ENABLE"];
                        break;
                    case 8:
                        stateName = _StartUIRep["ASO_RC_HANDSET_REMOVED"];
                        break;
                    case 7:
                        stateName = _StartUIRep["ASO_RC_READY"];
                        break;
                    case 6:
                        stateName = _StartUIRep["ASO_RC_BUSYCHANNEL"];
                        break;
                    default:
                        stateName = _StartUIRep["ASO_RC_DISABLE"];
                        break;
                }
                x.ChInfo += "/" + stateName;
            });

            var html = ReportGenerate.GetReportForProto(RepInfo, ColumnList, 4, valueList, _OtherForReport.ChannelsOther(NameBloc, "", CountChannel, State, InfoStr));

            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(html));

            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "ChannelInfo.html", streamRef);
        }
    }
}
