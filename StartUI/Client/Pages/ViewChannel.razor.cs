using System.ComponentModel;
using System.Net.Http.Json;
using BlazorLibrary;
using BlazorLibrary.Models;
using AsoDataProto.V1;
using SharedLibrary;
using SMDataServiceProto.V1;
using StartUI.Client.Shared;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using SMP16XProto.V1;
using System.Runtime.CompilerServices;
using SharedLibrary.Interfaces;

namespace StartUI.Client.Pages
{
    partial class ViewChannel : IAsyncDisposable, IPubSubMethod
    {
        private Dictionary<ChannelGroup, List<ChannelGroup>>? ViewModel = null;

        private List<ChannelInfo>? ChannelInfoList = null;

        private List<ChannelContainer>? contrInfo = null;

        private Dictionary<int, string> ThList = new();

        private readonly GetItemRequest request = new() { ObjID = new OBJ_ID() { SubsystemID = SubsystemType.SUBSYST_ASO }, LSortOrder = 0, BFlagDirection = 1 };

        private ChannelGroup? SelectItem = null;

        private bool IsPageLoad = true;

        TableVirtualize<ChannelInfo>? table;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_CHANNEL_NAME"] },
                { 1, StartUIRep["IDS_CHANNEL_STATE"] },
                { 2, StartUIRep["IDS_C_NOTREADYLINE"] },
                { 3, StartUIRep["IDS_C_NOTCONTROLLER"] },
                { 4, StartUIRep["IDS_C_ANSWER"] },
                { 5, StartUIRep["IDS_C_NOANSWER"] },
                { 6, StartUIRep["IDS_C_ABBUSY"] },
                { 7, StartUIRep["IDS_C_ANSWER_DTMF"] },
                { 8, StartUIRep["IDS_C_ANSWER_TICKER"] },
                { 9, StartUIRep["IDS_C_ERROR_ATS"] },
                { 10, StartUIRep["IDS_C_ANSWER_FAX"] },
                { 11, StartUIRep["IDS_C_INTERERROR"] },
                { 12, StartUIRep["IDS_C_ANSWER_SETUP"] },
                { 13, StartUIRep["IDS_C_UNDEFINED_ANSWER"] },
                { 14, StartUIRep["IDS_C_INFO"] }
            };

