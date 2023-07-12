using System.Net.Http.Json;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace ARMSettings.Client.Pages.SecuritySubSystem
{

    partial class SecuritySubSystem
    {
        private List<SharedLibrary.Models.UserInfo>? Model { get; set; }
        private SharedLibrary.Models.UserInfo? SelectItem { get; set; }
        private List<GsoUserSecurity>? SecurityObjectsList { get; set; }

        private List<SecurityGroup>? SecurityGroupList { get; set; }
        private SecurityGroup? SelectedSecurityGroup { get; set; }

        private List<SecurityParams>? SecurityParamsList { get; set; }

        private SecurityParams? SelectedSecurityParam { get; set; }

        private Dictionary<int, string>? thListSecurityParams;

        private int securityTab;
        private int typeId = 2;
        private bool isDeleteSecurityGroup;
        private bool isAddSecurityGroup;
        private bool isDeleteSecurityParam;
        private bool isAddSecurityParam;
        private bool isAddObject;

        protected override async Task OnInitializedAsync()
        {
            thListSecurityParams = new Dictionary<int, string>
            {
                { -1, ARMSetRep["NAME"] },
                { -2, ARMSetRep["ACCESS"] },
                { -3, ARMSetRep["DENY"] },
            };
            SelectItem = null;
            await SetList();
        }

        private async Task SetList()
        {
            var x = await Http.PostAsync("api/v1/GetGsoUserEx2", null);
            if (x.IsSuccessStatusCode)
            {
                Model = await x.Content.ReadFromJsonAsync<List<SharedLibrary.Models.UserInfo>>();
                await HandleUserSelected(Model);
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_E_GETUSER"]);
            }

            if (Model == null)
                Model = new();
        }

        private async Task HandleUserSelected(List<SharedLibrary.Models.UserInfo>? items)
        {
            SelectItem = items?.FirstOrDefault();
            SelectedSecurityGroup = null;
            SelectedSecurityParam = null;
            SecurityObjectsList = null;
            await FillSecurityObjects();
            await FillUserSecurityGroups();
            await FillUserSecurityParams();
        }

        async Task ChangeCat(ChangeEventArgs e)
        {
            typeId = Convert.ToInt32(e.Value);
            await FillSecurityObjects();
        }

        private async Task FillSecurityObjects()
        {
            if (SelectItem?.OBJID != null)
            {
                var request = new GsoUserSecurity()
                {
                    UserID = SelectItem.OBJID.ObjID,
                    TypeID = typeId
                };
                var x = await Http.PostAsJsonAsync("api/v1/GetGsoUserSecurity", request);
                if (x.IsSuccessStatusCode)
                {
                    SecurityObjectsList = await x.Content.ReadFromJsonAsync<List<GsoUserSecurity>>();
                }
            }

            if (SecurityObjectsList == null)
                SecurityObjectsList = new();
        }

        private async Task ChangeSecurityObject(GsoUserSecurity securityObject)
        {
            if (SelectItem == null) return;
            await Http.PostAsJsonAsync("api/v1/SetGsoUserSecurity", securityObject);
        }

        private async Task HandleSecurityObjectCheckBox(GsoUserSecurity securityObject)
        {
            securityObject.PermitAccess = Convert.ToInt32(!Convert.ToBoolean(securityObject.PermitAccess));
            await ChangeSecurityObject(securityObject);
        }
        private async Task HandleSecurityParamAccess(SecurityParams securityParam)
        {
            securityParam.Access = Convert.ToInt32(!Convert.ToBoolean(securityParam.Access));
            await AddUserSecurityParams(new List<SecurityParams>() { securityParam });
        }
        private async Task HandleSecurityParamDenied(SecurityParams securityParam)
        {
            securityParam.Denide = Convert.ToInt32(!Convert.ToBoolean(securityParam.Denide));
            await AddUserSecurityParams(new List<SecurityParams>() { securityParam });
        }

        private async Task CheckAllSecurityObjects()
        {
            if (SecurityObjectsList == null) return;
            foreach (var item in SecurityObjectsList.Where(item => item.PermitAccess == 0))
            {
                item.PermitAccess = 1;
                await ChangeSecurityObject(item);
            }
        }

        private async Task UncheckAllSecurityObjects()
        {
            if (SecurityObjectsList == null) return;
            foreach (var item in SecurityObjectsList.Where(item => item.PermitAccess == 1))
            {
                item.PermitAccess = 0;
                await ChangeSecurityObject(item);
            }
        }

        private async Task FillUserSecurityGroups()
        {
            if (SelectItem?.OBJID != null)
            {
                var x = await Http.PostAsJsonAsync("api/v1/GetUserSecurityGroup", SelectItem.OBJID.ObjID);
                if (x.IsSuccessStatusCode)
                {
                    SecurityGroupList = await x.Content.ReadFromJsonAsync<List<SecurityGroup>>();
                }
            }

            if (SecurityGroupList == null)
                SecurityGroupList = new();
        }

        private async Task RemoveSecurityUserGroup()
        {
            if (SelectedSecurityGroup == null) return;
            if (SelectItem?.OBJID != null)
            {
                var request = new CChangeSecurityUserGroup()
                {
                    SecurityGroupID = SelectedSecurityGroup.SecurityGroupID,
                    UserID = SelectItem.OBJID.ObjID
                };

                var x = await Http.PostAsJsonAsync("api/v1/RemoveSecurityUserGroup", request);
                if (x.IsSuccessStatusCode)
                {
                    await FillUserSecurityGroups();
                }
                SelectedSecurityGroup = null;
            }

            isDeleteSecurityGroup = false;
        }

        private async Task AddSecurityUserGroups(List<SecurityGroup>? securityUserGroups)
        {
            if (securityUserGroups != null)
            {
                if (SelectItem?.OBJID != null)
                {
                    foreach (var item in securityUserGroups)
                    {
                        var group = SecurityGroupList?.FirstOrDefault(r => r.SecurityGroupID == item.SecurityGroupID);

                        if (group != null) continue;

                        var request = new CChangeSecurityUserGroup()
                        {
                            SecurityGroupID = item.SecurityGroupID,
                            UserID = SelectItem.OBJID.ObjID
                        };
                        await Http.PostAsJsonAsync("api/v1/AddSecurityUserGroup", request);
                    }
                    await FillUserSecurityGroups();
                }
            }
            isAddSecurityGroup = false;
        }

        private async Task FillUserSecurityParams()
        {
            if (SelectItem?.OBJID != null)
            {
                var x = await Http.PostAsJsonAsync("api/v1/GetUserSecurityParams", SelectItem.OBJID.ObjID);
                if (x.IsSuccessStatusCode)
                {
                    SecurityParamsList = await x.Content.ReadFromJsonAsync<List<SecurityParams>>();
                }
            }

            if (SecurityParamsList == null)
                SecurityParamsList = new();
        }

        private async Task AddUserSecurityParams(List<SecurityParams> securityParams)
        {
            if (SelectItem?.OBJID != null)
            {
                foreach (var request in securityParams.Select(item => new CChangeUserSecurityParam()
                {
                    Access = item.Access,
                    Denide = item.Denide,
                    UserID = SelectItem.OBJID.ObjID,
                    SecurityParamID = item.SecurityParamID
                }))
                {
                    await Http.PostAsJsonAsync("api/v1/AddUserSecurityParam", request);
                }
                await FillUserSecurityParams();
            }
        }

        private async Task RemoveUserSecurityParam()
        {
            if (SelectedSecurityParam == null) return;
            if (SelectItem?.OBJID != null)
            {
                var request = new CChangeUserSecurityParam()
                {
                    Access = Convert.ToInt32(SelectedSecurityParam.Access),
                    Denide = Convert.ToInt32(SelectedSecurityParam.Denide),
                    UserID = SelectItem.OBJID.ObjID,
                    SecurityParamID = SelectedSecurityParam.SecurityParamID
                };
                await Http.PostAsJsonAsync("api/v1/RemoveUserSecurityParam", request);
                await FillUserSecurityParams();

                SelectedSecurityParam = null;
            }
            isDeleteSecurityParam = false;
        }
        private async Task AddNewUserSecurityParams(List<SecurityParams>? securityParams)
        {
            if (securityParams?.Count > 0)
            {
                if (SelectItem?.OBJID != null)
                {
                    foreach (var item in securityParams)
                    {
                        var param = SecurityParamsList?.FirstOrDefault(r => r.SecurityParamID == item.SecurityParamID);
                        if (param != null) continue;
                        await AddUserSecurityParams(new List<SecurityParams>() { item });
                    }
                }
            }

            isAddSecurityParam = false;
        }

        private async Task HandleAddObjects(List<OBJ_ID>? situationList)
        {
            if (situationList?.Count > 0)
            {
                if (SelectItem?.OBJID == null) return;
                foreach (var item in situationList)
                {
                    var request = new GsoUserSecurity()
                    {
                        ObjID = item,
                        TypeID = 3,
                        UserID = SelectItem.OBJID.ObjID,
                        PermitAccess = 1,
                        DenyAccess = 0
                    };
                    await Http.PostAsJsonAsync("api/v1/SetGsoUserSecurity", request);
                }
                await FillSecurityObjects();
            }
            isAddObject = false;
        }

    }
}
