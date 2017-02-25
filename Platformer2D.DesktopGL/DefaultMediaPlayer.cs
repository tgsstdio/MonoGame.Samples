using Microsoft.Xna.Framework.Media;
using System;

namespace Platformer2D.DesktopGL
{
    public class DefaultMediaPlayer : BaseMediaPlayer
    {
        private bool _isMuted;
        private bool _isRepeating;
        private bool _isShuffled;
        private MediaState _state;
        private float _volume;

        public DefaultMediaPlayer(IMediaQueue queue) : base(queue)
        {
            
        }

        protected override bool GetGameHasControl()
        {
            throw new NotImplementedException();
        }

        protected override bool GetIsMuted()
        {
            return _isMuted;
        }

        protected override bool GetIsRepeating()
        {
            return _isRepeating;
        }

        protected override bool GetIsShuffled()
        {
            return _isShuffled;
        }

        protected override TimeSpan GetPlayPosition()
        {
            throw new NotImplementedException();
        }

        protected override MediaState GetState()
        {
            return _state;
        }

        protected override float GetVolume()
        {
            return _volume;
        }

        protected override void OnMediaStateChange()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformPause()
        {
            if (Queue.ActiveSong == null)
                return;

            //Queue.ActiveSong.Pause();
        }

        protected override void PlatformPlaySong(ISong song)
        {
            if (Queue.ActiveSong == null)
                return;

            //song. SetEventHandler(OnSongFinishedPlaying);

            //song.Volume = _isMuted ? 0.0f : _volume;
            //song.Play();
        }

        protected override void PlatformResume()
        {
            if (Queue.ActiveSong == null)
                return;

            //Queue.ActiveSong.Resume();
        }

        protected override void PlatformStop()
        {
            throw new NotImplementedException();
        }

        protected override void RepeatCurrentSong()
        {
            throw new NotImplementedException();
        }

        protected override void SetIsMuted(bool value)
        {
            _isMuted = value;

            if (Queue.Count == 0)
                return;

            var newVolume = _isMuted ? 0.0f : _volume;
           // Queue.SetVolume(newVolume);
        }

        protected override void SetIsRepeating(bool value)
        {
            _isRepeating = value;
        }

        protected override void SetIsShuffled(bool value)
        {
            _isShuffled = value;
        }

        protected override void SetPlayPosition(TimeSpan value)
        {
            throw new NotImplementedException();
        }

        protected override void SetVolume(float value)
        {
            _volume = value;

            if (Queue.ActiveSong == null)
                return;

            //Queue.SetVolume(_isMuted ? 0.0f : _volume);
        }
    }
}
