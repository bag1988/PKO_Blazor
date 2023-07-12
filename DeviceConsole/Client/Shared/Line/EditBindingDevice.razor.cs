using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace DeviceConsole.Client.Shared.Line
{
    partial class EditBindingDevice
    {
        [Parameter]
        public int? LineID { get; set; }

        [Parameter]
        public BindingDevice? BindingDevice { get; set; }

        [Parameter]
        public BindingDevice? NewBindingDevice { get; set; }

        [Parameter]
        public EventCallback<BindingDevice?> Callback { get; set; }


        private List<BindingDevice>? BindingDeviceList = null;

        private BindingDevice? SelectItem = null;

        protected override async Task OnInitializedAsync()
        {
            await GetList();
        }

        private async Task GetList()
        {
            BindingDeviceList = new List<BindingDevice>();

            if (BindingDevice?.Name != GsoRep["IDS_STRING_DEVICE_NOT_PRESENT"])
                BindingDeviceList.Add(new BindingDevice() { Name = GsoRep["IDS_STRING_DEVICE_NOT_PRESENT"] });
            if (NewBindingDevice != null && BindingDevice != null)
                BindingDeviceList.Add(BindingDevice);

            await Http.PostAsync("api/v1/GetFreeChannelList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    BindingDeviceList.AddRange(await x.Result.Content.ReadFromJsonAsync<List<BindingDevice>>() ?? new());
                }
            });

            if (NewBindingDevice != null)
                BindingDeviceList.Remove(NewBindingDevice);
        }



        private async Task Close(BindingDevice? item = null)
        {
            if (Callback.HasDelegate)
            {
                await Callback.InvokeAsync(item);
            }
        }

    }
}
