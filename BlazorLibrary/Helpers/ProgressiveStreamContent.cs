using System.Net;

namespace BlazorLibrary.Helpers
{
    public class ProgressiveStreamContent : StreamContent
    {
        // Define the variables which is the stream that represents the file
        private readonly Stream _fileStream;
        // Maximum amount of bytes to send per packet
        private readonly int _maxBuffer = 1024 * 4;

        public event Action<long>? OnProgress;

        CancellationToken _token;

        public ProgressiveStreamContent(Stream stream, int maxBuffer, Action<long> onProgress, CancellationToken token) : base(stream)
        {
            _fileStream = stream;
            _maxBuffer = maxBuffer;
            OnProgress += onProgress;
            _token = token;
        }

        /// <summary>
        /// Event that we can subscribe to which will be triggered everytime after part of the file gets uploaded.
        /// It passes the total amount of uploaded bytes and the percentage as well
        /// </summary>

        //Override the SerialzeToStreamAsync method which provides us with the stream that we can write our chunks into it
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            // Define an array of bytes with the the length of the maximum amount of bytes to be pushed per time
            var buffer = new byte[_maxBuffer];
            var totalLength = _fileStream.Length;
            // Variable that holds the amount of uploaded bytes
            long uploaded = 0;
            int readBytes = 0;
            try
            {
                while ((readBytes = await _fileStream.ReadAsync(buffer, 0, _maxBuffer)) > 0 && !_token.IsCancellationRequested)
                {
                    uploaded += readBytes;
                    // Write the bytes to the HttpContent stream                   
                    await stream.WriteAsync(buffer.Take(readBytes).ToArray()).ConfigureAwait(false);
                    //await stream.FlushAsync().ConfigureAwait(false);
                    // Fire the event of OnProgress to notify the client about progress so far
                    OnProgress?.Invoke(uploaded);
                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            return false;
        }

    }
}
