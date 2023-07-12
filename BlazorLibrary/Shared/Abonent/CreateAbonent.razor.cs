using System.Net.Http.Json;
using AsoDataProto.V1;
using BlazorLibrary.Shared.LabelsComponent;
using Google.Protobuf;
using Label.V1;
using Microsoft.AspNetCore.Components;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;

namespace BlazorLibrary.Shared.Abonent
{
    partial class CreateAbonent
    {
        [Parameter]
        public int? Abon { get; set; }

        [Parameter]
        public EventCallback<bool?> CallbackEvent { get; set; }

        [Parameter]
        public string? CallbackUrl { get; set; }

        private List<Shedule>? SheduleList = null;
        private List<Shedule>? OldSheduleList = null;

        private bool IsSave = false;

        private AbonInfo Model = new();

        private List<Department> DepartmentList = new();

        private List<IntAndString> AbStatusList = new();

        private string Password = "";

        private bool AddPassword = false;

        bool IsProcessing = false;

        private string TitleError = "";

        private int LocalStaff = 0;

        Labels? _labels { get; set; }

        protected override async Task OnInitializedAsync()
        {
            LocalStaff = await _User.GetLocalStaff();
            TitleError = AsoRep[Abon != 0 ? "IDS_REG_AB_SAVE" : "IDS_REG_AB_INSERT"];
            IsSave = false;
            await GetAbInfo();
            await GetDepartmentList();
            await GetAbStatusList();
            await GetSheduleInfo();
        }

        private async Task GetAbInfo()
        {
            if (Abon != null && Abon != 0)
            {
                OBJ_ID request = new OBJ_ID() { ObjID = Abon.Value, StaffID = LocalStaff };
                var result = await Http.PostAsJsonAsync("api/v1/GetAbInfo", request);
                if (result.IsSuccessStatusCode)
                {
                    Model = await result.Content.ReadFromJsonAsync<AbonInfo>() ?? new() { Dep = new OBJ_ID(), AbPrior = 1, AbStatus = 1 };
                }
            }
            else
            {
                Model = new() { Abon = new OBJ_ID() { StaffID = LocalStaff }, Dep = new OBJ_ID(), AbPrior = 1, AbStatus = 1 };
            }
            if (!string.IsNullOrEmpty(Model.Password))
            {
                AddPassword = true;
                Password = Model.Password;
            }
        }

        OBJ_Key GetOBJ_Key
        {
            get
            {
                return new() { ObjID = new() { ObjID = Abon ?? 0, StaffID = LocalStaff } };
            }
        }

