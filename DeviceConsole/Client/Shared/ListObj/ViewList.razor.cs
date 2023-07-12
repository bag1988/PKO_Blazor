using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AsoDataProto.V1;
using SMSSGsoProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using SharedLibrary.Interfaces;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.GlobalEnums;
using FiltersGSOProto.V1;
using BlazorLibrary.Helpers;
using LibraryProto.Helpers;
using System.Diagnostics.Metrics;
using Google.Protobuf.WellKnownTypes;

namespace DeviceConsole.Client.Shared.ListObj
{
    partial class ViewList : IAsyncDisposable, IPubSubMethod
    {
        [CascadingParameter]
        public int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        private List<ListItem>? ExistsList = null;

        private List<ListItem>? SelectList = null;

        private bool? IsDelete = false;

        private bool? IsDeleteListAbon = false;

        string StatusImport = "";

        List<ListModelXml>? ImportList = null;

        List<IntAndString>? AbonList { get; set; }

        bool IsImport = false;

        bool IsCreateList = false;

        List<ListModelXml>? NoInsertList = null;

        private List<Items>? ChildAbon = null;

        private string TitleView = "";

        string NewListName = "";

        TableVirtualize<ListItem>? table;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_ASO)
                TitleView = GsoRep["IDS_STRING_LIST_AB"];
            else if (request.ObjID.SubsystemID == SubsystemType.SUBSYST_SZS)
                TitleView = GsoRep["IDS_STRING_LIST_DEVICES"];

