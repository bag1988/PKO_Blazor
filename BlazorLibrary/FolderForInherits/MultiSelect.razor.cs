using System.Numerics;
using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorLibrary.FolderForInherits
{
    public class MultiSelect<TItem> : ComponentBase
    {
        [Parameter]
        public IEnumerable<TItem>? Items { get; set; }

        [Parameter]
        public bool IsOnKeyDown { get; set; } = true;

        [Parameter]
        public EventCallback<List<TItem>?> SetSelectList { get; set; }

        [Parameter]
        public EventCallback<TItem?> DbClick { get; set; }

        [Parameter]
        public bool IsSetFocus { get; set; } = true;

        [Parameter]
        public List<TItem>? SelectList { get; set; }

        public ElementReference? Elem { get; set; }

        [Inject]
        IJSRuntime? JSRuntime { get; set; }

        public bool _shouldPreventDefault = false;

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

            if (!IsOnKeyDown)
            {
                return;
            }

            if (e.Code == "Enter")
            {
                await DbCallback();
                return;
            }
            else if (e.Code == "ArrowUp" || e.Code == "ArrowDown")
            {
                _shouldPreventDefault = true;
                if (Items == null || !Items.Any())
                    return;

                var index = e.Code == "ArrowUp" ? -1 : 1;

                TItem? newSelect;

                if (SelectList == null || SelectList.Count == 0)
                {
                    newSelect = Items.First();
                }
                else
                {
                    newSelect = Items.GetNextSelectItem(SelectList.Last(), index);
                }

                if (newSelect != null)
                {
                    if (SetSelectList.HasDelegate)
                    {
                        _ = SetSelectList.InvokeAsync(new() { newSelect });
                    }
                    else
                        SelectList = new() { newSelect };
                    _ = JSRuntime?.InvokeVoidAsync("ScrollToSelectElement", Elem, ".bg-select");
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

        public void AddSelectItem(MouseEventArgs e, TItem item)
        {
            if (SetSelectList.HasDelegate && (Items?.Any() ?? false))
            {
                if (SelectList == null) SelectList = new List<TItem>();

                var newSelect = new List<TItem>(SelectList);

                if (e.CtrlKey)
                {
                    if (newSelect.Contains(item))
                        newSelect.Remove(item);
                    else
                        newSelect.Add(item);
                }
                else if (e.ShiftKey && newSelect.Count > 0)
                {
                    var LastItem = newSelect.Last();
                    newSelect.Clear();

                    bool PlusOne = false;

                    newSelect.AddRange(Items.SkipWhile(x =>
                    {
                        if (x == null) return true;
                        if (x.Equals(item))
                        {
                            return false;
                        }
                        if (x.Equals(LastItem))
                        {
                            LastItem = item;
                            return false;
                        }
                        return true;
                    }).TakeWhile(x =>
                    {
                        if (x == null) return false;
                        if (x.Equals(LastItem))
                        {
                            PlusOne = true;
                            return true;
                        }
                        if (PlusOne)
                        {
                            return false;
                        }
                        return true;
                    }));
                }
                else
                    newSelect = new() { item };

                _ = SetSelectList.InvokeAsync(newSelect);
            }
        }
    }
}
