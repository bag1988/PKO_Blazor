using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.FolderForInherits
{
    public class TableParams<TItem> : MultiSelect<TItem>
    {
        [Parameter]
        public RenderFragment? Thead { get; set; }

        [Parameter]
        public virtual RenderFragment<TItem>? Tbody { get; set; }

        [Parameter, AllowNull]
        public RenderFragment? TSticky { get; set; } = null;

        [Parameter]
        public int? Colspan { get; set; } = 1;

        [Parameter]
        public int? MinWidth { get; set; } = 900;

        [Parameter]
        public double? Devision { get; set; } = 1; //разделить на

        [Parameter]
        public bool? IsSticky { get; set; } = true;
    }
}
