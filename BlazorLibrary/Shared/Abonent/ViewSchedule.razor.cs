using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using SMDataServiceProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.Abonent
{
    partial class ViewSchedule
    {
        [Parameter]
        public OBJ_ID? Abon { get; set; }

        [Parameter]
        public List<Shedule>? SheduleList { get; set; }

        private List<Objects> LocationList = new();

        private List<IntAndString> LineTypes = new();

        private List<IntAndString> ConnTypes = new();

        private List<Restrict> RestrictList = new();

        private Shedule? selectItem = null;

        private bool IsDuplicate = false;

        private bool ShowModal = false;

        private bool IsExistConnParam = false;

        private bool IsDeleteShedule = false;

        private TimeOnly StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59);


        private int StaffId = 0;
        protected override async Task OnInitializedAsync()
        {
            StaffId = await _User.GetLocalStaff();
            IsExistConnParam = false;
            selectItem = null;
            ShowModal = false;
            StartTime = new TimeOnly(0, 0);
            EndTime = new TimeOnly(23, 59);
            await GetLineTypeList();
            await GetConnTypeList();
            await GetLocationInfo();
        }

        private void SetPriorityShedule(int pos)
        {
            if (selectItem == null || SheduleList == null)
            {
                return;
            }

            var indexElem = SheduleList.IndexOf(selectItem);

            if ((indexElem + pos) < 0 || (indexElem + pos) > SheduleList.Count)
                return;

            var newItem = new Shedule(selectItem) { ASOShedule = new OBJ_ID(selectItem.ASOShedule) { ObjID = 0 } };

            selectItem = newItem;

            SheduleList.RemoveAt(indexElem);

            SheduleList.Insert((indexElem + pos), newItem);

            SheduleList.ForEach(x => x.ASOShedule.ObjID = 0);
        }

        private async Task GetLocationInfo()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetObjects_ILocation", new OBJ_ID() { StaffID = StaffId });
            if (result.IsSuccessStatusCode)
            {
                LocationList = await result.Content.ReadFromJsonAsync<List<Objects>>() ?? new();
            }
            if (LocationList == null)
                LocationList = new();
        }

        private async Task GetLineTypeList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLineTypeList", new IntID() { ID = SubsystemType.SUBSYST_ASO });
            if (result.IsSuccessStatusCode)
            {
                LineTypes = await result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
            }
            if (LineTypes == null)
                LineTypes = new();
        }

        private async Task GetConnTypeList()
        {
            var result = await Http.PostAsync("api/v1/GetConnTypeList", null);
            if (result.IsSuccessStatusCode)
            {
                ConnTypes = await result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
            }
            if (ConnTypes == null)
                ConnTypes = new();
        }

        private async Task ViewModal(bool? IsCreate = false)
        {

            if (!LocationList.Any())
            {
                MessageView?.AddError("", AsoRep["IDS_E_GETLOCATION"]);
                return;
            }


            ShowModal = true;

            if (SheduleList != null && selectItem != null && !SheduleList.Contains(selectItem) || IsCreate == true)
            {
                IsExistConnParam = false;
                selectItem = null;
                StartTime = new TimeOnly(0, 0);
                EndTime = new TimeOnly(23, 59);
            }
            if (selectItem == null)
            {
                selectItem = new() { ASOShedule = new() { ObjID = (SheduleList?.Count ?? 0 + 1) }, Loc = new(LocationList.FirstOrDefault()?.OBJID), BaseType = LineTypes.FirstOrDefault()?.Number ?? 0, DayWeek = "0000000" };
                selectItem.ConnType = ConnTypes.Where(x => selectItem.BaseType == (int)BaseLineType.LINE_TYPE_DIAL_UP ? (new int[] { (int)BaseLineType.LINE_TYPE_DIAL_UP, (int)BaseLineType.LINE_TYPE_PAGE }).Contains(x.Number) : x.Number == selectItem.BaseType).FirstOrDefault()?.Number ?? 0;
                selectItem.Beeper = 0;
                selectItem.Address = "";
                selectItem.ConnParam = "";
            }
            else
            {
                StartTime = new(selectItem.Begtime.ToTimeSpan().Ticks);
                EndTime = new(selectItem.Endtime.ToTimeSpan().Ticks);
            }

            await GetRestrictList();
        }

        private async Task SetConnType(ChangeEventArgs e)
        {
            int.TryParse(e?.Value?.ToString(), out int id);
            if (selectItem == null)
                selectItem = new();
            selectItem.BaseType = id;
            selectItem.ConnType = ConnTypes.Where(x => selectItem.BaseType == 1 ? (new int[] { 1, 3 }).Contains(x.Number) : x.Number == selectItem.BaseType).FirstOrDefault()?.Number ?? 0;
            selectItem.Beeper = 0;
            selectItem.Address = "";
            selectItem.ConnParam = "";
            selectItem.GlobalType = 0;
            selectItem.UserType = 0;
            await GetRestrictList();
        }

        private async Task GetRestrictList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetRestrictList", new IntID() { ID = selectItem?.BaseType ?? 0 });
            if (result.IsSuccessStatusCode)
            {
                RestrictList = await result.Content.ReadFromJsonAsync<List<Restrict>>() ?? new();
            }
            RestrictList = RestrictList.Where(x => x.RestrictType != 2).ToList();
        }

        private bool IsChecked(Restrict item)
        {
            bool isChecked = false;
            if (item.RestrictType == 0)
                isChecked = ((selectItem?.GlobalType >> item.BitNumber) & 0x01) > 0;
            if (item.RestrictType == 1)
                isChecked = ((selectItem?.UserType >> item.BitNumber) & 0x01) > 0;
            return isChecked;
        }

        private void SetRestrictBitStatus(Restrict item)
        {
            if (selectItem == null)
                selectItem = new();

            int BitNumber = item.BitNumber;

            int i = 0;
            i |= (1 << BitNumber);

            if (item.RestrictType == 0)
            {
                if (((selectItem.GlobalType >> BitNumber) & 0x01) > 0)
                {
                    selectItem.GlobalType -= i;
                }
                else
                {
                    selectItem.GlobalType += i;
                }
            }
            else if (item.RestrictType == 1)
            {
                if (((selectItem.UserType >> BitNumber) & 0x01) > 0)
                {
                    selectItem.UserType -= i;
                }
                else
                {
                    selectItem.UserType += i;
                }
            }


        }

        private void SetDayWeek(int item)
        {
            if (selectItem == null)
                return;
            char[] dayArray = selectItem.DayWeek == "" ? "0000000".ToCharArray() : selectItem.DayWeek.ToCharArray();

            dayArray[item] = dayArray[item] == '0' ? '1' : '0';

            selectItem.DayWeek = string.Join("", dayArray);
        }

        private void DeleteShedule()
        {
            if (selectItem != null && SheduleList != null)
            {
                if (SheduleList.Contains(selectItem))
                {
                    SheduleList.Remove(selectItem);
                }
            }

            selectItem = null;
            IsDeleteShedule = false;
        }

        void CloseModal()
        {
            ShowModal = false;            
            IsExistConnParam = false;
        }

        private async Task AddShedle()
        {
            if (selectItem != null)
            {
                selectItem.ConnParam = selectItem.ConnParam.Replace(" ", "");

                if (string.IsNullOrEmpty(selectItem.ConnParam))
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_NOTIFY_SHEDULE"], AsoRep["IDS_E_NOCONNPARAM"]);
                    return;
                }
                else
                {

                    switch (selectItem.DayType)
                    {
                        case 0: selectItem.DayWeek = "0000000"; break;   //всегда
                        case 1: selectItem.DayWeek = "0111110"; break;   //рабочие дни
                        case 2: selectItem.DayWeek = "1000001"; break;   //выходные
                        case 3: selectItem.DayWeek = "1111111"; break;   //празничные дни
                    }


                    selectItem.Begtime = Duration.FromTimeSpan(StartTime.ToTimeSpan());
                    selectItem.Endtime = Duration.FromTimeSpan(EndTime.ToTimeSpan());
                    selectItem.Loc = LocationList.FirstOrDefault(x => x.OBJID.ObjID == selectItem.Loc.ObjID)?.OBJID ?? new();
                    selectItem.TypeName = ConnTypes.FirstOrDefault(x => x.Number == selectItem.ConnType)?.Str ?? "";

                    if (selectItem.TimeType != 1)
                    {
                        selectItem.Begtime = Duration.FromTimeSpan(new TimeOnly(0, 0).ToTimeSpan());
                        selectItem.Endtime = Duration.FromTimeSpan(new TimeOnly(23, 59).ToTimeSpan());
                    }

                    if (selectItem.TypeName == "")
                    {
                        MessageView?.AddError(AsoRep["IDS_STRING_NOTIFY_SHEDULE"], AsoRep["IDS_STRING_SEL_DEV_CONN_TYPE"]);
                        return;
                    }

                    if (!IsExistConnParam)
                    {
                        CIsExistConnParam request = new();
                        request.ConnParam = selectItem.ConnParam;
                        request.ConnType = selectItem.ConnType;
                        request.Loc = new OBJ_ID(selectItem.Loc) { SubsystemID = Abon?.ObjID ?? 0 };
                        ExistConnParam? response = null;

                        if (SheduleList != null && SheduleList.Any(x => x.Loc?.ObjID == selectItem.Loc?.ObjID && x.ConnType == selectItem.ConnType && x.ConnParam == selectItem.ConnParam && x.ASOShedule.Equals(selectItem.ASOShedule)))
                        {
                            response = new ExistConnParam();
                        }
                        else
                        {
                            var result = await Http.PostAsJsonAsync("api/v1/IsExistConnParam", request);
                            if (result.IsSuccessStatusCode)
                            {
                                response = await result.Content.ReadFromJsonAsync<ExistConnParam>();
                            }
                        }
                        if (response != null && response.AbonId > 0)
                        {
                            IsExistConnParam = true;
                            return;
                        }
                    }

                    if (SheduleList == null)
                        SheduleList = new();

                    if (SheduleList.Any(x => x.ASOShedule.Equals(selectItem.ASOShedule)))
                    {
                        var indexElem = SheduleList.FindIndex(x => x.ASOShedule.Equals(selectItem.ASOShedule));
                        SheduleList[indexElem] = selectItem;
                    }
                    else
                        SheduleList.Add(selectItem);

                    IsExistConnParam = false;

                    if (IsDuplicate && selectItem.ConnType == (int)BaseLineType.LINE_TYPE_DIAL_UP && !SheduleList.Any(x => x.ConnType == (int)BaseLineType.LINE_TYPE_GSM_TERMINAL && x.ConnParam == selectItem.ConnParam))
                    {
                        selectItem = new Shedule(selectItem)
                        {
                            BaseType = (int)BaseLineType.LINE_TYPE_GSM_TERMINAL,
                            ConnType = (int)BaseLineType.LINE_TYPE_GSM_TERMINAL,
                            Address = "",
                            Beeper = 0,
                            ASOShedule = new() { ObjID = (SheduleList?.Count ?? 0 + 1) }
                        };
                        await AddShedle();
                    }
                    else
                    {
                        selectItem = null;
                        ShowModal = false;
                        StartTime = new(0, 0);
                        EndTime = new(23, 59);
                    }

                    IsDuplicate = false;
                }

            }

        }
    }
}
