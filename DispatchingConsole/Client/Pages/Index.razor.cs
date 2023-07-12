using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BlazorLibrary.Shared;
using DispatchingConsole.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using SharedLibrary;
using SharedLibrary.Extensions;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;

namespace DispatchingConsole.Client.Pages
{
    partial class Index : IAsyncDisposable
    {
        [CascadingParameter]
        public MainLayout? Layout { get; set; }

        private IJSObjectReference? _jsModuleRtc;

        List<ActiveConnect> ActiveConnect
        {
            get
            {
                return Layout?.ActiveConnectItems ?? new();
            }
        }

        List<CGetRegList>? SelectList
        {
            get
            {
                return Layout?.SelectList;
            }
            set
            {
                if (Layout != null)
                {
                    Layout.SelectList = value;
                }
            }
        }
        ActiveConnect? _activeConnect
        {
            get
            {
                if (ActiveConnect.Any(x => x.TypeCalling == TypeCall.Out && x.Items.Any(i => SelectList?.Any(s => IpAddressUtilities.CompareForHost(i.Url, s.CCURegistrList?.CUUNC)) ?? false)))
                {
                    return ActiveConnect.First(x => x.TypeCalling == TypeCall.Out && x.Items.Any(i => SelectList?.Any(s => IpAddressUtilities.CompareForHost(i.Url, s.CCURegistrList?.CUUNC)) ?? false));
                }
                else if (ActiveConnect.Any(x => x.TypeCalling == TypeCall.In && IpAddressUtilities.CompareForAuthority(x.HubUrl, SelectList?.LastOrDefault()?.CCURegistrList?.CUUNC)))
                {
                    return ActiveConnect.First(x => x.TypeCalling == TypeCall.In && IpAddressUtilities.CompareForAuthority(x.HubUrl, SelectList?.LastOrDefault()?.CCURegistrList?.CUUNC));
                }
                return null;
            }
        }


        private DotNetObjectReference<Index>? _jsThis;

        readonly Dictionary<string, HubConnection> _hubs = new();

        readonly List<MessageItem> _messages = new();

        string TempMessage = string.Empty;

        struct MessageItem
        {
            public string Url { get; set; }

            public string Message { get; set; }

            public DateTime Date { get; set; }
            public MessageItem(string url, string message)
            {
                Url = url;
                Message = message;
                Date = DateTime.Now;
            }
        }

        ElementReference? remoteVideoArray = null;

        protected override async Task OnInitializedAsync()
        {
            _jsModuleRtc = await JSRuntime.InvokeAsync<IJSObjectReference>("import", $"/js/WebRtcService.js?v={DateTime.Now.Second}");

            _jsThis = DotNetObjectReference.Create(this);
            _ = _jsModuleRtc?.InvokeVoidAsync("initialize", _jsThis, Layout?.localVideo, remoteVideoArray);
            _ = _HubContext.SubscribeAsync(this);
        }

