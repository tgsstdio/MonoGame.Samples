using MonoGame.Content;
using Microsoft.Xna.Framework.Media;
using MonoGame.Content.Audio;

namespace Platformer2D
{
    public class SongDevice
    {
        private IMediaPlayer mPlayer;
        private ISongReader mReader;

        public SongDevice(ISongReader reader, IMediaPlayer player)
        {
            mReader = reader;
            mPlayer = player;
        }

        public bool IsRepeating
        {
            get
            {
                return mPlayer.IsRepeating;
            }
            set
            {
                mPlayer.IsRepeating = value;
            }
        }

        public void Play(ISong music)
        {
            mPlayer.Play(music);
        }

        public ISong Load(AssetIdentifier assetIdentifier)
        {            
            return mReader.Load(assetIdentifier);            
        }
    }
}
