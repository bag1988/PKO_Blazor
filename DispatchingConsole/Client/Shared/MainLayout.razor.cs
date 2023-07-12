using System.Net.Http.Json;
using BlazorLibrary.Models;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace DispatchingConsole.Client.Shared
{
    partial class MainLayout : IAsyncDisposable
    {
        public readonly List<ActiveConnect> ActiveConnectItems = new();

        public ElementReference? localVideo = null;

        public List<CGetRegList>? SelectList = null;

        List<CGetRegList>? CacheItemsFirstLevel = null;

        string NewName = string.Empty;
        string NewIp = string.Empty;

        readonly Dictionary<CGetRegList, List<CGetRegList>> CacheItems = new();

        int StaffId = 0;

        bool IsLoadData = false;

        bool IsAdd = false;

        protected override Task OnInitializedAsync()
        {
            _ = PushOnInitAsync();
            return base.OnInitializedAsync();
        }

        IEnumerable<ChildItems<CGetRegList>>? GetItemsStaff
        {
            get
            {
                _ = LoadFirstLevel();
                List<ChildItems<CGetRegList>>? response = null;
                if (CacheItemsFirstLevel != null)
                {
                    response = CacheItemsFirstLevel.Select(x => new ChildItems<CGetRegList>(x, x.OBJID.StaffID > 0 && x.OBJID.ObjID > 0 ? GetChildItems : null)).ToList();
                }
                return response;
            }
        }

        IEnumerable<ChildItems<CGetRegList>>? GetChildItems(CGetRegList item)
        {
            List<ChildItems<CGetRegList>> response = new();
            if ((CacheItems?.ContainsKey(item) ?? false))
            {
                foreach (var child in CacheItems[item])
                {
                    if ((CacheItemsFirstLevel?.Any(x => x.OBJID.StaffID == child.OBJID.StaffID) ?? false))
                    {
                        response.Add(new ChildItems<CGetRegList>(child));
                    }
                    else if (CacheItems.Any(x => x.Key.OBJID.StaffID == child.OBJID.StaffID && x.Key.OBJID.ObjID != child.OBJID.ObjID))
                    {
                        response.Add(new ChildItems<CGetRegList>(child));
                    }
                    else
                        response.Add(new ChildItems<CGetRegList>(child, GetChildItems));
                }

            }
            return response;
        }


        async Task LoadFirstLevel()
        {
            if (StaffId == 0)
            {
                StaffId = await _User.GetLocalStaff();
            }
            if (!IsLoadData && StaffId > 0 && CacheItemsFirstLevel == null)
            {
                IsLoadData = true;
                CacheItemsFirstLevel = await GetItems_IRegistration(new GetItemsRegistration() { ChildStaff = StaffId });

                //if (MyNavigationManager.BaseUri.Contains("7222"))
                //{
                //    CacheItemsFirstLevel.Insert(0, new CGetRegList() { CCURegistrList = new() { CUName = "localhost:2291", CUUNC = "https://localhost:2291", CUType = DateTime.Now.Ticks.ToString() }, OBJID = new() });
                //}
                //else
                //{
                //    CacheItemsFirstLevel.Insert(0, new CGetRegList() { CCURegistrList = new() { CUName = "localhost:7222", CUUNC = "https://localhost:7222", CUType = DateTime.Now.Ticks.ToString() }, OBJID = new() });
                //}
            }
        }

        private async Task<List<CGetRegList>> GetItems_IRegistration(GetItemsRegistration request)
        {
            List<CGetRegList> response = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/chat/GetItems_IRegistration", request);
                if (result.IsSuccessStatusCode)
                {
                    response = await result.Content.ReadFromJsonAsync<List<CGetRegList>>() ?? new();

                    var i = 1;

                    response.ForEach(x =>
                    {
                        x.OutLong = i;
                        x.OBJID.ObjID = (CacheItemsFirstLevel?.Count ?? 0) + CacheItems.SelectMany(x => x.Value).Count() + i;
                        x.OBJID.SubsystemID = request.ChildStaff;
                        i++;
                    });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return response;
        }

        async Task SetSelectList(List<CGetRegList>? items)
        {
            SelectList = items;

            if (SelectList != null && SelectList.Count == 1)
            {
                CGetRegList lastElem = new(SelectList.Last());

                if (lastElem.OBJID?.StaffID > 0)
                {
                    if (CacheItems.ContainsKey(lastElem) && (CacheItemsFirstLevel?.Any(x => x.OBJID.StaffID == lastElem.OBJID.StaffID) ?? false))
                    {
                        SelectList = new() { CacheItemsFirstLevel.First(x => x.OBJID.StaffID == lastElem.OBJID.StaffID) };
                    }
                    else if (CacheItems.Any(x => x.Key.OBJID.StaffID > 0 && x.Key.OBJID.StaffID == lastElem.OBJID.StaffID))
                    {
                        SelectList = new() { CacheItems.First(x => x.Key.OBJID.StaffID == lastElem.OBJID.StaffID).Key };
                    }
                    else
                    {
                        var key = CacheItems.FirstOrDefault(x => x.Value.Contains(lastElem));

                        GetItemsRegistration requestChild = new();

                        requestChild.ParentStaff = key.Key?.OBJID?.StaffID ?? StaffId;

                        requestChild.ChildStaff = lastElem.OBJID?.StaffID ?? 0;

                        requestChild.ParentUrl = key.Key?.CCURegistrList?.CUUNC ?? string.Empty;

                        if (!CacheItems.ContainsKey(lastElem))
                        {
                            CacheItems.Add(lastElem, new());
                        }

                        if (CacheItems[lastElem].Count == 0 && lastElem.OBJID?.StaffID > 0)
                        {
                            CacheItems[lastElem] = await GetItems_IRegistration(requestChild);
                        }
                    }
                }


            }
        }


        public async Task AddAndSelect(string nameRoom, string inUrl)
        {
            if (CacheItemsFirstLevel?.Any(x => IpAddressUtilities.CompareForAuthority(x.CCURegistrList?.CUUNC, inUrl)) ?? false)
            {
                await SetSelectList(new List<CGetRegList>() { CacheItemsFirstLevel.First(x => IpAddressUtilities.CompareForAuthority(x.CCURegistrList?.CUUNC, inUrl)) });
            }
            else if (CacheItems.Any(x => x.Value.Any(x => IpAddressUtilities.CompareForAuthority(x.CCURegistrList?.CUUNC, inUrl))))
            {
                await SetSelectList(new List<CGetRegList>() { CacheItems.SelectMany(x => x.Value).First(x => IpAddressUtilities.CompareForAuthority(x.CCURegistrList?.CUUNC, inUrl)) });
            }
            else
            {
                var newItem = new CGetRegList()
                {
                    OBJID = new OBJ_ID(),
                    CCURegistrList = new CCURegistrList()
                    {
                        CUName = nameRoom,
                        CUUNC = inUrl,
                        CUType = DateTime.Now.Ticks.ToString()
                    }
                };

                if (CacheItemsFirstLevel == null)
                {
                    CacheItemsFirstLevel = new();
                }

                CacheItemsFirstLevel.Add(newItem);

                await SetSelectList(new List<CGetRegList>() { newItem });
            }
        }


        void AddConnectInfo()
        {
            if (!string.IsNullOrEmpty(NewName) && !string.IsNullOrEmpty(NewIp))
            {
                if (CacheItemsFirstLevel == null)
                    CacheItemsFirstLevel = new();

                CacheItemsFirstLevel.Add(new CGetRegList()
                {
                    OBJID = new OBJ_ID(),
                    CCURegistrList = new CCURegistrList()
                    {
                        CUName = NewName,
                        CUUNC = $"{NewIp}:2291",
                        CUType = DateTime.Now.Ticks.ToString()
                    }
                });
            }

            CloseDialog();
        }


        void CloseDialog()
        {
            NewName = string.Empty;
            NewIp = string.Empty;
            IsAdd = false;
        }

        public async ValueTask DisposeAsync()
        {
            await PushDisposeAsync();
        }
    }
}
