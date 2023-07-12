using System.Net.Http.Json;
using SMDataServiceProto.V1;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class ResultNotifySticky
    {
        private bool IsResultView = false;

        private string ResultTime = "";

        protected override async Task OnInitializedAsync()
        {
            await GetResultTime(0);
        }

        private async Task GetResultTime(int SessId)
        {
            await Http.PostAsJsonAsync("api/v1/GetSessTime", new IntID() { ID = SessId }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var re = await x.Result.Content.ReadFromJsonAsync<CGetSessIdTime>() ?? new();
                    ResultTime = StartUIRep["IDS_RESLVCAPTION"] + " " + re.Beg?.ToDateTime().ToLocalTime() + " - " + re.End?.ToDateTime().ToLocalTime();
                }
            });
        }

    }
}
