using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;

namespace ARMSettings.Client.Pages.SecuritySubSystem
{
    partial class AddSecurityGroup
    {
        [Parameter]
        public EventCallback<List<SecurityGroup>?> ActionBack { get; set; }

        private List<SecurityGroup>? SecurityGroupList { get; set; }
        private List<SecurityGroup>? SelectedSecurityGroupList { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await GetSecurityGroups();
        }

        private async Task GetSecurityGroups()
        {
            var x = await Http.PostAsync("api/v1/GetSecurityGroup", null);
            if (x.IsSuccessStatusCode)
            {
                SecurityGroupList = await x.Content.ReadFromJsonAsync<List<SecurityGroup>>() ?? new();
            }

            if (SecurityGroupList == null)
                SecurityGroupList = new();
        }
        private void AddItem(List<SecurityGroup>? item)
        {
            SelectedSecurityGroupList = item;
        }

        private async Task Confirm()
        {
            await ActionBack.InvokeAsync(SelectedSecurityGroupList);
        }

        private async Task Close()
        {
            await ActionBack.InvokeAsync(null);
        }
    }
}
