using System.ComponentModel;
using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;

namespace DeviceConsole.Client.Shared.Situation
{
    partial class NotifyObjectWithMessages
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        [Parameter]
        public int? SitId { get; set; }

        [Parameter]
        public EventCallback CloseDialog { get; set; }

        private List<CNotifyObjectWithMessages>? Model = null;
        private Dictionary<int, string>? ThList;

        private int StaffId = 0;

        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();

            ThList = new Dictionary<int, string>
            {
                { -1, GsoRep["IDS_STRING_NAME"] },
                { -2, GsoRep["MessageText"] }
            };
            await GetList();
        }

        private async Task GetList()
        {
            if (SitId != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/SelectNotifyObjectWithMessages", new OBJ_ID() { ObjID = SitId.Value, StaffID = StaffId, SubsystemID = SubsystemID });
                if (result.IsSuccessStatusCode && result.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    Model = await result.Content.ReadFromJsonAsync<List<CNotifyObjectWithMessages>>() ?? new();
                }
            }
            if (Model == null)
                Model = new();
        }

        async Task Close()
        {
            if (CloseDialog.HasDelegate)
                await CloseDialog.InvokeAsync();
        }

    }
}
