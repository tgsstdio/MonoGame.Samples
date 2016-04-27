#region File Description
//-----------------------------------------------------------------------------
// FullScreenSplash.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using MonoGame.Core;


#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Spacewar
{
    /// <summary>
    /// FullScreenSplash will show and close with a press of the A button or by a particular timeout
    /// Its behaviour can be changed by overriding the UpdateMethod
    /// </summary>
    public class FullScreenSplash : Screen
    {
        private ITexture2D screenTexture;
        private double timeout;
        private double endTime = -1;
        private GameState nextState = GameState.None;

        /// <summary>
        /// Creates a FullScreenSplash with just a texture. State handling must be dealt with in an overridden Update()
        /// </summary>
        /// <param name="textureName">The texture to fill the screen with</param>
        protected FullScreenSplash(Game game, string textureName)
            : base(game)
        {
            setTexture(textureName);
        }

        /// <summary>
        /// Creates a FullScreenSplash with a texture, a timeout and the next state to transition to
        /// </summary>
        /// <param name="textureName">The texture to fill the screen with</param>
        /// <param name="timeout">Time in seconds to display</param>
        /// <param name="nextState">The gamestate to transition to</param>
        public FullScreenSplash(Game game, string textureName, TimeSpan timeoutSpan, GameState nextState)
            : base(game)
        {
            setTexture(textureName);
            timeout = timeoutSpan.TotalSeconds;
            this.nextState = nextState;
        }

        private void setTexture(string textureName)
        {
            this.screenTexture = SpacewarGame.ContentManager.Load<ITexture2D>(SpacewarGame.Settings.MediaPath + textureName);
        }

        /// <summary>
        /// Checks for timeout or 'A' and moves to next state if that happens
        /// </summary>
        /// <param name="time">Gametime</param>
        /// <param name="elapsedTime">Elapsed time since last call</param>
        /// <returns>If the state has changed return the new state here - otherwise GameState.None</returns>
        public override GameState Update(TimeSpan time, TimeSpan elapsedTime)
        {
            if (endTime < 0)
            {
                endTime = time.TotalSeconds + timeout;
            }

            //If there is a state to progress to then wait for the timer or 'A'
            if (nextState != GameState.None &&
                    ((timeout != 0 && time.TotalSeconds > endTime)
                    || XInputHelper.GamePads[PlayerIndex.One].APressed
                    || XInputHelper.GamePads[PlayerIndex.Two].APressed
                    || XInputHelper.Keyboard.IsKeyPressed(Keys.A)))
            {
                return nextState;
            }
            else
            {
                return base.Update(time, elapsedTime);
            }
        }

        /// <summary>
        /// Renders the full screen texture
        /// </summary>
        public override void Render()
        {
            IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)GameInstance.Services.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = graphicsService.GraphicsDevice;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            device.DepthStencilState = DepthStencilState.DepthRead;

            SpriteBatch.Draw(screenTexture, Vector2.Zero, null, Color.White);

            SpriteBatch.End();

            //Render the backdrop
            base.Render();
        }
    }
}

