using System.Threading.Channels;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RemoteConnectLibrary;
using ServerLibrary.Extensions;
using SharedLibrary;
using SMDataServiceProto.V1;
using static SMSSGsoProto.V1.SMSSGso;
using static SyntezServiceProto.V1.SyntezService;

namespace ServerLibrary.HubsProvider
{
    [AllowAnonymous]
    public class SharedHub : Hub
    {
        readonly SMSSGsoClient _SMSGso;
        readonly SyntezServiceClient _TtsClient;
        readonly string BackupFolder;
        readonly GateServiceProto.V1.GateService.GateServiceClient _SMGate;
        readonly RemoteGateProvider _connectRemote;
        readonly string DirectoryTmp = Path.Combine("wwwroot", "tmp");

        private readonly ILogger<SharedHub> _logger;
        private readonly object locker = new();

        public SharedHub(ILogger<SharedHub> logger, SMSSGsoClient SMSGso, SyntezServiceClient ttsClient, GateServiceProto.V1.GateService.GateServiceClient sMGate, RemoteGateProvider connectRemote, IConfiguration conf)
        {
            _logger = logger;
            _SMSGso = SMSGso;
            _TtsClient = ttsClient;
            _connectRemote = connectRemote;
            _SMGate = sMGate;
            BackupFolder = conf["BackupFolder"] ?? "";
        }

        public async Task SendTopic(string NameTopic, object? Value = null)
        {
            try
            {
                if (Clients is not null)
                {
                    using var activity = this.ActivitySourceForHub()?.StartActivity();
                    activity?.AddTag("Пользователь ID", Context.ConnectionId);
                    activity?.AddTag("Метод", NameTopic);
                    await Clients.All.SendCoreAsync(NameTopic, new[] { Value });
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{NameTopic}");
            }
        }

        public async Task<bool> UploadTmpFile(ChannelReader<byte[]> stream, string FileName)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Временный файл", FileName);
            FileName = Path.Combine(DirectoryTmp, FileName);
            try
            {
                _ = RemoveOldTmpFile();
                if (!Directory.Exists(DirectoryTmp))
                {
                    Directory.CreateDirectory(DirectoryTmp);
                }

                if (System.IO.File.Exists(FileName))
                    return false;
                using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    while (await stream.WaitToReadAsync(Context.ConnectionAborted))
                    {
                        while (stream.TryRead(out var item))
                        {
                            await fs.WriteAsync(item, Context.ConnectionAborted);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(FileName))
                    System.IO.File.Delete(FileName);
                _logger.WriteLogError(ex, $"{nameof(UploadTmpFile)}");
                return false;
            }
        }

        public async Task<int> UploadDataBase(ChannelReader<byte[]> stream, string FileName)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Бэкап", FileName);
            int result = 1;
            try
            {
                if (!Directory.Exists(BackupFolder))
                {
                    Directory.CreateDirectory(BackupFolder);
                }

                if (string.IsNullOrEmpty(FileName))
                {
                    FileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                }

                FileName = Path.Combine(BackupFolder, FileName);

                if (System.IO.File.Exists(FileName))
                    return 2;

                using (var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    while (await stream.WaitToReadAsync(Context.ConnectionAborted))
                    {
                        while (stream.TryRead(out var item))
                        {
                            await fs.WriteAsync(item, Context.ConnectionAborted);
                        }
                    }
                }
                result = 0;
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(FileName))
                    System.IO.File.Delete(FileName);
                _logger.WriteLogError(ex, $"{nameof(UploadDataBase)}");
            }
            return result;
        }

