using System.Net.Http.Json;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ARMsred.Client.Pages
{
    partial class P16xGateLog
    {
        private int? WindowHeight = 800;

        private P16xLog[]? Model = null;

        private int TypeLoad = 0;

        private P16xLog? SelectItem = null;

        private P16xDeviceUnit[]? ListForSelect;

        //private GetItemRequest? request = new GetItemRequest() { TagOBJKey = new tagOBJ_Key() { ObjID = new OBJ_ID() { SubsystemID = SubsystemType.SUBSYST_GSO_STAFF } }, LSortOrder = 1, BFlagDirection = 1 };

        private string Sound = "";
        private string Format = "";
        //private async void SetSort(int id)
        //{
        //    if (id == request!.LSortOrder)
        //        request.BFlagDirection = request.BFlagDirection == 1 ? 0 : 1;
        //    else
        //    {
        //        request.LSortOrder = id;
        //        request.BFlagDirection = 1;
        //    }
        //    await GetList();
        //}

        protected override async Task OnInitializedAsync()
        {
            WindowHeight = await JSRuntime.InvokeAsync<int>("GetWindowHeight");
            await GetListForSelect();
        }

        protected override async Task OnParametersSetAsync()
        {
            await GetList();
        }

        private async Task GetList(int? ParamId = null)
        {
            IntID Id = new IntID();

            Id.ID = ParamId ?? 0;

            string UrlLoad = "GetP16xLog";//принятые
            if (TypeLoad == 1)
                UrlLoad = "GetPRDLog";//отправленые


            await Http.PostAsJsonAsync("api/v1/" + UrlLoad, Id).ContinueWith(async x =>
             {
                 Model = await x.Result.Content.ReadFromJsonAsync<P16xLog[]>();
                 StateHasChanged();
             });
        }


        private async Task GetListForSelect(int id = 0)
        {
            string UrlLoad = "GetListGateLog";//принятые
            TypeLoad = id;
            if (id == 1)
                UrlLoad = "GetState";//отправленые
            ListForSelect = null;
            await Http.PostAsync("api/v1/" + UrlLoad, null).ContinueWith(async x =>
            {
                ListForSelect = await x.Result.Content.ReadFromJsonAsync<P16xDeviceUnit[]>();
            });
        }

        private async Task SetSelect(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                int.TryParse(e?.Value.ToString(), out int id);
                await GetList(id);
            }
        }

        private async Task SetSelectItem(ChangeEventArgs e)
        {
            if (e.Value != null)
            {
                int.TryParse(e?.Value.ToString(), out int id);
                await GetListForSelect(id);
                await GetList();
            }
        }


        private async Task SetAudioFile(List<P16xLog>? items)
        {
            SelectItem = items?.FirstOrDefault();
            if (SelectItem != null && SelectItem.MsgID > 0 && SelectItem.MsgStaffID > 0)
            {
                OBJ_ID OBJID = new OBJ_ID();
                OBJID.ObjID = SelectItem.MsgID;
                OBJID.StaffID = SelectItem.MsgStaffID;
                //string tempFile = $"{MsgID}.{MsgStaffID}.{0}.wav";
                await Http.PostAsJsonAsync("api/v1/GetMessageFile", OBJID).ContinueWith(async x =>
                {
                    var response = await x.Result.Content.ReadFromJsonAsync<SMDataServiceProto.V1.MsgParam>();
                    //if (response != null)
                    //{
                    //    Sound = ByteString.CopyFrom(response.Sound.Split("-").Select(x => Convert.ToByte(x)).ToArray());
                    //    Format = ByteString.CopyFrom(response.Format.Split("-").Select(x => Convert.ToByte(x)).ToArray());
                    //}
                });
            }
        }

    }
}
