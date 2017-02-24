using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace Platformer2D.DesktopGL
{
    internal class DefaultSoundEffectReader : ISoundEffectReader
    {
        private ISoundEffectImplementation mPlatform;
        public DefaultSoundEffectReader(ISoundEffectImplementation platform)
        {
            mPlatform = platform;
        }

        public SoundEffect Read(BinaryReader input)
        {
            // NXB format for SoundEffect...
            //            
            // Byte [format size]	Format	WAVEFORMATEX structure
            // UInt32	Data size	
            // Byte [data size]	Data	Audio waveform data
            // Int32	Loop start	In bytes (start must be format block aligned)
            // Int32	Loop length	In bytes (length must be format block aligned)
            // Int32	Duration	In milliseconds

            // WAVEFORMATEX structure...
            //
            //typedef struct {
            //  WORD  wFormatTag;       // byte[0]  +2
            //  WORD  nChannels;        // byte[2]  +2
            //  DWORD nSamplesPerSec;   // byte[4]  +4
            //  DWORD nAvgBytesPerSec;  // byte[8]  +4
            //  WORD  nBlockAlign;      // byte[12] +2
            //  WORD  wBitsPerSample;   // byte[14] +2
            //  WORD  cbSize;           // byte[16] +2
            //} WAVEFORMATEX;

            byte[] header = input.ReadBytes(input.ReadInt32());
            byte[] data = input.ReadBytes(input.ReadInt32());
            int loopStart = input.ReadInt32();
            int loopLength = input.ReadInt32();
            input.ReadInt32();

            if (loopStart == loopLength)
            {
                // do nothing. just killing the warning for non-DirectX path 
            }
            if (header[0] == 2 && header[1] == 0)
            {
                // We've found MSADPCM data! Let's decode it here.
                using (MemoryStream origDataStream = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(origDataStream))
                    {
                        byte[] newData = MSADPCMToPCM.MSADPCM_TO_PCM(
                            reader,
                            header[2],
                            (short)((header[12] / header[2]) - 22)
                        );
                        data = newData;
                    }
                }

                // This is PCM data now!
                header[0] = 1;
            }

            int sampleRate = (
                (header[4]) +
                (header[5] << 8) +
                (header[6] << 16) +
                (header[7] << 24)
            );

            var channels = (header[2] == 2) ? AudioChannels.Stereo : AudioChannels.Mono;
            return new SoundEffect(mPlatform, data, sampleRate, channels);
        }
    }
}