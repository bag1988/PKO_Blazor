using System.Linq;
using System.Net.Http.Json;
using System.Security.AccessControl;
using BlazorLibrary.Models;
using Google.Protobuf.WellKnownTypes;
using ArmODProto.V1;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using Label.V1;

namespace ARMSettings.Client.Pages.ObjectARM
{
    partial class P16xObjectList : IAsyncDisposable
    {
        List<P16xGroup>? GroupList { get; set; } = null;

        List<GroupCommand>? CommandList { get; set; } = null;

        FillNodeGroupItems? SelectList { get; set; } = null;

        ReDrawList? SelectItem { get; set; } = null;

        bool IsAddGroup = false;

        bool IsDeleteGroup = false;

        bool IsAddObject = false;

        bool IsDeleteObject = false;

        public class CacheItem
        {
            public FillNodeGroupItems Key { get; set; }
            public List<FillNodeGroupItems>? Childs { get; set; }

            public CacheItem(FillNodeGroupItems key, List<FillNodeGroupItems>? childs = null)
            {
                Key = key;
                Childs = childs;
            }
        }

        readonly List<CacheItem> CacheChild = new();

        TableVirtualize<ReDrawList>? table;

        protected override async Task OnInitializedAsync()
        {
            request.NObjType = 8;
            ThList = new Dictionary<int, string>
            {
                { 0, ARMSetRep["NAME_OBJECT"] },//Наименование объекта
                { 1, SMDataRep["SUBSYST_SZ"] },//УЗС
                { 2, SMDataRep["SUBSYST_GSO_STAFF"] },//ПУ
                { 3, ARMSetRep["DEVICE_CONTROL"] },//Контроль устройств
            };

            request.ObjID.StaffID = await _User.GetLocalStaff();

            await P16xObjectList_ReDraw_GroupList();


            HintItems.Add(new HintItem(nameof(FiltrModel.NameObject), ARMSetRep["NAME_OBJECT"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.NameUUZS), SMDataRep["SUBSYST_SZ"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.NameStaff), SMDataRep["SUBSYST_GSO_STAFF"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Shedule), ARMSetRep["DEVICE_CONTROL"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrObjectArm);

        }


        ItemsProvider<ReDrawList> GetProvider => new ItemsProvider<ReDrawList>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<ReDrawList>> LoadChildList(GetItemRequest req)
        {
            List<ReDrawList> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/P16xObjectList_ReDraw_List", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<ReDrawList>>() ?? new();

                //if (CacheChild?.Count > 0)
                //{
                //    var list = CacheChild.FirstOrDefault(x => x.Key.GroupID == SelectList?.GroupID)?.Childs ?? new();
                //    newData = newData.Where(x => !list.Any(n => n.ObjectID == x.ObjectID)).ToList();
                //}
                //if (SelectItem != null && (!newData.Any(x => x.ObjectID == SelectItem.ObjectID)))
                //    SelectItem = null;
            }
            return newData;
        }


        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        void NewOrEditGroup(bool? isEdit = true)
        {
            if (isEdit == true && GetSelectGroup == null)
                return;

            if (isEdit == false)
            {
                SelectList = null;
            }
            IsAddGroup = true;
            StateHasChanged();
        }

        IEnumerable<ChildItems<FillNodeGroupItems>>? GetItemsGroup
        {
            get
            {
                List<ChildItems<FillNodeGroupItems>> response = new();
                if (GroupList != null)
                {
                    response.AddRange(GroupList.Select(x => new ChildItems<FillNodeGroupItems>(new FillNodeGroupItems() { GroupID = x.GroupID, GroupName = x.GroupName, ObjectID = x.GroupTypeID, ObjectName = $"{x.GroupName} ({x.GroupTypeID})" }, GetChildItems)));
                }
                return response;
            }
        }

        IEnumerable<ChildItems<FillNodeGroupItems>>? GetChildItems(FillNodeGroupItems item)
        {
            if (CacheChild.Any(x => x.Key.Equals(item)))
            {
                return CacheChild.First(x => x.Key.Equals(item)).Childs?.Select(x => new ChildItems<FillNodeGroupItems>(x));
            }
            return null;
        }

        void NewOrEdit(bool? isEdit = true)
        {
            if (isEdit == true && SelectItem == null)
                return;

            if (isEdit == false)
            {
                SelectItem = null;
            }

            IsAddObject = true;
        }

        async Task ActionBackGroup(bool? isUpdate = false)
        {
            if (isUpdate == true)
            {
                await P16xObjectList_ReDraw_GroupList();
            }
            IsAddGroup = false;
        }

        async Task ActionBackObject(bool? isUpdate = false)
        {
            if (isUpdate == true)
            {
                await CallRefreshData();
            }
            IsAddObject = false;
        }

        private async Task P16xObjectList_ReDraw_GroupList()
        {
            var result = await Http.PostAsync("api/v1/P16xObjectList_ReDraw_GroupList", null);
            if (result.IsSuccessStatusCode)
            {
                GroupList = await result.Content.ReadFromJsonAsync<List<P16xGroup>>() ?? new();
            }

            if (GroupList == null)
            {
                GroupList = new();
            }

            var d = GroupList.Select(x => new CacheItem(new FillNodeGroupItems() { GroupID = x.GroupID, GroupName = x.GroupName, ObjectID = x.GroupTypeID, ObjectName = $"{x.GroupName} ({x.GroupTypeID})" }));

            foreach (var item in d)
            {
                var key = CacheChild.FirstOrDefault(x => x.Key.GroupID == item.Key.GroupID)?.Key;

                if (key == null)
                {
                    CacheChild.Add(new(item.Key));
                }
                else if (key.GroupName != item.Key.GroupName || key.ObjectName != item.Key.ObjectName)
                {
                    CacheChild.First(x => x.Key.GroupID == item.Key.GroupID).Key = item.Key;
                }
            }
            if (SelectList != null)
            {
                SelectList = d.FirstOrDefault(x => x.Key.GroupID == SelectList.GroupID)?.Key;
            }
        }

        async Task SetSelectList(List<FillNodeGroupItems>? items)
        {
            var elemLast = items?.FirstOrDefault();

            var elem = GroupList?.FirstOrDefault(x => x.GroupID == elemLast?.GroupID && x.GroupName == elemLast?.GroupName && x.GroupTypeID == elemLast?.ObjectID && $"{x.GroupName} ({x.GroupTypeID})" == elemLast?.ObjectName);

            if (elem != null && (!elemLast?.Equals(SelectList) ?? true) && elemLast?.GroupID != SelectList?.GroupID)
            {
                SelectList = elemLast;
                await GetGroupCommandList(new IntID() { ID = elem.GroupID });
                await P16xObjectList_FillNode_GroupItems(new IntID() { ID = elem.GroupID });
            }
            SelectList = elemLast;
        }

        /// <summary>
        /// Получаем список команд
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task GetGroupCommandList(IntID request)
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetGroupCommandList", request);
            if (result.IsSuccessStatusCode)
            {
                CommandList = await result.Content.ReadFromJsonAsync<List<GroupCommand>>() ?? new();
            }
        }

