using System.Text.Json;
using BlazorLibrary.GlobalEnums;
using BlazorLibrary.Models;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace BlazorLibrary.ServiceColection
{
    public class LocalStorage
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorage(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<DateTime?> GetLastActiveDateAsync()
        {
            try
            {
                var s = await _jsRuntime.InvokeAsync<DateTime>("localStorage.getItem", CookieName.LastActiveDate);
                return s;
            }
            catch
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CookieName.LastActiveDate);
                return null;
            }
        }

        public async Task SetLastActiveDateAsync(DateTime dateTime)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieName.LastActiveDate, dateTime);
        }

        public async Task<string?> GetTokenAsync()
        {
            var s = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CookieName.Token);
            return s;
        }

        public async Task SetTokenAsync(string? token = null)
        {
            if (token == null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CookieName.Token);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieName.Token, token);
            }
        }

        public async Task<int?> GetSubSystemIdAsync()
        {
            var s = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CookieName.SubsystemID);

            int.TryParse(s, out var subSystemId);

            if (subSystemId == 0)
                subSystemId = SubsystemType.SUBSYST_ASO;

            return subSystemId;
        }

        public async Task SetSubSystemIdAsync(int? subSystemId = null)
        {
            if (subSystemId == null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CookieName.SubsystemID);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieName.SubsystemID, subSystemId);
            }
        }

        public async Task RemoveAllAsync()
        {
            await SetTokenAsync();
            await SetSubSystemIdAsync();
        }

        public async Task<string?> GetLastUserName()
        {
            var userName = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CookieName.LastUserName);
            return userName;
        }

        public async Task SetLastUserName(string userName)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieName.LastUserName, userName);
        }


        public async Task<List<OBJ_ID>> GetCurNotifySessID()
        {
            var CurNotifySessID = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CookieName.CurNotifySessID);
            List<OBJ_ID> response = new();
            if (!string.IsNullOrEmpty(CurNotifySessID))
            {
                try
                {
                    response = JsonSerializer.Deserialize<List<OBJ_ID>>(CurNotifySessID) ?? new();

                }
                catch (Exception ex)
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CookieName.CurNotifySessID);
                    Console.WriteLine(ex.Message);
                }
            }
            return response;
        }

        public async Task SetCurNotifySessID(IEnumerable<OBJ_ID> CurNotifySessIDList)
        {
            var list = await GetCurNotifySessID();
            list.RemoveAll(x => CurNotifySessIDList.Any(c => c.SubsystemID == x.SubsystemID));
            list.AddRange(CurNotifySessIDList);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieName.CurNotifySessID, JsonSerializer.Serialize(list));
        }

        public async Task<FiltrRequestItem?> FiltrSaveLastRequest(string userName, string filtrName, List<FiltrItem>? items)
        {
            FiltrCookieItem? cookieItem = null;
            try
            {
                var base64 = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}");
                List<FiltrCookieItem> listItem = new();
                if (!string.IsNullOrEmpty(base64))
                {
                    listItem = JsonSerializer.Deserialize<List<FiltrCookieItem>>(Convert.FromBase64String(base64)) ?? new();
                }

                cookieItem = listItem.FirstOrDefault(x => x.UserName == userName);
                if (cookieItem == null)
                {
                    cookieItem = new(userName);
                    listItem.Add(cookieItem);
                }

                if (cookieItem.Filters == null)
                {
                    cookieItem.Filters = new();
                }

                cookieItem.Filters.LastRequest = items;

                if (items != null && items.Count > 0 && !cookieItem.Filters.HistoryRequest.Any(x => items.SequenceEqual(x)))
                {
                    if (cookieItem.Filters.HistoryRequest.Count >= 5)
                        cookieItem.Filters.HistoryRequest.RemoveRange(4, cookieItem.Filters.HistoryRequest.Count - 4);
                    cookieItem.Filters.HistoryRequest.Insert(0, items);
                }
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}", Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(listItem)));
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}");
                Console.WriteLine(ex.Message);
            }
            return cookieItem?.Filters;
        }

        public async Task<FiltrRequestItem> FiltrGetLastRequest(string userName, string filtrName)
        {
            try
            {
                var base64 = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}");
                List<FiltrCookieItem> listItem = new();
                if (!string.IsNullOrEmpty(base64))
                {
                    listItem = JsonSerializer.Deserialize<List<FiltrCookieItem>>(Convert.FromBase64String(base64)) ?? new();
                }
                FiltrCookieItem cookieItem = listItem.FirstOrDefault(x => x.UserName == userName) ?? new(userName);
                return cookieItem.Filters ?? new();
            }
            catch
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}");
                return new();
            }

        }

        public async Task FiltrClearLastRequest(string userName, string filtrName)
        {
            var base64 = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}");
            List<FiltrCookieItem> listItem = new();
            if (!string.IsNullOrEmpty(base64))
            {
                listItem = JsonSerializer.Deserialize<List<FiltrCookieItem>>(Convert.FromBase64String(base64)) ?? new();
            }
            var cookieItem = listItem.FirstOrDefault(x => x.UserName == userName);

            if (cookieItem != null)
            {
                cookieItem.Filters.HistoryRequest.Clear();
                cookieItem.Filters.LastRequest = null;

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"{CookieName.FiltrRequest}.{filtrName.ToLower()}", Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(listItem)));
            }
        }

        public async Task<SndSetting?> GetSndSettingEx(SoundSettingsType type)
        {
            string? key = null;
            if (type == SoundSettingsType.RepSoundSettingType)
            {
                key = CookieName.SettingSoundEx;
            }
            else if (type == SoundSettingsType.RecSoundSettingType)
            {
                key = CookieName.SettingRecEx;
            }
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    var byteArray = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
                    if (byteArray != null)
                    {
                        GetSndSettingExResponse settings = GetSndSettingExResponse.Parser.ParseFrom(ByteString.FromBase64(byteArray));
                        if (settings.Info?.Length > 0)
                        {
                            SndSetting response = new(settings.Info.ToByteArray());
                            response.Interfece = string.IsNullOrEmpty(settings.Interface) ? null : settings.Interface;
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                Console.WriteLine($"Error GetSndSettingEx, message {ex.Message}");
            }
            return null;
        }

        public async Task SaveSndSettingEx(SoundSettingsType type, SndSetting settings)
        {
            string? key = null;
            if (type == SoundSettingsType.RepSoundSettingType)
            {
                key = CookieName.SettingSoundEx;
            }
            else if (type == SoundSettingsType.RecSoundSettingType)
            {
                key = CookieName.SettingRecEx;
            }
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    GetSndSettingExResponse request = new() { Interface = settings.Interfece ?? string.Empty, Info = UnsafeByteOperations.UnsafeWrap(settings.ToBytes()) };
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, request.ToByteString().ToBase64());
                }
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                Console.WriteLine($"Error SaveSndSettingEx, message {ex.Message}");
            }
        }
    }
}
