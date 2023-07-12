using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BlazorLibrary.FolderForInherits;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;

namespace BlazorLibrary.Shared.FiltrComponent
{
    partial class FiltrInput
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

        bool IsViewHint = false;

        HintItem? SelectHints { get; set; }

        HintItem? FocusHints { get; set; }

        OperationItem? FocusOperation { get; set; }

        bool IsViewHistory = false;
        bool IsFocusHistory = false;
        Hint? FocusHint { get; set; }

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
                    else if (SelectHints.Type == TypeHint.Duration || SelectHints.Type == TypeHint.Date || SelectHints.Type == TypeHint.DateOnly || SelectHints.Type == TypeHint.Time || SelectHints.Type == TypeHint.Number)
                    {
                        response.AddRange(new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.Greater, GetNameOperation(FiltrOperationType.Greater)),
                            new OperationItem(FiltrOperationType.Less, GetNameOperation(FiltrOperationType.Less)),
                            new OperationItem(FiltrOperationType.GreaterOrEqual, GetNameOperation(FiltrOperationType.GreaterOrEqual)),
                            new OperationItem(FiltrOperationType.LeesOrEqual, GetNameOperation(FiltrOperationType.LeesOrEqual)),
                            new OperationItem(FiltrOperationType.Range, GetNameOperation(FiltrOperationType.Range))
                        });
                    }
                    else if (SelectHints.Type == TypeHint.ContainsOnly)
                    {
                        response = new List<OperationItem>() {
                            new OperationItem(FiltrOperationType.Contains, GetNameOperation(FiltrOperationType.Contains)),
                            new OperationItem(FiltrOperationType.NotContains, GetNameOperation(FiltrOperationType.NotContains))
                        };
                    }
                }
                return response;
            }
        }

        string GetNameOperation(FiltrOperationType operation)
        {
            return Rep[operation.ToString().ToUpper()];
        }

        Hint TempValue = new(string.Empty);

        FiltrRequestItem LastRequest = new();

        string UserName = string.Empty;

        string? ValueSelect
        {
            get => TempValue?.Value;
            set
            {
                if (!IsViewHint)
                    IsViewHint = true;
                TempValue = new(value ?? string.Empty);
                if (!FocusOperation?.Name.Contains(value ?? "") ?? false)
                    FocusOperation = null;
                if (!FocusHints?.Name.Contains(value ?? "") ?? false)
                    FocusHints = null;
                if (!FocusHint?.Value.Contains(value ?? "") ?? false)
                    FocusHint = null;
                if (SelectHints != null && SelectHints.Operation != FiltrOperationType.None && SelectHints.Provider?.IsData == true)
                {
                    SelectHints.Provider.Request.BstrFilter = value;
                    FocusHints = null;
                    SelectHints.Provider.Reset();
                    _ = SelectHints.Provider.AddData().ContinueWith(x =>
                    {
                        StateHasChanged();
                    });
                }
            }
        }

        ElementReference? input;

        bool IsFocusHint = false;

        private DateTime? DateStart = null;

        private DateTime? DateEnd = null;

        private TimeSpan? DurationStart = null;

        private TimeSpan? DurationEnd = null;

        float LeftHint = 0;

        protected override async Task OnInitializedAsync()
        {
            UserName = await _User.GetName() ?? "";

            LastRequest = await _localStorage.FiltrGetLastRequest(UserName, PlaceHolder);
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

        async Task OnFocusInput()
        {
            IsViewHint = true;
            LeftHint = await JSRuntime.InvokeAsync<float>("GetElemLeftPosition", input);
            StateHasChanged();
        }

        async Task OnClickInput()
        {
            if (!IsViewHint)
                await OnFocusInput();
        }

        void OnBlurInput()
        {
            if (!IsFocusHint)
                IsViewHint = false;
        }

        void OnBlurHistory()
        {
            if (!IsFocusHistory)
                IsViewHistory = false;
        }

        void OnMouseOverHistory()
        {
            IsFocusHistory = true;
        }

        void OnMouseOutHistory()
        {
            IsFocusHistory = false;
        }

        void OnMouseOver()
        {
            IsFocusHint = true;
        }

        void OnMouseOut()
        {
            IsFocusHint = false;
        }

        void OnClickHint()
        {
            IsFocusHint = false;
        }

        async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.CtrlKey || e.AltKey || e.ShiftKey) return;

            if (e.Key == "Escape")
            {
                ClearActive();
            }
            else if (e.Key == "Enter")
            {
                if (FocusHints != null)
                    await SetActive(FocusHints);
                else if (FocusOperation != null && FocusOperation.Key != FiltrOperationType.None)
                    await SetOperation(FocusOperation);
                else if (FocusHint != null)
                    await SetValueHint(FocusHint);
                else
                {
                    await OnClick();
                    StateHasChanged();
                    await Task.Yield();
                    await OnFocusInput();
                }
            }
            else if (e.Key == "ArrowDown" || e.Key == "ArrowUp")
            {
                var index = e.Key == "ArrowUp" ? -1 : 1;

                if (SelectHints == null && Hints != null)
                {
                    FocusHints = Hints.Where(x => x.Name.Contains(TempValue.Value ?? "")).GetNextSelectItem(FocusHints, index);
                }
                else if (SelectHints != null && SelectHints.Operation == FiltrOperationType.None)
                {
                    FocusOperation = OperandTypes.Where(x => x.Name.Contains(TempValue.Value ?? "")).GetNextSelectItem(FocusOperation, index);
                }
                else if (SelectHints != null && SelectHints.Provider?.IsData == true)
                {
                    FocusHint = SelectHints.Provider.CacheItems.Where(x => x.Value.Contains(TempValue.Value ?? "")).GetNextSelectItem(FocusHint, index);
                }
            }
        }

        void ClearActive()
        {
            TempValue = new(string.Empty);
            IsViewHint = false;
            SelectHints = null;
            DateEnd = null;
            DateStart = null;
            FocusHints = null;
            FocusHint = null;
            FocusOperation = null;
        }

        async Task OnClick()
        {
            if (AddItemFiltr.HasDelegate && !string.IsNullOrEmpty(TempValue?.Value))
            {
                if (SelectHints == null)
                {
                    SelectHints = Hints.FirstOrDefault(x => x.Type == TypeHint.Select || x.Type == TypeHint.ContainsOnly || x.Type == TypeHint.Input);
                    if (SelectHints == null)
                    {
                        ClearActive();
                        return;
                    }
                    SelectHints.Operation = FiltrOperationType.Contains;
                }
                else
                {
                    if (SelectHints.Type == TypeHint.OnlySelect && (!SelectHints.Provider?.CacheItems.Any(x => x.Value == TempValue.Value) ?? true))
                    {
                        ClearActive();
                        return;
                    }
                }


                await AddItemFiltr.InvokeAsync(new() { new FiltrItem(SelectHints.Key, TempValue, SelectHints.Operation) });

                await Task.Yield();
                await SaveLocalStorge();
            }
            ClearActive();
        }

        async Task OnClickHistory(List<FiltrItem> items)
        {
            if (AddItemFiltr.HasDelegate)
            {
                await AddItemFiltr.InvokeAsync(items);
            }
            IsViewHistory = false;
        }

        async Task SetActive(HintItem item)
        {
            SelectHints = new(item.Key, item.Name, item.Type, item.Value, item.Operation, item.Provider);
            FocusHints = null;
            TempValue = new(string.Empty);
            input?.FocusAsync(true);
            await Task.Yield();
            await OnFocusInput();
        }

        async Task SaveLocalStorge()
        {

            LastRequest = await _localStorage.FiltrSaveLastRequest(UserName, PlaceHolder, Items) ?? new();
        }

        async Task RemoveItem(FiltrItem item)
        {
            if (RemoveItemsFiltr.HasDelegate)
                await RemoveItemsFiltr.InvokeAsync(new List<FiltrItem>() { item });
            await Task.Yield();
            await SaveLocalStorge();
            input?.FocusAsync(true);
        }

        async Task RemoveAll()
        {
            if (RemoveItemsFiltr.HasDelegate && Items?.Count > 0)
            {
                await RemoveItemsFiltr.InvokeAsync(new List<FiltrItem>(Items));
            }
            await Task.Yield();
            await SaveLocalStorge();
            ClearActive();
        }

        async Task SetValueDate()
        {
            if (SelectHints == null || (DateStart == null && DateEnd == null))
                return;

            string formatDate = SelectHints.Type == TypeHint.DateOnly ? "d" : "g";

            if (SelectHints.Operation == FiltrOperationType.Range)
                TempValue = new($"{DateStart?.ToString(formatDate)}-{DateEnd?.ToString(formatDate)}");
            else
            {
                TempValue = new($"{DateStart?.ToString(formatDate)}");
            }
            input?.FocusAsync(true);
            await OnFocusInput();
        }

        async Task SetValueTime()
        {
            if (SelectHints == null || (DateStart == null && DateEnd == null))
                return;

            if (SelectHints.Operation == FiltrOperationType.Range)
                TempValue = new($"{DateStart?.ToString("t")}-{DateEnd?.ToString("t")}");
            else
            {
                TempValue = new($"{DateStart?.ToString("t")}");
            }
            input?.FocusAsync(true);
            await OnFocusInput();
        }

        async Task SetValueDuration()
        {
            if (SelectHints == null || (DurationStart == null && DurationEnd == null))
                return;

            if (SelectHints.Operation == FiltrOperationType.Range)
                TempValue = new($"{DurationStart?.ToString("dd\\.hh\\:mm")}-{DurationEnd?.ToString("dd\\.hh\\:mm")}");
            else
            {
                TempValue = new($"{DurationStart?.ToString("dd\\.hh\\:mm")}");
            }
            input?.FocusAsync(true);
            await OnFocusInput();
        }

        async Task SetValueHint(Hint item)
        {
            if (SelectHints == null)
                return;
            TempValue = item;
            FocusHint = null;
            input?.FocusAsync(true);
            await OnFocusInput();
        }

        async Task SetValueHintBool(bool value)
        {
            if (SelectHints == null)
                return;
            TempValue = new(value ? GsoRep["YES"] : GsoRep["NO"], value.ToString());
            FocusHint = null;
            input?.FocusAsync(true);
            await OnFocusInput();
        }

        async Task SetOperation(OperationItem item)
        {
            if (SelectHints == null)
                return;
            SelectHints.Operation = item.Key;
            FocusOperation = null;
            TempValue = new(string.Empty);
            if (SelectHints.Provider?.IsData == true)
            {
                await SelectHints.Provider.AddData();
            }
            input?.FocusAsync(true);
            await OnFocusInput();
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
                    return newHints.Where(x => x.Name.Contains(TempValue.Value ?? "", StringComparison.OrdinalIgnoreCase));
                }
                return Hints.Where(x => x.Name.Contains(TempValue.Value ?? "", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
