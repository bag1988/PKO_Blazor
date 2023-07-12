using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using BlazorLibrary.Helpers;
using LibraryProto.Helpers;

namespace DeviceConsole.Client.Pages.ASO.Department
{
    partial class ViewDepartment : IAsyncDisposable, IPubSubMethod
    {
        private int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<DepartmentAso>? ExistsDepartment = null;

        private List<DepartmentAso>? SelectList = null;

        TableVirtualize<DepartmentAso>? table;

        string StatusImport = "";

        private bool? IsViewEdit = false;

        private bool? IsDelete = false;

        List<DepartmentXml>? ImportList = null;

        bool IsImport = false;

        public struct DepartmentXml
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, AsoRep["IDS_DEPARTMENT"] },
                { 1, AsoRep["IDS_DEPCOMMENT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), AsoRep["IDS_DEPARTMENT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), AsoRep["IDS_DEPCOMMENT"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrDepartment);

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateDepartment(uint Value)
        {
            SelectList = null;
            await CallRefreshData();
            StateHasChanged();
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteDepartment(uint Value)
        {
            SelectList = null;
            await CallRefreshData();
            StateHasChanged();
        }

        ItemsProvider<DepartmentAso> GetProvider => new ItemsProvider<DepartmentAso>(ThList, LoadChildList, request, new List<int>() { 50, 50 });

        private async ValueTask<IEnumerable<DepartmentAso>> LoadChildList(GetItemRequest req)
        {
            List<DepartmentAso> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/IDepartment_Aso_GetItems", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<DepartmentAso>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", AsoRep["IDS_EFAILGETDEPLISTINFO"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetDepNameForDepList", new IntAndString() { Number = request.ObjID.StaffID, Str = req.BstrFilter }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                if (response?.Count > 0)
                {
                    newData.AddRange(response.Select(x => new Hint(x.Str)));
                }
            }
            return newData ?? new();
        }

        private async Task RefreshTable()
        {
            SelectList = null;
            if (table != null)
                await table.ResetData();
        }

        async Task ExportSelectDepartment()
        {
            if (SelectList?.Count > 0)
            {
                try
                {
                    var formatter = new XmlSerializer(typeof(List<DepartmentXml>), new XmlRootAttribute { ElementName = "Department" });

                    using var ms = new EncodingStringWriter(Encoding.UTF8);

                    XmlWriterSettings settings = new();
                    settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
                    settings.NewLineChars = Environment.NewLine;
                    settings.ConformanceLevel = ConformanceLevel.Document;
                    settings.Indent = true;
                    using var writer = XmlWriter.Create(ms, settings);
                    {
                        formatter.Serialize(writer, SelectList.Select(x => new DepartmentXml()
                        {
                            Name = x.DepName,
                            Description = x.DepComm
                        }).ToList());
                    }
                    using var streamRef = new DotNetStreamReference(stream: new MemoryStream(Encoding.UTF8.GetBytes(ms.ToString())));

                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "Department.xml", streamRef);

                    streamRef.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        void ViewDialogImport()
        {
            StatusImport = "";
            ImportList = null;
            IsImport = true;
        }

        async Task LoadFiles(InputFileChangeEventArgs e)
        {
            try
            {
                ImportList = null;
                StatusImport = AsoRep["SearchRecords"];
                var ex = e.File.ContentType;//text/xml

                if (e.File.ContentType.ToLower() != "text/xml")
                    return;

                XmlSerializer formatter = new XmlSerializer(typeof(List<DepartmentXml>), new XmlRootAttribute { ElementName = "Department" });

                using var msXml = new MemoryStream();

                await e.File.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);

                using var sr = new StringReader(Encoding.Default.GetString(msXml.ToArray()));

                ImportList = formatter.Deserialize(sr) as List<DepartmentXml>;

                ImportList = ImportList?.Distinct().ToList();
                await GetExistsDepartment();
                await msXml.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            StatusImport = $"{AsoRep["FindRecords"]} {ImportList?.Count ?? 0} {GsoRep["RECORDS_COUNT"]}";
        }


        async Task GetExistsDepartment()
        {

            if (ImportList == null)
            {
                ExistsDepartment = null;
                return;
            }
            DepartmentFiltr requestDep = new();

            foreach (var item in ImportList)
            {
                requestDep.AddFiltrItemToFiltr(new FiltrItem(nameof(requestDep.Name), new Hint(item.Name), FiltrOperationType.OrEqual));
            }
            var result = await Http.PostAsJsonAsync("api/v1/IDepartment_Aso_GetItems", new GetItemRequest(request) { CountData = 0, BstrFilter = requestDep.PtoroToBase64() }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                ExistsDepartment = await result.Content.ReadFromJsonAsync<List<DepartmentAso>>() ?? new();
            }
        }

        async Task ImportDepartment()
        {
            ImportList?.RemoveAll(x => ExistsDepartment?.Any(m => m.DepName.Trim().Equals(x.Name.Trim())) ?? false);

            if (ImportList == null || ImportList.Count == 0) return;

            List<DepartmentAso> insertList = new();

            insertList.AddRange(ImportList.Select(x => new DepartmentAso()
            {
                StaffID = request.ObjID.StaffID,
                DepName = x.Name,
                DepComm = x.Description
            }));

            foreach (var item in ImportList)
            {
                DepartmentAso insertDep = new()
                {
                    StaffID = request.ObjID.StaffID,
                    DepName = item.Name,
                    DepComm = item.Description
                };

                await Http.PostAsJsonAsync("api/v1/SetDepartmentInfo", insertDep).ContinueWith(x =>
                {
                    if (x.Result.IsSuccessStatusCode)
                    {
                        MessageView?.AddMessage("", insertDep.DepName + " - " + AsoRep["INSERT"]);
                    }
                    else
                    {
                        MessageView?.AddError("", insertDep.DepName + " " + AsoRep["IDS_E_SAVEDEPARTMENT"]);
                    }
                });
            }

            ImportList = null;
            IsImport = false;
        }

        private async Task DeleteDepartment()
        {
            if (SelectList?.Count > 0)
            {
                List<string>? r = null;

                var lastElem = SelectList.Last();

                var result = await Http.PostAsJsonAsync("api/v1/IDepartment_Aso_GetLinkObjects", new OBJ_ID() { ObjID = lastElem.IDDep, StaffID = request.ObjID.StaffID });
                if (result.IsSuccessStatusCode)
                {
                    r = await result.Content.ReadFromJsonAsync<List<string>>();
                }

                if (r != null && r.Count > 0)
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_DELETE_DENIDE"] + ", " + AsoRep["ERR_DELETE_DENIDE"].ToString().Replace("{name}", lastElem.DepName), r);
                }
                else
                {
                    result = await Http.PostAsJsonAsync("api/v1/DeleteDepartment", new OBJ_ID() { ObjID = lastElem.IDDep, StaffID = request.ObjID.StaffID });
                    if (result.IsSuccessStatusCode)
                    {
                        SelectList = null;
                    }
                    else
                        MessageView?.AddError(AsoRep["IDS_STRING_DEPARTMENTS"], AsoRep["IDS_EFAIL_DELDEPARTMENT"]);
                }
                IsDelete = false;
            }
        }

        private void CallBackEvent(bool? update)
        {
            IsViewEdit = false;
            SelectList = null;
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
