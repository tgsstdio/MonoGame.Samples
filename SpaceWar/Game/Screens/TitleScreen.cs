#region File Description
//-----------------------------------------------------------------------------
// TitleScreen.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace Spacewar
{
    /// <summary>
    /// TitleScreen represents the title screen displayes while media loads and
    /// Allows users to select game type
    /// </summary>
    public class TitleScreen : FullScreenSplash
    {
        private bool showInfo;
        private bool playRetro;
        ITexture2D infoTexture;
        ITexture2D buttonTexture;

        /// <summary>
        /// Creates a new titlescreen
        /// </summary>
        public TitleScreen()
            : base(@"textures\spacewar_title_FINAL", TimeSpan.Zero, GameState.ShipSelection)
        {
            infoTexture = SpacewarGame.ContentManager.Load<ITexture2D>(SpacewarGame.Settings.MediaPath + @"textures\info_screen");
            buttonTexture = SpacewarGame.ContentManager.Load<ITexture2D>(SpacewarGame.Settings.MediaPath + @"textures\title_button_overlay");
        }

        /// <summary>
        /// Update for TitleScreen waits till the 1st image is displayed then preloads the media
        /// </summary>
        /// <param name="time">Game Time</param>
        /// <param name="elapsedTime">Elapsed Time since last Update</param>
        /// <returns>NextGameState</returns>
        public override GameState Update(TimeSpan time, TimeSpan elapsedTime)
        {
            //X displays the intro screen
            if (XInputHelper.GamePads[PlayerIndex.One].XPressed || XInputHelper.GamePads[PlayerIndex.Two].XPressed || XInputHelper.Keyboard.IsKeyPressed(Keys.X))
            {
                showInfo = true;
            }

            //B plays retro or cancels info screen
            if (XInputHelper.GamePads[PlayerIndex.One].BPressed || XInputHelper.GamePads[PlayerIndex.Two].BPressed || XInputHelper.Keyboard.IsKeyPressed(Keys.B))
            {
                if (showInfo)
                {
                    showInfo = false;
                }
                else
                {
                    playRetro = true;
                }
            }

            //Don't allow the base class to quit if the info screen is up
            if (showInfo)
            {
                return GameState.None;
            }
            else
            {
                GameState returnValue = base.Update(time, elapsedTime);
                //'A' is handled by FullScreenSplash.Update - handle 'B' here
                if (playRetro)
                    returnValue = GameState.PlayRetro;

                return returnValue;
            }
        }

        /// <summary>
        /// Renders the title screen
        /// </summary>
        public override void Render()
        {
            base.Render();

            //Once all the media is cached we can show the menu
            //IGraphicsDeviceService graphicsService = (IGraphicsDeviceService)game.Services.GetService(typeof(IGraphicsDeviceService));
            GraphicsDevice device = graphicsService.GraphicsDevice;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            device.DepthStencilState = DepthStencilState.None;

            if (showInfo)
            {
                SpriteBatch.Draw(infoTexture, new Vector2(270, 135), null, Color.White);
            }
            else
            {
                SpriteBatch.Draw(buttonTexture, new Vector2(950, 450), null, Color.White);
            }

            SpriteBatch.End();
        }

        public override void OnCreateDevice()
        {
            base.OnCreateDevice();
        }
    }
}
