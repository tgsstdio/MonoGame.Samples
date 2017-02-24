using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace Platformer2D.DesktopGL
{
    internal class NullTouchListener : ITouchListener
    {
        public int DisplayHeight
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public DisplayOrientation DisplayOrientation
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int DisplayWidth
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public GestureType EnabledGestures
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool EnableMouseGestures
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool EnableMouseTouchPoint
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsGestureAvailable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IntPtr WindowHandle
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void AddEvent(int id, TouchLocationState state, Vector2 position)
        {
            throw new NotImplementedException();
        }

        public void AddEvent(int id, TouchLocationState state, Vector2 position, bool isMouse)
        {
            throw new NotImplementedException();
        }

        public ITouchPanelCapabilities GetCapabilities()
        {
            throw new NotImplementedException();
        }

        public GestureSample GetGestumeSample()
        {
            throw new NotImplementedException();
        }

        public TouchCollection GetState()
        {
            throw new NotImplementedException();
        }

        public void ReleaseAllTouches()
        {
            throw new NotImplementedException();
        }
    }
}