        public async Task<bool> UploadMessages(ChannelReader<byte[]> stream, OBJ_ID msgId, int SaveFileOrBase = 1)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("ID сообщения", msgId.ObjID);
            bool result = false;
            try
            {
                using var callFile = _SMSGso.WriteSoundMessagesStream(new Metadata() { new Metadata.Entry(MetaDataName.MSGOBJID, JsonFormatter.Default.Format(msgId)), new Metadata.Entry(MetaDataName.SAVEFILEORBASE, SaveFileOrBase.ToString()) }, deadline: DateTime.UtcNow.AddMinutes(10), Context.ConnectionAborted);

                while (await stream.WaitToReadAsync())
                {
                    while (stream.TryRead(out var item))
                    {
                        await callFile.RequestStream.WriteAsync(new BytesValue() { Value = UnsafeByteOperations.UnsafeWrap(item) }, Context.ConnectionAborted);
                    }
                }

                await callFile.RequestStream.CompleteAsync();
                var b = await callFile;
                result = b.Value;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(UploadMessages)}");
            }
            return result;
        }

        public async Task<byte[]> ConvertSoundToTmpFile(ChannelReader<byte[]> stream, string FileName)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Временный файл", FileName);

            string FileTemp = Path.Combine(DirectoryTmp, FileName);
            WavHeaderModel w = new();
            try
            {
                _ = RemoveOldTmpFile();
                if (!Directory.Exists(DirectoryTmp))
                {
                    Directory.CreateDirectory(DirectoryTmp);
                }

                using var call = _TtsClient.ConvertFileToFileStream(deadline: DateTime.UtcNow.AddMinutes(10), cancellationToken: Context.ConnectionAborted);

                var readTask = Task.Run(async () =>
                {
                    try
                    {
                        while (await stream.WaitToReadAsync())
                        {
                            while (stream.TryRead(out var item))
                            {
                                await call.RequestStream.WriteAsync(new BytesValue() { Value = UnsafeByteOperations.UnsafeWrap(item) }, Context.ConnectionAborted);
                            }
                        }
                        await call.RequestStream.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLogError(ex, $"{nameof(ConvertSoundToTmpFile)}");
                    }
                });
                using (var write = new FileStream(FileTemp, FileMode.Create))
                {
                    while (await call.ResponseStream.MoveNext(Context.ConnectionAborted))
                    {
                        var value = call.ResponseStream.Current;
                        await write.WriteAsync(value.Value.ToArray(), Context.ConnectionAborted);
                    }
                    //перезаписываем формат
                    byte[] buffer = new byte[1000];
                    write.Seek(0, SeekOrigin.Begin);
                    var readCount = await write.ReadAsync(buffer, Context.ConnectionAborted);

                    if (readCount > 0)
                    {
                        w = new(buffer);
                        var filelength = write.Length;
                        w.ChunkHeaderSize = (uint)filelength - 8;
                        w.SubChunk = (uint)(filelength - w.GetAllHeaderLength());
                        write.Seek(0, SeekOrigin.Begin);
                        await write.WriteAsync(w.ToBytesAllHeader(), Context.ConnectionAborted);
                    }
                }
                await readTask;

            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(ConvertSoundToTmpFile)}");
            }

            return w.ToBytesAllHeader();
        }

        public async Task RecordSoundToTmpFile(ChannelReader<byte[]> stream, string FileName)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Временный файл", FileName);
            string FileTemp = Path.Combine(DirectoryTmp, FileName);
            WavHeaderModel w = new();
            try
            {
                _ = RemoveOldTmpFile();
                if (!Directory.Exists(DirectoryTmp))
                {
                    Directory.CreateDirectory(DirectoryTmp);
                }
                using (var write = new FileStream(FileTemp, FileMode.Create))
                {
                    while (await stream.WaitToReadAsync())
                    {
                        while (stream.TryRead(out var item))
                        {
                            await write.WriteAsync(item, Context.ConnectionAborted);
                        }
                    }
                    //перезаписываем формат
                    byte[] buffer = new byte[1000];
                    write.Seek(0, SeekOrigin.Begin);
                    var readCount = await write.ReadAsync(buffer, Context.ConnectionAborted);

                    if (readCount > 0)
                    {
                        w = new(buffer);
                        var filelength = write.Length;
                        w.ChunkHeaderSize = (uint)filelength - 8;
                        w.SubChunk = (UInt32)(filelength - w.GetAllHeaderLength());
                        write.Seek(0, SeekOrigin.Begin);
                        await write.WriteAsync(w.ToBytesAllHeader(), Context.ConnectionAborted);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(RecordSoundToTmpFile)}");
            }
        }

        public async Task<bool> ConvertSoundDataToGsmStream(uint sessionRecords, string uncRemoteCu, ChannelReader<byte[]> stream)
        {
            using var activity = this.ActivitySourceForHub()?.StartActivity();
            activity?.AddTag("Ip сервера", uncRemoteCu);
            activity?.AddTag("Id session", sessionRecords);

            GateServiceProto.V1.GateService.GateServiceClient? GetSMGateClient = null;

            if (!string.IsNullOrEmpty(uncRemoteCu))
            {
                GetSMGateClient = _connectRemote.GetGateClient($"http://{uncRemoteCu}");
            }
            else GetSMGateClient = _SMGate;

            if (GetSMGateClient == null)
                throw new Grpc.Core.RpcException(new Status(Grpc.Core.StatusCode.InvalidArgument, "Ошибка подключения к удаленному серверу"));

            try
            {
                using var call = _TtsClient.ConvertSoundDataToGsmStream(deadline: DateTime.UtcNow.AddMinutes(10));

                var readTask = Task.Run(async () =>
                {
                    while (await stream.WaitToReadAsync())
                    {
                        while (stream.TryRead(out var item))
                        {
                            await call.RequestStream.WriteAsync(new BytesValue() { Value = UnsafeByteOperations.UnsafeWrap(item) });
                        }
                    }
                    await call.RequestStream.CompleteAsync();

                });

                UInt32AndBytes request = new()
                {
                    Number = sessionRecords,
                    Bytes = ByteString.Empty
                };

                while (await call.ResponseStream.MoveNext())
                {
                    var value = call.ResponseStream.Current;
                    request.Bytes = value.Value;
                    await GetSMGateClient.Rem_WriteManualGSMBufferAsync(request);
                }
                await readTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(ConvertSoundDataToGsmStream)}");
            }
            return false;
        }

        private Task RemoveOldTmpFile()
        {
            try
            {
                if (Directory.Exists(DirectoryTmp))
                {
                    var listFile = new DirectoryInfo(DirectoryTmp).EnumerateFileSystemInfos().Where(x => (DateTime.UtcNow - x.CreationTimeUtc).Hours > 1);
                    foreach (var file in listFile)
                    {
                        System.IO.File.Delete(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(RemoveOldTmpFile)}");
            }
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                lock (locker)
                {
                    Context.Items.TryAdd(Context.ConnectionId, Context.ConnectionId);
                    UserHandler.ConnectedIds.Add(Context.ConnectionId);
                }
                using var activity = this.ActivitySourceForHub()?.StartActivity();
                activity?.AddTag("Kлиент", Context.ConnectionId);
                activity?.AddTag("Кол-во подключений", UserHandler.ConnectedIds.Count);
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
                lock (locker)
                {
                    Context.Items.Remove(Context.ConnectionId);
                    UserHandler.ConnectedIds.Remove(Context.ConnectionId);
                }
                using var activity = this.ActivitySourceForHub()?.StartActivity();
                activity?.AddTag("Kлиент", Context.ConnectionId);
                activity?.AddTag("Кол-во подключений", UserHandler.ConnectedIds.Count);
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, $"{nameof(OnDisconnectedAsync)}");
            }

            return base.OnDisconnectedAsync(exception);
        }

    }
}
