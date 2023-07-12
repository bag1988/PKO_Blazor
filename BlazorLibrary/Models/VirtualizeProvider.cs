using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorLibrary.Shared;
using SharedLibrary.Extensions;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Models
{
    public class VirtualizeProvider<TItem>
    {
        public VirtualizeProvider(GetItemRequest request, Func<GetItemRequest, ValueTask<IEnumerable<TItem>>> items)
        {
            Request = request;
            Items = items;
            if (items != null)
                IsData = true;
        }

        public GetItemRequest Request { get; set; } = new() { ObjID = new() };

        public bool IsData { get; } = false;

        public bool IsLoadData { get; private set; } = false;

        public List<TItem> CacheItems { get; set; } = new();

        public void Reset()
        {
            CacheItems.Clear();
            Request.SkipItems = 0;
            IsScrollData = true;
        }


        public async Task AddData()
        {
            if (IsData && IsScrollData && !IsLoadData)
            {
                IsLoadData = true;
                Request.SkipItems = Request.SkipItems + Request.CountData;
                var newData = await Items.Invoke(Request) ?? new List<TItem>();
                IsScrollData = newData.Count() == Request.CountData;
                CacheItems.AddRange(newData.Except(CacheItems));
                IsLoadData = false;
            }
        }



        private bool IsScrollData = true;

        private Func<GetItemRequest, ValueTask<IEnumerable<TItem>>> Items { get; }
    }
}
