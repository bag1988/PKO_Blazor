using System.Net.Http.Json;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.Location
{
    partial class CreateLocation
    {
        [Parameter]
        public int? LocationID { get; set; }

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        private string TitleError = "";

        private ActualizeLocationListItem? Model = null;

        private int StaffId = 0;

        bool IsProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            TitleError = GsoRep[LocationID != null ? "IDS_REG_LOC_UPDATE" : "IDS_REG_LOC_INSERT"];
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
                var result = await Http.PostAsJsonAsync("api/v1/GetLocationInfo", new OBJ_ID() { ObjID = LocationID ?? 0, StaffID = StaffId });
                if (result.IsSuccessStatusCode)
                {
                    Model = await result.Content.ReadFromJsonAsync<ActualizeLocationListItem>();
                }
                else
                {
                    Model = new();
                    MessageView?.AddError(TitleError, DeviceRep["ErrorGetInfo"]);
                }
            }
        }


        private async Task SaveLocation()
        {
            if (Model != null)
            {
                IsProcessing = true;
                if (string.IsNullOrEmpty(Model.SzName))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + GsoRep["IDS_STRING_LOCATION"]);
                }
                else if (string.IsNullOrEmpty(Model.SzRegion))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + DeviceRep["CountryCode"]);
                    return;
                }
                else if (string.IsNullOrEmpty(Model.SzCity))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + DeviceRep["CityCode"]);
                }
                else if (string.IsNullOrEmpty(Model.SzInterNationalPrefix))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + DeviceRep["InterNationalPrefix"]);
                }
                else if (string.IsNullOrEmpty(Model.SzInterUrbanPrefix))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + DeviceRep["InterUrbanPrefix"]);
                }
                else if (!PrefixTest(Model.SzInterUrbanPrefix))
                {
                    MessageView?.AddError(TitleError, DeviceRep["InterUrbanPrefix"] + " " + DeviceRep["ErrorValue"]);
                }
                else if (!PrefixTest(Model.SzLocalCallPrefix))
                {
                    MessageView?.AddError(TitleError, DeviceRep["LocalCallPrefix"] + " " + DeviceRep["ErrorValue"]);
                }
                else if (!string.IsNullOrEmpty(Model.SzLocalCallPrefix) && string.IsNullOrEmpty(Model.SzLocalATS))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorLocalCall"]);
                }
                else
                {
                    Model.StaffID = StaffId;

                    var result = await Http.PostAsJsonAsync("api/v1/SetLocationInfo", Model);
                    if (result.IsSuccessStatusCode)
                    {
                        await CallEvent(true);
                    }
                }
            }
            IsProcessing = false;
        }


        private bool PrefixTest(string pText)
        {
            foreach (var item in pText.ToCharArray())
            {
                if ((item >= '0' && item <= '9') || item == 'w' || item == 'W' || item == ',' || (item >= 'A' && item <= 'F') || item == '*')
                    continue;
                else
                    return false;
            }
            return true;
        }

        private async Task Close()
        {
            await CallEvent(null);
        }

        private async Task CallEvent(bool? b)
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(b);
        }

    }
}
