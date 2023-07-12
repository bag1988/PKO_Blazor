using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using SharedLibrary.Extensions;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class ActivNotifySticky : IAsyncDisposable
    {
        bool IsResultView = false;

        DateTime StartDate = DateTime.Now;

        TimeSpan NotificationGoes = new(0, 0, 0);

        readonly System.Timers.Timer timer = new(1000);

        protected override async Task OnInitializedAsync()
        {
            await GetStartTime();
            timer.Elapsed += (sender, eventArgs) =>
            {
                NotificationGoes = (DateTime.Now - StartDate);
                StateHasChanged();
            };
            if (timer != null)
                timer.Start();
        }


        async Task GetStartTime()
        {
            await Http.PostAsync("api/v1/GetStartCommandTime", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var t = await x.Result.Content.ReadFromJsonAsync<Timestamp>() ?? new();
                    StartDate = t.ToDateTime().ToLocalTime();
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            timer.Stop();
            await timer.DisposeAsync();
        }
    }
}
