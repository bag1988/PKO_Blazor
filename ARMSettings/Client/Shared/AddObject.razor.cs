using System.Net.Http.Json;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace ARMSettings.Client.Shared
{
    partial class AddObject
    {
        [Parameter]
        public EventCallback<List<OBJ_ID>?> ActionBack { get; set; }

        private List<SituationItem>? Model { get; set; }

        List<SituationItem>? SelectedList
        {
            get
            {
                var result = Model?.Where(x => SelectItems?.Any(s => s.SubsystemID == subSystem && x.SitID == s.ObjID) ?? false).ToList();
                return result;
            }
        }

        List<OBJ_ID>? SelectItems { get; set; } = null;

        private int subSystem = 1;
        private Dictionary<int, string>? thList;
        readonly GetItemRequest request = new() { ObjID = new OBJ_ID(), LSortOrder = 0, BFlagDirection = 0 };

        protected override async Task OnInitializedAsync()
        {
            thList = new Dictionary<int, string>
            {
                { -1, GsoRep["IDS_STRING_NAME"] },     //Cценарий
                { -2, ARMSetRep["CODE"] },                //Код
                { -3, AsoRep["IDS_STRING_COMMENT"]  }, //Комментарий
                { -4, ARMSetRep["PRIO"] },                //Приоритет
            };
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.NObjType = await _User.GetUserSessId();
            await GetList();
        }
        private async Task GetList()
        {
            request.ObjID.SubsystemID = subSystem;
            var x = await Http.PostAsJsonAsync("api/v1/GetItems_ISituation_WithObjId", request);
            if (x.IsSuccessStatusCode)
            {
                Model = await x.Content.ReadFromJsonAsync<List<SituationItem>>() ?? new();
            }
            if (Model == null)
                Model = new();
        }


        private async Task HandleSubsystemChanged(ChangeEventArgs e)
        {
            subSystem = Convert.ToInt32(e.Value);
            await GetList();
        }

        private void AddItem(List<SituationItem> items)
        {
            if (SelectItems == null)
                SelectItems = new();

            SelectItems.RemoveAll(x => x.SubsystemID == subSystem && !items.Any(i => i.SitID == x.ObjID));

            items.RemoveAll(x => SelectItems.Any(i => i.ObjID == x.SitID && i.SubsystemID == subSystem));

            SelectItems.AddRange(items.Select(x => new OBJ_ID()
            {
                ObjID = x.SitID,
                StaffID = request.ObjID.StaffID,
                SubsystemID = subSystem
            }));
        }

        private async Task Confirm()
        {
            await CallBack(SelectItems);
        }

        async Task CallBack(List<OBJ_ID>? request = null)
        {
            if (ActionBack.HasDelegate)
            {
                await ActionBack.InvokeAsync(request);
            }
        }

    }
}
