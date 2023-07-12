using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;
using SharedLibrary;

namespace StartUI.Client.Pages
{
    partial class ViewProcessing : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public int ActivTab { get; set; } = 1;

        [Parameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<CStatistic>? Model = null;

        protected override async Task OnInitializedAsync()
        {
            await GetList();

            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateStatListCache(byte[] Value)
        {
            if (ActivTab == 2)
                await GetList();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (ActivTab != 1)
                await GetList();
        }

        private async Task GetList()
        {
            string url = "";

            switch (ActivTab)
            {
                case 2: url = "api/v1/GetStaticsInfo"; break;
                case 3: url = "api/v1/GetResultsInfo"; break;
            }

            if (!string.IsNullOrEmpty(url))
            {
                await Http.PostAsJsonAsync(url, SubsystemID).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        Model = await x.Result.Content.ReadFromJsonAsync<List<CStatistic>>() ?? new();
                    }
                    else
                        Model = new List<CStatistic>();


                });
                StateHasChanged();
            }
        }

        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }
    }
}
