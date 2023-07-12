using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorLibrary.Shared.Modal
{
    partial class ModalDialog
    {
        [Parameter]
        public RenderFragment? BodyContent { get; set; }

        [Parameter]
        public RenderFragment? ButtonContent { get; set; }

        [Parameter]
        public bool RemoveDialog { get; set; } = false;

        [Parameter]
        public EventCallback ButtonCloseEvent { get; set; }

        [Parameter]
        public string? Title { get; set; }

        private ElementReference? elem { get; set; }

        private int ZIndex = 1051;

        protected override async Task OnInitializedAsync()
        {
            await Task.Yield();
            elem?.FocusAsync();
            ZIndex = await JSRuntime.InvokeAsync<int>("GetMaxIndexModal");
            ZIndex = ZIndex + 10;
            if (string.IsNullOrEmpty(Title))
                Title = Rep["Load"];

        }

        private async Task RemoveDialogAction()
        {
            if (RemoveDialog)
                await ButtonCloseAction();
        }

        private async Task ButtonCloseAction()
        {
            if (ButtonCloseEvent.HasDelegate)
                await ButtonCloseEvent.InvokeAsync();
        }
    }
}
