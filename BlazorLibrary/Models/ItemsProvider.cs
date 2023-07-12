using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Models
{
    public class ItemsProvider<TItem>
    {
        public ItemsProvider(Dictionary<int, string> thList, Func<GetItemRequest, ValueTask<IEnumerable<TItem>>>? items, GetItemRequest? defaultRequestItems, List<int>? thWidthProcent = null)
        {
            Items = items;
            ThList = thList;
            ThWidthProcent = thWidthProcent;
            DefaultRequestItems = defaultRequestItems;
            if (items != null)
            {
                IsScrollData = true;
            }
                
        }

        public Func<GetItemRequest, ValueTask<IEnumerable<TItem>>>? Items { get; init; }

        public Dictionary<int, string>? ThList { get; init; }

        public List<int>? ThWidthProcent { get; init; }

        public GetItemRequest? DefaultRequestItems { get; init; }

        public bool IsScrollData { get; init; } = false;
    }
}
