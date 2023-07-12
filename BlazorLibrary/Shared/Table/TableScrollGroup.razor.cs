using Microsoft.AspNetCore.Components;


namespace BlazorLibrary.Shared.Table
{
    partial class TableScrollGroup<TItem, TKey>
    {
        [Parameter]
        public RenderFragment<Tuple<bool, TItem>>? TbodyGroup { get; set; }

        [Parameter]
        public IEnumerable<IGrouping<TKey, TItem>>? ItemsGroup { get; set; }

        [Parameter]
        public bool IsSelectGroup { get; set; } = false;

        protected override void OnParametersSet()
        {
            Items = ItemsGroup?.SelectMany(x => x);
        }
    }
}
