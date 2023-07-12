using System.Net.Http.Json;
using Google.Protobuf;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SituationItem = AsoDataProto.V1.SituationItem;

namespace DeviceConsole.Client.Pages.ASO.PattensMessage
{
    partial class CreatePatterns
    {
        [Parameter]
        public AbonMsgParam? Model { get; set; }

        AbonMsgParam NewModel { get; set; } = new();

        [Parameter]
        public EventCallback<bool?> CallBack { get; set; }

        List<SituationItem>? SitList { get; set; }

        List<IntAndString>? AbonList { get; set; }

        int StaffID = 0;

        protected override async Task OnInitializedAsync()
        {
            NewModel = new(Model);
            StaffID = await _User.GetLocalStaff();

            await GetSitList();
        }

        async Task SaveMsgParam()
        {
            if (NewModel.Equals(Model))
            {
                await CallEvent(null);
                return;
            }

            if (string.IsNullOrEmpty(NewModel.ParamName))
            {
                MessageView?.AddError("", DeviceRep["ErrorNull"] + " " + AsoRep["PARAM_NAME"]);
                return;
            }
            if (string.IsNullOrEmpty(NewModel.ParamValue))
            {
                MessageView?.AddError("", DeviceRep["ErrorNull"] + " " + AsoRep["PARAM_VALUE"]);
                return;
            }

            if (!string.IsNullOrEmpty(NewModel.AbonName) && NewModel.AbonID == 0)
            {

                MessageView?.AddError("", DeviceRep["ErrorNull"] + " " + AsoRep["IDS_ABONENT"]);
                return;
            }

            NewModel.ParamName = NewModel.ParamName.ToUpper();
            await DeleteMsgParam();
            NewModel.SubsystemID = SubsystemType.SUBSYST_ASO;
            NewModel.StaffID = StaffID;
            AbonMsgParamList request = new();
            request.Array.Add(NewModel);
            await Http.PostAsJsonAsync("api/v1/AddMsgParamList", JsonFormatter.Default.Format(request)).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    await CallEvent(true);
                }
                else
                {
                    MessageView?.AddError("", AsoRep["ERROR_SAVE_MSGPARAM"]);
                }
            });
        }


        async Task DeleteMsgParam()
        {
            if (Model != null)
            {
                await Http.PostAsJsonAsync("api/v1/DeleteMsgParam", new List<AbonMsgParam>() { Model });
            }
        }


        async Task Close()
        {
            await CallEvent(null);
        }

        async Task CallEvent(bool? b)
        {
            if (CallBack.HasDelegate)
                await CallBack.InvokeAsync(b);
        }

        async Task GetSitList()
        {
            await Http.PostAsJsonAsync("api/v1/GetItems_ISituation", new GetItemRequest() { NObjType = await _User.GetUserSessId(), ObjID = new OBJ_ID() { StaffID = StaffID, SubsystemID = SubsystemType.SUBSYST_ASO }, LSortOrder = 0, BFlagDirection = 0 }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    SitList = await x.Result.Content.ReadFromJsonAsync<List<SituationItem>>();
                }
            });
        }

        private async Task GetFiltrAbonForName(ChangeEventArgs e)
        {
            NewModel.AbonID = 0;
            NewModel.AbonName = e.Value?.ToString();
            if (e.Value == null || e.Value.ToString()?.Length < 3)
            {
                AbonList = null;
                return;
            }

            var result = await Http.PostAsJsonAsync("api/v1/GetFiltrAbonForName", new IntAndString() { Number = StaffID, Str = e.Value.ToString() }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                AbonList = await result.Content.ReadFromJsonAsync<List<IntAndString>>();
            }
            else
            {
                AbonList = new();
                MessageView?.AddError("", GsoRep["ERROR_ABON_LIST"]);
            }
            NewModel.AbonID = AbonList?.FirstOrDefault()?.Number ?? 0;
        }

        void SetAbName(FocusEventArgs e)
        {
            if (AbonList?.Count > 0)
            {
                NewModel.AbonName = AbonList.FirstOrDefault(x => x.Number == NewModel.AbonID)?.Str ?? "";

                if (string.IsNullOrEmpty(NewModel.AbonName))
                {
                    NewModel.AbonID = 0;
                }
            }
        }

    }
}
