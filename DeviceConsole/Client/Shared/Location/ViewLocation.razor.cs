using System.ComponentModel;
using System.Net.Http.Json;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using LibraryProto.Helpers;
using Google.Protobuf.WellKnownTypes;
using BlazorLibrary.FolderForInherits;
using BlazorLibrary.GlobalEnums;

namespace DeviceConsole.Client.Shared.Location
{
    partial class ViewLocation : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private LocationItem? SelectItem = null;

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        TableVirtualize<LocationItem>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_LOCATION_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Location), GsoRep["IDS_STRING_LOCATION_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpLocationName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrLocation);

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateLocation(ulong Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteLocation(ulong Value)
        {
            SelectItem = null;
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<LocationItem> GetProvider => new ItemsProvider<LocationItem>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<LocationItem>> LoadChildList(GetItemRequest req)
        {
            List<LocationItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ILocation", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<LocationItem>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", AsoRep["IDS_EGETLOCATIONLIST"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpLocationName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetLocationName", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        private async Task DeleteLocation()
        {
            if (SelectItem != null)
            {
                var obj = new OBJ_ID() { ObjID = SelectItem.LocationID, StaffID = request.ObjID.StaffID };

                var r = await LinkObject(obj);

                if (r != null && r.Count > 0)
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_DELETE_DENIDE"] + ", " + AsoRep["ERR_DELETE_DENIDE"].ToString().Replace("{name}", $"{GsoRep["IDS_STRING_LOCATION"]} {SelectItem.Name}"), r);
                }
                else
                {
                    var result = await Http.PostAsJsonAsync("api/v1/DeleteLocation", obj, ComponentDetached);
                    if (result.IsSuccessStatusCode)
                    {
                        SelectItem = null;
                    }
                    else
                        MessageView?.AddError(AsoDataRep["IDS_STRING_LOCATION_COMMENT"], AsoRep["IDS_ERRORCAPTION"]);
                }
            }
            IsDelete = false;
        }

        private async Task<List<string>?> LinkObject(OBJ_ID request)
        {
            List<string>? r = null;

            var result = await Http.PostAsJsonAsync("api/v1/ILocation_Aso_GetLinkObjects", request);
            if (result.IsSuccessStatusCode)
            {
                r = await result.Content.ReadFromJsonAsync<List<string>>();
            }
            return r;
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
