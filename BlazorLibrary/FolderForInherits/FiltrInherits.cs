using BlazorLibrary.Helpers;
using BlazorLibrary.Models;
using BlazorLibrary.ServiceColection;
using Google.Protobuf;
using LibraryProto.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ReplaceLibrary;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace BlazorLibrary.FolderForInherits
{
    public class FiltrInherits<TItem> : CancellableComponent where TItem : IMessage, new()
    {
        [Inject]
        LocalStorage? _localStorage { get; set; }

        [Inject]
        GetUserInfo? _User { get; set; }

        [Inject]
        IStringLocalizer<ReplaceDictionary> Rep { get; set; } = default!;

        readonly public GetItemRequest request = new() { ObjID = new OBJ_ID(), LSortOrder = 0, BFlagDirection = 1, CountData = 100, SkipItems = 0 };

        public Dictionary<int, string> ThList = new();

        public bool IsPageLoad = true;

        readonly public List<HintItem> HintItems = new();

        public Func<Task>? RefreshData;

        public TItem FiltrModel = new();

        public string? PlaceHolder { get; set; }

        public async Task OnInitFiltr(Func<Task> refreshData, string placeHolder)
        {
            RefreshData = refreshData;
            PlaceHolder = placeHolder;
            await LoadLastRequest();
            IsPageLoad = false;
        }
        public async Task OnInitFiltr(string placeHolder)
        {
            PlaceHolder = placeHolder;
            await LoadLastRequest();
            IsPageLoad = false;
        }

        public List<FiltrItem> FiltrItems
        {
            get
            {
                List<FiltrItem> response = new();
                response.AddRange(FiltrModel.CreateListFiltrItemFromFiltrModel());
                return response;
            }
        }

        public string FiltrItemsToString
        {
            get
            {
                List<string> _filtr = new();
                if (FiltrItems.Count > 0)
                {
                    foreach (var item in FiltrItems)
                    {
                        _filtr.Add($"{HintItems?.FirstOrDefault(x => x.Key == item.Key)?.Name} {Rep[item.Operation.ToString().ToUpper()]} - {item.Value?.Value}");
                    }
                }
                return string.Join("; ", _filtr);
            }
        }

        public async Task AddItemFiltr(List<FiltrItem> items)
        {
            if (FiltrModel == null)
                FiltrModel = new();
            foreach (var item in items)
            {
                FiltrModel.AddFiltrItemToFiltr(item);
            }
            await CallRefreshData();
        }
        public async Task RemoveItemsFiltr(List<FiltrItem>? items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    FiltrModel.RemoveFiltrItemFromFiltr(item);
                }
                await CallRefreshData();
            }
        }

        public async Task CallRefreshData()
        {
            request.BstrFilter = FiltrModel.PtoroToBase64();
            if (RefreshData != null)
                await RefreshData.Invoke();
        }

        protected async Task LoadLastRequest()
        {
            if (_User != null && _localStorage != null && !string.IsNullOrEmpty(PlaceHolder))
            {
                var userName = await _User.GetName() ?? "";

                var LastRequest = await _localStorage.FiltrGetLastRequest(userName, PlaceHolder);

                if (LastRequest.LastRequest?.Count > 0)
                {
                    await AddItemFiltr(LastRequest.LastRequest);
                }
            }
        }

    }
}
