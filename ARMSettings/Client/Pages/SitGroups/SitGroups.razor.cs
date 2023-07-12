using System.Net.Http.Json;
using Google.Protobuf;
using SMSSGsoProto.V1;
using SMDataServiceProto.V1;

namespace ARMSettings.Client.Pages.SitGroups
{
    partial class SitGroups
    {
        private List<SitGroupInfo>? SituationGroups { get; set; }
        private SitGroupInfo? SelectedSituationGroup { get; set; }

        private List<SitGroupLinkInfo_tag>? SituationList { get; set; }
        private SitGroupLinkInfo_tag? SelectedSituation { get; set; }

        private bool isAddSitGroup;
        private bool isDeleteSitGroup;
        private bool isAddSit;

        private bool isDeleteSitLink;

        private Dictionary<int, string>? subsystems;
        private Dictionary<int, string>? thList;
        private Dictionary<int, string>? situationThList;
        protected override async Task OnInitializedAsync()
        {
            thList = new Dictionary<int, string>
            {
                { -1, GsoRep["IDS_STRING_NAME"] },
                { -2, ARMSetRep["STATE"] },
            };
            situationThList = new Dictionary<int, string>
            {
                { -1, ARMSetRep["SCENARIO"] },
                { -2, ARMSetRep["SUBSYSTEM"] },
                { -3, ARMSetRep["CODE"] },
                { -4, ARMSetRep["COMMENT"] },
            };
            subsystems = new Dictionary<int, string>
            {
                { 1, ARMSetRep["ASO"] },
                { 2, ARMSetRep["UZS"] },
                { 3, ARMSetRep["PU"] },
            };
            await FillSitGroups();
            await FillSituations();
        }

        private async Task FillSitGroups()
        {
            var x = await Http.PostAsync("api/v1/GetSitGroupList", null);
            if (x.IsSuccessStatusCode)
            {
                SituationGroups = await x.Content.ReadFromJsonAsync<List<SitGroupInfo>>();

                SelectedSituationGroup = SituationGroups?.FirstOrDefault();
            }
            if (SituationGroups == null)
                SituationGroups = new();
        }

        async Task IsUpdateGroups(SitGroupInfo? item = null)
        {
            if (item != null)
            {
                if (SituationGroups == null) SituationGroups = new();

                if (!SituationGroups.Any(x => x.SitGroupID == item.SitGroupID))
                {
                    SituationGroups.Add(item);
                    await HandleSitGroupChanged(new List<SitGroupInfo> { item });
                }
                else
                {
                    var index = SituationGroups.FindIndex(x => x.SitGroupID == item.SitGroupID);
                    SituationGroups.RemoveAt(index);
                    SituationGroups.Insert(index, item);
                    SelectedSituationGroup = item;
                }

            }
            isAddSitGroup = false;
        }


        private async Task RemoveSitGroup()
        {
            if (SelectedSituationGroup == null)
                return;
            var x = await Http.PostAsJsonAsync("api/v1/RemoveSitGroup", SelectedSituationGroup.SitGroupID);
            if (!x.IsSuccessStatusCode) return;

            SituationGroups?.Remove(SelectedSituationGroup);
            await HandleSitGroupChanged(null);
            isDeleteSitGroup = false;
        }

        private void AddSitGroup()
        {
            SelectedSituationGroup = null;
            isAddSitGroup = true;
        }

        private async Task HandleSitGroupChanged(List<SitGroupInfo>? e)
        {
            if (!SelectedSituationGroup?.Equals(e?.FirstOrDefault()) ?? true)
            {
                SelectedSituationGroup = e?.FirstOrDefault();
                SituationList = null;
                SelectedSituation = null;
                await FillSituations();
            }
        }

        private async Task FillSituations()
        {
            if (SelectedSituationGroup != null)
            {
                var x = await Http.PostAsJsonAsync("api/v1/GetSitGroupListLink", SelectedSituationGroup.SitGroupID);

                if (x.IsSuccessStatusCode)
                {
                    SituationList = await x.Content.ReadFromJsonAsync<List<SitGroupLinkInfo_tag>>();
                }
            }

            if (SituationList == null)
                SituationList = new();
        }

        private async Task HandleAddObjects(List<OBJ_ID>? items)
        {
            if (SelectedSituationGroup == null) return;

            if (items != null)
            {
                List<SitGroupLinkList> linkList = items.Select(item => new SitGroupLinkList()
                {
                    ObjID = item.ObjID,
                    StaffID = item.StaffID,
                    SubsystemID = item.SubsystemID
                }).ToList();

                SitGroupLinkRequest request = new()
                {
                    SitGroupID = SelectedSituationGroup.SitGroupID,
                    SitGroupLinkListArray = new()
                };
                request.SitGroupLinkListArray.Array.AddRange(linkList);
                var x = await Http.PostAsJsonAsync("api/v1/AddSitGroupLink", JsonFormatter.Default.Format(request));
                if (!x.IsSuccessStatusCode) return;
                await FillSituations();
            }


            isAddSit = false;

        }

        private async Task RemoveSitLink()
        {
            if (SelectedSituationGroup == null) return;
            if (SelectedSituation == null) return;
            List<SitGroupLinkList> linkList = new();
            var item = new SitGroupLinkList()
            {
                ObjID = SelectedSituation.SitID,
                StaffID = SelectedSituation.StaffID,
                SubsystemID = SelectedSituation.SitSubsystemID
            };
            linkList.Add(item);
            SitGroupLinkRequest request = new()
            {
                SitGroupID = SelectedSituationGroup.SitGroupID,
                SitGroupLinkListArray = new SitGroupLinkListArray()
            };
            request.SitGroupLinkListArray.Array.AddRange(linkList);
            var x = await Http.PostAsJsonAsync("api/v1/RemoveSitGroupLink", JsonFormatter.Default.Format(request));

            isDeleteSitLink = false;
            SelectedSituation = null;
            await FillSituations();
        }

    }
}
