using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using StartUI.Client.Shared;
using BlazorLibrary.Shared.Table;
using SMDataServiceProto.V1;
using BlazorLibrary.Models;
using static BlazorLibrary.Shared.Main;
using LibraryProto.Helpers;
using FiltersGSOProto.V1;
using BlazorLibrary.Helpers;
using BlazorLibrary.GlobalEnums;
using SharedLibrary;
using SharedLibrary.Interfaces;
using System.ComponentModel;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class ViewSitListCache : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        public List<Situation> SelectList { get; set; } = new();

        private Dictionary<Situation, List<string>>? InfoList = null;
        private KeyValuePair<Situation, List<string>> TempItem = new();

        private bool IsConfirmSit = false;

        private bool IsViewInfoSit = false;

        private bool IsButtonViewInfo = false;

        private bool IsAddNewItem = false;

        private TimeSpan TimerCancel = TimeSpan.Zero;

        public TableVirtualize<Situation>? table;

        private int StaffId = 0;

        private int UserSessId = 0;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_SITNAME"] },
                { 1, StartUIRep["IDS_NOTIFYSTATUS"] }
            };

            StaffId = await _User.GetLocalStaff();
            UserSessId = await _User.GetUserSessId();

            request.ObjID.ObjID = UserSessId;
            request.ObjID.StaffID = StaffId;
            request.ObjID.SubsystemID = SubsystemID;

            HintItems.Add(new HintItem(nameof(FiltrModel.Situation), StartUIRep["IDS_SITNAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpSitName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrViewSit);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_Redraw_LV(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = OBJ_ID.Parser.ParseFrom(value);

                    if (newItem != null && newItem.SubsystemID == SubsystemID)
                    {
                        SelectList.ForEach(x =>
                        {
                            if (x.OBJID.Equals(newItem))
                            {
                                x.SeqNum = 0;
                                x.StatusId = 0;
                                return;
                            }
                        });

                        await table.ForEachItems(x =>
                        {
                            if (x.OBJID.Equals(newItem))
                            {
                                x.SeqNum = 0;
                                x.StatusId = 0;
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
                            SelectList.RemoveAll(x => x.OBJID.Equals(newItem.SitID));
                            await table.RemoveAllItem(x => x.OBJID.Equals(newItem.SitID));
                        }
                        else
                        {
                            if (!table.AnyItemMatch(x => x.OBJID.Equals(newItem.SitID)))
                            {
                                if (SubsystemID == SubsystemType.SUBSYST_GSO_STAFF && !string.IsNullOrEmpty(newItem.CodeName))
                                {
                                    newItem.SitName = $"({newItem.CodeName}) {newItem.SitName}";
                                }
                                await table.AddItem(new Situation()
                                {
                                    OBJID = newItem.SitID,
                                    SitPrior = newItem.SitPrior,
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
                        if (SubsystemID == SubsystemType.SUBSYST_GSO_STAFF && !string.IsNullOrEmpty(newItem.CodeName))
                        {
                            newItem.SitName = $"({newItem.CodeName}) {newItem.SitName}";
                        }

                        SelectList.ForEach(x =>
                        {
                            if (x.OBJID.Equals(newItem.SitID))
                            {
                                x.SitName = newItem.SitName;
                                x.SitPrior = newItem.SitPrior;
                                return;
                            }
                        });

                        await table.ForEachItems(x =>
                        {
                            if (x.OBJID.Equals(newItem.SitID))
                            {
                                x.SitName = newItem.SitName;
                                x.SitPrior = newItem.SitPrior;
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

        ItemsProvider<Situation> GetProvider => new ItemsProvider<Situation>(ThList, LoadChildList, request, new List<int>() { 60 });

        private async ValueTask<IEnumerable<Situation>> LoadChildList(GetItemRequest req)
        {
            List<Situation> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/S_CreateSitListCache", req);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<Situation>>() ?? new();
            }
            else
            {
                MessageView?.AddError(StartUIRep["IDS_SITLVCAPTION"], AsoRep["IDS_STRING_ERR_GET_DATA"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationForSitListCache", new IntAndString() { Number = request.ObjID.SubsystemID, Str = req.BstrFilter });
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
        private async Task RefreshTable()
        {
            await Task.Yield();
            SelectList.Clear();
            if (table != null)
                await table.ResetData();
        }

        private async Task SetSelectList(List<Situation>? items)
        {
            if (items == null)
            {
                InfoList = new();
                SelectList = new();
                return;
            }

            if (InfoList == null) InfoList = new();

            SelectList.RemoveAll(x => !items.Contains(x));

            foreach (var item in InfoList)
            {
                if (!items.Contains(item.Key))
                    InfoList.Remove(item.Key);
            }
            IsButtonViewInfo = false;
            if (items.Count == 1)
            {
                if (SelectList?.Count == 1 && SelectList.First().Equals(items.First()))
                    return;
                IsButtonViewInfo = true;
                InfoList = new();
                SelectList = new();
            }


            var newItems = items.Where(x => !SelectList.Contains(x)).ToList();

            foreach (var item in newItems)
            {
                TimerCancel = new TimeSpan(0, 0, 10);
                IsConfirmSit = MainLayout.Settings.SitConfirm ?? true;
                IsAddNewItem = false;
                TempItem = new(item, await GetInfoSit(item));
                StateHasChanged();
                while (IsConfirmSit && TimerCancel.TotalSeconds > 1)
                {
                    await Task.Delay(100);
                    if (!IsViewInfoSit)
                    {
                        TimerCancel = TimerCancel.Add(new TimeSpan(0, 0, 0, 0, -100));
                        StateHasChanged();
                    }
                }
                if (IsAddNewItem && !InfoList.ContainsKey(TempItem.Key))
                {
                    InfoList.Add(TempItem.Key, TempItem.Value);
                    SelectList.Add(item);
                }
                IsConfirmSit = false;
            }
        }

        private async Task<List<string>> GetInfoSit(Situation SelectItem)
        {
            List<string> InfoItem = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSituationDryInfo", SelectItem.OBJID);
            if (result.IsSuccessStatusCode)
            {
                var InfoStr = await result.Content.ReadFromJsonAsync<StringValue>() ?? new();
                if (!string.IsNullOrEmpty(InfoStr.Value))
                {
                    InfoItem = InfoStr.Value.Split("\n").ToList();
                }
            }
            return InfoItem;
        }

        public async Task Refresh()
        {
            await CallRefreshData();
        }

        public void ClearSelect()
        {
            SelectList.Clear();
            InfoList?.Clear();
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }

    }
}
