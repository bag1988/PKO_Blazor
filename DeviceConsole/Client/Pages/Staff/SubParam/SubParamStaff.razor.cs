using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using StaffDataProto.V1;
using SharedLibrary;
using SharedLibrary.Models;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.Staff.SubParam
{
    partial class SubParamStaff
    {
        private StaffSubsystemParam? Model = null;

        private StaffSubsystemParam? OldModel = new();

        private bool IsUpdate = false;
        bool IsProcessing = false;
        private int StaffId = 0;
        private int SubsystemID = 0;

        private bool m_BNotifyStaff = false;

        private string? m_geolocation = "";

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            SubsystemID = SubsystemType.SUBSYST_GSO_STAFF;

            await GetSubsystemParam();
            await GetParams();
            await GetGeolocation();

            OldModel = new StaffSubsystemParam(Model);
        }

        private async Task GetSubsystemParam()
        {
            await Http.PostAsJsonAsync("api/v1/GetSubsystemParamStaff", new OBJ_ID() { StaffID = StaffId, SubsystemID = SubsystemID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    Model = await x.Result.Content.ReadFromJsonAsync<StaffSubsystemParam>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_EGETSUBSYSTPARAMS"]);
                }
            });
        }


        private async Task GetGeolocation()
        {
            await Http.PostAsJsonAsync("api/v1/GetGeolocation", new IntID() { ID = StaffId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var g = await x.Result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.String>() ?? new();
                    m_geolocation = g.Value;
                }
                else
                {
                    MessageView?.AddError("", GSOFormRep["IDS_E_GET_GEOLOCATION"]);
                }
            });
        }

        private async Task GetParams()
        {
            await Http.PostAsJsonAsync("api/v1/GetParams", new StringValue() { Value = nameof(ParamsSystem.NotifyStaff) }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var g = await x.Result.Content.ReadFromJsonAsync<StringValue>() ?? new();
                    m_BNotifyStaff = g.Value == "1" ? true : false;
                }
                else
                {
                    MessageView?.AddError("", AsoRep["IDS_STRING_ERR_GET_DATA"]);
                    MessageView?.AddMessage("", GsoRep["NoGeolocation"]);
                }
            });
        }


        private async Task SetParams()
        {
            var result = await Http.PostAsJsonAsync("api/v1/SetParams", new SetParamRequest() { ParamName = nameof(ParamsSystem.NotifyStaff), ParamValue = m_BNotifyStaff == true ? "1" : "0" });
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.String>();
                if (string.IsNullOrEmpty(r?.Value))
                {
                    MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
                }
                else
                {
                    if (!IsUpdate)
                    {
                        IsUpdate = true;
                        _ = Task.Delay(2000).ContinueWith(x =>
                        {
                            IsUpdate = false; StateHasChanged();
                        });
                    }
                }
            }
            else
                MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);

        }

        private async Task SetGeolocation()
        {
            var result = await Http.PostAsJsonAsync("api/v1/SetGeolocation", new CSetGeolocation() { Geolocation = m_geolocation, StaffId = StaffId });
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<BoolValue>();
                if (r?.Value != true)
                {
                    MessageView?.AddError("", GSOFormRep["IDS_E_SET_GEOLOCATION"]);
                }
                else
                {
                    if (!IsUpdate)
                    {
                        IsUpdate = true;
                        _ = Task.Delay(2000).ContinueWith(x =>
                        {
                            IsUpdate = false; StateHasChanged();
                        });
                    }
                }
            }
            else
                MessageView?.AddError("", GSOFormRep["IDS_E_SET_GEOLOCATION"]);
        }

        private async Task SaveParam()
        {
            IsProcessing = true;
            IsUpdate = false;
            if (Model != null && (!OldModel?.Equals(Model) ?? false))
            {
                if (Model.RedrawTimeout == 0)
                {
                    MessageView?.AddError("", AsoRep["ERR_REDRAW_TIME"]);
                }
                else
                {
                    var result = await Http.PostAsJsonAsync("api/v1/UpdateSubsystemParamStaff", Model);
                    if (!result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError("", GSOFormRep["IDS_EFAILUPDATESUBSYSTPARAMS"]);
                    }
                    else
                    {
                        IsUpdate = true;
                        _ = Task.Delay(2000).ContinueWith(x =>
                        {
                            IsUpdate = false; StateHasChanged();
                        });
                    }
                }
            }
            await SetGeolocation();
            await SetParams();

            IsProcessing = false;
        }
    }
}
