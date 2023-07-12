using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Interfaces;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.NavLink
{
    partial class NavMenu : IAsyncDisposable
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? Title { get; set; }

        [Parameter]
        public int? Width { get; set; } = 250;

        private DateTime dateNow = DateTime.Now;

        private bool collapseNavMenu = true;

        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

        readonly System.Timers.Timer timer = new(1000);

        private bool IsViewAbout = false;

        private ProductVersion? PVersion = null!;

        private ElementReference div = default!;

        private int WindowHeight = 800;

        protected override async Task OnInitializedAsync()
        {
            await Task.Run(() =>
            {
                timer.Elapsed += (sender, eventArgs) =>
                {
                    dateNow = DateTime.Now;
                    StateHasChanged();
                };
                timer.Start();
            });
            await PVersionFull();
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await Task.Yield();
                var d = await JSRuntime.InvokeAsync<double>("GetWindowHeight", div);

                WindowHeight = (int)Math.Truncate(d);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task KeySet(KeyboardEventArgs e)
        {
            if (e.Code == "ArrowUp" || e.Code == "ArrowDown")
            {
                var index = e.Code == "ArrowUp" ? -1 : 1;
                await JSRuntime.InvokeVoidAsync("HotKeys.SetFocusLink", div, index);
            }
        }

        private async Task Logout()
        {
            await AuthenticationService.Logout();
        }

        private async Task PVersionFull()
        {
            var result = await Http.PostAsync("api/v1/allow/PVersionFull", null);
            if (result.IsSuccessStatusCode)
            {
                PVersion = await result.Content.ReadFromJsonAsync<ProductVersion>();
            }
        }

        string GetCompanyName
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }
                if (PVersion.CompanyName == "kae")
                    return Rep["KAE_NAME"];
                return Rep["SensorM"];
            }
        }

        string GetPoName
        {
            get
            {
                if (PVersion == null)
                {
                    return "";
                }
                if (PVersion.CompanyName == "kae")
                    return Rep["PO_NAME_KAE"];
                return Rep["PO_NAME_SENSOR"];
            }
        }

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        public ValueTask DisposeAsync()
        {
            timer.Stop();
            timer.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
