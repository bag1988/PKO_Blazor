using Microsoft.AspNetCore.Components;

namespace BlazorLibrary.FolderForInherits
{
    public class CancellableComponent : ComponentBase, IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        protected CancellationToken ComponentDetached => (_cancellationTokenSource ??= new()).Token;

        public void ResetToken()
        {
            try
            {
                Dispose();
            }
            finally
            {
                _cancellationTokenSource = new();
            }
        }

        public void DisposeToken()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
