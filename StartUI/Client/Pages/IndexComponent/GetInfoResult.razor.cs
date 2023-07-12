using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class GetInfoResult
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 0;

        private List<string>? Info = null;

        protected override async Task OnInitializedAsync()
        {
            await GetInfo();
        }
        private async Task GetInfo()
        {
            Info = new();
            await Http.PostAsJsonAsync("api/v1/GetSessionTextResults", new IntID() { ID = SubsystemID }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var s = await x.Result.Content.ReadFromJsonAsync<StringValue>() ?? new();

                    if (!string.IsNullOrEmpty(s.Value))
                    {
                        Info = s.Value.Split("\n").ToList();
                    }
                }
            });
        }
    }
}
