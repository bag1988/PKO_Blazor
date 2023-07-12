using System.ComponentModel;
using System.Net.Http.Json;
using AsoDataProto.V1;
using StaffDataProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using SharedLibrary.Interfaces;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class ViewStatListCache
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = 0;
    }
}
