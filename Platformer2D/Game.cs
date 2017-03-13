#region File Description
//-----------------------------------------------------------------------------
// PlatformerGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using MonoGame.Core;


#endregion

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.Graphics;
using MonoGame.Content;
using Magnesium;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using System.Diagnostics;

namespace Platformer2D
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private IGraphicsDeviceManager mManager;
        private IMgSpriteBatch spriteBatch;
        Vector2 baseScreenSize = new Vector2(800, 480);
        private Matrix globalTransformation;

        // Global content.
        private SpriteFont hudFont;

        private IMgTexture winOverlay;
        private IMgTexture loseOverlay;
        private IMgTexture diedOverlay;

        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private GamePadState gamePadState;
        private KeyboardState keyboardState;
        private TouchCollection touchState;
        private AccelerometerState accelerometerState;

        private VirtualGamePad virtualGamePad;

        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        private const int numberOfLevels = 3;

        private IKeyboardInputListener mKeyboard;
        private ITouchListener mTouch;
        private GamePad mGamepad;
        private IContentStreamer mContentStreamer;
        private IGamePlatform mGamePlatform;
        private IMgTextureLoader mTextures;
        private IMgGraphicsConfiguration mGraphicsConfiguration;
        private IPresentationParameters mPresentationParameters;
        private DefaultSwapchain mSwapchain;
        private SongDevice mSongs;
        private SoundDevice mEffects;
        private IShaderContentStreamer mShaderContent;
        private SpriteBatchEffect mSpriteEffect;

        public PlatformerGame
        (
            IGraphicsDeviceManager manager,
            IMgGraphicsConfiguration graphicsConfiguration,
            IPresentationParameters presentationParameters,
            DefaultSwapchain swapChain,
            IShaderContentStreamer shaderContent,

            IKeyboardInputListener keyboard,
            ITouchListener touch,
            IGamepadPlatform gamepadPlatform,
            IContentStreamer contentStreamer,
            IGamePlatform gamePlatform,
            IMgTextureLoader textures,
            //IMediaPlayer mediaPlayer,
            //ISongReader songs,
            SongDevice songs,
            SoundDevice effects,
            SpriteBatchEffect spriteEffect
        )
        {
            mManager = manager;
            mGraphicsConfiguration = graphicsConfiguration;
            mPresentationParameters = presentationParameters;
            mSwapchain = swapChain;
            mShaderContent = shaderContent;

            mKeyboard = keyboard;
            mTouch = touch;
            mGamepad = new GamePad(gamepadPlatform);
            mContentStreamer = contentStreamer;
            mGamePlatform = gamePlatform;
            mTextures = textures;
            mSongs = songs;
            mEffects = effects;
            mSpriteEffect = spriteEffect;

            var width = (uint)mPresentationParameters.BackBufferWidth;
            var height = (uint)mPresentationParameters.BackBufferHeight;

            mGraphicsConfiguration.Initialize(width, height);

            var dsCreateInfo = new MgGraphicsDeviceCreateInfo
            {
                Color = MgFormat.R8G8B8A8_UINT,
                DepthStencil = MgFormat.D24_UNORM_S8_UINT,
                Width = width,
                Height = height,
                Samples = MgSampleCountFlagBits.COUNT_1_BIT,
            };
            
            // IMPORTANT : REQUIRED 
            mManager.CreateDevice(dsCreateInfo);

            mTextures.Initialize();
            mSwapchain.Initialize();

            mManager.IsFullScreen = false;

            mManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

            Accelerometer.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        public override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            /// TODO spriteBatch = new SpriteBatch();

            // Load fonts
           // TODO : hudFont = Content.Load<SpriteFont>("Fonts/Hud");

            // Load overlay textures
            winOverlay =    mTextures.Load(new AssetIdentifier { AssetId = 0x40000000 }); // "Overlays/you_win"
            loseOverlay =   mTextures.Load(new AssetIdentifier { AssetId = 0x40000001 }); // "Overlays/you_lose"
            diedOverlay =   mTextures.Load(new AssetIdentifier { AssetId = 0x40000002 }); // "Overlays/you_died"

            //Work out how much we need to scale our graphics to fill the screen
            float horScaling = mPresentationParameters.BackBufferWidth / baseScreenSize.X;
            float verScaling = mPresentationParameters.BackBufferHeight / baseScreenSize.Y;
            Vector3 screenScalingFactor = new Vector3(horScaling, verScaling, 1);
            globalTransformation = Matrix.CreateScale(screenScalingFactor);

            virtualGamePad = new VirtualGamePad(baseScreenSize, globalTransformation, mTextures.Load(new AssetIdentifier { AssetId = 0x20000000 })); // "Sprites/VirtualControlArrow"

            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                //mMediaPlayer.IsRepeating = true;
                mSongs.IsRepeating = true;

                var music = mSongs.Load(new AssetIdentifier { AssetId = 5U }); // "Sounds/Music"
                mSongs.Play(music);
            }
            catch { }

            LoadNextLevel();

            // SPRITEBATCH
            mSpriteEffect.Initialise();

            var descriptorSet = mSpriteEffect.DescriptorPool.CreateDescriptorSet();
            Debug.Assert(mSpriteEffect.VariantEffects.Length >= 1);
            var variant = mSpriteEffect.VariantEffects[0];

            spriteBatch = mSpriteEffect.Create(Color.CornflowerBlue, variant, descriptorSet);

            var frameInstances = new List<SubmitInfoGraphNode>();

            var noOfFrameBuffers = mManager.Device.Framebuffers.Length;
            mCmdBufs = new IMgCommandBuffer[noOfFrameBuffers];

            var allocateInfo = new MgCommandBufferAllocateInfo
            {
                CommandBufferCount = (uint) noOfFrameBuffers,
                CommandPool = mGraphicsConfiguration.Partition.CommandPool,
                Level = MgCommandBufferLevel.PRIMARY,
            };

            var err = mGraphicsConfiguration.Device.AllocateCommandBuffers(allocateInfo, mCmdBufs);
            Debug.Assert(err == Result.SUCCESS);

            for (var i = 0; i < noOfFrameBuffers; i += 1)
            {
                var frame = mManager.Device.Framebuffers[i];
                var cmdBuf = mCmdBufs[i];

                var beginInfo = new MgCommandBufferBeginInfo();

                err = cmdBuf.BeginCommandBuffer(beginInfo);
                spriteBatch.Compile(cmdBuf, frame);
                err = cmdBuf.EndCommandBuffer();

                var instance = new SubmitInfoGraphNode
                {
                    Fence = null,
                    Submit = new MgSubmitInfo
                    {
                        CommandBuffers = new[] { cmdBuf },
                    }
                };

                frameInstances.Add(instance);
            }

            var graphNode = new PrecompiledGraphNode(frameInstances.ToArray());
            mSwapchain.Graph.Renderables.Add(graphNode);
        }

        private IMgCommandBuffer[] mCmdBufs;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // Handle polling for our input and handling high-level input
            HandleInput(gameTime);

            // update our level, passing down the GameTime along with all of our input states
            level.Update(gameTime, keyboardState, gamePadState, 
                         accelerometerState, mPresentationParameters.DisplayOrientation);

            if (level.Player.Velocity != Vector2.Zero)
                virtualGamePad.NotifyPlayerIsMoving();

            base.Update(gameTime);
        }

        private void HandleInput(GameTime gameTime)
        {
            // get all of our input states
            keyboardState = mKeyboard.GetState();
            touchState = mTouch.GetState();
            gamePadState = virtualGamePad.GetState(touchState, mGamepad.GetState(PlayerIndex.One));
            accelerometerState = Accelerometer.GetState();

#if !NETFX_CORE
            // Exit the game when back is pressed.
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                mGamePlatform.Exit();
#endif
            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A) ||
                touchState.AnyTouch();

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife();
                }
                else if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                        LoadNextLevel();
                    else
                        ReloadCurrentLevel();
                }
            }

            wasContinuePressed = continuePressed;

            virtualGamePad.Update(gameTime);
        }

        private void LoadNextLevel()
        {
            // move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            //string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);

            var levelData = new[]
            {
                new AssetIdentifier { AssetId = 0x80000000 },
                new AssetIdentifier { AssetId = 0x80000001 },
                new AssetIdentifier { AssetId = 0x80000002 }
            };

            var backgroundLevels = new []
            {
                new AssetIdentifier { AssetId = 0xd0000000 },
                new AssetIdentifier { AssetId = 0xd0000003 },
                new AssetIdentifier { AssetId = 0xd0000006 }
            };

            var nextLevelId = levelData[levelIndex];
            var nextLevelLayerId = backgroundLevels[levelIndex];

            using (Stream fileStream = mContentStreamer.LoadContent(nextLevelId, new[] { ".txt" }))
            {
                level = new Level(mTextures, mEffects, fileStream, nextLevelLayerId);
            }
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            // Overwritten with mg/vulkan draw approach
            //mManager.GraphicsDevice.Clear(Color.CornflowerBlue);

            //spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null,null, globalTransformation);

            if (mManager.Device == null)
                return;

            level.Draw(gameTime, spriteBatch);

            //DrawHud();

            //spriteBatch.End();

            mSwapchain.Render(gameTime);

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            var vp = mManager.Device.CurrentViewport;
            Rectangle titleSafeArea = new Rectangle((int) vp.X, (int) vp.Y, (int) vp.Width, (int) vp.Height);
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            //Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
            //                             titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            Vector2 center = new Vector2(baseScreenSize.X / 2, baseScreenSize.Y / 2);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);
           
            // Determine the status overlay message to show.
            IMgTexture status = null;
            if (level.TimeRemaining == TimeSpan.Zero)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }

            if (touchState.IsConnected)
                virtualGamePad.Draw(spriteBatch);
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }

        protected override void ReleaseManagedResources()
        {
            
        }

        protected override void ReleaseUnmanagedResources()
        {
            if (spriteBatch != null)
            {
                spriteBatch.Dispose();
            }

            if (mSpriteEffect != null)
            {
                mSpriteEffect.Dispose();
            }

            if (mSwapchain != null)
            {
                mSwapchain.Dispose();
            }
        }
    }
}
