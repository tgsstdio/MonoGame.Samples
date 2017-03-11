using System;
using System.Runtime.InteropServices;

namespace Platformer2D
{
    public class VkIndexedIndirectCommandSerializer : IMgIndexedIndirectCommandSerializer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct VkDrawIndexedIndirectCommand
        {
            public uint IndexCount { get; set; }
            public uint InstanceCount { get; set; }
            public uint FirstIndex { get; set; }
            public int VertexOffset { get; set; }
            public uint FirstInstance { get; set; }
        };

        public int GetStride()
        {
            return Marshal.SizeOf(typeof(VkDrawIndexedIndirectCommand));
        }

        public void Serialize(IntPtr dest, uint offset, MgDrawIndexedIndirectCommand[] commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            int destOffset = (int)offset;
            int stride = GetStride();
            foreach (var command in commands)
            {                 
                if (command != null)
                {
                    IntPtr bufferDest = IntPtr.Add(dest, destOffset);

                    // ALL OF THIS IS TO HANDLE VERTEX OFFSET BEING int instead of uint for Metal / OpenGL
                    // IMPLEMENTATION for vk - copy data to uint struct and marshal that object instead.

                    var structData = new VkDrawIndexedIndirectCommand
                    {
                        IndexCount = command.IndexCount,
                        FirstIndex = command.FirstIndex,
                        FirstInstance = command.FirstInstance,
                        VertexOffset = command.VertexOffset,
                        InstanceCount = command.InstanceCount,
                    };

                    Marshal.StructureToPtr(structData, bufferDest, false);

                    destOffset += stride;
                }
            }
        }
    }
    
}
