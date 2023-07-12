using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace StartUI.Client.Pages.IndexComponent
{
    partial class GetInfoActiv
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<string>? Info = null;

        protected override async Task OnInitializedAsync()
        {
            await GetActivNotify();
        }

        private async Task GetActivNotify()
        {
            OBJ_ID SessID = new() { ObjID = 0, SubsystemID = SubsystemID };

            Info = new();

            await Http.PostAsJsonAsync("api/v1/GetAppropriateNotifyInfo", SessID).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<StringValue>() ?? new();

                    if (!string.IsNullOrEmpty(r.Value))
                    {
                        Info = r.Value.Split("\n").ToList();
                    }
                }
                else
                {
                    MessageView?.AddError(StartUIRep["IDC_STATIC_ACTIVE_SIT_LIST"], AsoRep["IDS_STRING_ERR_GET_DATA"]);
                }
            });


        }
    }
}
