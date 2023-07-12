
using System.Net.Http.Json;
using BlazorLibrary.Shared;
using Google.Protobuf.WellKnownTypes;
using ReplaceLibrary;
using SharedLibrary;
using SharedLibrary.Models;
using static BlazorLibrary.Shared.Main;

namespace DeviceConsole.Client.Pages.Security
{
    partial class Index
    {

        private List<UserInfo>? Model = null;

        private List<UserInfo>? OldModel = null;

        private List<UserInfo>? ChangeItemList = null;

        private LoginMode? lMode = new() { LoginUserMode = "0" };

        private LoginMode? OldlMode = new() { LoginUserMode = "0" };

        private bool IsSave = false;

        private UserInfo? SelectItem = null;

        private UserInfo NewUser = new();

        private string? NewPassword = null;

        private bool IsDelete = false;

        private bool IsAddUser = false;

        private bool IsChangePassword = false;

        private ChangePassword ChangePass = new();

        private int StaffId = 0;

        readonly List<ItemPermission> PermissionsList = new();

        class ItemPermission
        {
            public string? Name { get; set; }
            public int PosBit { get; set; }
            public string? NameFiled { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            SelectItem = null;
            StaffId = await _User.GetLocalStaff();
            IsSave = false;
            SetPermissionsList();
            await GetList();
            await GetParamLogin();
        }

        void SetPermissionsList()
        {
            PermissionsList.Clear();
            foreach (var item in new ValuePermissions().GetType().GetProperties().OfType<System.Reflection.PropertyInfo>())
            {
                if (item.PropertyType.Equals(new Permission[0].GetType()))
                {
                    if (((Permission[]?)item.GetValue(new ValuePermissions()))?.Any() ?? false)
                    {
                        foreach (var child in (Permission[]?)item.GetValue(new ValuePermissions()) ?? new Permission[0])
                        {
                            PermissionsList.Add(new ItemPermission()
                            {
                                Name = item.Name,
                                PosBit = child.PosBit,
                                NameFiled = GsoRep[BaseReplace.Get<GSOReplase>(child.NameField)]
                            });
                        }
                    }
                }

            }
        }

        private async Task GetList()
        {
            var result = await Http.PostAsync("api/v1/GetGsoUserEx2", null);
            if (result.IsSuccessStatusCode)
            {
                Model = await result.Content.ReadFromJsonAsync<List<UserInfo>>();
                if (Model?.Any() ?? false)
                {
                    Model = Model.OrderBy(x => x.Login).ToList();
                    SelectItem = Model.First();
                    OldModel = Model.Select(x => new UserInfo() { OBJID = x.OBJID, SuperVision = x.SuperVision }).ToList();
                }
            }
            else
            {
                MessageView?.AddError("", GsoRep["IDS_E_GETUSER"]);
                Model = new();
            }
        }




        private async Task GetParamLogin()
        {
            var result = await Http.PostAsync("api/v1/GetParamLogin", null);
            if (result.IsSuccessStatusCode)
            {
                lMode = await result.Content.ReadFromJsonAsync<LoginMode>() ?? new();
            }
            else
            {
                MessageView?.AddError("", Rep["Login"] + " - " + GsoRep["IDS_STRING_ERR_GET_DATA"]);
                lMode = new() { LoginUserMode = "0" };
            }

            OldlMode = new(lMode);

            StateHasChanged();
        }


        private async Task<bool> SetParamLogin()
        {
            if (lMode == null || OldlMode == null)
                return false;

            if (lMode.LoginUserMode == OldlMode.LoginUserMode && lMode.LoginSSOMode == OldlMode.LoginSSOMode && lMode.LoginADBase == OldlMode.LoginADBase)
                return false;

            var result = await Http.PostAsJsonAsync("api/v1/SetParamLogin", lMode);
            if (!result.IsSuccessStatusCode)
            {
                MessageView?.AddError("", GsoRep["ErrorSaveLoginMode"]);
            }
            else
                OldlMode = new(lMode);

            return true;
        }

        private async Task SetUserPer()
        {
            IsSave = false;

            var b = await SetParamLogin();

            if (OldModel != null && Model != null)
            {
                foreach (var o in OldModel.Where(x => x.OBJID != null))
                {
                    if (Model.FirstOrDefault(x => x.OBJID?.ObjID == o.OBJID?.ObjID)?.SuperVision != o.SuperVision)
                    {
                        AddList(Model.FirstOrDefault(x => x.OBJID?.ObjID == o.OBJID?.ObjID));
                    }
                }
            }


            if (!b && ChangeItemList == null)
            {
                MessageView?.AddError("", AsoRep["NotChangeSave"]);
                //return;
            }
            else if (ChangeItemList != null)
            {

                await SaveServer(ChangeItemList);
                if (IsSave)
                    ChangeItemList = null;
            }

        }


        private async Task SaveServer(List<UserInfo> list)
        {
            var result = await Http.PostAsJsonAsync("api/v1/SetGsoUserEx", list);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<BoolValue>();

                if (r?.Value == true)
                {
                    IsSave = true;
                    _ = Task.Delay(2000).ContinueWith(x =>
                    {
                        IsSave = false; StateHasChanged();
                    });
                }
            }

            if (!IsSave)
            {
                MessageView?.AddError("", AsoRep["IDS_EFAILSAVECHANGES"]);
            }
        }


