using DryIoc;
using Magnesium;
using Microsoft.Xna.Framework;
using MonoGame.Audio.OpenAL;
using MonoGame.Platform.DesktopGL;
using OpenTK;
using System;

namespace Platformer2D.DesktopGL
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var container = new Container())
                {
                    // GAME
                    container.Register<Microsoft.Xna.Framework.Game, Platformer2D.PlatformerGame>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.Input.Touch.ITouchListener, NullTouchListener>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.Input.IGamepadPlatform, NullGamepadPlatform>(Reuse.Singleton);
                    container.Register<Platformer2D.SoundDevice>(Reuse.InCurrentScope);
                    container.Register<MonoGame.Core.Audio.ISoundEffectReader, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLWAVReader>(Reuse.InCurrentScope);
                    container.Register<Platformer2D.SongDevice>(Reuse.InCurrentScope);
                    container.Register<MonoGame.Content.Audio.ISongReader, MonoGame.Content.Audio.OpenAL.NVorbis.NVorbisSongReader>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.Media.IMediaPlayer, DefaultMediaPlayer>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.Media.IMediaQueue, Microsoft.Xna.Framework.Media.DefaultMediaQueue>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.Audio.ISoundPlayer, Microsoft.Xna.Framework.Audio.SoundPlayer>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.Audio.ISoundEffectImplementation, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLSoundEffectImplementation>(Reuse.InCurrentScope);

                    // Magnesium IN SCOPE RESOLUTION
                    container.Register<Magnesium.IMgPresentationLayer, Magnesium.MgPresentationLayer>(Reuse.InCurrentScope);
                    container.Register<Magnesium.IMgPresentationBarrierEntrypoint, Magnesium.MgPresentationBarrierEntrypoint>(Reuse.InCurrentScope);

                    // MONOGAME
                    container.Register<MonoGame.Graphics.IGraphicsDeviceManager, MonoGame.Platform.DesktopGL.MgDesktopGLGraphicsDeviceManager>(Reuse.InCurrentScope);
                    container.Register<Microsoft.Xna.Framework.IGameBackbone, Microsoft.Xna.Framework.GameBackbone>(Reuse.InCurrentScope);

                    // Magnesium
                    container.Register<Magnesium.MgDriverContext>(Reuse.Singleton);
                    container.Register<Magnesium.IMgGraphicsConfiguration, Magnesium.MgDefaultGraphicsConfiguration>(Reuse.Singleton);
                    container.Register<Magnesium.IMgImageTools, Magnesium.MgImageTools>(Reuse.Singleton);


                    // TODO: fix shader functions
                    SetupVulkan(container);

                    //// AUDIO
                    container.Register<MonoGame.Audio.OpenAL.IOpenALSoundContext, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLOpenALSoundContext>(Reuse.Singleton);
                    container.Register<MonoGame.Audio.OpenAL.IOpenALSoundController, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLOALSoundController>(Reuse.Singleton);
                    container.Register<MonoGame.Audio.OpenAL.IOALSourceArray, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLOALSourcesArray>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.Audio.ISoundEffectInstancePoolPlatform, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLSoundEffectInstancePoolPlatform>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.Audio.ISoundEffectInstancePool, MonoGame.Audio.OpenAL.DesktopGL.DesktopGLOALSoundEffectInstancePool>(Reuse.Singleton);
                    //container.Register<ISoundEnvironment, SoundEnvironment>(Reuse.Singleton);

                    // RUNTIME
                    container.Register<Microsoft.Xna.Framework.Content.IContentManager, Microsoft.Xna.Framework.Content.NullContentManager>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.Content.IContentTypeReaderManager, Microsoft.Xna.Framework.Content.NullContentTypeReaderManager>(Reuse.Singleton);

                    // MonoGame.Platform.DesktopGL
                    container.Register<Microsoft.Xna.Framework.IGamePlatform, MonoGame.Platform.DesktopGL.OpenTKGamePlatform>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.IPlatformActivator, Microsoft.Xna.Framework.PlatformActivator>(Reuse.Singleton);
                    container.Register<MonoGame.Core.IThreadSleeper, MonoGame.Platform.DesktopGL.DesktopGLThreadSleeper>(Reuse.Singleton);
                    container.Register<MonoGame.Platform.DesktopGL.IWindowExitStrategy, MonoGame.Platform.DesktopGL.DesktopGLExitStrategy>(Reuse.Singleton);

                    // OPENTK BACKBUFFER STUFF
                    container.Register<MonoGame.Platform.DesktopGL.IOpenTKWindowResetter, MonoGame.Platform.DesktopGL.DesktopGLWindowResetter>(Reuse.Singleton);

                    container.Register<MonoGame.Platform.DesktopGL.IOpenTKGameWindow, MonoGame.Platform.DesktopGL.OpenTKGameWindow>(Reuse.Singleton);
                    container.RegisterMapping<Microsoft.Xna.Framework.IGameWindow, MonoGame.Platform.DesktopGL.IOpenTKGameWindow>();
                    container.Register<Microsoft.Xna.Framework.Input.IMouseListener, MonoGame.Platform.DesktopGL.Input.DesktopGLMouseListener>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.Input.IKeyboardInputListener, Microsoft.Xna.Framework.Input.KeyboardInputListener>(Reuse.Singleton);

                    // MonoGame
                    container.Register<Microsoft.Xna.Framework.Graphics.IPresentationParameters, MonoGame.Core.Graphics.DefaultPresentationParameters>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.IBackBufferPreferences, MonoGame.Platform.DesktopGL.DesktopGLBackBufferPreferences>();
                    container.Register<Microsoft.Xna.Framework.Graphics.IGraphicsAdapterCollection, MonoGame.Platform.DesktopGL.Graphics.DesktopGLGraphicsAdapterCollection>(Reuse.Singleton);
                    container.Register<MonoGame.Core.IGraphicsProfiler, MonoGame.Core.DefaultGraphicsDeviceProfiler>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.IDrawSuppressor, Microsoft.Xna.Framework.DrawSupressor>(Reuse.Singleton);
                    container.Register<MonoGame.Core.IClientWindowBounds, MonoGame.Core.DefaultClientWindowBounds>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.IGraphicsDeviceQuery, MonoGame.Core.DefaultGraphicsDeviceQuery>(Reuse.Singleton);

                    // MAGNESIUM TEXTURES 
                    container.Register<MonoGame.Graphics.IMgTextureLoader, MonoGame.Textures.FreeImageNET.FITexture2DLoader>(Reuse.Singleton);
                    container.Register<MonoGame.Content.IContentStreamer, MonoGame.Content.ContentStreamer>(Reuse.Singleton);
                    container.Register<MonoGame.Content.IBlockLocator, MonoGame.Content.MaskedBlockLocator>(Reuse.Singleton);
                    container.Register<MonoGame.Content.IFileSystem, MonoGame.Content.Dirs.DirectoryFileSystem>(Reuse.Singleton);
                    container.Register<Microsoft.Xna.Framework.ITitleContainer, MonoGame.Platform.DesktopGL.DesktopGLTitleContainer>(Reuse.Singleton);
                    container.Register<MonoGame.Core.ITextureSortingKeyGenerator, MonoGame.Core.DefaultTextureSortingKeyGenerator>(Reuse.Singleton);


                    using (var outerScope = container.OpenScope())
                    {
                        using (var window = new NativeWindow())
                        {
                            outerScope.RegisterInstance<INativeWindow>(window);
                            using (var driver = outerScope.Resolve<MgDriverContext>())
                            {
                                var errorCode = driver.Initialize(
                                    new MgApplicationInfo
                                    {
                                        ApplicationName = "HelloMagnesium",
                                        EngineName = "Magnesium",
                                        ApplicationVersion = 1,
                                        EngineVersion = 1,
                                        ApiVersion = MgApplicationInfo.GenerateApiVersion(1, 0, 39),
                                    },
                                    MgInstanceExtensionOptions.ALL
                                    );

                                if (errorCode != Result.SUCCESS)
                                {
                                    throw new InvalidOperationException("mDriverContext error : " + errorCode);
                                }

                                using (var graphicsConfiguration = outerScope.Resolve<IMgGraphicsConfiguration>())
                                using (var innerScope = container.OpenScope())
                                {
                                    using (var audioContext = innerScope.Resolve<IOpenALSoundContext>())
                                    {
                                        audioContext.Initialize();
                                        using (var backbone = innerScope.Resolve<IGameBackbone>())
                                        {
                                            var exitStrategy = innerScope.Resolve<IWindowExitStrategy>();
                                            exitStrategy.Initialize();

                                            backbone.Run();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void SetupVulkan(Container container)
        {
            container.Register<Magnesium.IMgTextureGenerator, Magnesium.MgStagingBufferOptimizer>(Reuse.Singleton);

            // Magnesium.VUlkan
            container.Register<Magnesium.IMgEntrypoint, Magnesium.Vulkan.VkEntrypoint>(Reuse.Singleton);

            // IMgGraphicsDevice
            container.Register<Magnesium.IMgGraphicsDevice, Magnesium.MgDefaultGraphicsDevice>(Reuse.InCurrentScope);

            // IMgSwapchainCollection
            container.Register<Magnesium.IMgSwapchainCollection, Magnesium.MgSwapchainCollection>(Reuse.InCurrentScope);

            container.Register<MonoGame.Graphics.IShaderContentStreamer, MonoGame.Graphics.SPIRVShaderContentStreamer>(Reuse.Singleton);

            // WINDOW 
            container.Register<Magnesium.IMgPresentationSurface, Magnesium.PresentationSurfaces.OpenTK.VulkanPresentationSurface>(Reuse.Singleton);
        }
    }
}