        [Description(DaprMessage.PubSubName)]
        public async Task JoinToChatRoom(string nameRoom, string inUrl, TypeConnect typeConn, IEnumerable<ConnectItem> otherUrl)
        {
            try
            {
                _messages.Clear();
                if (!ActiveConnect.Any(x => x.NameRoom == nameRoom))
                {
                    var newItem = new ActiveConnect(nameRoom, inUrl, TypeCall.In, typeConn);
                    newItem.Items.AddRange(otherUrl);

                    ActiveConnect.Add(newItem);

                    if (Layout != null)
                        await Layout.AddAndSelect(nameRoom, inUrl);

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// абонент завершает вызов
        /// </summary>
        /// <param name="inUrl">абонент завершивший вызов</param>
        /// <param name="nameRoom"></param>
        /// <returns></returns>
        [Description(DaprMessage.PubSubName)]
        public async Task CloseCall(string inUrl, string nameRoom)
        {
            try
            {
                if (_jsModuleRtc != null)
                    await CloseP2P(inUrl);

                _messages.Add(new(inUrl, $"Завершил вызов"));

                var conn = ActiveConnect.FirstOrDefault(x => x.NameRoom == nameRoom);
                if (conn != null)
                {
                    conn.Items.RemoveAll(x => IpAddressUtilities.CompareForAuthority(x.Url, inUrl));

                    if (conn.Items.Count == 0)
                    {
                        conn.State = StateCall.Out;
                        await RemoveHubs(conn.HubUrl);
                        ActiveConnect.Remove(conn);
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public Task SendMessageGroup(string inUrl, string message)
        {
            try
            {
                _messages.Add(new(inUrl, message));
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SendAnswer(string answer, string nameRoom, string url)
        {
            try
            {
                var conn = ActiveConnect.FirstOrDefault(y => y.NameRoom == nameRoom);
                if (conn != null)
                {
                    conn.State = StateCall.Connecting;
                    if (conn.TypeCalling == TypeCall.Out)
                        await RemoveHubs(url);
                    var con = conn.Items.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.Url, url));
                    if (con != null)
                    {
                        con.Answer = answer;
                        con.State = StateCall.Connecting;

                        if (_jsModuleRtc != null && !string.IsNullOrEmpty(answer) && !string.IsNullOrEmpty(con.Offer))
                        {
                            await _jsModuleRtc.InvokeVoidAsync("processAnswer", answer, IpAddressUtilities.GetAuthority(url));
                        }
                        else
                            con.Offer = string.Empty;
                    }

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        [Description(DaprMessage.PubSubName)]
        public async Task ConnectionP2P(string nameRoom, string offer, string inUrl)
        {
            try
            {
                var conn = ActiveConnect.FirstOrDefault(y => y.NameRoom == nameRoom);
                if (conn != null)
                {
                    var con = conn.Items.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.Url, inUrl));
                    if (con != null)
                    {
                        con.State = StateCall.Calling;
                        if (_jsModuleRtc != null)
                        {
                            await _jsModuleRtc.InvokeVoidAsync("processOffer", offer, conn.NameRoom, IpAddressUtilities.GetAuthority(inUrl), conn.TypeConn == TypeConnect.Video);
                        }
                        else
                            con.Offer = string.Empty;
                    }
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public async Task SendCandidate(string candidate, string url)
        {
            try
            {
                if (_jsModuleRtc != null && ActiveConnect.Any(x => x.Items.Any(i => IpAddressUtilities.CompareForAuthority(i.Url, url))))
                    await _jsModuleRtc.InvokeVoidAsync("processCandidate", candidate, IpAddressUtilities.GetAuthority(url));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// абонент отменил вызов
        /// </summary>
        /// <param name="inUrl"></param>
        /// <returns></returns>
        [Description(DaprMessage.PubSubName)]
        public async Task CancelCall(string inUrl)
        {
            try
            {
                _messages.Add(new(inUrl, $"Отменил вызов"));

                _ = CloseP2P(inUrl);

                var conn = ActiveConnect.FirstOrDefault(x => x.Items.Any(i => IpAddressUtilities.CompareForAuthority(i.Url, inUrl)));

                if (_hubs.ContainsKey(IpAddressUtilities.GetAuthority(inUrl)))
                {
                    await RemoveHubs(inUrl);
                }
                if (conn != null)
                {
                    conn.Items.RemoveAll(x => IpAddressUtilities.CompareForAuthority(x.Url, inUrl));
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Description(DaprMessage.PubSubName)]
        public Task CancelOutCall(string hubUrl)
        {
            try
            {
                _messages.Add(new(hubUrl, $"Отменил вызов"));
                var conn = ActiveConnect.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.HubUrl, hubUrl));
                if (conn != null)
                {
                    lock (conn)
                    {
                        if (_hubs.ContainsKey(IpAddressUtilities.GetAuthority(hubUrl)))
                        {
                            _ = RemoveHubs(hubUrl);
                        }

                        foreach (var forUrl in conn.Items)
                        {
                            _ = CloseP2P(forUrl.Url);
                        }
                        conn.State = StateCall.Out;
                        ActiveConnect.Remove(conn);
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }


        /// <summary>
        /// Создаем исходящий вызов
        /// </summary>
        /// <param name="typeConnect"></param>
        /// <returns></returns>
        private async Task StartConnect(TypeConnect typeConnect)
        {
            if (SelectList == null)
                return;

            var listConnect = new List<string>(SelectList.Select(x => $"https://{IpAddressUtilities.ParseEndPoint(x.CCURegistrList?.CUUNC)}:2291") ?? new List<string>());

            //var listConnect = new List<string>(SelectList.Select(x => x.CCURegistrList?.CUUNC ?? string.Empty) ?? new List<string>());

            _messages.Clear();
            _messages.Add(new(MyNavigationManager.BaseUri, $"Установка соединения с {string.Join("; ", SelectList.Select(x => x.CCURegistrList?.CUUNC))}"));

            //string _nameRoom = Guid.NewGuid().ToString();
            string _nameRoom = listConnect.Count > 1 ? $"Группа ({listConnect.Count})" : SelectList.First().CCURegistrList?.CUName ?? MyNavigationManager.BaseUri;
            //создаем группу
            await _HubContext.SendCoreAsync("CreateOrAddToChatRoom", new[] { _nameRoom });

            //Создаем подключения к Hubs
            if (await CreateHub(listConnect))
            {
                var newConnect = new ActiveConnect(_nameRoom, MyNavigationManager.BaseUri, TypeCall.Out, typeConnect);

                newConnect.Items.AddRange(listConnect.Select(x => new ConnectItem(x)));

                if (typeConnect != TypeConnect.Message && _jsModuleRtc != null)
                {
                    foreach (var forUrl in listConnect)
                    {
                        //создаем P2P и получаем "предложение" для подключения
                        var offer = await _jsModuleRtc.InvokeAsync<string?>("callAction", IpAddressUtilities.GetAuthority(forUrl), typeConnect == TypeConnect.Video);

                        var elem = newConnect.Items.First(x => x.Url == forUrl);

                        if (string.IsNullOrEmpty(offer))
                        {
                            newConnect.Items.Remove(elem);
                            _messages.Add(new(MyNavigationManager.BaseUri, $"Ошибка создания подключения с {forUrl}"));
                            await RemoveHubs(forUrl);
                            _ = CloseP2P(forUrl);
                        }
                        else
                        {
                            elem.Offer = offer;
                        }
                    }
                }

                if (newConnect.Items.Count > 0)
                {
                    /*отправляем запрос на присоединение к группе
                 <param name="inUrl">Кто отправляет запрос</param>
                 <param name="offer">запрос на P2P</param>*/
                    foreach (var forUrl in listConnect)
                    {
                        var connect = newConnect.Items.FirstOrDefault(x => x.Url == forUrl);
                        var url = IpAddressUtilities.GetAuthority(forUrl);
                        if (connect != null && _hubs.ContainsKey(url))
                        {
                            await _hubs[url].SendCoreAsync("JoinToChatRoom", new object[] { url, _nameRoom, MyNavigationManager.BaseUri, typeConnect, newConnect.Items.Select(x => new ConnectItem(x.Url == forUrl ? MyNavigationManager.BaseUri : x.Url ?? string.Empty, x.Url == forUrl ? x.Offer ?? string.Empty : string.Empty, typeConnect)) });
                            connect.State = StateCall.Calling;
                        }
                    }
                }
                else
                {
                    _messages.Add(new(MyNavigationManager.BaseUri, $"Отсутствуют адреса для подключения!"));
                }

                ActiveConnect.Add(newConnect);
                newConnect.State = StateCall.Calling;
                foreach (var forUrl in listConnect)
                {
                    _ = LoadAnswerForUrl(newConnect.Key, forUrl);
                }
            }
        }


        async Task LoadAnswerForUrl(Guid key, string urlLoad)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));

            var conn = ActiveConnect.FirstOrDefault(x => x.Key == key);

            if (conn != null)
            {
                var state = conn.Items.FirstOrDefault(i => i.Url == urlLoad);

                if (state == null || state.State != StateCall.Connecting)
                {
                    await RemoveHubs(urlLoad);
                    if (state != null)
                    {
                        if (state.State != StateCall.Connecting)
                            _messages.Add(new(MyNavigationManager.BaseUri, $"Истекло время ожидания подключения с {urlLoad}"));
                        conn?.Items.Remove(state);
                    }
                    _ = CloseP2P(urlLoad);

                    StateHasChanged();
                }
            }


        }

        /// <summary>
        /// Отменяем исходящий вызов
        /// </summary>
        /// <returns></returns>
        async Task CancelOutCall()
        {
            if (_activeConnect == null) return;

            _activeConnect.State = StateCall.Out;
            StateHasChanged();
            if (_activeConnect.Items.Count > 0)
            {
                //отправляем всем кто подключился или еще в процессе
                foreach (var forUrl in _activeConnect.Items)
                {
                    //закрываем все соединения которые были созданы в callAction
                    _ = CloseP2P(forUrl.Url);

                    //если идет дозвон удаляем подключение
                    var url = IpAddressUtilities.GetAuthority(forUrl.Url);
                    if (!string.IsNullOrEmpty(url) && _hubs.ContainsKey(url))
                    {
                        await _hubs[url].SendCoreAsync("CancelOutCall", new object[] { url, MyNavigationManager.BaseUri, string.Empty });
                        _ = RemoveHubs(forUrl.Url);
                    }
                }
            }

            _ = _HubContext.SendCoreAsync("CancelOutCall", new[] { string.Empty, MyNavigationManager.BaseUri, _activeConnect.NameRoom });
            _ = _HubContext.SendCoreAsync("RemoveGroup", new[] { _activeConnect.NameRoom });

            ActiveConnect.RemoveAll(x => x.State == StateCall.Out);

            _messages.Add(new(MyNavigationManager.BaseUri, $"Вызов отменен"));

        }


        /// <summary>
        /// Генерируем ответ
        /// </summary>
        /// <returns></returns>
        private async Task CreateAnswer(TypeConnect typeConnect)
        {
            if (_activeConnect?.TypeCalling == TypeCall.In && _activeConnect.State == StateCall.Create)
            {
                var _conn = ActiveConnect.FirstOrDefault(x => x.NameRoom == _activeConnect.NameRoom && x.HubUrl == _activeConnect.HubUrl);

                if (_conn == null) return;

                _conn.State = StateCall.Connecting;
                StateHasChanged();
                _conn.TypeConn = typeConnect;
                _messages.Add(new(MyNavigationManager.BaseUri, $"Установка соединения с {_conn.HubUrl}"));

                //SelectItems = ActiveConnect.Url;

                //создаем обратное подключение
                await CreateHub(new() { _conn.HubUrl });

                var hubUri = IpAddressUtilities.GetAuthority(_conn.HubUrl);

                if (_hubs.ContainsKey(hubUri))
                {
                    await _hubs[hubUri].SendCoreAsync("CreateOrAddToChatRoom", new[] { _conn.NameRoom });

                    //Далее создаем ответ на P2P при необходимости
                    var firstElem = _conn.Items.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.Url, _conn.HubUrl));
                    if (firstElem != null)
                    {
                        if (_jsModuleRtc != null && _conn.TypeConn != TypeConnect.Message && !string.IsNullOrEmpty(firstElem.Offer))
                        {
                            var elemOffer = _conn.Items.First(x => IpAddressUtilities.CompareForAuthority(x.Url, _conn.HubUrl) && !string.IsNullOrEmpty(x.Offer));

                            elemOffer.State = StateCall.Calling;
                            await _jsModuleRtc.InvokeVoidAsync("processOffer", elemOffer.Offer, _conn.NameRoom, hubUri, _conn.TypeConn == TypeConnect.Video);


                            if (_conn.Items.Count > 0)
                            {
                                foreach (var item in _conn.Items.Where(x => string.IsNullOrEmpty(x.Offer)))
                                {
                                    if (!string.IsNullOrEmpty(item.Url))
                                    {
                                        var offer = await _jsModuleRtc.InvokeAsync<string?>("callAction", IpAddressUtilities.GetAuthority(item.Url), typeConnect == TypeConnect.Video);

                                        if (!string.IsNullOrEmpty(offer))
                                        {
                                            _ = _hubs[hubUri].SendCoreAsync("ConnectionP2P", new[] { IpAddressUtilities.GetAuthority(item.Url), _conn.NameRoom, offer, MyNavigationManager.BaseUri });
                                            item.Offer = offer;
                                            item.State = StateCall.Calling;
                                        }
                                        else
                                            _ = CloseP2P(item.Url);

                                    }

                                }
                            }

                        }
                        else
                        {
                            firstElem.TypeConn = TypeConnect.Message;

                            await _hubs[hubUri].SendCoreAsync("SendAnswer", new[] { hubUri, string.Empty, _conn.NameRoom, MyNavigationManager.BaseUri });

                        }
                    }
                }
                else
                {
                    _messages.Add(new(MyNavigationManager.BaseUri, $"Ошибка установки соединения с {_conn.HubUrl}"));
                    ActiveConnect.Remove(_conn);
                }
            }
        }


        public async Task SendMessage()
        {
            if (_activeConnect != null && !string.IsNullOrEmpty(TempMessage))
            {
                if (_activeConnect.TypeCalling == TypeCall.In)
                    await SendAll("SendMessageGroup", new[] { _activeConnect.NameRoom, MyNavigationManager.BaseUri, TempMessage });
                else
                    await _HubContext.SendCoreAsync("SendMessageGroup", new[] { _activeConnect.NameRoom, MyNavigationManager.BaseUri, TempMessage });
            }
            TempMessage = string.Empty;
        }


        /// <summary>
        /// Отменяем входящий вызов
        /// </summary>
        async Task CancelCall()
        {
            if (_activeConnect?.TypeCalling == TypeCall.In)
            {
                if (_HubContext != null)
                {
                    await _HubContext.SendCoreAsync("CancelCall", new[] { IpAddressUtilities.GetAuthority(_activeConnect.HubUrl), MyNavigationManager.BaseUri });
                }
                _messages.Add(new(MyNavigationManager.BaseUri, $"Входящий вызов {_activeConnect.HubUrl} отменен"));
                _activeConnect.State = StateCall.Out;

                ActiveConnect.Remove(_activeConnect);
            }
        }



        /// <summary>
        /// Завершаем вызов, выходим из группы
        /// </summary>
        /// <returns></returns>
        private async Task CloseCallAction()
        {
            _messages.Add(new(MyNavigationManager.BaseUri, $"Вызов с {_activeConnect?.HubUrl ?? "Нет данных"} завершен"));

            if (_jsModuleRtc == null) throw new InvalidOperationException();

            if (_activeConnect != null)
            {
                foreach (var forUrl in _activeConnect.Items)
                {
                    _ = CloseP2P(forUrl.Url);
                }
                _ = CloseP2P(_activeConnect.HubUrl);

                await _HubContext.SendCoreAsync("CloseCall", new[] { MyNavigationManager.BaseUri, _activeConnect.NameRoom });
                if (_hubs.ContainsKey(IpAddressUtilities.GetAuthority(_activeConnect.HubUrl)))
                {
                    await SendAll("CloseCall", new[] { MyNavigationManager.BaseUri, _activeConnect.NameRoom });
                    _ = RemoveHubs(_activeConnect.HubUrl);
                }

                ActiveConnect.Remove(_activeConnect);
            }
        }

        private async Task SendAll(string methodName, object?[] args, string? urlSend = null)
        {
            if (_hubs.Count > 0)
            {
                if (string.IsNullOrEmpty(urlSend))
                {
                    foreach (var hub in _hubs.Values)
                    {
                        try
                        {
                            await hub.SendCoreAsync(methodName, args);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                else if (_hubs.ContainsKey(IpAddressUtilities.GetAuthority(urlSend)))
                {
                    await _hubs[IpAddressUtilities.GetAuthority(urlSend)].SendCoreAsync(methodName, args);
                }
            }
        }

        private async Task<bool> CreateHub(List<string> hubsUrl)
        {
            foreach (var url in hubsUrl)
            {
                try
                {
                    var uri = IpAddressUtilities.GetAuthority(url);
                    if (IpAddressUtilities.CompareForAuthority(uri, MyNavigationManager.BaseUri))
                        continue;
                    if (_hubs.ContainsKey(uri))
                        continue;

                    var absoluteUri = Path.Combine(url, "communicationhub");
                    var hub = new HubConnectionBuilder()
                        .WithUrl(absoluteUri)
                        .Build();

                    hub.On<string, string>("SendCandidate", SendCandidate);
                    hub.On<string, string, string>("SendAnswer", SendAnswer);
                    hub.On<string, string, string>("ConnectionP2P", ConnectionP2P);
                    hub.On<string>("CancelCall", CancelCall);
                    hub.On<string>("CancelOutCall", CancelOutCall);
                    hub.On<string, string>("CloseCall", CloseCall);
                    hub.On<string, string>("SendMessageGroup", SendMessageGroup);

                    await hub.StartAsync();
                    _hubs.Add(uri, hub);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    _messages.Add(new(url, "Ошибка подключения"));
                }
            }
            return true;
        }

        /// <summary>
        /// Сгенерирован ответ
        /// </summary>
        /// <param name="nameroom"></param>
        /// <param name="answer"></param>
        /// <param name="forUrl">ip Authority => (127.0.0.1:2291)</param>
        /// <returns></returns>
        [JSInvokable]
        public async Task SendAnswerJs(string nameroom, string answer, string forUrl)
        {
            if (string.IsNullOrEmpty(answer))
            {
                _messages.Add(new(MyNavigationManager.BaseUri, $"Ошибка создания подключения P2P!"));
            }
            else
            {
                var conn = ActiveConnect.FirstOrDefault(x => x.Items.Any(i => i.Url?.Contains(forUrl) ?? false));

                var item = conn?.Items.FirstOrDefault(i => i.Url?.Contains(forUrl) ?? false);
                if (item != null)
                {
                    item.State = StateCall.Connecting;
                    StateHasChanged();
                }
                await SendAll("SendAnswer", new[] { forUrl, answer, nameroom, MyNavigationManager.BaseUri }, conn?.HubUrl);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="forUrl">ip Authority => (127.0.0.1:2291)</param>
        /// <returns></returns>
        [JSInvokable]
        public async Task SendCandidateJs(string candidate, string forUrl)
        {
            var conn = ActiveConnect.FirstOrDefault(x => x.Items.Any(i => i.Url?.Contains(forUrl) ?? false));
            if (conn?.TypeCalling == TypeCall.Out)
            {
                await _HubContext.SendCoreAsync("SendCandidate", new[] { forUrl, candidate, MyNavigationManager.BaseUri });
            }
            else
                await SendAll("SendCandidate", new[] { forUrl, candidate, MyNavigationManager.BaseUri }, conn?.HubUrl);
        }


        async Task CloseP2P(string? url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                if (_jsModuleRtc != null)
                {
                    await _jsModuleRtc.InvokeVoidAsync("closePeerConnection", IpAddressUtilities.GetAuthority(url));
                }
            }
        }


        string GetTypeInConnect
        {
            get
            {
                return _activeConnect?.TypeConn switch
                {
                    TypeConnect.Video => "Входящий видеовызов",
                    TypeConnect.Sound => "Входящий вызов",
                    TypeConnect.Message => "Входящее подключение",
                    _ => throw new ArgumentNullException()
                };
            }
        }

        async ValueTask RemoveHubs(string? urlRemove = null)
        {
            var url = IpAddressUtilities.GetAuthority(urlRemove);
            if (_hubs.Count > 0)
            {
                if (string.IsNullOrEmpty(urlRemove))
                {
                    foreach (var hub in _hubs)
                    {
                        await hub.Value.DisposeAsync();
                    }
                    _hubs.Clear();
                }
                else if (_hubs.ContainsKey(url))
                {
                    await _hubs[url].DisposeAsync();
                    _hubs.Remove(url);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await RemoveHubs();
            if (_jsModuleRtc != null)
                await _jsModuleRtc.DisposeAsync();
            if (_jsThis != null)
                await _jsThis.DisposeAsync();

            await _HubContext.DisposeAsync();
        }
    }
}
