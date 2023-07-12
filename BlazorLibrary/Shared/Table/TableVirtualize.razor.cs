using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Table
{
    partial class TableVirtualize<TItem>
    {
        [Parameter]
        public ItemsProvider<TItem>? Provider { get; init; }
                
        [Parameter]
        public bool IsSetFirstSelect { get; set; } = false;

        int Colspan
        {
            get
            {
                return Provider?.ThList?.Count ?? 0;
            }
        }

        ElementReference? div;

        int WindowHeight = 800;

        GetItemRequest request = new();

        bool IsLoadData = false;

        bool IsAddData = true;

        readonly int OverscanCount = 50;

        Virtualize<TItem>? virtualize;

        protected override async Task OnInitializedAsync()
        {
            request = Provider?.DefaultRequestItems ?? new() { ObjID = new OBJ_ID() { SubsystemID = 0, ObjID = 0, StaffID = 0 }, LSortOrder = 1, BFlagDirection = 1, SkipItems = 0 };
            request.CountData = 200;
            await LoadSelectItem();
        }
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (IsSticky == true)
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

        private ValueTask<ItemsProviderResult<TItem>> GetItems(ItemsProviderRequest req)
        {
            if (!IsLoadData && (req.StartIndex + req.Count) >= (Items?.Count ?? 0))
            {
                _ = AddData();
            }

            return new(new ItemsProviderResult<TItem>(
                Items?.Skip(req.StartIndex).Take(req.Count) ?? new List<TItem>(),
                Items?.Count ?? 0));

        }

        async Task GetList()
        {
            if (Provider == null || Provider.Items == null)
            {
                return;
            }
            IsLoadData = true;
            IsAddData = false;
            try
            {
                IEnumerable<TItem>? newData = await Provider.Items.Invoke(request);
                if (Items == null)
                    Items = new();
                if (newData.Count() > 0)
                {
                    Items.AddRange(newData.Except(Items));
                }
                if (newData.Count() == request.CountData)
                {
                    IsAddData = true;
                }
            }
            finally
            {
                IsLoadData = false;
                await RefreshVirtualize();
            }
        }

        public int IndexOfItem(TItem item)
        {
            return Items?.IndexOf(item) ?? -1;
        }

        public async Task AddItem(TItem item)
        {
            if (Items == null)
                Items = new();

            if (!Items.Contains(item))
            {
                Items.Add(item);
                if (virtualize == null)
                    StateHasChanged();
                await RefreshVirtualize();
            }
        }

        public async Task AddItem(TItem item, Func<TItem, bool> match)
        {
            if (Items == null)
                Items = new();
            if (!Items.Any(match))
            {
                Items.Add(item);
                if (virtualize == null)
                    StateHasChanged();
                await RefreshVirtualize();
            }
        }

        public async Task InsertItem(int index, TItem item)
        {
            if (Items == null)
                Items = new();

            if (!Items.Contains(item))
            {
                Items.Insert(index, item);
                if (virtualize == null)
                    StateHasChanged();
                await RefreshVirtualize();
            }
        }

        public async Task<bool> RemoveItem(TItem item)
        {
            bool removed = false;
            if (Items != null)
            {
                removed = Items.Remove(item);
                await RefreshVirtualize();
            }
            return removed;
        }

        public async Task<int> RemoveAllItem(Predicate<TItem> match)
        {
            int countRemove = 0;
            if (Items != null)
            {
                countRemove = Items.RemoveAll(match);
                await RefreshVirtualize();
            }
            return countRemove;
        }

        public async Task ForEachItems(Action<TItem> action)
        {
            if (Items != null)
            {
                Items.ForEach(action);
                await RefreshVirtualize();
            }
        }

        public List<TItem>? GetCurrentItems => Items;

        public async Task RemoveListItem(IEnumerable<TItem> items)
        {
            if (Items != null)
            {
                foreach (var item in items)
                {
                    Items.Remove(item);
                }
                await RefreshVirtualize();
            }
        }

        public async Task RefreshVirtualize()
        {
            if (virtualize != null)
            {
                await virtualize.RefreshDataAsync();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Сброс данных
        /// </summary>
        /// <returns></returns>
        public async Task ResetData()
        {
            var t = Task.Delay(1000);
            //если идет загрузка, ждем секунду
            while (IsLoadData && !t.IsCompletedSuccessfully)
            {
                await Task.Delay(100);
            }
            //if (Items?.Count > 0)
            IsAddData = true;
            Items = null;

            await RefreshVirtualize();
            await LoadSelectItem();
        }


        async Task LoadSelectItem()
        {
            if (IsSetFirstSelect && SetSelectList.HasDelegate)
            {
                var t = Task.Delay(1000);
                //если идет загрузка, ждем секунду
                while (Items == null && !t.IsCompletedSuccessfully)
                {
                    await Task.Delay(100);
                }

                var newSelect = GetNextOrFirstItem;
                if (newSelect != null)
                {
                    await SetSelectList.InvokeAsync(new List<TItem>() { newSelect });
                }
                else
                    await SetSelectList.InvokeAsync();
            }
        }

        async Task SetSort(int? id)
        {
            if (id == request!.LSortOrder)
                request.BFlagDirection = request.BFlagDirection == 1 ? 0 : 1;
            else
            {
                request.LSortOrder = id ?? 0;
                request.BFlagDirection = 1;
            }
            await ResetData();
        }

        async ValueTask AddData()
        {
            if (Provider != null && Provider.IsScrollData)
            {
                var t = Task.Delay(1000);
                //если идет загрузка, ждем секунду
                while (IsLoadData && !t.IsCompletedSuccessfully)
                {
                    await Task.Delay(100);
                }
                if (IsAddData && !IsLoadData)
                {
                    request.SkipItems = Items?.Count ?? 0;
                    await GetList();
                }
            }
        }
    }
}
