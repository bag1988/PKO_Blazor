using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorLibrary.Models
{
    public class FiltrCookieItem
    {
        public FiltrCookieItem()
        {
            UserName = string.Empty;
            Filters = new();
        }
        public FiltrCookieItem(string userName, FiltrRequestItem? filters = null)
        {
            UserName = userName;
            Filters = filters ?? new();
        }

        public string UserName { get; set; }

        public FiltrRequestItem Filters { get; set; }

    }

    public class FiltrRequestItem
    {
        public List<FiltrItem>? LastRequest { get; set; }

        public List<List<FiltrItem>> HistoryRequest { get; set; } = new();
    }
}
