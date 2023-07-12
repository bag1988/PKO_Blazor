using System.Net.Http.Json;
using System.Timers;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Extensions;
using SMDataServiceProto.V1;

namespace ARMsred.Client.Pages
{
    partial class ScenListWithGroupsControl : IAsyncDisposable
    {
        [Parameter]
        public EventCallback<List<OBJ_ID>> SetSelectSitList { get; set; }

        public List<SitGroupInfo>? SituationGroups { get; set; }

        List<SitGroupInfo> ActiveSituationGroups { get; set; } = new();

        List<SitGroupLinkInfo_tag> SitList { get; set; } = new();

        public List<SituationState>? SituationStateList { get; set; } = new();

        List<SituationState>? SelectSit { get; set; }


        private Dictionary<int, string>? ThList;
        private Dictionary<int, string>? SubsystemsList;
        int UserId { get; set; }

        int StaffId { get; set; }

        readonly System.Timers.Timer timer = new(TimeSpan.FromSeconds(3));
        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 1, ARMRep["SIT_NAME"] },
                { 2, ARMRep["SUBSYSTEM"] },
                { 3, ARMRep["GLOBAL_NUMBER"] },
                { 4, ARMRep["STATE_NOTIFY"] },
                { 5, ARMRep["IDS_STATIST"] }
            };

            SubsystemsList = new Dictionary<int, string>
            {
                { 1, SMDataRep["SUBSYST_ASO"] },
                { 2, SMDataRep["SUBSYST_SZ"] },
                { 3, SMDataRep["SUBSYST_GSO_STAFF"] },
            };

            UserId = await _User.GetUserId();
            StaffId = await _User.GetLocalStaff();
            await ReadSitGroups();
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }


        private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await GetSituationState();
            }
            catch
            {

            }
        }

        private async Task HandleSitButtonClicked(SitGroupInfo item)
        {
            if (ActiveSituationGroups.FirstOrDefault(r => r.SitGroupID == item.SitGroupID) != null)
            {
                var itemDel = ActiveSituationGroups.FirstOrDefault(r => r.SitGroupID == item.SitGroupID);
                if (itemDel != null)
                {
                    ActiveSituationGroups.Remove(itemDel);

                    foreach (var sit in SitList.Where(x => x.SitGroupID == item.SitGroupID))
                    {
                        SituationStateList?.RemoveAll(x => x.SitID?.Equals(new OBJ_ID() { ObjID = sit.SitID, StaffID = sit.StaffID, SubsystemID = sit.SubsystemID }) ?? false);
                        SelectSit?.RemoveAll(x => sit.SitID == x.SitID?.ObjID && sit.StaffID == x.SitID?.StaffID && sit.SubsystemID == x.SitID?.SubsystemID);
                    }
                    SitList.RemoveAll(x => x.SitGroupID == item.SitGroupID);

                }
            }
            else
            {
                ActiveSituationGroups.Add(item);
                await ReadSitGroupLinks();
            }
        }

        public List<OBJ_ID>? GetActivSitId
        {
            get
            {
                return SituationStateList?.Where(x => x.SessStat == 1 || x.SessStat == 2).Select(x => new OBJ_ID() { SubsystemID = x.SitID.SubsystemID }).Distinct().ToList();
            }
        }

        async Task SetSelectSit(List<SituationState>? items)
        {
            SelectSit = items;
            if (SetSelectSitList.HasDelegate)
                await SetSelectSitList.InvokeAsync(SelectSit?.Select(x => x.SitID).ToList());
        }


        //IEnumerable<SituationState>? GetSitList
        //{
        //    get
        //    {
        //        ActiveSituationGroups?.RemoveAll(x => !SituationGroups?.Any(s => s.SitGroupID == x.SitGroupID) ?? true);

        //        return SituationStateList?.Where(x => SitList.Any(s => s.SitID == x.SitID?.ObjID));
        //    }
        //}

        /// <summary>
        /// получить список сценариев для группы с учётом прав доступа пользователя
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="sitGroupID"></param>
        /// <returns></returns>
        private async Task ReadSitGroupLinks()
        {
            foreach (var item in ActiveSituationGroups.Where(x => !SitList.Any(s => s.SitGroupID == x.SitGroupID)))
            {
                SitGroupLinkInfoRequest request = new() { UserID = (uint)UserId, SitGroupID = item.SitGroupID };
                var x = await Http.PostAsJsonAsync("api/v1/remote/S_GetSitGroupListLink", request);
                if (!x.IsSuccessStatusCode) return;
                var res = await x.Content.ReadFromJsonAsync<List<SitGroupLinkInfo_tag>>();
                if (res != null)
                {
                    if (!SitList.Any(s => s.SitGroupID == item.SitGroupID))
                    {
                        SitList.AddRange(res);
                        await GetSituationState();
                    }
                }
            }
        }

        /// <summary>
        /// получить список сценариев для группы с учётом прав доступа пользователя
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="sitGroupID"></param>
        /// <returns></returns>
        private async Task GetSituationState()
        {
            timer.Stop();
            if (SituationStateList == null)
                SituationStateList = new();

            var request = new OBJ_Key()
            {
                ObjType = 0,
                ObjID = new OBJ_ID() { StaffID = StaffId }
            };
            if (SitList?.Count > 0)
            {
                var x = await Http.PostAsJsonAsync("api/v1/remote/GetSituationState", request);
                if (x.IsSuccessStatusCode)
                {
                    var res = await x.Content.ReadFromJsonAsync<List<SituationState>>();

                    if (res != null)
                    {
                        foreach (var item in res.Where(x => SitList.Any(s => x.SitID?.Equals(new OBJ_ID() { ObjID = s.SitID, StaffID = s.StaffID, SubsystemID = s.SubsystemID }) ?? false)))
                        {
                            if (!SituationStateList.Any(x => x.SitID?.Equals(item.SitID) ?? false))
                            {
                                SituationStateList.Add(item);
                            }
                            else
                            {
                                var indexElem = SituationStateList.FindIndex(x => x.SitID?.Equals(item.SitID) ?? false);
                                SituationStateList.RemoveAt(indexElem);
                                SituationStateList.Insert(indexElem, item);

                                if (SelectSit?.Any(x => x.SitID?.Equals(item.SitID) ?? false) ?? false)
                                {
                                    SelectSit.RemoveAll(x => x.SitID.Equals(item.SitID));
                                    SelectSit.Add(item);
                                }
                            }
                        }
                    }
                }
            }

            timer.Start();
        }

        /// <summary>
        /// получить список групп сценариев с учётом прав доступа пользователя
        /// </summary>
        /// <returns></returns>
        public async Task ReadSitGroups()
        {
            SitGroupInfoRequest request = new() { UserID = UserId, OBJID = new() };
            var x = await Http.PostAsJsonAsync("api/v1/remote/S_GetSitGroupList", request);
            if (x.IsSuccessStatusCode)
            {
                SituationGroups = await x.Content.ReadFromJsonAsync<List<SitGroupInfo>>();
            }
            if (SituationGroups == null)
                SituationGroups = new();
        }


        public bool HasSelectedActiveScens
        {
            get
            {
                if (SituationStateList == null)
                    return false;

                foreach (var item in SituationStateList)
                {
                    switch (item.SessStat)
                    {
                        case 1:
                        case 2:
                            return true;
                    }
                }
                return false;
            }
        }


        private string SessionStateFromInt(int id)
        {
            switch (id)
            {
                case 1: return ARMRep["SessStateNotify"];
                case 2: return ARMRep["SessStateAddtNotify"];
                case 3: return ARMRep["SessStateCompleted"];
                case 4: return ARMRep["SessStateInterrupted"];
                default: return ARMRep["SessStateUnknown"];
            }
        }


        public async ValueTask DisposeAsync()
        {
            timer.Elapsed -= Timer_Elapsed;
            timer.Stop();
            await timer.DisposeAsync();
        }
    }
}
