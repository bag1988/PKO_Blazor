using System.Net.Http.Json;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Google.Protobuf.WellKnownTypes;
using AsoDataProto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;
using static BlazorLibrary.Shared.Main;
using BlazorLibrary.Shared.Table;
using BlazorLibrary.Models;
using BlazorLibrary.GlobalEnums;
using FiltersGSOProto.V1;

namespace DeviceConsole.Client.Pages.ASO.PattensMessage
{
    partial class ViewPatterns
    {
        private int SubsystemID { get; set; } = SubsystemType.SUBSYST_ASO;

        List<AbonMsgParam>? SelectList = null;

        TableVirtualize<AbonMsgParam>? table;

        List<SituationItem>? SitList { get; set; }

        List<MsgParamItem>? ImportList = null;

        List<IntAndString>? AbonList { get; set; }


        string StatusImport = "";

        bool? IsViewEdit = false;

        bool? IsDelete = false;

        bool IsImport = false;

        List<AbonMsgParam>? NoInsertList = null;

        protected override async Task OnInitializedAsync()
        {
            request.ObjID.StaffID = await _User.GetLocalStaff();
            request.ObjID.SubsystemID = SubsystemID;

            ThList = new Dictionary<int, string>
            {
                { 0, AsoRep["Value"] },
                { 1, AsoRep["Parameter"] },
                { 2, AsoRep["IDS_ABONENT"] },
                { 3, StartUIRep["IDS_SITUATION"] }
            };
            HintItems.Add(new HintItem(nameof(FiltrModel.Value), AsoRep["Value"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Param), AsoRep["Parameter"], TypeHint.Input));
            HintItems.Add(new HintItem(nameof(FiltrModel.Abonent), AsoRep["IDS_ABONENT"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpAbonName)));
            HintItems.Add(new HintItem(nameof(FiltrModel.Situation), StartUIRep["IDS_SITUATION"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 20 }, LoadHelpSitName)));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrPatterns);

