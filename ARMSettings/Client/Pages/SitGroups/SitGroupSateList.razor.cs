using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;

namespace ARMSettings.Client.Pages.SitGroups
{
    partial class SitGroupSateList
    {
        [Parameter]
        public List<SitGroupInfo>? SituationGroups { get; set; }

        private List<SitGroupInfo> ActiveSituationGroups { get; set; } = new();

        private Dictionary<SitGroupInfo, List<SituationState>>? SituationStateList { get; set; } = new();

        private Dictionary<int, string>? thList;
        private Dictionary<int, string>? subsystems;

        protected override void OnInitialized()
        {
            thList = new Dictionary<int, string>
            {
                { -1, ARMSetRep["SCENARIO"] },
                { -2, ARMSetRep["SUBSYSTEM"] },
                { -3, ARMSetRep["TYPE"] },
                { -4, ARMSetRep["CODE"] },
                { -5, ARMSetRep["COMMENT"] },
                { -6, ARMSetRep["PRIO"] },
                { -7, ARMSetRep["BEGIN"] },
                { -8, ARMSetRep["END"] },
                { -9, ARMSetRep["STATE"] },
                { -10, ARMSetRep["NOTIFY_OK"] },
                { -11, ARMSetRep["NOTIFY_BAD"] },
                { -12, ARMSetRep["PU"] },
                { -13, ARMSetRep["PRIO_PU"] },
                { -14, ARMSetRep["ID"] },
                { -15, ARMSetRep["MESSAGE"] },
                { -16, ARMSetRep["COMMENT"] },
                { -17, ARMSetRep["MESSAGE_TYPE"] },
                { -18, ARMSetRep["USER"] },

            };
            subsystems = new Dictionary<int, string>
            {
                { 1, ARMSetRep["ASO"] },
                { 2, ARMSetRep["UZS"] },
                { 3, ARMSetRep["PU"] },
            };
        }

        IEnumerable<SituationState>? GetSitList
        {
            get
            {
                ActiveSituationGroups?.RemoveAll(x => !SituationGroups?.Any(s => s.SitGroupID == x.SitGroupID) ?? true);
                return SituationStateList?.Where(x => ActiveSituationGroups?.Contains(x.Key) ?? false).SelectMany(x => x.Value);
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
                    SituationStateList?.Remove(itemDel);
                }
            }
            else
            {
                ActiveSituationGroups.Add(item);
                await FillSituationState();
            }
        }

        private async Task FillSituationState()
        {
            if (SituationStateList == null)
                SituationStateList = new();

            foreach (var item in ActiveSituationGroups.Where(x => !SituationStateList.ContainsKey(x)))
            {
                var request = new OBJ_Key()
                {
                    ObjType = item.SitGroupID,
                    ObjID = new OBJ_ID()
                };
                var x = await Http.PostAsJsonAsync("api/v1/GetSituationState", request);
                if (!x.IsSuccessStatusCode) return;
                var res = await x.Content.ReadFromJsonAsync<List<SituationState>>();
                if (res != null)
                {
                    if (!SituationStateList.ContainsKey(item))
                    {
                        SituationStateList.Add(item, res);
                    }
                }
            }
        }

        private string GetNotifyState(int id)
        {
            switch (id)
            {
                case 1: return ARMSetRep["NOTIFY_STATE_NOTIFYING"];
                case 2: return ARMSetRep["NOTIFY_STATE_RE_NOTIFYING"];
                case 3: return ARMSetRep["NOTIFY_STATE_FINISHED"];
                case 4: return ARMSetRep["NOTIFY_STATE_ABORTED"];
                default: return ARMSetRep["NOTIFY_STATE_UNKNOWN"];
            }
        }
    }
}
