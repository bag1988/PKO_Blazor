using System.Net.Http.Json;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.ASO.Department
{
    partial class CreateDepartment
    {
        [Parameter]
        public DepartmentAso? Model { get; set; } = new();

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        private string TitleError = "";
                
        private int StaffId = 0;
        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            TitleError = AsoRep[Model != null ? "IDS_REG_DEP_UPDATE" : "IDS_REG_DEP_INSERT"];
        }

        private async Task SaveDepartment()
        {
            if (Model != null)
            {
                if (string.IsNullOrEmpty(Model.DepName))
                {
                    MessageView?.AddError(TitleError, DeviceRep["ErrorNull"] + " " + AsoRep["IDS_DEPARTMENT"]);
                    return;
                }
                if (!await GetLocationInfo())
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_E_GETLOCATION"]);
                    return;
                }

                Model.StaffID = StaffId;

                await Http.PostAsJsonAsync("api/v1/SetDepartmentInfo", Model).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        await CallEvent(true);
                    }
                    else
                    {
                        MessageView?.AddError(TitleError, AsoRep["IDS_E_SAVEDEPARTMENT"]);
                    }
                });
            }
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


        private async Task<bool> GetLocationInfo()
        {
            bool response = false;
            await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var b = await x.Result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();

                    if (b.Count > 0)
                    {
                        response = true;
                    }
                }
            });
            return response;
        }
    }
}
