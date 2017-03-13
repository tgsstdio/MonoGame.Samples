using Magnesium;
using System;
using System.Diagnostics;

namespace Platformer2D
{
    public class EffectDescriptorPool : IDisposable
    {
        public IMgDescriptorPool DescriptorPool { get; internal set; }
        public IMgDescriptorSetLayout DescriptorSetLayout { get; internal set; }
        public IMgPipelineLayout PipelineLayout { get; internal set; }
        public IMgDevice Device { get; internal set; }
        public uint MaxNoOfDescriptorSets { get; internal set; }

        public EffectPipelineDescriptorSet CreateDescriptorSet()
        {
            IMgDescriptorSet[] dSets;
            var allocateInfo = new MgDescriptorSetAllocateInfo
            {
                DescriptorPool = DescriptorPool,
                DescriptorSetCount = 1,
                SetLayouts = new[]
                {
                    DescriptorSetLayout,
                }
            };
            var err = Device.AllocateDescriptorSets(allocateInfo, out dSets);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            var effectSet = new EffectPipelineDescriptorSet(Device, dSets[0], PipelineLayout);
            return effectSet;
        }

        ~EffectDescriptorPool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;
        protected void Dispose(bool disposed)
        {
            if (mIsDisposed)
                return;

            ReleaseUnmanagedResources();

            if (disposed)
            {
                ReleaseManagedResources();
            }

            mIsDisposed = true;
        }

        private void ReleaseManagedResources()
        {

        }

        private void ReleaseUnmanagedResources()
        {
            if (Device != null)
            {
                if (DescriptorPool != null)
                {
                    DescriptorPool.ResetDescriptorPool(Device, 0);
                    DescriptorPool.DestroyDescriptorPool(Device, null);
                }
            }
        }
    }
}