        private async Task SetAbInfo()
        {
            if (Model != null)
            {
                IsProcessing = true;
                if (string.IsNullOrEmpty(Model.AbName))
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_NOABNAME"]);
                }
                else if (Model.AbPrior < 0 && Model.AbPrior > 100)
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_ERRORCREATEABONENT"]);
                }
                else if (string.IsNullOrEmpty(Model.Password) && (SheduleList?.Any(x => x.Beeper == 2) ?? false))
                {
                    MessageView?.AddError(TitleError, GsoRep["IDS_E_SAVE_SHEDULE_PASSWORD"]);
                }
                else if (!string.IsNullOrEmpty(Model.Password) && Model.Password != Password)
                {
                    MessageView?.AddError(TitleError, Rep["ERROR_RE_PASSWORD"]);
                }
                else if (SheduleList?.Any() ?? false)
                {
                    if (Model.Abon == null)
                    {
                        Model.Abon = new OBJ_ID() { StaffID = LocalStaff };
                    }

                    Model.Dep = DepartmentList.FirstOrDefault(x => x.Dep.ObjID == Model.Dep.ObjID)?.Dep ?? new();

                    if (string.IsNullOrEmpty(Model.Password))
                    {
                        Password = "";
                        Model.Password = "";
                    }

                    var result = await Http.PostAsJsonAsync("api/v1/SetAbInfo", Model);
                    if (result.IsSuccessStatusCode)
                    {
                        var id = await result.Content.ReadFromJsonAsync<IntID>();
                        if (id != null && id.ID != 0)
                        {
                            await SaveShulde(id.ID);
                            await SaveSpecifications(id.ID);
                            IsSave = true;
                            _ = Task.Delay(2000).ContinueWith(x =>
                            {
                                IsSave = false; StateHasChanged();
                            });
                            if (CallbackEvent.HasDelegate)
                                await CallbackEvent.InvokeAsync(true);
                        }
                        else
                        {
                            MessageView?.AddError(TitleError, AsoRep["IDS_E_FAILCREATEABONENT"]);
                        }
                    }
                    else
                    {
                        MessageView?.AddError(TitleError, AsoRep["IDS_E_CREATEABONENT"]);
                    }
                }
                else
                {
                    MessageView?.AddError(TitleError, AsoRep["IDS_E_NOSHEDULE"]);
                }
            }
            IsProcessing = false;
        }

        private async Task SaveShulde(int abonId)
        {
            try
            {
                if (SheduleList != null)
                {
                    int StaffID = LocalStaff;
                    SheduleList.ForEach(x =>
                    {
                        x.Abon = new() { ObjID = abonId, StaffID = StaffID };
                    });

                    if (Abon > 0 && OldSheduleList != null)
                    {
                        var deleteShedule = OldSheduleList?.Where(x => !SheduleList.Any(s => s.ASOShedule.Equals(x.ASOShedule))).ToList();

                        if (deleteShedule?.Count > 0)
                        {
                            foreach (var item in deleteShedule)
                            {
                                await DeleteShedule(item);
                            }
                        }
                    }

                    List<Shedule>? request = new();

                    if (OldSheduleList != null)
                    {
                        request = SheduleList.Except(OldSheduleList).ToList();
                    }

                    if (request.Count > 0)
                    {
                        request.ForEach(x =>
                        {
                            if (x.ASOShedule.ObjID > 0 && x.ASOShedule.StaffID == 0)
                                x.ASOShedule = new();
                        });
                        var result = await Http.PostAsJsonAsync("api/v1/SetSheduleInfo", request);
                        if (!result.IsSuccessStatusCode)
                        {
                            MessageView?.AddError("", AsoRep["IDS_E_CREATESHEDULEREC"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private async Task SaveSpecifications(int abonId)
        {
            try
            {
                if (_labels != null)
                {
                    if (_labels.keyList != null)
                    {
                        LabelFieldAndOBJKey requestItem = new()
                        {
                            FieldList = _labels.keyList.FieldList,
                            ObjKey = new() { ObjID = new() { ObjID = abonId, StaffID = LocalStaff } }
                        };

                        string json = JsonFormatter.Default.Format(requestItem);

                        await Http.PostAsJsonAsync("api/v1/UpdateLabelFieldAsoAbonent", json);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task DeleteShedule(Shedule item)
        {
            if (Abon > 0)
            {
                IntID response = new IntID() { ID = 1 };
                response.ID = 1;
                var result = await Http.PostAsJsonAsync("api/v1/DeleteSheduleInfo", new OBJ_ID(item.ASOShedule) { SubsystemID = Abon.Value });
                if (result.IsSuccessStatusCode)
                {
                    response = await result.Content.ReadFromJsonAsync<IntID>() ?? new() { ID = 1 };
                }

                if (response.ID == 1)
                {
                    MessageView?.AddError(item.ConnParam, AsoRep["IDS_E_DELETE"]);
                }
            }
        }

        private async Task GetSheduleInfo()
        {
            if (Abon != null)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetSheduleInfo", new OBJ_ID() { ObjID = Abon.Value, StaffID = LocalStaff });
                if (result.IsSuccessStatusCode)
                {
                    SheduleList = await result.Content.ReadFromJsonAsync<List<Shedule>>() ?? new();
                }
                else
                    SheduleList = new();
            }
            else
                SheduleList = new();
            if (SheduleList != null)
                OldSheduleList = new(SheduleList.Select(x => new Shedule(x)));
        }

        private async Task GetDepartmentList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetDepartmentList", new IntID() { ID = LocalStaff });
            if (result.IsSuccessStatusCode)
            {
                DepartmentList = await result.Content.ReadFromJsonAsync<List<Department>>() ?? new();
            }
        }

        private async Task GetAbStatusList()
        {
            var result = await Http.PostAsync("api/v1/GetAbStatusList", null);
            if (result.IsSuccessStatusCode)
            {
                AbStatusList = await result.Content.ReadFromJsonAsync<List<IntAndString>>() ?? new();
            }
        }

    }
}
