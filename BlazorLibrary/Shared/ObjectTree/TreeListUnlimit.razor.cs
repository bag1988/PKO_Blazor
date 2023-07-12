using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.ObjectTree
{
    partial class TreeListUnlimit<TItem>
    {
        [Parameter]
        public RenderFragment<TItem>? ContentView { get; set; }

        [Parameter]
        public IEnumerable<ChildItems<TItem>>? Items { get; set; } = null;

        [Parameter]
        public EventCallback DbClick { get; set; }

        [Parameter]
        public EventCallback<List<TItem>> SetSelectList { get; set; }

        [Parameter]
        public EventCallback<TItem> SetCurrentItem { get; set; }

        [Parameter]
        public List<TItem>? SelectList { get; set; }

        [Parameter]
        public bool IsSetFocus { get; set; } = true;

        public ElementReference? Elem { get; set; }

        public bool _shouldPreventDefault = false;

        ViewTreeItem<TItem>? ElemTree { get; set; }

        protected override async Task OnInitializedAsync()
        {
            if (IsSetFocus)
            {
                await Task.Yield();
                Elem?.FocusAsync();
            }
        }

        public void SetFocus()
        {
            Elem?.FocusAsync();
        }

        public async Task KeySet(KeyboardEventArgs e)
        {
            _shouldPreventDefault = false;
            if (e.Code == "Enter")
            {
                await DbCallback();
                return;
            }
            else if (e.Code == "ArrowUp" || e.Code == "ArrowDown")
            {
                if (Items == null)
                    return;
                _shouldPreventDefault = true;

                if (ElemTree != null)
                {
                    ElemTree.SetLevel(e);
                    _ = JSRuntime?.InvokeVoidAsync("ScrollToSelectElement", Elem, ".bg-select");
                }
            }
            else if (e.Code == "ArrowLeft" || e.Code == "ArrowRight")
            {
                _shouldPreventDefault = false;
                if (ElemTree != null)
                {
                   await ElemTree.ViewLevel(e);
                }

            }
        }
        

        public async Task DbCallback()
        {
            if (DbClick.HasDelegate)
            {
                if (SelectList != null)
                {
                    await DbClick.InvokeAsync(SelectList.LastOrDefault());
                }
                return;
            }
        }


        async Task SetSelectCallback(List<TItem>? item)
        {
            if (SetSelectList.HasDelegate)
                await SetSelectList.InvokeAsync(item);
            else
            {
                SelectList = item;
            }
        }
    }
}
