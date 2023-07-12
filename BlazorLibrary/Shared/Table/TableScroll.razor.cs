using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace BlazorLibrary.Shared.Table
{
    partial class TableScroll<TItem>
    {     
        [Parameter]
        public int? MaxHeight { get; set; }

        [Parameter]
        public bool IsSetMaxHeight { get; set; } = true;

        [Parameter]
        public EventCallback ScrollAction { get; set; }

        public ElementReference? div;

        public int WindowHeight = 800;

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (MaxHeight != null)
                {
                    WindowHeight = MaxHeight.Value;
                }
                else if (IsSetMaxHeight)
                {
                    await Task.Yield();
                    var d = await JSRuntime.InvokeAsync<double>("GetWindowHeight", div);
                    if (Devision != null && Devision != 0 && d > 0)
                    {
                        d = d / Devision.Value;
                    }
                    WindowHeight = (int)Math.Truncate(d);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            StateHasChanged();
        }

        public async Task OnScroll()
        {
            if (ScrollAction.HasDelegate)
            {
                var scrollY = await JSRuntime.InvokeAsync<double>("GetScrollElement", div);
                if (scrollY > 0.8)
                    await ScrollAction.InvokeAsync();
            }
        }
    }
}