            await UpdateList();
            IsPageLoad = false;
            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateChannels(ulong Value)
        {
            await UpdateList();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateControllingDevice(long Value)
        {
            await UpdateList();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteControllingDevice(long Value)
        {
            await UpdateList();
        }

        private async Task UpdateList()
        {
            await GetList();
            await ReplaceChannelInfo();
        }

        async Task RefreshData()
        {
            if (table != null)
                await table.ResetData();
        }

        ItemsProvider<ChannelInfo> GetProvider => new ItemsProvider<ChannelInfo>(ThList, LoadChildList, request, new List<int>() { 5, 10 });

        private ValueTask<IEnumerable<ChannelInfo>> LoadChildList(GetItemRequest req)
        {
            SortList.Sort(ref ChannelInfoList, req.LSortOrder, req.BFlagDirection);
            return new(ChannelInfoList?.Skip(req.SkipItems).Take(req.CountData) ?? new List<ChannelInfo>());
        }

        IEnumerable<ChildItems<ChannelGroup>>? GetTopLevel
        {
            get
            {
                List<ChildItems<ChannelGroup>> response = new();
                if (ViewModel != null)
                {
                    response.Add(new ChildItems<ChannelGroup>(new ChannelGroup()
                    {
                        Name = StartUIRep["AllDevice"],
                        ObjId = new()
                    },
                    GetItems));
                }
                return response;
            }
        }

        IEnumerable<ChildItems<ChannelGroup>>? GetItems(ChannelGroup item)
        {

            List<ChildItems<ChannelGroup>> response = new();
            if (ViewModel != null)
            {
                response.AddRange(ViewModel.Select(x => new ChildItems<ChannelGroup>(x.Key, GetChildItems)));
            }
            return response;
        }

        IEnumerable<ChildItems<ChannelGroup>>? GetChildItems(ChannelGroup item)
        {
            List<ChildItems<ChannelGroup>> response = new();
            if ((ViewModel?.ContainsKey(item) ?? false))
            {
                response.AddRange(ViewModel[item].Select(x => new ChildItems<ChannelGroup>(x)));
            }
            return response;
        }

        private async Task SetSelectItem(List<ChannelGroup>? items)
        {
            SelectItem = items?.LastOrDefault();
            await ReplaceChannelInfo();
        }

        private async Task GetChannelInfo()
        {
            OBJ_ID request = new();
            if (SelectItem != null)
            {
                request = SelectItem.ObjId;
            }

            var result = await Http.PostAsJsonAsync("api/v1/GetChannelInfoList", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                contrInfo = await result.Content.ReadFromJsonAsync<List<ChannelContainer>>();

                //Отображаем только подключенные каналы
                if (MainLayout.Settings.ChannelConn == true && (contrInfo?.Any() ?? false))
                {
                    contrInfo = contrInfo.Where(x => x.ContrInfo.LChannelStatus > 0).ToList();
                }
            }
            else
                MessageView?.AddError("", StartUIRep["IDS_ERRCHANNELINFO"]);
        }

        private async Task ReplaceChannelInfo()
        {
            if (contrInfo?.Any() ?? false)
            {
                var item = SelectItem;

                if (item == null || item.ObjId == null || item.ObjId.ObjID == 0)
                {
                    ChannelInfoList = new List<ChannelInfo>(contrInfo.Select(x => x.Info));
                }
                else
                {
                    int startPos = item.Temp * item.CountCh - item.CountCh;
                    int endPos = item.Temp * item.CountCh;
                    if (item.Temp == -1)
                    {
                        startPos = 0;
                        endPos = 99;
                    }
                    ChannelInfoList = new List<ChannelInfo>(contrInfo.Where(x => x.ContrInfo.LChannelID <= endPos && x.ContrInfo.LChannelID > startPos && x.ContrInfo.NDevID == item.ObjId.ObjID).Select(x => x.Info));
                }
                ChannelInfoList.ForEach(x =>
                {
                    x.ChInfo = !string.IsNullOrEmpty(x.ChInfo) ? StartUIRep[x.ChInfo] : "";
                });
            }
            else
                ChannelInfoList = new();

            await RefreshData();
        }


        private async Task GetList()
        {
            var result = await Http.PostAsync("api/v1/GetChannelGroupItem", null, ComponentDetached);
            try
            {
                ViewModel = new();
                if (result.IsSuccessStatusCode)
                {
                    var Model = await result.Content.ReadFromJsonAsync<List<ChannelGroup>>() ?? new();

                    foreach (var item in Model.Where(x => x.Temp == -1))
                    {
                        ViewModel.Add(new ChannelGroup(item)
                        {
                            Name = item.Name + "/" + (item.Status == 0 ? AsoDataRep["IDS_STRING_OFFED"] : item.Status == 1 ? AsoDataRep["IDS_STRING_ACTIVED"] : AsoDataRep["IDS_STRING_UNKNOWN_STATE"]) + " /" + item.CountCh + " " + AsoDataRep["IDS_STRING_BY_CHANNELS"]
                        },
                        Model.Where(x => x.Temp != -1 && x.ObjId.Equals(item.ObjId)).Select(x => new ChannelGroup(x)
                        {
                            Name = x.Name + " " + AsoDataRep["IDS_STRING_CONTROLLER"] + " №" + x.Temp + "/" + (x.Status == 0 ? AsoDataRep["IDS_STRING_NOT_USED"] : x.Status == 1 ? AsoDataRep["IDS_STRING_USED"] : "")
                        }).ToList());
                    }
                }
                else
                {
                    MessageView?.AddError(AsoRep["IDS_ERRORCAPTION"], StartUIRep["IDS_ERRCHANNELINFO"]);
                    ViewModel = new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (ViewModel == null)
            {
                ViewModel = new();
            }
            await GetChannelInfo();
        }


        string GetChState(int state) => state switch
        {
            1 => StartUIRep["ASO_RC_ENABLE"],
            8 => StartUIRep["ASO_RC_HANDSET_REMOVED"],
            7 => StartUIRep["ASO_RC_READY"],
            6 => StartUIRep["ASO_RC_BUSYCHANNEL"],
            _ => StartUIRep["ASO_RC_DISABLE"]
        };

        private async Task GetReport()
        {
            await _GenerateReportChannels.GetReport(ChannelInfoList, SelectItem);
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
