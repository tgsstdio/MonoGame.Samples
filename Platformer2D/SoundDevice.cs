using Microsoft.Xna.Framework.Audio;
using MonoGame.Content;
using MonoGame.Core.Audio;
using System.IO;

namespace Platformer2D
{
    public class SoundDevice
    {
        public SoundDevice(IContentStreamer contentStreamer, ISoundPlayer player, ISoundEffectReader reader)
        {
            mContentStreamer = contentStreamer;
            mPlayer = player;
            mReader = reader;
        }

        private ISoundEffectReader mReader;
        private ISoundPlayer mPlayer;
        private IContentStreamer mContentStreamer;

        internal SoundEffect Load(AssetIdentifier assetIdentifier)
        {
            using (var fs = mContentStreamer.LoadContent(assetIdentifier, new[] { ".wav", ".aiff", ".ac3", ".mp3" }))
            using (var br = new BinaryReader(fs))
            {
                return mReader.Read(br);
            }
        }

        internal void Play(SoundEffect jumpSound)
        {
            mPlayer.Play(jumpSound);
        }
    }
}
