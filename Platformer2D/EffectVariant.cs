using Magnesium;
using System;

namespace Platformer2D
{
    public class EffectVariant : IDisposable
    {
        public IMgGraphicsDevice GraphicsDevice { get; internal set; }
        public IMgPipeline Pipeline { get; internal set; }
        public IMgDevice Device { get; internal set; }

        ~EffectVariant()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (mIsDisposed)
                return;

            ReleaseUnmanagedResources();

            if (disposing)
            {
                ReleaseManagedResources();
            }

            mIsDisposed = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (Pipeline != null)
                Pipeline.DestroyPipeline(Device, null);
        }

        private void ReleaseManagedResources()
        {
           
        }
    }
}