        /// <summary>
        /// Удаляем группу
        /// </summary>
        /// <returns></returns>
        async Task RemoveGroup()
        {
            if (GetSelectGroup == null)
                return;
            IntID request = new() { ID = GetSelectGroup.GroupID };
            BoolValue response = new();
            var result = await Http.PostAsJsonAsync("api/v1/RemoveGroup", request);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
            }

            if (response.Value == true)
            {
                IsDeleteGroup = false;
                GroupList?.Remove(GetSelectGroup);
                SelectList = null;
            }
            else
            {
                MessageView?.AddError("", ARMSetRep["ERROR_DELETE_GROUP"] + " " + GetSelectGroup.GroupName);
            }
        }


        /// <summary>
        /// Удаляем объект
        /// </summary>
        /// <returns></returns>
        private async Task P16xObjectList_ToolStripButtonRemoveObject_Click()
        {
            if (SelectItem == null)
                return;
            IntID request = new() { ID = SelectItem.ObjectID };
            BoolValue response = new();
            var result = await Http.PostAsJsonAsync("api/v1/P16xObjectList_ToolStripButtonRemoveObject_Click", request);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
            }

            if (response.Value == true)
            {
                ReDrawList? nextElem = null;
                if (table != null)
                    nextElem = table.GetCurrentItems?.LastOrDefault() != SelectItem ? table.GetNextOrFirstItem : null;

                IsDeleteObject = false;

                if (table != null)
                    await table.RemoveItem(SelectItem);
                if (nextElem != null)
                    SelectItem = nextElem;
                else
                    SelectItem = table?.GetCurrentItems?.LastOrDefault();
            }
            else
            {
                MessageView?.AddError("", ARMSetRep["ERROR_DELETE_OBJECT"] + " " + SelectItem.ObjectName);
            }
        }

        FillNodeGroupItems? GetSelectObj
        {
            get
            {
                return CacheChild.FirstOrDefault(x => x.Key.GroupID == SelectList?.GroupID)?.Childs?.FirstOrDefault(x => x.ObjectID == SelectList?.ObjectID);
            }
        }


        P16xGroup? GetSelectGroup
        {
            get
            {
                return GroupList?.FirstOrDefault(x => x.GroupID == SelectList?.GroupID && x.GroupName == SelectList?.GroupName && x.GroupTypeID == SelectList?.ObjectID && $"{x.GroupName} ({x.GroupTypeID})" == SelectList?.ObjectName);
            }
        }

        /// <summary>
        /// Удаляем объект из группы
        /// </summary>
        /// <returns></returns>
        async Task DeleteItemToGroup()
        {
            if (GetSelectObj == null)
                return;

            FillNodeGroupItems request = new()
            {
                GroupID = GetSelectObj.GroupID,
                ObjectID = GetSelectObj.ObjectID
            };

            BoolValue response = new();
            var result = await Http.PostAsJsonAsync("api/v1/P16xObjectList_ToolStripButtonRemoveGroup_Click_DeleteCount", request);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
            }

            if (response.Value == true)
            {
                var k = CacheChild.FirstOrDefault(x => x.Key.GroupID == SelectList?.GroupID)?.Key;
                var l = CacheChild.FirstOrDefault(x => x.Key.GroupID == SelectList?.GroupID)?.Childs;

                var index = l?.IndexOf(SelectList ?? new()) ?? 0;

                l?.RemoveAt(index);

                if (l?.Count > 0)
                {
                    if (index >= 0 && index < l.Count)
                        SelectList = l.ElementAtOrDefault(index);
                    else
                        SelectList = l.LastOrDefault();
                }
                else
                {
                    SelectList = k;
                }
            }
            else
            {
                MessageView?.AddError("", string.Format(ARMSetRep["ERROR_DELETE_OBJ_GROUP"], new object[] { GetSelectObj.ObjectName, GetSelectObj.GroupName }));
            }
        }


        /// <summary>
        /// Добавляем объект в группу
        /// </summary>
        /// <returns></returns>
        async Task AddItemToGroup()
        {
            if (SelectItem == null || SelectList == null || SelectList.GroupID == 0)
                return;

            FillNodeGroupItems request = new()
            {
                GroupID = SelectList.GroupID,
                ObjectID = SelectItem.ObjectID
            };

            BoolValue response = new();
            var result = await Http.PostAsJsonAsync("api/v1/P16xObjectList_ToolStripButtonAddObjectInToGroup_Click_Insert", request);
            if (result.IsSuccessStatusCode)
            {
                response = await result.Content.ReadFromJsonAsync<BoolValue>() ?? new();
            }

            if (response.Value == true)
            {
                //ReDrawList? nextElem = null;
                //if (table != null)
                //    nextElem = table.GetCurrentItems?.LastOrDefault() != SelectItem ? table.GetNextOrFirstItem : null;

                var key = CacheChild.FirstOrDefault(x => x.Key.GroupID == request.GroupID)?.Key;

                if (key != null)
                {
                    CacheChild.RemoveAll(x => x.Key.Equals(key));
                }

                await P16xObjectList_FillNode_GroupItems(new IntID() { ID = SelectList.GroupID });

                //if (nextElem != null)
                //    SelectItem = nextElem;
                //else
                //    SelectItem = table?.GetCurrentItems?.LastOrDefault();

            }
            else
            {
                MessageView?.AddError("", ARMSetRep["ERROR_ADD_TO_GROUP"] + " " + SelectItem.ObjectName);
            }
        }

        /// <summary>
        /// Получаем список элементов группы
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task P16xObjectList_FillNode_GroupItems(IntID request)
        {
            var elem = GroupList?.FirstOrDefault(x => x.GroupID == request.ID);

            if (elem != null)
            {
                var key = new FillNodeGroupItems()
                {
                    GroupID = elem.GroupID,
                    GroupName = elem.GroupName,
                    ObjectID = elem.GroupTypeID,
                    ObjectName = $"{elem.GroupName} ({elem.GroupTypeID})"
                };

                if (!CacheChild.Any(x => x.Key.Equals(key)) || CacheChild.First(x => x.Key.Equals(key)).Childs == null || CacheChild.First(x => x.Key.Equals(key)).Childs?.Count == 0)
                {
                    var result = await Http.PostAsJsonAsync("api/v1/P16xObjectList_FillNode_GroupItems", request);
                    if (result.IsSuccessStatusCode)
                    {
                        var response = await result.Content.ReadFromJsonAsync<List<FillNodeGroupItems>>();

                        if (CacheChild.Any(x => x.Key.Equals(key)))
                            CacheChild.First(x => x.Key.Equals(key)).Childs = response;
                        else
                            CacheChild.Add(new(key, response));
                    }
                }
            }

        }

        /// <summary>
        /// Изменяем наименование команды
        /// </summary>
        /// <param name="e"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        async Task ChangeCommandName(ChangeEventArgs e, GroupCommand item)
        {
            item.CommandName = e.Value?.ToString() ?? "";
            await Http.PostAsJsonAsync("api/v1/AddGroupCommand", item);
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }

    }
}
