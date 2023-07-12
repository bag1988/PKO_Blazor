using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.Shared
{
    partial class ResultView<TItem>
    {
        [Parameter]
        public RenderFragment<TItem>? ChildContent { get; set; }

        [Parameter]
        public string? ClassItems { get; set; } = "my-1 px-2 py-1 pointer";

        [Parameter]
        public string? SetClass { get; set; } = string.Empty;
    }
}
