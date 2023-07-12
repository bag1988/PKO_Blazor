using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary;
using DocumentFormat.OpenXml.EMMA;

namespace DeviceConsole.Client.Pages.Settings
{
    partial class SettingsSystem
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 1;

        private ParamsSystem? Model;

        private List<SituationItem>? ListSit = null;

        private bool IsSave = false;

        private bool IsViewDir = false;

        bool IsChangeStafID = false;

        private string TestText = "";

        private string? SelectNameFields = null;

        readonly GetItemRequest request = new() { ObjID = new OBJ_ID(), LSortOrder = 0, BFlagDirection = 0 };

        private List<string>? SelectDir = null;

        private List<string[]>? ChildDirectories = null;

        private string? IpAdress = null;
        private int? Port = null;

        bool IsProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            TestText = GsoRep["IDS_STRING_SINTEZ_EXAMPLE"];
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemType.SUBSYST_ASO;
            request.NObjType = await _User.GetUserSessId();
            await GetAppPortInfo();
            await GetParamSystem();
            await GetChildDirectories();
            await GetListSit();
        }

        private async Task GetListSit()
        {
            await Http.PostAsJsonAsync("api/v1/GetItems_ISituation", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ListSit = await x.Result.Content.ReadFromJsonAsync<List<SituationItem>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", StartUIRep["IDS_SITLVCAPTION"] + "-" + AsoRep["IDS_STRING_ERR_GET_DATA"]);
                    ListSit = new();
                }
            });
        }

        async Task GetAppPortInfo()
        {
            await Http.PostAsJsonAsync("api/v1/GetAppPortInfo", new BoolValue() { Value = false }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var response = await x.Result.Content.ReadFromJsonAsync<AppPorts>() ?? new();

                    Port = response.GATESERVICEAPPPORT;
                }
            });
        }

        private async Task GenerateStaffId()
        {
            BoolValue? response = new() { Value = false };
            await Http.PostAsync("api/v1/GenerateStaffId", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<BoolValue>();
                }
            });

            if (response?.Value == true)
            {
                if (MessageView != null)
                    MessageView.AddCallback = new EventCallback(this, async () =>
                    {
                        await AuthenticationService.Logout();
                    });
                MessageView?.AddMessage("", GsoRep["OK_UPDATE_STAFFID"]);
            }
            else
            {
                MessageView?.AddError("", GsoRep["ERROR_UPDATE_STAFFID"]);
            }
            IsChangeStafID = false;
        }

        private async Task GetParamSystem()
        {
            var result = await Http.PostAsync("api/v1/GetParamsList", null);
            if (result.IsSuccessStatusCode)
            {
                Model = await result.Content.ReadFromJsonAsync<ParamsSystem>();

                if (!string.IsNullOrEmpty(Model?.LocalIpAddress))
                {
                    IpAddressUtilities.ParseEndPoint(Model.LocalIpAddress, out IpAdress, out int? port);
                }
            }
            else
            {
                MessageView?.AddError("", AsoRep["IDS_STRING_ERR_GET_DATA"]);
                Model = new();
            }
        }

        private async Task SetParamsList()
        {
            if (Model == null)
                return;

            IsSave = false;

            IsProcessing = true;

            Model.LocalIpAddress = IpAdress + (Port > 0 ? $":{Port}" : "");

            IpAddressUtilities.ParseEndPoint(Model.LocalIpAddress, out IpAdress, out int? port);
            if (string.IsNullOrEmpty(IpAdress))
                Model.LocalIpAddress = null;

            var result = await Http.PostAsJsonAsync("api/v1/SetParamsList", Model);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<BoolValue>();

                if (r?.Value == true)
                {
                    IsSave = true;
                    _ = Task.Delay(2000).ContinueWith(x =>
                    {
                        IsSave = false;

                        StateHasChanged();
                    });
                }
            }

            if (!IsSave)
            {
                MessageView?.AddError("", AsoRep["IDS_E_REPORTER_SAVE"]);
            }

            IsProcessing = false;
        }

        private async Task GetChildDirectories()
        {
            await Http.PostAsJsonAsync("api/v1/GetChildDirectories", SelectDir ?? new()).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        ChildDirectories = await x.Result.Content.ReadFromJsonAsync<List<string[]>>() ?? new();
                    }
                    else
                    {
                        IsViewDir = false;
                        MessageView?.AddError(DeviceRep["PathRecord"], AsoRep["IDS_STRING_ERR_GET_DATA"]);
                    }

                });
        }

        private async Task ChangeSelectDir(List<string>? e)
        {
            SelectDir = e;
            await GetChildDirectories();
        }

        private async Task GoBackDir(string? item = null)
        {
            if (SelectDir == null || !SelectDir.Any())
                return;

            if (item == null)
            {
                SelectDir = null;
            }
            else
            {
                if (SelectDir.Count() == 1)
                    SelectDir = null;
                else
                {

                    if (SelectDir.Any(x => x == item))
                    {
                        int startRemove = SelectDir.IndexOf(item) + 1;
                        int count = SelectDir.Count - startRemove;


                        SelectDir.RemoveRange(startRemove, count);
                    }

                    if (string.IsNullOrEmpty(SelectDir.Last()))
                    {
                        SelectDir.Remove(SelectDir.Last());
                    }
                }
            }
            await GetChildDirectories();
        }

        private async Task ViewFolder(string? NameFields)
        {
            var param = Model?.GetType().GetProperties().FirstOrDefault(x => x.Name == NameFields) ?? null;

            if (param != null)
            {
                var v = param.GetValue(Model, null);
                if (!string.IsNullOrEmpty(v?.ToString()))
                {
                    SelectDir = v?.ToString()?.Split("/").ToList();
                }
                else
                {
                    SelectDir = null;
                }
            }

            IsViewDir = true;
            await GetChildDirectories();
            SelectNameFields = NameFields;
            StateHasChanged();
        }

        private void SetNewPathToParam()
        {
            var param = Model?.GetType().GetProperties().FirstOrDefault(x => x.Name == SelectNameFields);

            if (param != null)
            {
                string? newValue = null;
                if (SelectDir != null)
                    newValue = string.Join("/", SelectDir);
                param.SetValue(Model, newValue);
            }
            IsViewDir = false;
        }
    }
}
