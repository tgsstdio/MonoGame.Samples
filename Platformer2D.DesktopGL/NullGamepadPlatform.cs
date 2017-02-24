using System;
using Microsoft.Xna.Framework.Input;

namespace Platformer2D.DesktopGL
{
    internal class NullGamepadPlatform : IGamepadPlatform
    {
        public GamePadCapabilities GetCapabilities(int index)
        {
            throw new NotImplementedException();
        }

        public GamePadState GetState(int index, GamePadDeadZone deadZoneMode)
        {
            throw new NotImplementedException();
        }

        public bool SetVibration(int index, float leftMotor, float rightMotor)
        {
            throw new NotImplementedException();
        }
    }
}