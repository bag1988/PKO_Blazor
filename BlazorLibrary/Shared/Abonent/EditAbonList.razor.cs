using System.Net.Http.Json;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Abonent
{
    partial class EditAbonList
    {
        [Parameter]
        public IEnumerable<AbonentItem>? ModelList { get; set; }

        [Parameter]
        public EventCallback<bool?> CallbackEvent { get; set; }

        private AbonInfo Model = new() { Abon = new OBJ_ID(), Dep = new OBJ_ID(), AbPrior = 1, AbStatus = 1 };

        private AbonInfo OldModel = new();

        private bool IsSave = false;

        private List<Department> DepartmentList = new();

        private List<IntAndString> AbStatusList = new();

        private string Password = "";

        private bool AddPassword = false;

        bool IsProcessing = false;

        private string TitleError = "";

        private int LocalStaff = 0;

        protected override async Task OnInitializedAsync()
        {
            LocalStaff = await _User.GetLocalStaff();
            TitleError = AsoRep["IDS_STRING_MULTI_EDIT"];
            IsSave = false;
            await GetDepartmentList();
            await GetAbStatusList();
            SetGeneralFields();
        }


        private void SetGeneralFields()
        {
            if (ModelList != null)
            {
                var firstModel = ModelList.FirstOrDefault() ?? new();

                //OBJ_ID dep = new() { ObjID = firstModel.IDDep, StaffID = firstModel.StaffID };               
                Model.Dep = ModelList.Any(x => !x.IDDep.Equals(firstModel.IDDep)) ? new() { ObjID = -1 } : new() { ObjID = firstModel.IDDep, StaffID = firstModel.StaffID };
                Model.Position = ModelList.Any(x => !x.Position.Equals(firstModel.Position)) ? "" : firstModel.Position;
                Model.AbStatus = ModelList.Any(x => !x.AbStatus.Equals(firstModel.AbStatus)) ? -1 : firstModel.AbStatus;
                Model.Role = ModelList.Any(x => !x.Role.Equals(firstModel.Role)) ? -1 : firstModel.Role;
                Model.AbPrior = ModelList.Any(x => !x.AbPrior.Equals(firstModel.AbPrior)) ? 0 : firstModel.AbPrior;
                Model.Password = ModelList.Any(x => !x.Password.Equals(firstModel.Password)) ? "clear for delete password" : firstModel.Password;

                if (!string.IsNullOrEmpty(Model.Password))
                {
                    AddPassword = true;
                    Password = Model.Password;
                }
                OldModel = new AbonInfo(Model);
            }
        }

        private void OnSaveChange()
        {            
            if (!string.IsNullOrEmpty(Model.Password) && Model.Password != Password)
            {
                MessageView?.AddError(TitleError, Rep["ERROR_RE_PASSWORD"]);
                return;
            }
            if (!OldModel.Equals(Model))
            {
                if (string.IsNullOrEmpty(Model.Password))
                {
                    Password = "";
                    Model.Password = "";
                }
                IsSave = true;
            }               
            else
            {
                MessageView?.AddError(TitleError, AsoRep["NotChangeSave"]);
            }
        }

        private async Task SetAbInfo()
        {
            IsProcessing = true;
            if (ModelList != null)
            {
                if (!OldModel.Equals(Model))
                {
                    if (Model.AbPrior < 0 && Model.AbPrior > 100)
                    {
                        MessageView?.AddError(TitleError, AsoRep["IDS_ERRORCREATEABONENT"]);                        
                    }
                    else
                    {
                        foreach (var item in ModelList)
                        {
                            var r = await GetAbInfo(item.IDAb);
                            if (r != null)
                            {
                                bool isUpdate = false;
                                if (!OldModel.Dep.ObjID.Equals(Model.Dep.ObjID) && !r.Dep.ObjID.Equals(Model.Dep.ObjID))
                                {
                                    isUpdate = true;
                                    r.Dep = DepartmentList.FirstOrDefault(x => x.Dep.ObjID == Model.Dep.ObjID)?.Dep ?? new();
                                }
                                if (!OldModel.Position.Equals(Model.Position) && !r.Position.Equals(Model.Position))
                                {
                                    isUpdate = true;
                                    r.Position = Model.Position;
                                }
                                if (!OldModel.AbStatus.Equals(Model.AbStatus) && !r.AbStatus.Equals(Model.AbStatus))
                                {
                                    isUpdate = true;
                                    r.AbStatus = Model.AbStatus;
                                }
                                if (!OldModel.Role.Equals(Model.Role) && !r.Role.Equals(Model.Role))
                                {
                                    isUpdate = true;
                                    r.Role = Model.Role;
                                }
                                if (!OldModel.AbPrior.Equals(Model.AbPrior) && !r.AbPrior.Equals(Model.AbPrior))
                                {
                                    isUpdate = true;
                                    r.AbPrior = Model.AbPrior;
                                }
                                if (!OldModel.Password.Equals(Model.Password) && !r.Password.Equals(Model.Password))
                                {
                                    isUpdate = true;
                                    r.Password = Model.Password;
                                }

                                if (isUpdate)
                                {
                                    await Http.PostAsJsonAsync("api/v1/SetAbInfo", r).ContinueWith(async x =>
                                    {
                                        if (x.Result.IsSuccessStatusCode)
                                        {
                                            var id = await x.Result.Content.ReadFromJsonAsync<IntID>();
                                            if (id == null || id.ID == 0)
                                            {
                                                MessageView?.AddError(TitleError, item.AbName + " " + AsoRep["IDS_E_FAILCREATEABONENT"]);
                                            }
                                        }
                                        else
                                        {
                                            MessageView?.AddError(TitleError, item.AbName + " " + AsoRep["IDS_E_CREATEABONENT"]);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                MessageView?.AddError(TitleError, item.AbName + " " + AsoRep["IDS_EGETABONENTINFO"]);
                            }
                        }
                    }
                }
            }
            IsSave = false;
            await CallBackOn(true);
            IsProcessing = false;
        }


        private async Task CallBackOn(bool? update = null)
        {           
            if (CallbackEvent.HasDelegate)
                await CallbackEvent.InvokeAsync(update);
        }

        private async Task<AbonInfo?> GetAbInfo(int Abon)
        {
            AbonInfo? abonInfo = null;
            if (Abon != 0)
            {
                OBJ_ID request = new OBJ_ID() { ObjID = Abon, StaffID = LocalStaff };
                await Http.PostAsJsonAsync("api/v1/GetAbInfo", request).ContinueWith(async x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        abonInfo = await x.Result.Content.ReadFromJsonAsync<AbonInfo>();
                    }
                });
            }
            return abonInfo;
        }


        private async Task GetDepartmentList()
        {
            await Http.PostAsJsonAsync("api/v1/GetDepartmentList", new IntID() { ID = LocalStaff }).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    DepartmentList = await x.Result.Content.ReadFromJsonAsync<List<Department>>() ?? new();
                }
            });
        }

        private async Task GetAbStatusList()
        {
            await Http.PostAsync("api/v1/GetAbStatusList", null).ContinueWith(async x =>
            {
                if (x.Result.IsSuccessStatusCode)
                {
                    AbStatusList = await x.Result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
                }
            });
        }

    }
}
