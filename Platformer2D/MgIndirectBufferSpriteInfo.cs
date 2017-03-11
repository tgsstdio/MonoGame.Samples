using Magnesium;
using System;
using System.Diagnostics;

namespace Platformer2D
{
    public class MgIndirectBufferSpriteInfo : IDisposable
    {
        private bool mWasSupplied;
        private IMgThreadPartition mThreadPartition;
        private IMgIndexedIndirectCommandSerializer mSerializer;
        public IMgBuffer Buffer { get; private set; }
        public IMgDeviceMemory DeviceMemory { get; private set; }
        public uint UpdateOffset { get; private set; }
        public ulong BindOffset { get; private set; }
        public uint Stride { get; private set; }

        private IMgAllocationCallbacks mAllocator;
        public MgIndirectBufferSpriteInfo(
            IMgThreadPartition partition,
            IMgIndexedIndirectCommandSerializer serializer,
            IMgAllocationCallbacks allocator)
        {
            mThreadPartition = partition ?? throw new ArgumentNullException(nameof(partition));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Stride = (uint)mSerializer.GetStride();
            mAllocator = allocator;

            mWasSupplied = false;
            UpdateOffset = 0U;
            BindOffset = 0UL;

            AllocateOwnMemory();
        }

        public MgIndirectBufferSpriteInfo(
            IMgThreadPartition partition,
            IMgIndexedIndirectCommandSerializer serializer,
            IMgBuffer buffer,
            IMgDeviceMemory deviceMemory,
            ulong offset)
        {
            mThreadPartition = partition ?? throw new ArgumentNullException(nameof(partition));
            mSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Stride = (uint)mSerializer.GetStride();
            mAllocator = null;

            mWasSupplied = true;
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            DeviceMemory = deviceMemory ?? throw new ArgumentNullException(nameof(deviceMemory));

            if (offset < uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            UpdateOffset = (uint)offset;
            BindOffset = offset;
        }

        public void Update(MgDrawIndexedIndirectCommand command)
        {
            const uint FLAGS = 0U;

            var device = mThreadPartition.Device;

            IntPtr dest;
            var err = DeviceMemory.MapMemory(device, BindOffset, Stride, FLAGS, out dest);
            Debug.Assert(err == Result.SUCCESS);
            mSerializer.Serialize(dest, UpdateOffset, new[] { command });
            DeviceMemory.UnmapMemory(device);
        }

        private void AllocateOwnMemory()
        {            
            IMgDevice device = mThreadPartition.Device;

            Debug.Assert(device != null);

            IMgBuffer buffer;
            MgBufferCreateInfo createInfo = new MgBufferCreateInfo
            {
                Size = (ulong)Stride,
                Usage = MgBufferUsageFlagBits.INDIRECT_BUFFER_BIT,
            };
            device.CreateBuffer(createInfo, mAllocator, out buffer);

            MgMemoryRequirements memReqs;
            device.GetBufferMemoryRequirements(buffer, out memReqs);

            uint memoryTypeIndex;
            var memoryPropertyFlags = MgMemoryPropertyFlagBits.HOST_VISIBLE_BIT | MgMemoryPropertyFlagBits.HOST_COHERENT_BIT;
            mThreadPartition.GetMemoryType(memReqs.MemoryTypeBits, memoryPropertyFlags, out memoryTypeIndex);

            var memAlloc = new MgMemoryAllocateInfo
            {
                MemoryTypeIndex = memoryTypeIndex,
                AllocationSize = memReqs.Size,
            };

            IMgDeviceMemory deviceMemory;
            var err = device.AllocateMemory(memAlloc, mAllocator, out deviceMemory);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            buffer.BindBufferMemory(device, deviceMemory, BindOffset);

            Buffer = buffer;
            DeviceMemory = deviceMemory;
        }

        ~MgIndirectBufferSpriteInfo()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;        
        private void Dispose(bool disposed)
        {
            if (mIsDisposed)
                return;

            ReleaseUnmanagedResources();

            mIsDisposed = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (mThreadPartition != null)
            {
                if (!mWasSupplied)
                {
                    var device = mThreadPartition.Device;

                    if (DeviceMemory != null)
                    {
                        DeviceMemory.FreeMemory(device, mAllocator);
                    }

                    if (Buffer != null)
                    {
                        Buffer.DestroyBuffer(device, mAllocator);
                    }
                }
            }
        }
    }
    
}
