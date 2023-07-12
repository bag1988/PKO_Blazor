using System;
using System.Numerics;
using BlazorLibrary.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorLibrary.FolderForInherits
{
    public class MultiSelectVirtualize<TItem> : ComponentBase
    {
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

        protected List<TItem>? Items { get; set; }

        public bool _shouldPreventDefault = false;

        TItem? FocusItem { get; set; } = default;

        protected override void OnInitialized()
        {
            if (IsSetFocus)
            {
                Task.Run(async () =>
                {
                    await Task.Yield();
                    Elem?.FocusAsync();
                });
            }
        }

        public void SetFocus()
        {
            Elem?.FocusAsync();
        }


        public string GetClassElem(TItem item)
        {
            string classList = "";
            if (FocusItem?.Equals(item) ?? false)
                classList = "bg-focus";
            else if (SelectList?.Contains(item) ?? false)
                classList = "bg-select";

            if (SetSelectList.HasDelegate)
                classList = classList + " pointer";

            return classList;
        }

        public async Task KeySet(KeyboardEventArgs e)
        {
            _shouldPreventDefault = false;

            if (!IsOnKeyDown)
            {
                return;
            }
            if (e.Key == "Escape")
            {
                if (SelectList?.Count > 0)
                {
                    if (FocusItem == null)
                        FocusItem = SelectList.LastOrDefault();
                    await SetSelectList.InvokeAsync(null);
                }
                else if (FocusItem != null)
                    FocusItem = default;
            }
            else if (e.Key == "Enter")
            {
                if (FocusItem != null && (SelectList == null || !SelectList.Contains(FocusItem)))
                {
                    _shouldPreventDefault = true;
                    _ = Task.Run(() =>
                     {
                         return AddSelectItem(FocusItem, e.CtrlKey, e.ShiftKey);
                     });
                }
                else
                {
                    await DbCallback();
                }
            }
            else if (e.Key == "ArrowUp" || e.Key == "ArrowDown")
            {
                _shouldPreventDefault = true;
                if (Items == null || !Items.Any())
                    return;

                var index = e.Key == "ArrowUp" ? -1 : 1;

                FocusItem = Items.GetNextSelectItem(FocusItem ?? (SelectList != null ? SelectList.LastOrDefault() : default), index);

                if (FocusItem != null)
                {
                    _ = JSRuntime?.InvokeVoidAsync("ScrollToSelectElement", Elem, ".bg-focus");
                }

            }
        }

        public TItem? GetNextOrFirstItem
        {
            get
            {
                if (SelectList != null)
                    return Items.GetNextSelectItem(SelectList.LastOrDefault());
                return Items.GetNextSelectItem(default, 1);
            }
        }

        public TItem? GetNextItemMatch(Func<TItem, bool> match)
        {
            return Items.GetNextSelectItem(match);
        }

        public TItem? FindItemMatch(Func<TItem, bool> match)
        {
            if (Items != null)
                return Items.FirstOrDefault(match);
            return default;
        }

        public bool AnyItemMatch(Func<TItem, bool> match)
        {
            if (Items != null)
            {
                if (Items.FirstOrDefault(match) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public int CountItemMatch(Func<TItem, bool> match)
        {
            if (Items != null)
            {
                return Items.Count(match);
            }
            return 0;
        }

        public int GetIndexItem(TItem? item)
        {
            if (Items != null && item != null)
            {
                return Items.IndexOf(item);
            }
            return -1;
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

        public Task StartAddSelectItem(MouseEventArgs e, TItem item)
        {
            return AddSelectItem(item, e.CtrlKey, e.ShiftKey);
        }

        async Task AddSelectItem(TItem item, bool ctrlKey, bool shiftKey)
        {
            if (SetSelectList.HasDelegate && (Items?.Any() ?? false))
            {
                if (SelectList == null) SelectList = new List<TItem>();

                var newSelect = new List<TItem>(SelectList);

                if (ctrlKey)
                {
                    if (newSelect.Contains(item))
                        newSelect.Remove(item);
                    else
                        newSelect.Add(item);
                }
                else if (shiftKey && newSelect.Count > 0)
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


                await SetSelectList.InvokeAsync(newSelect);

                if ((!item?.Equals(FocusItem) ?? true) || (SelectList != null && (SelectList.LastOrDefault()?.Equals(FocusItem) ?? false)))
                    FocusItem = default;

            }
        }
    }
}