            ThList = new Dictionary<int, string>
            {
                { 0, GsoRep["IDS_STRING_NAME"] },
                { 1, GsoRep["IDS_STRING_SIT_PRIOR"] },
                { 2, GsoRep["IDS_STRING_COUNT"] },
                { 3, GsoRep["IDS_STRING_SIT_COMMENT"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Name), GsoRep["IDS_STRING_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Prior), GsoRep["IDS_STRING_SIT_PRIOR"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Count), GsoRep["IDS_STRING_COUNT"], TypeHint.Number));
            HintItems.Add(new HintItem(nameof(FiltrModel.Comment), GsoRep["IDS_STRING_SIT_COMMENT"], TypeHint.Input));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrList);

            _ = _HubContext.SubscribeAsync(this);
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_UpdateList(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = ListItem.Parser.ParseFrom(value);
                    if (newItem != null && newItem.List.SubsystemID == SubsystemID)
                    {
                        SelectList?.ForEach(x =>
                        {
                            if (x.List.Equals(newItem.List))
                            {
                                x.Comm = newItem.Comm;
                                x.Count = newItem.Count;
                                x.Priority = newItem.Priority;
                                x.Name = newItem.Name;
                                StateHasChanged();
                                return;
                            }
                        });

                        //await table.ForEachItems(x =>
                        //{
                        //    if(x.List.Equals(newItem.List))
                        //    {
                        //        x.Comm = newItem.Comm;
                        //        x.Count = newItem.Count;
                        //        x.Priority = newItem.Priority;
                        //        return;
                        //    }
                        //});

                        if (table.AnyItemMatch(x => x.List.Equals(newItem.List)))
                        {
                            var item = table.FindItemMatch(x => x.List.Equals(newItem.List));
                            if (item != null)
                            {
                                var index = table.GetIndexItem(item);
                                await table.RemoveItem(item);
                                await table.InsertItem(index, newItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteList(byte[] value)
        {
            try
            {
                if (table != null)
                {
                    var newItem = ListItem.Parser.ParseFrom(value);

                    if (newItem != null && newItem.List.SubsystemID == SubsystemID)
                    {
                        if (string.IsNullOrEmpty(newItem.Name) && string.IsNullOrEmpty(newItem.Comm) && newItem.Priority == 0)
                        {
                            SelectList?.RemoveAll(x => x.List.Equals(newItem.List));
                            await table.RemoveAllItem(x => x.List.Equals(newItem.List));
                            StateHasChanged();
                        }
                        else
                        {
                            if (!table.AnyItemMatch(x => x.List.Equals(newItem.List)))
                            {
                                await table.AddItem(newItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ItemsProvider<ListItem> GetProvider => new ItemsProvider<ListItem>(ThList, LoadChildList, request, new List<int>() { 40, 10, 10, 40 });

        private async ValueTask<IEnumerable<ListItem>> LoadChildList(GetItemRequest req)
        {
            List<ListItem> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IList", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<ListItem>>() ?? new();
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetListNameForList", new IntAndString() { Number = SubsystemID, Str = req.BstrFilter }, ComponentDetached);
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

        async Task ExportSelectList()
        {
            if (SelectList?.Count > 0)
            {
                try
                {
                    var result = await Http.PostAsJsonAsync("api/v1/ExportListAbonent", SelectList.Select(x => x.List));
                    if (result.IsSuccessStatusCode)
                    {
                        var list = await result.Content.ReadFromJsonAsync<List<ListModelXml>>();

                        if (list?.Count > 0)
                        {
                            var formatter = new XmlSerializer(typeof(List<ListModelXml>), new XmlRootAttribute { ElementName = "ListAbonent" });

                            using var ms = new EncodingStringWriter(Encoding.UTF8);

                            XmlWriterSettings settings = new();
                            settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
                            settings.NewLineChars = Environment.NewLine;
                            settings.ConformanceLevel = ConformanceLevel.Document;
                            settings.Indent = true;
                            using var writer = XmlWriter.Create(ms, settings);
                            {
                                formatter.Serialize(writer, list);
                            }
                            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(Encoding.UTF8.GetBytes(ms.ToString())));

                            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "Списки абонентов.xml", streamRef);
                            streamRef.Dispose();
                        }
                    }
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
            NoInsertList = null;
            IsImport = true;
        }

        async Task LoadFiles(InputFileChangeEventArgs e)
        {
            try
            {
                NoInsertList = null;
                ImportList = null;
                StatusImport = AsoRep["SearchRecords"];

                if (e.File.ContentType.ToLower() != "text/xml")
                    return;

                XmlSerializer formatter = new XmlSerializer(typeof(List<ListModelXml>), new XmlRootAttribute { ElementName = "ListAbonent" });

                using var msXml = new MemoryStream();

                await e.File.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);
                //msXml.Position= 0;
                using var sr = new StringReader(Encoding.Default.GetString(msXml.ToArray()));

                ImportList = formatter.Deserialize(sr) as List<ListModelXml>;
                ImportList = ImportList?.Distinct().ToList();
                await GetExistsList();
                await msXml.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            StatusImport = $"{AsoRep["FindRecords"]} {ImportList?.GroupBy(x => x.Name).Count() ?? 0} {GsoRep["RECORDS_COUNT"]}";
        }

        async Task GetExistsList()
        {

            if (ImportList == null)
            {
                ExistsList = null;
                return;
            }
            ListFiltr requestDep = new();

            foreach (var item in ImportList)
            {
                requestDep.AddFiltrItemToFiltr(new FiltrItem(nameof(requestDep.Name), new Hint(item.Name), FiltrOperationType.OrEqual));
            }
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_IList", new GetItemRequest(request) { CountData = 0, BstrFilter = requestDep.PtoroToBase64() }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                ExistsList = await result.Content.ReadFromJsonAsync<List<ListItem>>() ?? new();
            }
        }

        async Task InsertListAbon()
        {
            if (ImportList == null || ImportList.Count == 0) return;


            var result = await Http.PostAsJsonAsync("api/v1/ImportListAbon", ImportList);
            if (result.IsSuccessStatusCode)
            {
                NoInsertList = await result.Content.ReadFromJsonAsync<List<ListModelXml>>() ?? new();
            }

            StatusImport = $"{AsoRep["INSERT"]} {ImportList.Count - NoInsertList?.Count} {GsoRep["RECORDS_COUNT"]}";

            ImportList = null;
        }

        private async Task GetFiltrAbonForName(ChangeEventArgs e)
        {
            if (e.Value == null || e.Value.ToString()?.Length < 3)
            {
                AbonList = null;
                return;
            }

            var result = await Http.PostAsJsonAsync("api/v1/GetFiltrAbonForName", new IntAndString() { Number = request.ObjID.StaffID, Str = e.Value.ToString() }, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                AbonList = await result.Content.ReadFromJsonAsync<List<IntAndString>>();
            }
            else
            {
                AbonList = new();
                MessageView?.AddError("", GsoRep["ERROR_ABON_LIST"]);
            }

            if (AbonList == null)
                AbonList = new();
        }

        async Task RepeatedInsert()
        {
            if (NoInsertList == null || NoInsertList.Count == 0) return;

            ImportList = new(NoInsertList.Select(x => new ListModelXml(x) { AbonName = x.AbonName.Replace("%exists%", "") }));

            NoInsertList = null;

            await InsertListAbon();
        }

        void ChangeAbName(string oldAb)
        {
            int newAb = AbonList?.FirstOrDefault()?.Number ?? -1;

            if (NoInsertList == null || newAb == -1) return;

            NoInsertList.ForEach(x =>
            {
                if (x.AbonName == oldAb)
                {
                    x.AbonName = AbonList?.FirstOrDefault(a => a.Number == newAb)?.Str ?? "";
                }
            });
            AbonList = null;
        }

        void ChangeListName(ChangeEventArgs e)
        {
            NewListName = e.Value?.ToString() ?? "";
        }

        void SetNewListName(string oldName)
        {
            if (string.IsNullOrEmpty(NewListName))
                return;

            ImportList?.ForEach(x =>
            {
                if (x.Name == oldName)
                {
                    x.Name = NewListName;
                }
            });

            NewListName = "";
        }


        private async Task DeleteList()
        {
            if (SelectList?.Count > 0)
            {
                List<string>? r = null;

                var lastElem = new ListItem(SelectList.Last());

                OBJ_ID obj = new OBJ_ID(lastElem.List) { SubsystemID = request.ObjID.SubsystemID };

                var result = await Http.PostAsJsonAsync("api/v1/GetLinkObjects_IList", obj);
                if (result.IsSuccessStatusCode)
                {
                    r = await result.Content.ReadFromJsonAsync<List<string>>();
                }


                string nameList = lastElem.Name;

                if (r != null && r.Count > 0)
                {
                    MessageView?.AddError(AsoRep["IDS_STRING_DELETE_DENIDE"] + ", " + AsoRep["ERR_DELETE_DENIDE"].ToString().Replace("{name}", nameList), r);
                }
                else
                {
                    // Удаление абонентов АСО, входящих в список
                    if (lastElem.List.SubsystemID == SubsystemType.SUBSYST_ASO)
                    {
                        await DeleteAsoAbonentFromList(lastElem.List);
                    }

                    result = await Http.PostAsJsonAsync("api/v1/DeleteList", lastElem.List);
                    if (!result.IsSuccessStatusCode)
                    {
                        MessageView?.AddError(GsoRep["IDS_REG_LIST_DELETE"], AsoRep["IDS_ERRORCAPTION"] + " " + nameList);
                    }
                    else
                    {
                        MessageView?.AddMessage(GsoRep["IDS_REG_LIST_DELETE"], AsoRep["IDS_OK_DELETE"] + " " + nameList);
                    }
                }
                SelectList.Remove(lastElem);

                IsDelete = false;
            }
        }

        private async Task DeleteAsoAbonentFromList(OBJ_ID request)
        {
            if (SelectList?.Count > 0)
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetListItems", request);
                if (result.IsSuccessStatusCode)
                {
                    ChildAbon = await result.Content.ReadFromJsonAsync<List<Items>>();
                }

                if (ChildAbon == null)
                    return;

                IsDeleteListAbon = true;
                StateHasChanged();
                while (IsDeleteListAbon == true)
                {
                    await Task.Delay(1000);
                }

                if (ChildAbon == null)
                    return;


                result = await Http.PostAsJsonAsync("api/v1/DeleteAndExportAbon", ChildAbon.Select(x => x.AsoAbon).ToList());
                if (result.IsSuccessStatusCode)
                {
                    var r = Convert.FromBase64String(await result.Content.ReadAsStringAsync());
                    if (r != null)
                    {
                        using var streamRef = new DotNetStreamReference(stream: new MemoryStream(r));
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "ASO Abonent Backup.xml", streamRef);
                        streamRef.Dispose();
                    }
                }
                ChildAbon = null;
            }

        }

        void ViewCreateDialog(bool isCreate)
        {
            if (isCreate && SelectList?.Count > 0)
            {
                SelectList = null;
            }
            IsCreateList = true;
        }

        private void DbClick()
        {
            if (SelectList?.Count > 0)
            {
                ViewCreateDialog(false);
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return _HubContext.DisposeAsync();
        }
    }
}
