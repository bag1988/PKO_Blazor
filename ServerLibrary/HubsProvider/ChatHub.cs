
using System;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using ServerLibrary.Extensions;
using SharedLibrary;
using SharedLibrary.Models;
using WebPush;
using System.Text.Json;
using SharedLibrary.Extensions;
using Dapr.Client;
using SharedLibrary.Utilities;

namespace ServerLibrary.HubsProvider
{
    [AllowAnonymous]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly object locker = new();
        private readonly DaprClient _daprClient;
        private readonly static ConnectionMapping<string> _connections = new();

        public ChatHub(ILogger<ChatHub> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        List<CallLoad> LoadItems { get; set; } = new();

        class CallLoad
        {
            public string SendToUrl { get; set; }
            public string NameRoom { get; set; }
            public string HubUrl { get; set; }
            public TypeConnect TypeConn { get; set; }
            public IEnumerable<ConnectItem> OtherUrl { get; set; }
            public Guid Key { get; } = Guid.NewGuid();

            public bool IsLoad { get; set; } = true;

            public CallLoad(string sendToUrl, string nameRoom, string hubUrl, TypeConnect typeConn, IEnumerable<ConnectItem> otherUrl)
            {
                SendToUrl = sendToUrl;
                NameRoom = nameRoom;
                HubUrl = hubUrl;
                TypeConn = typeConn;
                OtherUrl = otherUrl;
            }
        }


        private async Task SendPushAsync(string inUrl)
        {
            try
            {
                bool IsReplaceFile = false;

                List<VapidDetails>? listKeys = await _daprClient.GetStateAsync<List<VapidDetails>?>(StateNameConst.StateStore, StateNameConst.VapidDetails);

                if (listKeys == null || listKeys.Count == 0)
                    return;

                var subscription = await _daprClient.GetStateAsync<List<NotificationSubscription>?>(StateNameConst.StateStore, StateNameConst.PushSetting);

                if (subscription == null || !subscription.Any())
                    return;

                if (subscription.Any(x => x.CountError > 5))
                {
                    subscription.RemoveAll(x => x.CountError > 5);
                    IsReplaceFile = true;
                }

                var payload = JsonSerializer.Serialize(new
                {
                    url = string.Empty,
                    message = $"Входящий вызов от {inUrl}"
                });

                foreach (var item in subscription)
                {
                    if (string.IsNullOrEmpty(item.IpClient))
                        continue;

                    var vapidDetailsKey = listKeys.FirstOrDefault(x => x.Subject == item.IpClient);

                    if (vapidDetailsKey == null || string.IsNullOrEmpty(vapidDetailsKey.PublicKey) || string.IsNullOrEmpty(vapidDetailsKey.PrivateKey))
                        continue;

                    var pushSubscription = new PushSubscription(item.Url, item.P256dh, item.Auth);

                    var vapidDetails = new VapidDetails(item.IpClient, vapidDetailsKey.PublicKey, vapidDetailsKey.PrivateKey);
                    var webPushClient = new WebPushClient();
                    try
                    {
                        await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                        await webPushClient.DisposeAsync();
                    }
                    catch
                    {
                        item.CountError++;
                        IsReplaceFile = true;
                    }
                }

                if (IsReplaceFile)
                {
                    await _daprClient.SaveStateAsync(StateNameConst.StateStore, StateNameConst.PushSetting, subscription);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Отправляем запрос на подключение к группе
        /// </summary>
        /// <param name="sendToUrl"></param>
        /// <param name="nameRoom"></param>
        /// <param name="hubUrl"></param>
        /// <param name="typeConn"></param>
        /// <param name="otherUrl"></param>
        /// <returns></returns>
        public async Task JoinToChatRoom(string sendToUrl, string nameRoom, string hubUrl, TypeConnect typeConn, IEnumerable<ConnectItem> otherUrl)
        {
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            await SendPushAsync(hubUrl);
            if (r?.Count() > 0)
            {
                await Clients.Clients(r).SendCoreAsync("JoinToChatRoom", new object[] { nameRoom, hubUrl, typeConn, otherUrl });
            }
            else if (!LoadItems.Any(x => x.SendToUrl == sendToUrl && x.NameRoom == nameRoom && x.HubUrl == hubUrl))
            {
                CallLoad newItem = new(sendToUrl, nameRoom, hubUrl, typeConn, otherUrl);
                LoadItems.Add(newItem);
                _ = AddLoadItems(sendToUrl, newItem.Key);
            }
            
        }

        async Task AddLoadItems(string sendToUrl, Guid Key)
        {
            int i = 300;
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            while (i > 0)
            {
                await Task.Delay(100);
                r = _connections.GetConnectionsIdForKey(sendToUrl);
                if (r.Count() > 0)
                {
                    break;
                }
                i--;
            }

            var item = LoadItems.FirstOrDefault(x => x.Key == Key);
            if (item != null && item.IsLoad)
            {
                item.IsLoad = false;
                await JoinToChatRoom(item.SendToUrl, item.NameRoom, item.HubUrl, item.TypeConn, item.OtherUrl);
                LoadItems.Remove(item);
            }
        }

        public async Task ConnectionP2P(string sendToUrl, string nameRoom, string offer, string inUrl)
        {
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            if (r?.Count() > 0)
            {
                await Clients.Clients(r).SendCoreAsync("ConnectionP2P", new object[] { nameRoom, offer, inUrl });
            }
        }


        public async Task CreateOrAddToChatRoom(string nameRoom)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, nameRoom);

            var url = _connections.GetUrlForConnectionId(Context.ConnectionId);
            if (!string.IsNullOrEmpty(url))
            {
                await Clients.OthersInGroup(nameRoom).SendCoreAsync("SendMessageGroup", new[] { url, "Присоединился к чату" });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="nameRoom"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task SendAnswer(string sendToUrl, string answer, string nameRoom, string url)
        {
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            if (r?.Count() > 0)
            {
                await Clients.Clients(r).SendCoreAsync("SendAnswer", new[] { answer, nameRoom, url });
            }
        }

        public async Task SendCandidate(string sendToUrl, string candidate, string url)
        {
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            if (r?.Count() > 0)
            {
                await Clients.Clients(r).SendCoreAsync("SendCandidate", new[] { candidate, url });
            }
        }


        public async Task CloseCall(string url, string nameRoom)
        {
            await Clients.OthersInGroup(nameRoom).SendCoreAsync("CloseCall", new[] { url, nameRoom });
        }

        public async Task RemoveGroup(string nameRoom)
        {
            LoadItems.RemoveAll(x => x.NameRoom == nameRoom);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, nameRoom);
        }

        public async Task CancelOutCall(string sendToUrl, string hubUrl, string? nameRoom = null)
        {
            if (!string.IsNullOrEmpty(nameRoom))
            {
                await Clients.OthersInGroup(nameRoom).SendCoreAsync("CancelOutCall", new[] { hubUrl });
            }
            else
            {
                var r = _connections.GetConnectionsIdForKey(sendToUrl);
                if (r?.Count() > 0)
                {
                    await Clients.Clients(r).SendCoreAsync("CancelOutCall", new[] { hubUrl });
                }
            }
        }

        /// <summary>
        /// Отменяем входящий вызов
        /// </summary>
        /// <param name="sendToUrl">кто отправил запрос на подключение</param>
        /// <param name="localUrl">кто отменил</param>
        /// <returns></returns>
        public async Task CancelCall(string sendToUrl, string localUrl)
        {
            var r = _connections.GetConnectionsIdForKey(sendToUrl);
            if (r?.Count() > 0)
            {
                await Clients.Clients(r).SendCoreAsync("CancelCall", new[] { localUrl });
            }
        }

        public async Task SendMessageGroup(string nameRoom, string url, string message)
        {
            await Clients.Group(nameRoom).SendCoreAsync("SendMessageGroup", new[] { url, message });
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                StringValues result = new();
                var context = Context.GetHttpContext();
                context?.Request.Headers.TryGetValue("Origin", out result);
                var name = result.FirstOrDefault();
                var isAuthorize = context?.User.Identity?.IsAuthenticated ?? false;

                if (!string.IsNullOrEmpty(name))
                    _connections.Add(IpAddressUtilities.GetAuthority(name), Context.ConnectionId, isAuthorize);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(OnConnectedAsync)}");
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                StringValues result = new();
                Context.GetHttpContext()?.Request.Headers.TryGetValue("Origin", out result);
                var name = result.FirstOrDefault();
                if (!string.IsNullOrEmpty(name))
                    _connections.Remove(IpAddressUtilities.GetAuthority(name), Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(OnDisconnectedAsync)}");
            }
            return base.OnDisconnectedAsync(exception);
        }

    }
}
