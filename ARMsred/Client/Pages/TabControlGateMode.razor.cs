using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace ARMsred.Client.Pages
{
    partial class TabControlGateMode
    {
        [Parameter]
        public EventCallback<P16xGroup?> SetGroupId { get; set; }

        private List<P16xGroup>? P16Groups;

        private P16xGroup? ActiveTab = null;

        protected override async Task OnInitializedAsync()
        {
            await GetList();
        }

        private async Task GetList()
        {
            P16Groups = null;
            //S_GetGroupList
            await Http.PostAsJsonAsync("api/v1/remote/SGetGroupList", new IntID() { ID = await _User.GetUserId() }, ComponentDetached).ContinueWith(async x =>
             {
                 if (x.Result.IsSuccessStatusCode)
                 {
                     P16Groups = await x.Result.Content.ReadFromJsonAsync<List<P16xGroup>>();

                     ActiveTab = P16Groups?.FirstOrDefault();

                     await SetGroupId.InvokeAsync(ActiveTab);
                 }
                 else
                     await SetGroupId.InvokeAsync();
             });

            if (P16Groups == null)
                P16Groups = new();
        }

    }
}
