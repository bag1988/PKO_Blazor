using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Buttons;
using BlazorLibrary.Shared.Table;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Label.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using Field = Label.V1.Field;

namespace BlazorLibrary.Shared.LabelsComponent
{
    partial class Labels
    {
        [Parameter]
        public OBJ_Key? IdForm { get; set; }

        public GetFieldList? keyList { get; set; }

        TableItem? SelectItem { get; set; }

        bool IsViewSpecification = false;

        bool IsPageLoad = true;

        public Dictionary<int, string> ThList = new();

        TableVirtualize<TableItem>? table;

        enum TypeField
        {
            Select = 1,
            Input
        }

        class TableItem
        {
            public int Id { get; set; }
            public TypeField Type { get; set; }
            public int IdField { get; set; }
            public string? NameField { get; set; }
            public int IdValue { get; set; }
            public string? NameValue { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { -1, DeviceRep["SPECIFICATION"]},
                { -2, AsoRep["Value"] }
            };

            await GetKeyList();

            IsPageLoad = false;
        }

        ItemsProvider<TableItem> GetProvider => new ItemsProvider<TableItem>(ThList, LoadChildList, new GetItemRequest() { ObjID = IdForm?.ObjID ?? new() }, new List<int>() { 50, 50 });

        private ValueTask<IEnumerable<TableItem>> LoadChildList(GetItemRequest req)
        {
            var newData = new List<TableItem>();

            if (keyList != null)
            {
                if (keyList.FieldList?.List?.Count(x => !string.IsNullOrEmpty(x.ValueField) || x.NameField == SelectItem?.NameField) > 0)
                {
                    newData.AddRange(keyList.FieldList.List.Where(x => !string.IsNullOrEmpty(x.ValueField) || x.NameField == SelectItem?.NameField).Select(x => new TableItem()
                    {
                        Id = newData.Count + 1,
                        IdField = x.IdNameField,
                        IdValue = x.IdValueField,
                        NameField = x.NameField,
                        NameValue = x.ValueField,
                        Type = TypeField.Input
                    }));
                }
            }
            return new(newData);
        }

        private async Task GetKeyList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLabelFieldAsoAbonent", IdForm);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(json))
                {
                    keyList = JsonParser.Default.Parse<GetFieldList>(json);
                }
            }
            if (keyList == null)
            {
                keyList = new() { FieldList = new(), FieldHelpList = new() };
            }

            if (keyList.FieldList?.List?.Count(x => !string.IsNullOrEmpty(x.ValueField)) > 0)
                IsViewSpecification = true;
        }


        async Task SetNameField(ChangeEventArgs e)
        {
            if (keyList == null)
                keyList = new() { FieldList = new(), FieldHelpList = new() };

            if ((!keyList.FieldList.List?.Any(x => x.NameField == e.Value?.ToString()) ?? true))
            {
                Field newItem = new()
                {
                    NameField = e.Value?.ToString()
                };

                keyList.FieldList.List?.Add(newItem);
            }
            SelectItem = new() { NameField = e.Value?.ToString() };

            if (table != null)
            {
                await table.ResetData();
                SelectItem = table.FindItemMatch(x => x.NameField == e.Value?.ToString());
            }
            else
            {
                SelectItem = null;
            }
        }


        async Task SetValueField(ChangeEventArgs e)
        {
            if (SelectItem == null || keyList == null)
                return;

            string nameField = SelectItem.NameField ?? "";

            if (SelectItem.Type == TypeField.Input)
            {
                var item = keyList.FieldList?.List?.FirstOrDefault(x => x.NameField == nameField);

                if (item != null)
                {
                    item.ValueField = e.Value?.ToString();
                }
            }
            if (table != null)
            {
                await table.ResetData();
                SelectItem = table.FindItemMatch(x => x.NameField == nameField);
            }
        }

        private IEnumerable<string> GetHelpValueList
        {
            get
            {
                List<string> valueList = new();

                if (!string.IsNullOrEmpty(SelectItem?.NameField) && SelectItem.Type == TypeField.Input)
                {
                    if (keyList?.FieldHelpList?.List?.Count > 0)
                    {
                        valueList = keyList.FieldHelpList.List.FirstOrDefault(x => x.NameField == SelectItem.NameField)?.HelpStringList?.List?.Select(x => x.Value)?.ToList() ?? new List<string>();
                    }
                }
                return valueList;
            }
        }

       
        IEnumerable<string>? GetFreeKey
        {
            get
            {
                List<string> freeKeyList = new();
                                
                if (keyList?.FieldHelpList?.List?.Count > 0)
                {
                    freeKeyList.AddRange(keyList.FieldHelpList.List.Where(x => !table?.AnyItemMatch(k => k.NameField == x.NameField) ?? true).Select(x => x.NameField));
                }
                return freeKeyList;
            }
        }


        async Task AddSpicification()
        {
            if (table != null)
            {
                var newItem = new TableItem() { Id = table.CountItemMatch(x => x.Id > 0) + 1, Type = TypeField.Input };
                await table.AddItem(newItem);
                SelectItem = newItem;
            }
        }


        void SetSelectItem(List<TableItem>? items)
        {
            if (SelectItem?.Equals(items?.LastOrDefault()) ?? false)
                return;

            SelectItem = items?.LastOrDefault();
        }
                
    }
}
