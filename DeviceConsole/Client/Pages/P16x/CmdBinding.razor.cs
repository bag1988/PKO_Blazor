using System.Net.Http.Json;
using Google.Protobuf;
using SMP16XProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using Label.V1;
using BlazorLibrary.GlobalEnums;
using FiltersGSOProto.V1;

namespace DeviceConsole.Client.Pages.P16x
{
    partial class CmdBinding : IPubSubMethod
    {
        int SubsystemID => SubsystemType.SUBSYST_P16x;

        private Dictionary<int, string> ThListCommand = new();

        private List<CCmdListWithSit>? Old_m_vecCmdArray = new();

        private List<CContrDevice>? m_ComboP16xDevice;

        private List<CmdInfo>? m_CmdList = new();
        private Tuple<bool, CCmdListWithSit>? SelectItem = null;

        private List<Objects>? m_psaAllSitInfo;

        private List<Objects>? SelectSit = null;

        private bool IsAddCommand = false;
        private bool IsDeleteCommand = false;

        private string NewNameCmd = "";
        private int NewCmdId = 1;
        bool IsProcessing = false;
        private int m_ComboP16xDeviceSelect = 0;

        TableVirtualize<Tuple<bool, CCmdListWithSit>>? table;

        protected override async Task OnInitializedAsync()
        {
            ThListCommand = new Dictionary<int, string>
            {
                { -1, "№" },
                { -2, SMP16xFormRep["IDS_STRING_CMD_P16X"] },//Команды П160 (П164) или ПДУ
                { -3, SMP16xFormRep["IDS_STRING_MESSAGE_RECORD"] },//Запись сообщения
                { -4, SMP16xFormRep["IDS_STRING_MODE"] },//Режим
                { -5, SMP16xFormRep["IDS_STRING_NAME_SITS"] }//Наименование сценария
            };

            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;
            request.NObjType = 8;

            await GetItems_IControllingDevice();
            await GetSituationList();
            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep["IDS_STRING_NAME"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), AsoRep["IDS_STRING_COMMENT"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.SubSystemId), SMP16xFormRep["IDS_SUBSYSTEM"], TypeHint.OnlySelect, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpSubName)));

            await OnInitFiltr(RefreshTableSit, FiltrName.FiltrSitObjects);

        }

        ItemsProvider<Tuple<bool, CCmdListWithSit>> GetProvider => new ItemsProvider<Tuple<bool, CCmdListWithSit>>(ThListCommand, LoadChildList, request, new List<int>() { 20, 20 });

        private async ValueTask<IEnumerable<Tuple<bool, CCmdListWithSit>>> LoadChildList(GetItemRequest req)
        {
            List<Tuple<bool, CCmdListWithSit>> response = new();

            if (m_ComboP16xDeviceSelect > 0)
            {
                List<CGetSituationInfo> sitInfo = new();
                var result = await Http.PostAsJsonAsync("api/v1/GetSituationInfo", new OBJ_ID(request.ObjID) { ObjID = m_ComboP16xDeviceSelect }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    sitInfo = await result.Content.ReadFromJsonAsync<List<CGetSituationInfo>>() ?? new();
                }
                else
                {
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_INFO_CMD"]);
                }
                Old_m_vecCmdArray = new();
                if (sitInfo.Count > 0)
                {
                    foreach (var item in sitInfo)
                    {
                        var cmdFirst = m_CmdList?.FirstOrDefault(x => x.CmdCode?.CmdID?.Equals(item.CStaffSitItem?.CmdID) ?? false);

                        Old_m_vecCmdArray.Add(new CCmdListWithSit()
                        {
                            SitName = item.SitName,
                            SitItem = item.CStaffSitItem,
                            CmdName = cmdFirst?.CmdName ?? string.Empty,
                            ConfirmMode = cmdFirst?.CmdCode?.ConfirmMode ?? 0,
                            CustomMessage = cmdFirst?.CmdCode?.CustomMessage == true ? 1 : 0
                        });
                    }
                    foreach (var item in Old_m_vecCmdArray.OrderBy(x => x.SitItem.CmdID.ObjID).GroupBy(x => x.SitItem.CmdID))
                    {
                        foreach (var t in item)
                        {
                            response.Add(new Tuple<bool, CCmdListWithSit>((item.Count() == 0 || t.Equals(item.First()) ? true : false), new CCmdListWithSit(t)));
                        }
                    }
                }
            }
            return response;
        }

        private ValueTask<IEnumerable<Hint>> LoadHelpSubName(GetItemRequest req)
        {
            List<Hint>? newData = new()
            {
                new Hint(GetSubName(SubsystemType.SUBSYST_ASO), SubsystemType.SUBSYST_ASO.ToString()),
                new Hint(GetSubName(SubsystemType.SUBSYST_SZS), SubsystemType.SUBSYST_SZS.ToString()),
                new Hint(GetSubName(SubsystemType.SUBSYST_GSO_STAFF), SubsystemType.SUBSYST_GSO_STAFF.ToString())
            };
            return new(newData);
        }

        private async Task RefreshTable()
        {
            SelectItem = null;
            if (table != null)
                await table.ResetData();
        }

        private async Task RefreshTableSit()
        {
            SelectSit = null;
            await GetSituationList();
        }


        private async Task OnOK()
        {
            if (table == null)
                return;

            var currList = table.GetCurrentItems?.Select(x => x.Item2).ToList() ?? new();

            if (currList.SequenceEqual(Old_m_vecCmdArray ?? new()))
                return;

            var FirstDevice = m_ComboP16xDevice?.FirstOrDefault(x => x.Unit?.ObjID == m_ComboP16xDeviceSelect)?.Unit ?? new();

            var deleteItem = Old_m_vecCmdArray?.Where(x => !currList.Any(m => m.SitItem?.SitID?.Equals(x.SitItem?.SitID) ?? false));
            if (deleteItem?.Count() > 0)
            {
                CStaffSitItemList DeleteRequest = new();
                DeleteRequest.Array.AddRange(deleteItem.Select(x => new SMP16XProto.V1.CStaffSitItem(x.SitItem) { UnitID = FirstDevice }));
                var result = await Http.PostAsJsonAsync("api/v1/DeleteSitItem", JsonFormatter.Default.Format(DeleteRequest), ComponentDetached);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_DEL_SIT"]);
                }
                Old_m_vecCmdArray = Old_m_vecCmdArray?.Except(deleteItem).ToList();
            }

            var insertItem = currList.Where(x => (!Old_m_vecCmdArray?.Any(m => m.SitItem?.SitID?.Equals(x.SitItem?.SitID) ?? false) ?? false) && x.SitItem?.SitID != null);

            if (insertItem?.Count() > 0)
            {
                CStaffSitItemList InsertRequest = new();
                InsertRequest.Array.AddRange(insertItem.Select(x => new SMP16XProto.V1.CStaffSitItem(x.SitItem) { UnitID = FirstDevice }));
                var result = await Http.PostAsJsonAsync("api/v1/UpdateSituation", JsonFormatter.Default.Format(InsertRequest));
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_ADD_SIT"]);
                }
            }

            var updateCmd = currList.Select(x => new CmdCode()
            {
                CmdID = x.SitItem?.CmdID ?? new(),
                ConfirmMode = x.ConfirmMode,
                CustomMessage = x.CustomMessage == 1 ? true : false,
                UnitID = FirstDevice
            }).Distinct();

            var Old_updateCmd = Old_m_vecCmdArray?.Select(x => new CmdCode()
            {
                CmdID = x.SitItem?.CmdID ?? new(),
                ConfirmMode = x.ConfirmMode,
                CustomMessage = x.CustomMessage == 1 ? true : false,
                UnitID = FirstDevice
            }).Distinct();

            if (Old_updateCmd != null)
                updateCmd = updateCmd.Except(Old_updateCmd);

            if (updateCmd.Count() > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/EditCommand", updateCmd, ComponentDetached);
                if (!result.IsSuccessStatusCode)
                {
                    MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_SOUND_REC_CHECK"]);
                }
            }

            Old_m_vecCmdArray = new(currList.Select(x => new CCmdListWithSit(x)));

        }

        private async Task ChangeSound(ChangeEventArgs e)
        {
            if (SelectItem == null)
                return;
            int.TryParse(e.Value?.ToString(), out int code);
            if (table != null)
            {
                await table.ForEachItems(x =>
                {
                    if (x.Item2.SitItem?.CmdID?.Equals(SelectItem.Item2.SitItem?.CmdID) ?? false)
                    {
                        x.Item2.CustomMessage = code;
                    }
                });
            }
        }

        private async Task ChangeMode(ChangeEventArgs e)
        {
            if (SelectItem == null)
                return;
            int.TryParse(e.Value?.ToString(), out int code);

            if (table != null)
            {
                await table.ForEachItems(x =>
                {
                    if (x.Item2.SitItem?.CmdID?.Equals(SelectItem.Item2.SitItem?.CmdID) ?? false)
                    {
                        x.Item2.ConfirmMode = code;
                    }
                });
            }
        }

        /// <summary>
        /// получаем список устройтсв
        /// </summary>
        /// <returns></returns>
        private async Task GetItems_IControllingDevice()
        {
            m_ComboP16xDevice = null;
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IControllingDevice", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                m_ComboP16xDevice = await result.Content.ReadFromJsonAsync<List<CContrDevice>>();

                if (m_ComboP16xDevice?.Count > 0)
                {
                    m_ComboP16xDeviceSelect = m_ComboP16xDevice.FirstOrDefault()?.Unit?.ObjID ?? 0;
                    await UpdateCommandList();
                    await RefreshTable();
                }
            }
            else
            {
                m_ComboP16xDevice = new();
                MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_INFO_DEVICES"]);
            }
        }

        /// <summary>
        /// Получаем список команд для устройства
        /// </summary>
        /// <param name="m_ComboP16xDeviceSelect"></param>
        /// <returns></returns>
        private async Task UpdateCommandList()
        {
            if (m_ComboP16xDeviceSelect == 0)
            {
                m_CmdList = new();
                return;
            }
            m_CmdList = null;

            var result = await Http.PostAsJsonAsync("api/v1/GetUnitCommandList", new OBJ_ID(request.ObjID) { ObjID = m_ComboP16xDeviceSelect }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                m_CmdList = await result.Content.ReadFromJsonAsync<List<CmdInfo>>();
            }
            else
            {
                m_CmdList = new();
                MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_CMD_LIST"]);
            }
        }

        private async Task GetUnitCommandList(ChangeEventArgs e)
        {
            int.TryParse(e.Value?.ToString(), out m_ComboP16xDeviceSelect);
            await UpdateCommandList();
            await RefreshTable();
        }

        private async Task RemoveCommand()
        {
            if (SelectItem == null || SelectItem.Item2.SitItem == null)
                return;
            List<CmdInfo> request = new()
            {
                new CmdInfo() { CmdName = "deleted", CmdCode = new() { CmdID = new(SelectItem.Item2.SitItem.CmdID) { SubsystemID = SubsystemType.SUBSYST_P16x } } }
            };
            var result = await Http.PostAsJsonAsync("api/v1/RemoveCommand", request, ComponentDetached);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_DEL_CMD"]);
            }
            else
            {
                if (table != null)
                {
                    await table.RemoveAllItem(x => x.Item2.SitItem?.CmdID?.ObjID == request.FirstOrDefault()?.CmdCode?.CmdID?.ObjID);
                }
            }
            IsDeleteCommand = false;
            SelectItem = null;
        }

        private async Task AddCommand()
        {
            if (NewCmdId == 0)
                return;
            IsProcessing = true;
            List<CmdInfo> request = new()
            {
                new CmdInfo() { CmdName = NewNameCmd, CmdCode = new() { CmdID = new() { ObjID = NewCmdId, SubsystemID = SubsystemType.SUBSYST_P16x } } }
            };
            var result = await Http.PostAsJsonAsync("api/v1/AddCommand", request, ComponentDetached);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", SMP16xFormRep["IDS_STRING_ERR_ADD_CMD"]);
            }
            else
            {
                if (table != null)
                {
                    await table.AddItem(new Tuple<bool, CCmdListWithSit>(true, new CCmdListWithSit()
                    {
                        CmdName = NewNameCmd,
                        SitItem = new()
                        {
                            CmdID = new() { ObjID = NewCmdId },
                            SitID = new(),
                            UnitID = new()

                        }
                    }));
                }
            }
            IsAddCommand = false;
            IsProcessing = false;
        }

        /// <summary>
        /// Получаем сценарии систем
        /// </summary>
        /// <returns></returns>
        private async Task GetSituationList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ISituationForFiltr", request, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                m_psaAllSitInfo = await result.Content.ReadFromJsonAsync<List<Objects>>();
            }
            else
            {
                m_psaAllSitInfo = new();
                MessageView?.AddError("", SMP16xFormRep["ERROR_GET_SIT_OBJ"]);
            }
        }

        private async Task DeleteSit()
        {
            try
            {
                if (SelectItem?.Item2?.SitItem?.SitID?.ObjID > 0)
                {
                    if (table != null)
                    {
                        if (!SelectItem.Item1)
                        {
                            var nextItem = table.GetNextOrFirstItem;
                            await table.RemoveItem(SelectItem);
                            SelectItem = nextItem;
                        }
                        else
                        {
                            var nextItem = table.GetNextItemMatch(x => !x.Item1 && x.Item2.SitItem.CmdID?.ObjID > 0 && x.Item2.SitItem.CmdID?.ObjID == SelectItem.Item2.SitItem.CmdID?.ObjID && !x.Item2.SitItem.SitID.Equals(SelectItem.Item2.SitItem.SitID));
                            if (nextItem != null && nextItem.Item2.SitItem.CmdID?.ObjID == SelectItem.Item2.SitItem.CmdID?.ObjID && !nextItem.Item2.SitItem.SitID.Equals(SelectItem.Item2.SitItem.SitID))
                            {
                                await table.RemoveItem(nextItem);
                                await table.ForEachItems(x =>
                                {
                                    if (x.Item2.SitItem.CmdID?.ObjID > 0 && x.Item2.SitItem.CmdID.Equals(SelectItem.Item2.SitItem.CmdID) && x.Item2.SitItem.SitID.Equals(SelectItem.Item2.SitItem.SitID))
                                    {
                                        x.Item2.SitItem.SitID = nextItem.Item2.SitItem.SitID;
                                        x.Item2.SitName = nextItem.Item2.SitName;
                                        return;
                                    }
                                });
                                SelectItem.Item2.SitItem.SitID = nextItem.Item2.SitItem.SitID;
                                SelectItem.Item2.SitName = nextItem.Item2.SitName;
                            }
                            else
                            {
                                await table.ForEachItems(x =>
                                {
                                    if (x.Item2.SitItem.CmdID?.ObjID > 0 && x.Item2.SitItem.CmdID.Equals(SelectItem.Item2.SitItem.CmdID) && x.Item2.SitItem.SitID.Equals(SelectItem.Item2.SitItem.SitID))
                                    {
                                        x.Item2.SitItem.SitID = null;
                                        x.Item2.SitName = string.Empty;
                                        return;
                                    }
                                });
                                SelectItem.Item2.SitItem.SitID = null;
                                SelectItem.Item2.SitName = string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private async Task AddSit()
        {
            if (SelectSit == null || SelectSit.Count == 0)
                return;

            if (table == null)
                return;

            if (SelectItem == null || SelectItem.Item2.SitItem == null || SelectItem.Item2.SitItem.CmdID == null)
                return;

            int insertIndex = table.IndexOfItem(SelectItem) + 1;

            foreach (var item in SelectSit)
            {
                if (table.CountItemMatch(x => (x.Item2.SitItem?.CmdID?.Equals(SelectItem.Item2.SitItem?.CmdID) ?? false) && x.Item2.SitItem?.SitID?.ObjID > 0) > 0)
                {
                    var newItem = new CCmdListWithSit(SelectItem.Item2);
                    newItem.SitName = item.Name;
                    newItem.SitItem.SitID = new OBJ_ID(item.OBJID);
                    await table.InsertItem(insertIndex, new Tuple<bool, CCmdListWithSit>(false, newItem));
                }
                else
                {
                    var first = table.FindItemMatch(x => (x.Item2.SitItem?.CmdID?.Equals(SelectItem.Item2.SitItem?.CmdID) ?? false));
                    if (first != null)
                    {
                        first.Item2.SitName = item.Name;
                        first.Item2.SitItem.SitID = new OBJ_ID(item.OBJID);
                    }
                }
            }

            SelectSit = null;
        }

        private Task SetSelectList(List<Objects>? newItems = null)
        {
            SelectSit = newItems;
            return Task.CompletedTask;
        }


        private void OnSelectCmd(List<Tuple<bool, CCmdListWithSit>>? item)
        {
            SelectItem = item?.FirstOrDefault();
            SelectSit = null;
        }

        string? GetNameSit(CCmdListWithSit item)
        {
            return item.SitItem?.SitID?.SubsystemID switch
            {
                SubsystemType.SUBSYST_ASO => $"{item.SitName} ({SMDataRep["SUBSYST_ASO"]})",
                SubsystemType.SUBSYST_SZS => $"{item.SitName} ({SMDataRep["SUBSYST_SZS"]})",
                SubsystemType.SUBSYST_GSO_STAFF => $"{item.SitName} ({SMDataRep["SUBSYST_STAFF"]})",
                _ => null
            };
        }

        string GetSubName(int subSystemId)
        {
            return subSystemId switch
            {
                SubsystemType.SUBSYST_ASO => SMDataRep["SUBSYST_ASO"],
                SubsystemType.SUBSYST_SZS => SMDataRep["SUBSYST_SZS"],
                SubsystemType.SUBSYST_GSO_STAFF => SMDataRep["SUBSYST_STAFF"],
                _ => string.Empty
            };
        }

        private List<Objects> ShowSituationList
        {
            get
            {
                if (SelectItem?.Item2.SitItem?.CmdID == null || SelectItem.Item2.SitItem.CmdID.ObjID == 0)
                    return new();

                OBJ_ID pCmdID = SelectItem.Item2.SitItem.CmdID;

                List<Objects> m_SitList = new();

                var listSitItem = table?.GetCurrentItems?.Where(x => x.Item2.SitItem?.CmdID?.Equals(pCmdID) ?? false);

                if (m_psaAllSitInfo?.Count > 0)
                {
                    foreach (var item in m_psaAllSitInfo)
                    {
                        if (!listSitItem?.Any(x => x.Item2.SitItem?.SitID?.Equals(item.OBJID) ?? false) ?? false)
                        {
                            m_SitList.Add(new Objects(item) { Name = item.Name + $" ({GetSubName(item.OBJID.SubsystemID)})" });
                        }
                    }
                }
                return m_SitList;
            }

        }
    }
}