        private async Task DeleteUser()
        {
            if (SelectItem == null)
                return;
            var result = await Http.PostAsJsonAsync("api/v1/DeleteGsoUserEx", SelectItem);
            if (result.IsSuccessStatusCode)
            {
                var r = await result.Content.ReadFromJsonAsync<BoolValue>();

                if (r?.Value == true)
                {
                    MessageView?.AddMessage("", AsoRep["IDS_OK_DELETE"]);
                    await GetList();
                    StateHasChanged();
                }
                else
                {
                    MessageView?.AddError("", AsoRep["IDS_E_DELETE"]);
                }
            }

            IsDelete = false;
            SelectItem = null;
        }


        private async Task AddNewUser()
        {
            bool Error = false;

            if (NewUser == null)
            {
                MessageView?.AddError("", AsoRep["IDS_ENTERMISMATCHERROR"]);
                return;
            }

            if (string.IsNullOrEmpty(NewUser.Login))
            {
                MessageView?.AddError("", GSOFormRep["IDS_E_USER"]);
                Error = true;
            }

            if (!NewPassword?.Equals(NewUser.Passw) ?? false)
            {
                MessageView?.AddError("", GsoRep["ErrorPassword"]);
                Error = true;
            }

            if (Model?.Any(x => x.Login == NewUser.Login) ?? false)
            {
                MessageView?.AddError("", GsoRep["ErrorNameUser"].ToString().Replace("#Login", NewUser.Login));
                Error = true;
            }

            if (Error)
                return;


            NewUser.OBJID = new() { StaffID = StaffId };

            IsSave = false;

            await SaveServer(new List<UserInfo> { NewUser });

            if (IsSave)
            {
                await OnInitializedAsync();
                SelectItem = Model?.FirstOrDefault(x => x.Login == NewUser.Login);
            }

            NewPassword = null;
            NewUser = new();
            IsAddUser = false;

        }

        private async Task ChangePasswordUser()
        {
            bool Error = false;

            if (SelectItem == null)
            {
                MessageView?.AddError("", AsoRep["IDS_ENTERMISMATCHERROR"]);
                return;
            }


            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (!NewPassword?.Equals(ChangePass.NewPassword) ?? true)
                {
                    MessageView?.AddError("", GsoRep["ErrorPassword"]);
                    return;
                }
            }

            ChangePass.EncryptPassword = SelectItem.Passw;

            await Http.PostAsJsonAsync("api/v1/CheckPassword", ChangePass).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    var r = await x.Result.Content.ReadFromJsonAsync<ChangePassword>();

                    if (r == null || r.EncryptPassword == null)
                    {
                        MessageView?.AddError("", GsoRep["ErrorOldPassword"]);
                        Error = true;
                    }
                    else
                    {
                        SelectItem.Passw = r.EncryptPassword;
                    }
                }
                else
                {
                    MessageView?.AddError("", AsoRep["IDS_ERRORCAPTION"]);
                    return;
                }

            });

            if (Error)
                return;

            IsSave = false;

            await SaveServer(new List<UserInfo> { SelectItem });

            NewPassword = null;
            ChangePass = new();
            IsChangePassword = false;

        }

        private void AddListChange()
        {
            AddList(SelectItem);
        }

        private void AddList(UserInfo? item)
        {
            if (item == null)
                return;

            if (ChangeItemList == null)
                ChangeItemList = new();

            if (!ChangeItemList.Contains(item))
                ChangeItemList.Add(item);
        }

        private void ChangePerUser(string? nameField, int pos)
        {
            if (SelectItem == null || SelectItem.Status == 10 || string.IsNullOrEmpty(nameField))
                return;

            if (SelectItem.Permisions == null)
                return;

            var p = SelectItem.Permisions.GetType().GetProperty(nameField);

            if (p == null)
                return;

            var b = (byte[]?)p.GetValue(SelectItem.Permisions, null);

            if (b == null)
                return;

            int byteIndex = pos / 8;
            int bitInByteIndex = pos % 8;

            byte mask = (byte)(1 << bitInByteIndex);

            int nByteBit = pos % 8;

            var val = (b[byteIndex] & mask) != 0;

            if (!val)
                b[byteIndex] |= (byte)(1 << nByteBit);
            else
                b[byteIndex] &= (byte)(~(1 << nByteBit));

            AddListChange();

        }

        void SelectAllPer()
        {
            if (SelectItem == null || SelectItem.Status == 10)
                return;

            foreach (var item in PermissionsList)
            {
                if (!IsCheckedPer(item.Name, item.PosBit))
                    ChangePerUser(item.Name, item.PosBit);
            }

        }

        void UnSelectAllPer()
        {
            if (SelectItem == null || SelectItem.Status == 10)
                return;

            foreach (var item in PermissionsList)
            {
                if (IsCheckedPer(item.Name, item.PosBit))
                    ChangePerUser(item.Name, item.PosBit);
            }

        }

        private bool IsCheckedPer(string? nameField, int pos)
        {

            if (SelectItem?.Permisions == null || string.IsNullOrEmpty(nameField))
                return false;

            var p = SelectItem?.Permisions.GetType().GetProperty(nameField);

            if (p == null)
                return false;

            var b = (byte[]?)p.GetValue(SelectItem?.Permisions, null);

            if (b == null)
                return false;

            int byteIndex = pos / 8;
            int bitInByteIndex = pos % 8;
            byte mask = (byte)(1 << bitInByteIndex);

            var r = (b[byteIndex] & mask) != 0;


            return r;
        }
    }
}
