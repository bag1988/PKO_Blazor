using System.Net.Http.Json;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Shared.Line
{
    partial class AddUserParam
    {
        [Parameter]
        public int? LineType { get; set; }

        [Parameter]
        public EventCallback Callback { get; set; }

        private string? NameRestrict;

        private async Task AddRestrict()
        {
            if (LineType != null && !string.IsNullOrEmpty(NameRestrict))
            {
                AddRestrictRequest request = new AddRestrictRequest() { MLRestrictType = 1, MBstrName = NameRestrict, MLLineType = LineType.Value };

                await Http.PostAsJsonAsync("api/v1/AddRestrict", request).ContinueWith(async x =>
                {
                    if (!x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError(GSOFormRep["AddUserParam"], AsoRep["IDS_ERRORCAPTION"]);
                    }

                    await CallbackInvoke();
                });
            }
            else
            {
                MessageView?.AddError(GSOFormRep["AddUserParam"], DeviceRep["ErrorNull"] + " " + GSOFormRep["NameUserParam"]);
                return;
            }
        }

        private async Task CallbackInvoke()
        {
            if (Callback.HasDelegate)
                await Callback.InvokeAsync();
        }
    }
}
