using BlazorLibrary.Shared;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorLibrary.FolderForInherits
{
    public class ButtonDefault : ComponentBase
    {
        [Parameter]
        public string? Text { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? AddClass { get; set; } = "m-1";

        [Parameter]
        public EventCallback OnClick { get; set; }

        [Parameter]
        public string? HotKey { get; set; }

        [Parameter]
        public bool? TextWrap { get; set; } = false;

        [Parameter]
        public bool? IsOutline { get; set; } = false;

        [Parameter]
        public bool IsProcessing { get; set; } = false;

        protected RenderFragment? _content
        {
            get
            {
                var r = ChildContent;
                if (IsProcessing == true)
                {
                    r = (builder) =>
                    {
                        builder.OpenElement(0, "span");
                        builder.AddAttribute(0, "class", "spinner-border spinner-border-sm me-2");
                        builder.AddAttribute(0, "role", "status");
                        builder.AddAttribute(0, "aria-hidden", "true");
                        builder.CloseElement();
                        builder.AddContent(1, Text);
                    };
                }
                else
                {
                    if (Text != null && r == null)
                    {
                        r = (builder) =>
                        {
                            builder.AddContent(0, Text);
                        };
                    }
                }
                return r;
            }
        }

        public async Task OnClickAction()
        {
            IsProcessing = true;
            if (OnClick.HasDelegate)
                await OnClick.InvokeAsync();
            IsProcessing = false;
        }
    }
}
