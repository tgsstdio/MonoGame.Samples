using MonoGame.Graphics;
using MonoGame.Content;
using Magnesium;
using System.Diagnostics;
using Magnesium.Ktx;

namespace Platformer2D
{
    public class MgTexture2DLoader : IMgTextureLoader
    {
        private readonly IMgGraphicsConfiguration mConfiguration;
        private readonly IContentStreamer mContentStreamer;
        private readonly IKTXTextureLoader mLoader;

        public MgTexture2DLoader(
            IMgGraphicsConfiguration configuration,
            IKTXTextureLoader loader,
            IContentStreamer contentStreamer
            )
        {
            mConfiguration = configuration;
            mLoader = loader;
            mContentStreamer = contentStreamer;     
        }

        private MgPhysicalDeviceFeatures mFeatures;
        private MgPhysicalDeviceProperties mPhysicalDeviceProperties;
        public void Initialize()
        {
            mConfiguration.Partition.PhysicalDevice.GetPhysicalDeviceFeatures(out mFeatures);
            mConfiguration.Partition.PhysicalDevice.GetPhysicalDeviceProperties(out mPhysicalDeviceProperties);
        }

        public IMgTexture Load(AssetIdentifier assetId)
        {
            using (var fs = mContentStreamer.LoadContent(assetId, new[] {".ktx"}))
            {
                var result = mLoader.Load(fs);

                // Create sampler
                // In Vulkan textures are accessed by samplers
                // This separates all the sampling information from the 
                // texture data
                // This means you could have multiple sampler objects
                // for the same texture with different settings
                // Similar to the samplers available with OpenGL 3.3
                var samplerCreateInfo = new MgSamplerCreateInfo
                {
                    MagFilter = MgFilter.LINEAR,
                    MinFilter = MgFilter.LINEAR,
                    MipmapMode = MgSamplerMipmapMode.LINEAR,
                    AddressModeU = MgSamplerAddressMode.REPEAT,
                    AddressModeV = MgSamplerAddressMode.REPEAT,
                    AddressModeW = MgSamplerAddressMode.REPEAT,
                    MipLodBias = 0.0f,
                    CompareOp = MgCompareOp.NEVER,
                    MinLod = 0.0f,
                    BorderColor = MgBorderColor.FLOAT_OPAQUE_WHITE,
                };

                // Set max level-of-detail to mip level count of the texture
                var mipLevels = (uint)result.Source.Mipmaps.Length;
                samplerCreateInfo.MaxLod = (float)mipLevels;
                // Enable anisotropic filtering
                // This feature is optional, so we must check if it's supported on the device
                //mManager.Configuration.Partition.


                if (mFeatures.SamplerAnisotropy)
                {
                    // Use max. level of anisotropy for this example
                    samplerCreateInfo.MaxAnisotropy = mPhysicalDeviceProperties.Limits.MaxSamplerAnisotropy;
                    samplerCreateInfo.AnisotropyEnable = true;
                }
                else
                {
                    // The device does not support anisotropic filtering
                    samplerCreateInfo.MaxAnisotropy = 1.0f;
                    samplerCreateInfo.AnisotropyEnable = false;
                }

                IMgSampler sampler;
                var err = mConfiguration.Device.CreateSampler(samplerCreateInfo, null, out sampler);
                Debug.Assert(err == Result.SUCCESS);

                // Create image view
                // Textures are not directly accessed by the shaders and
                // are abstracted by image views containing additional
                // information and sub resource ranges
                var viewCreateInfo = new MgImageViewCreateInfo
                {
                    Image = result.TextureInfo.Image,
                    // TODO : FETCH VIEW TYPE FROM KTX 
                    ViewType = MgImageViewType.TYPE_2D,
                    Format = result.Source.Format,
                    Components = new MgComponentMapping
                    {
                        R = MgComponentSwizzle.R,
                        G = MgComponentSwizzle.G,
                        B = MgComponentSwizzle.B,
                        A = MgComponentSwizzle.A,
                    },
                    // The subresource range describes the set of mip levels (and array layers) that can be accessed through this image view
                    // It's possible to create multiple image views for a single image referring to different (and/or overlapping) ranges of the image
                    SubresourceRange = new MgImageSubresourceRange
                    {
                        AspectMask = MgImageAspectFlagBits.COLOR_BIT,
                        BaseMipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                        LevelCount = mipLevels,
                    }
                };

                IMgImageView view;
                err = mConfiguration.Device.CreateImageView(viewCreateInfo, null, out view);
                Debug.Assert(err == Result.SUCCESS);

                var texture = new MgTexture
                (
                    image: result.TextureInfo.Image,
                    view: view, sampler: sampler,
                    deviceMemory: result.TextureInfo.DeviceMemory
                )
                {
                    Width = result.Source.Width,
                    Height = result.Source.Height,
                    Depth = 1,
                    MipmapLevels = mipLevels,
                    // TODO : fix array layers for cube maps
                    ArrayLayers = 1,
                };
                return texture;
            }
        }
    }
}
