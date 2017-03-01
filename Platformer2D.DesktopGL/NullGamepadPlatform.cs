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

        private GamePadState mNullState = new GamePadState();
        public GamePadState GetState(int index, GamePadDeadZone deadZoneMode)
        {
            return mNullState;
        }

        public bool SetVibration(int index, float leftMotor, float rightMotor)
        {
            throw new NotImplementedException();
        }
    }
}