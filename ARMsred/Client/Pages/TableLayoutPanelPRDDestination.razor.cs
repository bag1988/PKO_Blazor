using System.Net.Http.Json;
using System.Timers;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace ARMsred.Client.Pages
{
    partial class TableLayoutPanelPRDDestination : IAsyncDisposable
    {
        [Parameter]
        public int? GroupId { get; set; }

        [Parameter]
        public EventCallback<int> DbClick { get; set; }

        [Parameter]
        public EventCallback SetStopState { get; set; }

        [Parameter]
        public List<P16xGateDevice> SelectItem { get; set; } = new();

        private List<HardwareMonitor>? CUStateList;

        public List<P16xGateDevice>? stateArray { get; set; }

        readonly System.Timers.Timer timer = new(TimeSpan.FromSeconds(1));

        protected override void OnInitialized()
        {
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await GetList();
            }
            catch
            {

            }
        }

        private async Task GetList()
        {
            timer.Stop();
            bool isCancel = false;
            if (GroupId == null)
            {
                var result = await Http.PostAsync("api/v1/remote/GetState", null, ComponentDetached);

                if (result.IsSuccessStatusCode)
                {
                    stateArray = await result.Content.ReadFromJsonAsync<List<P16xGateDevice>>();
                }
            }
            else
            {
                S_GetGroupListLinkRequest request = new() { LGroupID = GroupId.Value, LUserID = await _User.GetUserId() };
                //GetP16xGateState
                var result = await Http.PostAsJsonAsync("api/v1/remote/S_GetGroupListLink", request, ComponentDetached);

                if (result.IsSuccessStatusCode)
                {
                    stateArray = await result.Content.ReadFromJsonAsync<List<P16xGateDevice>>();
                }
                else
                {
                    isCancel = true;
                    stateArray = new();
                }
            }
            if (stateArray == null)
                stateArray = new();

            if (ComponentDetached.IsCancellationRequested || isCancel)
                return;

            await GetCUStateList();
            timer.Start();
        }

        async Task SetSelectItem(P16xGateDevice item)
        {
            if (!SelectItem.Any(x => x.ObjectID == item.ObjectID))
                SelectItem.Add(item);
            else SelectItem.RemoveAll(x => x.ObjectID == item.ObjectID);

            if (SetStopState.HasDelegate)
                await SetStopState.InvokeAsync();
        }


        async Task ActionDbClick(P16xGateDevice item)
        {
            if (item.DevID > 0)
                return;

            if (DbClick.HasDelegate)
                await DbClick.InvokeAsync(item.StaffID);
        }

        private async Task GetCUStateList()
        {
            CUStateList = null;
            var result = await Http.PostAsJsonAsync("api/v1/remote/GetResultList", new OBJ_Key() { ObjType = (int)HMT.Staff, ObjID = new() }, ComponentDetached);

            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        CUStateList = JsonParser.Default.Parse<HardwareMonitorList>(json).Array.ToList();
                    }
                    catch
                    {
                        Console.WriteLine($"Error convert json to HardwareMonitorList");
                    }
                }
            }

            if (CUStateList == null)
            {
                CUStateList = new();
            }

            if (SetStopState.HasDelegate)
                await SetStopState.InvokeAsync();

            StateHasChanged();
        }

        private ButtonColor SetStatus(P16xGateDevice objectOO)
        {
            ButtonColor BC = new();
            // признак того что бэкграунд дефолтный, т.е. НЕ цветной
            bool isBackColorDefault = true;

            BC.BGColor = SelectItem.Any(x => x.ObjectID == objectOO.ObjectID) ? "gray" : GroupId == null ? "light" : "outline-gray";

            PimpAnswerState pstate = PimpAnswerState.NotFound;

            if (objectOO.LastCmd > 0)
            {
                //рисуем саму кнопку
                if (objectOO.StaffID != 0 && (uint)objectOO.StaffID != 0xFFFFFFFF && objectOO.DevID > 0)
                {
                    isBackColorDefault = false;
                    string startBG = "lightwarning", endBG = "lightwarning";
                    BC.BGColor = "lightwarning";


                    if (objectOO.StaffSitID > 0 && objectOO.StaffSessID > 0)
                    {
                        endBG = "warning";
                        if (objectOO.ODConfirm != 0)
                            endBG = "success";
                    }

                    if (objectOO.Confirm == 1) startBG = "warning";
                    else if (objectOO.Confirm == 2) startBG = "success";

                    if (startBG == endBG) { BC.BGColor = startBG; }
                    else
                    {
                        BC.BGColor = startBG + endBG;
                    }
                }
                //// далее отображение команды, заливка и цвет тектста
                else
                {
                    // Для ПРД
                    if (objectOO.StaffID == 0 && objectOO.DevID != 0)
                    {
                        if (objectOO.Confirm == 0) // Ожидание передачи команды
                        {
                            BC.BGColor = "lightwarning";
                            isBackColorDefault = false;
                        }
                        else
                        if (objectOO.Confirm == 1) // Команда передана
                        {
                            BC.BGColor = "warning";
                            isBackColorDefault = false;
                        }
                        else
                        if (objectOO.Confirm == 2) // Получено подтверждение
                        {
                            BC.BGColor = "success";
                            isBackColorDefault = false;
                        }

                    }
                    else if (objectOO.StaffID != 0 && (uint)objectOO.StaffID != 0xFFFFFFFF) // Для ПУ
                    {
                        isBackColorDefault = false;
                        BC.BGColor = "warning";
                        if (objectOO.StaffSitID == 0 || objectOO.StaffSessID == 0)
                        {
                            BC.BGColor = "lightwarning";
                        }

                        if (objectOO.ODConfirm != 0)
                        {
                            BC.BGColor = "success";
                        }

                        else
                        if (objectOO.ODNotConfirm != 0)
                        {
                            // Пожелание Нагорного/Никитина/заказчика чтобы тут был адекватный цвет и не напоминал ЛГБТ-сообщество
                            BC.BGColor = "danger";
                        }

                        else
                        if (objectOO.ActiveNotify == 0)
                        {
                            BC.BGColor = "info";
                            if (objectOO.StaffSessID != 0) // Оповещение ПУ завершено
                            {
                                BC.BGColor = "lightsuccess";
                            }
                        }
                    }
                    // Для УЗС
                    else if ((uint)objectOO.StaffID == 0xFFFFFFFF && objectOO.DevID != 0)
                    {
                        isBackColorDefault = false;
                        BC.BGColor = "warning";
                        if (objectOO.StaffSitID == 0 || objectOO.StaffSessID == 0)
                        {
                            BC.BGColor = "lightwarning";
                        }

                        if (objectOO.ODConfirm != 0)
                        {
                            BC.BGColor = "success";
                        }
                        else if (objectOO.ActiveNotify != 0)
                        {
                            BC.BGColor = "warning";
                        }
                        else
                        {
                            BC.BGColor = "danger";
                        }
                    }
                }

            }

            if (SelectItem.Any(x => x.ObjectID == objectOO.ObjectID))
            {
                BC.BGColor = BC.BGColor + " outline-blue";
            }

            pstate = CalcPimpState(objectOO.StaffID);

            if (isBackColorDefault)
            {
                switch (pstate)
                {
                    case PimpAnswerState.Answered16:
                        BC.Color = "success";
                        break;
                    case PimpAnswerState.NotAnswered16:
                        BC.Color = "danger";
                        break;
                }
            }

            if (objectOO != null && SelectItem.Contains(objectOO))
            {
                BC.Color = "dark";
            }


            return BC;

        }

        PimpAnswerState CalcPimpState(int StaffID)
        {
            if (CUStateList != null)
            {
                foreach (HardwareMonitor hwm in CUStateList)
                {
                    if (StaffID != hwm.ObjKey.ObjID.ObjID)
                        continue;

                    if (hwm.AnswerType == 16)
                        return PimpAnswerState.Answered16;

                    return PimpAnswerState.NotAnswered16;
                }
            }
            return PimpAnswerState.NotFound;
        }

        int GetCountColumn
        {
            get
            {
                int count = 1;
                if (stateArray != null)
                    count = (int)Math.Ceiling(Math.Sqrt((double)stateArray.Count));

                if (count < 3)
                    count = 3;
                return count;
            }
        }

        public async Task Refresh()
        {
            ResetToken();
            timer.Stop();
            stateArray = null;
            await GetList();
        }

        public async ValueTask DisposeAsync()
        {
            timer.Elapsed -= Timer_Elapsed;
            timer.Stop();
            await timer.DisposeAsync();
            DisposeToken();
        }
    }
}
