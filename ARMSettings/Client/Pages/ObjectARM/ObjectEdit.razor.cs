using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using ArmODProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;

namespace ARMSettings.Client.Pages.ObjectARM
{
    partial class ObjectEdit
    {
        [Parameter]
        public EventCallback<bool?> ActionBack { get; set; }

        [Parameter]
        public ReDrawList Item { get; set; } = new();

        ReDrawList OldItem { get; set; } = new();

        List<PRD> ListPRD = new();

        List<StaffControlUnit> ListCu = new();

        List<PRD> ListUuzs = new();

        int TypeObject = 0;

        bool IsActive
        {
            get
            {
                return Item.ObjectState == 1;
            }
            set
            {
                if (value)
                    Item.ObjectState = 1;
                else
                    Item.ObjectState = -1;
            }
        }

        int GetStaffId
        {
            get
            {
                if (Item.StaffID > 0)
                {
                    return Item.StaffID;
                }
                return 0;
            }

            set
            {
                Item.StaffID = 0;
                if (ListCu.Any(x => x.StaffID == value))
                {
                    Item.StaffID = value;
                }

                if (Item.DevID > 0 && !ListPRD.Any(x => x.DevID == Item.DevID))
                {
                    Item.DevID = 0;
                }

            }
        }

        int GetDevId
        {
            get
            {
                if (TypeObject == 0)
                {
                    if (Item.DevID > 0 && ListPRD.Any(x => x.DevID == Item.DevID))
                    {
                        return Item.DevID;
                    }
                }
                if (TypeObject == 1)
                {
                    if (Item.DevID > 0 && ListUuzs.Any(x => x.DevID == Item.DevID))
                    {
                        return Item.DevID;
                    }
                }
                return 0;
            }

            set
            {
                Item.DevID = 0;
                if (TypeObject == 0)
                {
                    if (ListPRD.Any(x => x.DevID == value))
                    {
                        Item.DevID = value;
                    }
                    if (Item.StaffID == -1)
                        Item.StaffID = 0;
                }
                else
                {
                    Item.StaffID = -1;
                    if (ListUuzs.Any(x => x.DevID == value))
                    {
                        Item.DevID = value;
                    }
                }
            }
        }

        bool IsPageLoad = true;

        protected override async Task OnInitializedAsync()
        {
            OldItem = new(Item);
            await P16xObjectManage_ReDraw_PRDlist();
            await P16xObjectManage_ReDraw_StaffList();
            await P16xObjectManage_ReDraw_UZSlist();

            if (Item.DevID > 0 && ListUuzs.Any(x => x.DevID == Item.DevID))
                TypeObject = 1;

            IsPageLoad = false;
        }

        private async Task P16xObjectManage_ReDraw_PRDlist()
        {
            await Http.PostAsync("api/v1/P16xObjectManage_ReDraw_PRDlist", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ListPRD = await x.Result.Content.ReadFromJsonAsync<List<PRD>>() ?? new();
                }
            });
        }

        private async Task P16xObjectManage_ReDraw_StaffList()
        {
            await Http.PostAsync("api/v1/P16xObjectManage_ReDraw_StaffList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ListCu = await x.Result.Content.ReadFromJsonAsync<List<StaffControlUnit>>() ?? new();
                }
            });
        }

        private async Task P16xObjectManage_ReDraw_UZSlist()
        {
            await Http.PostAsync("api/v1/P16xObjectManage_ReDraw_UZSlist", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    ListUuzs = await x.Result.Content.ReadFromJsonAsync<List<PRD>>() ?? new();
                }
            });
        }

        async Task SaveItem()
        {
            if (!OldItem.Equals(Item))
            {
                var objId = await P16xObjectList_DoObjectManage_ObjectID(Item);

                if (objId.ID > 0)
                {
                    await P16xObjectList_DoObjectManage_Delete(objId);

                    string[] token = Item.Shedule.Split(',');

                    foreach (string SN in token)
                    {
                        int iSN = 0;
                        Int32.TryParse(SN, out iSN);
                        if (iSN == 0)
                            continue;
                        await P16xObjectList_DoObjectManage_Insert(new P16xGateObjectControl()
                        {
                            ObjectID = objId.ID,
                            SubsystemID = SubsystemType.SUBSYST_SZS,
                            ObjID = iSN
                        });
                    }
                }

                await CloseModal(true);
            }
            else
            {
                await CloseModal();
            }
        }

        private async Task<IntID> P16xObjectList_DoObjectManage_ObjectID(ReDrawList request)
        {
            IntID response = new();
            await Http.PostAsJsonAsync("api/v1/P16xObjectList_DoObjectManage_ObjectID", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();
                }
            });
            return response;
        }

        private async Task<BoolValue> P16xObjectList_DoObjectManage_Delete(IntID request)
        {
            BoolValue response = new();
            await Http.PostAsJsonAsync("api/v1/P16xObjectList_DoObjectManage_Delete", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                }
            });
            return response;
        }

        private async Task<BoolValue> P16xObjectList_DoObjectManage_Insert(P16xGateObjectControl request)
        {
            BoolValue response = new();
            await Http.PostAsJsonAsync("api/v1/P16xObjectList_DoObjectManage_Insert", request).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    response = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                }
            });
            return response;
        }

        async Task CloseModal(bool? isUpdate = false)
        {
            if (ActionBack.HasDelegate)
            {
                await ActionBack.InvokeAsync(isUpdate);
            }
        }
    }
}
