using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;

namespace ARMSettings.Client.Pages.SecuritySubSystem
{
    partial class AddSecurityParam
    {
        [Parameter]
        public EventCallback<List<SecurityParams>?> ActionBack { get; set; }
        private List<SecurityParams>? SecurityParamsList { get; set; }
        private List<SecurityParams>? SelectedSecurityParamsList { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await GetSecurityParams();
        }

        private async Task GetSecurityParams()
        {
            var x = await Http.PostAsync("api/v1/GetUserSecurityParams", null);
            if (x.IsSuccessStatusCode)
            {
                SecurityParamsList = await x.Content.ReadFromJsonAsync<List<SecurityParams>>() ?? new();
            }

            if (SecurityParamsList == null)
            {
                SecurityParamsList = new();
            }
        }

        private void AddItem(List<SecurityParams>? item)
        {
            SelectedSecurityParamsList = item;
        }
        private async Task Confirm()
        {
            await ActionBack.InvokeAsync(SelectedSecurityParamsList);
        }

        private async Task Close()
        {
            await ActionBack.InvokeAsync(null);
        }
    }
}
