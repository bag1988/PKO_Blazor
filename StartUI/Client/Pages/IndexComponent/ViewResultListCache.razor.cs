using Microsoft.AspNetCore.Components;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class ViewResultListCache
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 0;
    }
}
