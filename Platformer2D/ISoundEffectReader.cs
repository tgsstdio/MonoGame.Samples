using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace Platformer2D
{
    public interface ISoundEffectReader
    {
        SoundEffect Read(BinaryReader input);
    }
}