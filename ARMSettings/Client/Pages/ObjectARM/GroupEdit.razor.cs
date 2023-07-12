using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace ARMSettings.Client.Pages.ObjectARM
{
    partial class GroupEdit
    {
        [Parameter]
        public P16xGroup Item { get; set; } = new();

        P16xGroup OldItem { get; set; } = new();

        [Parameter]
        public EventCallback<bool?> ActionBack { get; set; }

        bool IsProcessing = false;
        protected override void OnInitialized()
        {
            OldItem = new(Item);
        }


        async Task SaveItem()
        {
            if (string.IsNullOrEmpty(Item.GroupName))
                return;

            IsProcessing = true;

            if (!OldItem.Equals(Item))
            {
                if (Item.GroupID == 0)
                {
                    IntID response = new();
                    await Http.PostAsJsonAsync("api/v1/AddGroup", Item).ContinueWith(async x =>
                    {
                        if (x.Result.IsSuccessStatusCode)
                        {
                            response = await x.Result.Content.ReadFromJsonAsync<IntID>() ?? new();
                        }
                    });

                    if (response.ID == 0)
                    {
                        MessageView?.AddError("", ARMSetRep["ERROR_ADD_GROUP"] + " " + Item.GroupName);
                    }
                    else
                        await CloseModal(true);
                }
                else
                {
                    BoolValue response = new();
                    await Http.PostAsJsonAsync("api/v1/EditGroup", Item).ContinueWith(async x =>
                    {
                        if (x.Result.IsSuccessStatusCode)
                        {
                            response = await x.Result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
                        }
                    });

                    if (response.Value == false)
                    {
                        MessageView?.AddError("", ARMSetRep["ERROR_EDIT_GROUP"] + " " + Item.GroupName);
                    }
                    else
                        await CloseModal(true);
                }
            }
            else
            {
                await CloseModal();
            }
            IsProcessing = false;
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