            await GetSitList();
        }


        ItemsProvider<AbonMsgParam> GetProvider => new ItemsProvider<AbonMsgParam>(ThList, LoadChildList, request, new List<int>() { 25, 25, 25, 25 });

        private async ValueTask<IEnumerable<AbonMsgParam>> LoadChildList(GetItemRequest req)
        {
            List<AbonMsgParam> newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetMsgParamList", req, ComponentDetached);
            if (result.IsSuccessStatusCode)
            {
                newData = await result.Content.ReadFromJsonAsync<List<AbonMsgParam>>() ?? new();
            }
            else
            {
                MessageView?.AddError("", AsoRep["ERROR_GET_PATTERNS"]);
            }
            return newData;
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpAbonName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetAbonNameForPatterns", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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


        private async ValueTask<IEnumerable<Hint>> LoadHelpSitName(GetItemRequest req)
        {
            List<Hint>? newData = new();
            var result = await Http.PostAsJsonAsync("api/v1/GetSitNameForPatterns", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
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

        async Task GetSitList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetItems_ISituation", new GetItemRequest() { NObjType = await _User.GetUserSessId(), ObjID = new OBJ_ID() { StaffID = request.ObjID.StaffID, SubsystemID = SubsystemType.SUBSYST_ASO }, LSortOrder = 0, BFlagDirection = 0 });
            if (result.IsSuccessStatusCode)
            {
                SitList = await result.Content.ReadFromJsonAsync<List<SituationItem>>();
            }

            if (SitList == null)
                SitList = new();

            SitList?.Insert(0, new SituationItem() { SitName = AsoRep["ALL_SIT"], SitID = 0 });
        }

        private async Task DeleteMsgParam()
        {
            if (SelectList?.Count > 0)
            {
                Int32Value r = new();

                var result = await Http.PostAsJsonAsync("api/v1/DeleteMsgParam", SelectList);
                if (result.IsSuccessStatusCode)
                {
                    r = await result.Content.ReadFromJsonAsync<Int32Value>() ?? new();
                }

                if (r.Value >= 0)
                {
                    MessageView?.AddMessage("", $"{GsoRep["DELETE_COUNT"]}: {r.Value + 1} {GsoRep["RECORDS_COUNT"]}");
                }
                else
                    MessageView?.AddError("", AsoRep["ERROR_DELETE_PATTERNS"]);

                IsDelete = false;
                SelectList = null;

                await CallRefreshData();
            }
        }

        private async Task CallBackEvent(bool? update)
        {
            if (update == true)
            {
                SelectList = new();
                await CallRefreshData();
            }
            IsViewEdit = false;
        }

        async Task ExportSelectMsgParam()
        {
            if (SelectList?.Count > 0)
            {
                try
                {
                    var formatter = new XmlSerializer(typeof(List<MsgParamItem>), new XmlRootAttribute { ElementName = "MsgParam" });

                    using var ms = new EncodingStringWriter(Encoding.UTF8);

                    XmlWriterSettings settings = new();
                    settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
                    settings.NewLineChars = Environment.NewLine;
                    settings.ConformanceLevel = ConformanceLevel.Document;
                    settings.Indent = true;
                    using var writer = XmlWriter.Create(ms, settings);
                    {
                        formatter.Serialize(writer, SelectList.Select(x => new MsgParamItem()
                        {
                            AbonName = x.AbonID > 0 ? x.AbonName : AsoRep["ALL_ABON"],
                            SitName = x.SitID > 0 ? SitList?.FirstOrDefault(s => s.SitID == x.SitID)?.SitName ?? AsoRep["ALL_SIT"] : AsoRep["ALL_SIT"],
                            ParamName = x.ParamName,
                            ParamValue = x.ParamValue
                        }).ToList());
                    }
                    using var streamRef = new DotNetStreamReference(stream: new MemoryStream(Encoding.UTF8.GetBytes(ms.ToString())));

                    await JSRuntime.InvokeVoidAsync("downloadFileFromStream", "Параметры шаблонов.xml", streamRef);

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

                XmlSerializer formatter = new XmlSerializer(typeof(List<MsgParamItem>), new XmlRootAttribute { ElementName = "MsgParam" });

                using var msXml = new MemoryStream();

                await e.File.OpenReadStream(maxAllowedSize: 3003486).CopyToAsync(msXml);
                //msXml.Position= 0;
                using var sr = new StringReader(Encoding.Default.GetString(msXml.ToArray()));

                ImportList = formatter.Deserialize(sr) as List<MsgParamItem>;

                ImportList = ImportList?.Distinct().ToList();

                await msXml.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            StatusImport = $"{AsoRep["FindRecords"]} {ImportList?.Count ?? 0} {GsoRep["RECORDS_COUNT"]}";
        }

        async Task InsertMsgParam()
        {
            if (ImportList == null || ImportList.Count == 0) return;

            List<AbonMsgParam> insertList = new();

            insertList.AddRange(ImportList.Select(x => new AbonMsgParam()
            {
                StaffID = request.ObjID.StaffID,
                SubsystemID = SubsystemType.SUBSYST_ASO,
                ParamName = x.ParamName,
                ParamValue = x.ParamValue,
                AbonName = x.AbonName,
                SitID = SitList?.FirstOrDefault(s => s.SitName.Trim() == x.SitName.Trim())?.SitID ?? -1,
                AbonID = AsoRep["ALL_ABON"].Value == x.AbonName ? 0 : -1
            }));

            insertList.RemoveAll(x => x.SitID == -1);

            if (insertList.Count == 0)
                return;

            NoInsertList = null;
            var result = await Http.PostAsJsonAsync("api/v1/ImportMsgParam", insertList);
            if (result.IsSuccessStatusCode)
            {
                NoInsertList = await result.Content.ReadFromJsonAsync<List<AbonMsgParam>>() ?? new();
            }

            StatusImport = $"{AsoRep["INSERT"]} {ImportList.Count - NoInsertList?.Count} {GsoRep["RECORDS_COUNT"]}";

            if (ImportList.Count - NoInsertList?.Count > 0)
            {
                await CallRefreshData();
            }
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
            if (AsoRep["ALL_ABON"].Value.Contains(e.Value.ToString() ?? ""))
                AbonList.Insert(0, new IntAndString() { Number = 0, Str = AsoRep["ALL_ABON"] });
        }

        void RepeatedInsert(MsgParamItem item)
        {

            if (ImportList?.Contains(item) ?? false)
                return;

            if (ImportList == null)
                ImportList = new();

            ImportList.Add(item);

        }

        void ChangeAbName(string oldAb)
        {
            int newAb = AbonList?.FirstOrDefault()?.Number ?? -1;

            if (NoInsertList == null || newAb == -1) return;

            NoInsertList.RemoveAll(x =>
            {
                if (x.AbonName == oldAb)
                {
                    RepeatedInsert(new MsgParamItem()
                    {
                        AbonName = AbonList?.FirstOrDefault(a => a.Number == newAb)?.Str ?? "",
                        ParamName = x.ParamName,
                        ParamValue = x.ParamValue.Split("$old$")[0],
                        SitName = SitList?.FirstOrDefault(s => s.SitID == x.SitID)?.SitName ?? ""
                    });
                    return true;
                }
                return false;
            });
            StatusImport = $"{AsoRep["WILL_BE_ADDED"]} {ImportList?.Count ?? 0} {GsoRep["RECORDS_COUNT"]}";
            AbonList = null;
        }

        void ChangeSitName(ChangeEventArgs e, string oldName)
        {
            var newName = e.Value?.ToString();

            if (ImportList == null || string.IsNullOrEmpty(newName)) return;

            ImportList.ForEach(x =>
            {
                if (x.SitName == oldName)
                {
                    x.SitName = newName;
                }
            });
        }

        async Task DeleteOldParam()
        {
            if (NoInsertList == null) return;

            if (!NoInsertList.Any(x => x.AbonID >= 0)) return;

            SelectList = new();

            SelectList.AddRange(NoInsertList.Where(x => x.AbonID >= 0 && x.ParamValue.Contains("$old$")).Select(x => new AbonMsgParam(x) { ParamValue = x.ParamValue.Split("$old$")[1] }));

            await DeleteMsgParam();

            NoInsertList.RemoveAll(x =>
            {
                if (x.ParamValue.Contains("$old$"))
                {
                    RepeatedInsert(new MsgParamItem()
                    {
                        AbonName = x.AbonName,
                        ParamName = x.ParamName,
                        ParamValue = x.ParamValue.Split("$old$")[0],
                        SitName = SitList?.FirstOrDefault(s => s.SitID == x.SitID)?.SitName ?? ""
                    });
                    return true;
                }
                return false;
            });
            StatusImport = $"{AsoRep["WILL_BE_ADDED"]} {ImportList?.Count ?? 0} {GsoRep["RECORDS_COUNT"]}";
        }
    }
}
