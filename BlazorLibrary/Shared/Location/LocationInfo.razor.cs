using System.Net.Http.Json;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Location
{
    partial class LocationInfo
    {
        [Parameter]
        public int? LocationID { get; set; }

        [Parameter]
        public EventCallback Callback { get; set; }

        private ActualizeLocationListItem? Model = null;

        private int StaffId = 0;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            if (LocationID != null)
                await GetInfo();
            else
                Model = new();
        }

        private async Task GetInfo()
        {
            if (LocationID != null)
            {
                await Http.PostAsJsonAsync("api/v1/GetLocationInfo", new OBJ_ID() { ObjID = LocationID ?? 0, StaffID = StaffId }).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        Model = await x.Result.Content.ReadFromJsonAsync<ActualizeLocationListItem>();
                    }
                    else
                    {
                        Model = new();
                        MessageView?.AddError(GsoRep["IDS_STRING_LOCATION"], DeviceRep["ErrorGetInfo"]);
                    }
                });
            }
        }

        private async Task CallbackInvoke()
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync();
        }
    }

}
