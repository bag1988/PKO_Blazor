using System.ComponentModel;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using LibraryProto.Helpers;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace DeviceConsole.Client.Shared.Line
{
    partial class ViewLine : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private LineItem? SelectItem = null;

        TableVirtualize<LineItem>? table;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_NAME"] },
                { 1, GsoRep["IDS_STRING_AB_NUMBER"] },
                { 2, GsoRep["IDS_STRING_LOCATION"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep["IDS_STRING_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));

            HintItems.Add(new HintItem(nameof(FiltrModel.Number), GsoRep["IDS_STRING_AB_NUMBER"], TypeHint.Number));

            HintItems.Add(new HintItem(nameof(FiltrModel.Location), GsoRep["IDS_STRING_LOCATION"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpLocationName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrLine);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateLine(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteLine(long Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<LineItem> GetProvider => new ItemsProvider<LineItem>(ThList, LoadChildList, request, new List<int>() { 50, 20, 30 });

        private async ValueTask<IEnumerable<LineItem>> LoadChildList(GetItemRequest req)
        {
            List<LineItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ILine", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<LineItem>>() ?? new();
            }
            else
            {
                MessageView?.AddError(AsoDataRep["IDS_STRING_LINE_COMMENT"], GSOFormRep["IDS_EGETLINETYPE"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetLineNameForLine", new IntAndString() { Number = SubsystemID, Str = req.BstrFilter }, ComponentDetached);
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

        private async ValueTask<IEnumerable<Hint>> LoadHelpLocationName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetLocationNameForLine", new IntAndString() { Number = SubsystemID, Str = req.BstrFilter }, ComponentDetached);
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
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task DeleteLine()
        {
            if (SelectItem != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/DeleteLine", new IntID() { ID = SelectItem.LineID });
                if (result.IsSuccessStatusCode)
                {
                    var r = await result.Content.ReadFromJsonAsync<BoolValue>();
                }
                else
                {
                    MessageView?.AddError(AsoDataRep["IDS_STRING_LOCATION_COMMENT"], AsoRep["IDS_ERRORCAPTION"]);
                }
                IsDelete = false;
                SelectItem = null;
            }
        }

        private void CallBackEvent(bool? update)
        {
            IsViewEdit = false;
            SelectItem = null;
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
