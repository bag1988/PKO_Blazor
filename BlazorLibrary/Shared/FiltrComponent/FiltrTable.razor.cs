using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.FiltrComponent
{
    partial class FiltrTable
    {
        [Parameter]
        public List<FiltrItem> Items { get; set; } = new();

        [Parameter]
        public List<HintItem> Hints { get; set; } = new();

        [Parameter]
        public EventCallback<List<FiltrItem>> AddItemFiltr { get; set; }

        [Parameter]
        public EventCallback<List<FiltrItem>?> RemoveItemsFiltr { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; } = FiltrName.FiltrResult;

        HintItem? SelectHints { get; set; }

        bool IsViewHistory = false;

        record class OperationItem
        {
            public OperationItem(FiltrOperationType key, string name)
            {
                Key = key;
                Name = name;
            }
            public FiltrOperationType Key { get; set; }
            public string Name { get; set; }
        }

        List<OperationItem> OperandTypes
        {
            get
            {
                var response = new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.Equal, GetNameOperation(FiltrOperationType.Equal)),
                            new OperationItem(FiltrOperationType.NotEqual, GetNameOperation(FiltrOperationType.NotEqual))
                        };

                if (Items.Count > 0)
                {
                    response.AddRange(new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.OrEqual, GetNameOperation(FiltrOperationType.OrEqual)),
                            new OperationItem(FiltrOperationType.OrNotEqual, GetNameOperation(FiltrOperationType.OrNotEqual))
                        });
                }

                if (SelectHints != null)
                {
                    if (SelectHints.Type == TypeHint.Select || SelectHints.Type == TypeHint.Input)
                    {
                        response.AddRange(new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.Contains, GetNameOperation(FiltrOperationType.Contains)),
                            new OperationItem(FiltrOperationType.NotContains, GetNameOperation(FiltrOperationType.NotContains))
                        });
                    }
                    else if (SelectHints.Type == TypeHint.Date || SelectHints.Type == TypeHint.DateOnly || SelectHints.Type == TypeHint.Time || SelectHints.Type == TypeHint.Number)
                    {
                        response.AddRange(new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.Greater, GetNameOperation(FiltrOperationType.Greater)),
                            new OperationItem(FiltrOperationType.Less, GetNameOperation(FiltrOperationType.Less)),
                            new OperationItem(FiltrOperationType.GreaterOrEqual, GetNameOperation(FiltrOperationType.GreaterOrEqual)),
                            new OperationItem(FiltrOperationType.LeesOrEqual, GetNameOperation(FiltrOperationType.LeesOrEqual)),
                            new OperationItem(FiltrOperationType.Range, GetNameOperation(FiltrOperationType.Range))
                        });
                    }
                }
                return response;
            }
        }

        string GetNameOperation(FiltrOperationType operation)
        {
            return Rep[operation.ToString().ToUpper()];
        }

        FiltrRequestItem LastRequest = new();

        string UserName = string.Empty;

        string? ValueSelect
        {
            get => SelectFiltrItem?.Value?.Value;
            set
            {
                if (SelectFiltrItem == null)
                    return;

                var item = table?.FindItemMatch(x => x.Key == SelectFiltrItem.Key && x.Operation == SelectFiltrItem.Operation && x.Value?.Value == value);

                if (item == null)
                {
                    SelectFiltrItem.Value = new(value ?? string.Empty);
                    if (GetHintSelectFiltr?.Provider?.IsData == true)
                    {
                        GetHintSelectFiltr.Provider.Request.BstrFilter = value;
                        GetHintSelectFiltr.Provider.Reset();
                        _ = GetHintSelectFiltr.Provider.AddData().ContinueWith(x =>
                        {
                            StateHasChanged();
                        });
                    }
                }
                else
                {
                    _ = table?.RemoveItem(SelectFiltrItem);
                    SelectFiltrItem = item;
                }

            }
        }

        DateTime? DateStart
        {
            get
            {
                if (SelectFiltrItem != null && !string.IsNullOrEmpty(SelectFiltrItem.Value?.Value))
                {
                    var dateRange = SelectFiltrItem.Value.Value.Split("-") ?? new string[0];
                    DateTime.TryParse(dateRange[0], out var dateStart);
                    return dateStart;
                }
                return DateTime.Now;
            }
            set
            {
                if (SelectFiltrItem == null)
                    return;

                if (SelectFiltrItem == null || (value == null && DateEnd == null))
                    return;

                string formatDate = GetHintSelectFiltr?.Type == TypeHint.DateOnly ? "d" : "g";

                var newValue = string.Empty;
                if (SelectFiltrItem.Operation == FiltrOperationType.Range)
                    newValue = $"{value?.ToString(formatDate)}-{DateEnd?.ToString(formatDate)}";
                else
                {
                    newValue = $"{value?.ToString(formatDate)}";
                }
                var newSelect = new FiltrItem(SelectFiltrItem.Key, new(newValue), SelectFiltrItem.Operation);

                _ = ReplaceItem(newSelect);

            }
        }
        private DateTime? DateEnd
        {
            get
            {
                if (SelectFiltrItem != null && !string.IsNullOrEmpty(SelectFiltrItem.Value?.Value))
                {
                    var dateRange = SelectFiltrItem.Value.Value.Split("-") ?? new string[0];
                    if (dateRange.Length > 0)
                    {
                        DateTime.TryParse(dateRange[1], out var dateEnd);
                        return dateEnd;
                    }
                }
                return DateTime.Now;
            }
            set
            {
                if (SelectFiltrItem == null)
                    return;

                if (SelectFiltrItem == null || (DateStart == null && value == null))
                    return;

                string formatDate = GetHintSelectFiltr?.Type == TypeHint.DateOnly ? "d" : "g";

                var newValue = string.Empty;
                if (SelectFiltrItem.Operation == FiltrOperationType.Range)
                    newValue = $"{DateStart?.ToString(formatDate)}-{value?.ToString(formatDate)}";
                else
                {
                    newValue = $"{DateStart?.ToString(formatDate)}";
                }
                var newSelect = new FiltrItem(SelectFiltrItem.Key, new(newValue), SelectFiltrItem.Operation);

                _ = ReplaceItem(newSelect);

            }
        }

        bool IsPageLoad = true;
        public Dictionary<int, string> ThList = new();

        TableVirtualize<FiltrItem>? table;

        FiltrItem? SelectFiltrItem = null;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { -1, DeviceRep["SPECIFICATION"]},
                { -2, GsoRep["CONDITION"]},
                { -3, AsoRep["Value"] }
            };

            UserName = await _User.GetName() ?? "";
            LastRequest = await _localStorage.FiltrGetLastRequest(UserName, PlaceHolder);
            IsPageLoad = false;
        }

        ItemsProvider<FiltrItem> GetProvider => new ItemsProvider<FiltrItem>(ThList, LoadChildList, new GetItemRequest(), new List<int>() { 40, 20, 40 });

        private ValueTask<IEnumerable<FiltrItem>> LoadChildList(GetItemRequest req)
        {
            var newData = new List<FiltrItem>();

            if (Items != null)
            {
                return new(Items);
            }
            return new(newData);
        }

        HintItem? GetHintSelectFiltr => Hints?.FirstOrDefault(x => x.Key == SelectFiltrItem?.Key);

        async Task OnSetKey(ChangeEventArgs e)
        {
            var newKey = e.Value?.ToString();

            if (string.IsNullOrEmpty(newKey) || SelectFiltrItem == null) { return; }

            var newSelect = new FiltrItem(newKey, SelectFiltrItem.Value, SelectFiltrItem.Operation);

            await ReplaceItem(newSelect);

            if (GetHintSelectFiltr?.Provider?.IsData == true)
            {
                GetHintSelectFiltr.Provider.Request.BstrFilter = string.Empty;
                GetHintSelectFiltr.Provider.Reset();
                _ = GetHintSelectFiltr.Provider.AddData().ContinueWith(x =>
                {
                    StateHasChanged();
                });
            }
        }
        async Task OnSetOperation(ChangeEventArgs e)
        {
            var newOperation = e.Value?.ToString();

            if (string.IsNullOrEmpty(newOperation) || SelectFiltrItem == null) { return; }

            System.Enum.TryParse(newOperation, out FiltrOperationType result);

            var newSelect = new FiltrItem(SelectFiltrItem.Key, SelectFiltrItem.Value, result);

            await ReplaceItem(newSelect);
        }

        async Task ReplaceItem(FiltrItem newItem)
        {
            if (SelectFiltrItem != null && table != null)
            {
                if (!table.AnyItemMatch(x => x.Equals(newItem)))
                {
                    var index = table.IndexOfItem(SelectFiltrItem);
                    await table.RemoveItem(SelectFiltrItem);
                    await table.InsertItem(index, newItem);
                    if (!string.IsNullOrEmpty(newItem.Value?.Value) && newItem.Operation != FiltrOperationType.None && !string.IsNullOrEmpty(newItem.Key))
                    {
                        if (RemoveItemsFiltr.HasDelegate)
                            await RemoveItemsFiltr.InvokeAsync(new List<FiltrItem>() { SelectFiltrItem });
                        SelectFiltrItem = newItem;
                        await OnClick();
                    }
                }
                SelectFiltrItem = newItem;
            }
        }

        async Task SetSelectList(List<FiltrItem>? items)
        {
            if (SelectFiltrItem == null || !SelectFiltrItem.Equals(items?.LastOrDefault()))
            {
                if (SelectFiltrItem != null)
                {
                    if (table != null)
                    {
                        if (string.IsNullOrEmpty(SelectFiltrItem.Key) || SelectFiltrItem.Operation == FiltrOperationType.None || string.IsNullOrEmpty(SelectFiltrItem.Value?.Value))
                        {
                            await table.RemoveItem(SelectFiltrItem);
                            await RemoveItem(SelectFiltrItem);
                            await SaveLocalStorge();
                        }
                    }
                }
                SelectFiltrItem = items?.LastOrDefault();
            }
        }

        async Task AddFiltr()
        {
            if (table != null)
            {
                FiltrItem newItem = new(string.Empty, null, FiltrOperationType.None);
                await table.AddItem(newItem);
                SelectFiltrItem = newItem;
            }
        }

        string GetNameHint(FiltrItem item)
        {
            return $"{Hints?.FirstOrDefault(x => x.Key == item.Key)?.Name} {GetNameOperation(item.Operation)} {item.Value?.Value}";
        }

        string GetNameHints(List<FiltrItem> items)
        {
            List<string> names = new();

            foreach (var item in items)
            {
                names.Add(GetNameHint(item));
            }

            return string.Join(" ", names);

        }

        async Task OnClick()
        {
            if (AddItemFiltr.HasDelegate && SelectFiltrItem != null && !string.IsNullOrEmpty(SelectFiltrItem.Key) && SelectFiltrItem.Operation != FiltrOperationType.None)
            {
                if (!string.IsNullOrEmpty(SelectFiltrItem.Value?.Value))
                {
                    await Task.Yield();
                    await AddItemFiltr.InvokeAsync(new() { SelectFiltrItem });

                    if (table != null && table.GetCurrentItems != null)
                    {
                        var deleteList = Items.Except(table.GetCurrentItems);
                        if(deleteList.Any())
                        {
                            if (RemoveItemsFiltr.HasDelegate)
                                await RemoveItemsFiltr.InvokeAsync(deleteList.ToList());
                        }
                    }
                    await SaveLocalStorge();
                }
                else
                    await RemoveItem(SelectFiltrItem);
            }
        }

        async Task OnClickHistory(List<FiltrItem> items)
        {
            if (AddItemFiltr.HasDelegate)
            {
                await AddItemFiltr.InvokeAsync(items);
            }

            if (table != null)
            {
                foreach (var item in items.Where(x => !string.IsNullOrEmpty(x.Value?.Value)))
                {
                    await table.AddItem(item, x => x.Equals(item));
                }

            }
            IsViewHistory = false;
        }

        async Task SaveLocalStorge()
        {
            LastRequest = await _localStorage.FiltrSaveLastRequest(UserName, PlaceHolder, Items.Where(x => !string.IsNullOrEmpty(x.Value?.Value)).ToList()) ?? new();
        }

        async Task RemoveItem(FiltrItem item)
        {
            if (RemoveItemsFiltr.HasDelegate)
                await RemoveItemsFiltr.InvokeAsync(new List<FiltrItem>() { item });
            if (table != null && SelectFiltrItem != null)
            {
                await table.RemoveItem(SelectFiltrItem);
            }
            SelectFiltrItem = null;
            await Task.Yield();
            await SaveLocalStorge();
        }

        async Task SetValueHintBool(bool value)
        {
            if (SelectFiltrItem == null)
                return;
            Hint newValue = new(value ? GsoRep["YES"] : GsoRep["NO"], value.ToString());
            var newSelect = new FiltrItem(SelectFiltrItem.Key, newValue, SelectFiltrItem.Operation);
            await ReplaceItem(newSelect);
        }

        async Task ClearHistory()
        {
            IsViewHistory = false;
            LastRequest = new();
            await _localStorage.FiltrClearLastRequest(UserName, PlaceHolder);
        }

        IEnumerable<HintItem> GetHintsItems
        {
            get
            {
                if (Items != null && Items.Any(x => x.Operation == FiltrOperationType.BoolEqual))
                {
                    var newHints = Hints.Where(x => !Items.Any(i => x.Type == TypeHint.Bool && x.Key == i.Key && i.Operation == FiltrOperationType.BoolEqual));
                    return newHints;
                }
                else
                {
                    return Hints;
                }

            }
        }
    }
}
