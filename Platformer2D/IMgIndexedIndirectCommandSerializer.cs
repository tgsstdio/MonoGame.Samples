using System;

namespace Platformer2D
{
    public interface IMgIndexedIndirectCommandSerializer
    {
        int GetStride();
        void Serialize(IntPtr dest, uint offset, MgDrawIndexedIndirectCommand[] commands);
    }    
}
