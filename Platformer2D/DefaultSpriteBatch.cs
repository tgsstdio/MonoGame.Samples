using System;
using Microsoft.Xna.Framework;
using MonoGame.Core;
using MonoGame.Graphics;
using Magnesium;
using MonoGame.Content;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Platformer2D
{
    internal class DefaultSpriteBatch : IMgSpriteBatch
    {
        public interface IMgDeviceSuccessLogger
        {
            void Log(Result result);
        }

        private IMgThreadPartition mPartition;
        private IMgDescriptorSetLayout mDescriptorSetLayout;
        private IMgPipelineLayout mPipelineLayout;
        private IShaderContentStreamer mContent;

        public uint NoOfTextureSlots { get; private set; }

        public DefaultSpriteBatch(IMgThreadPartition partition, IShaderContentStreamer content)
        {
            mPartition = partition;
            mContent = content;
        }

        public EffectVariant[] Load(uint noOfTextureSlots, EffectVariantSeed[] seeds)
        {
            NoOfTextureSlots = noOfTextureSlots;
            SetupDescriptorSetLayout();
            SetupPipelineLayout();

            PopulateVariantSeeds(seeds);

            return CreateEffectVariants(seeds);
        }

        public EffectDescriptorPool CreateDescriptorPool(uint maxNoOfDescriptorSets)
        {
            IMgDescriptorPool descriptorPool;

            MgDescriptorPoolCreateInfo createInfo = new MgDescriptorPoolCreateInfo
            {
                MaxSets = maxNoOfDescriptorSets,
                PoolSizes = new[]
                {
                    new MgDescriptorPoolSize
                    {
                        DescriptorCount = maxNoOfDescriptorSets * NoOfTextureSlots,
                        Type = MgDescriptorType.COMBINED_IMAGE_SAMPLER,
                    },
                    new MgDescriptorPoolSize
                    {
                        DescriptorCount = maxNoOfDescriptorSets,
                        Type = MgDescriptorType.STORAGE_BUFFER,
                    }
                },
            };
            var err = mPartition.Device.CreateDescriptorPool(createInfo, null, out descriptorPool);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            return new EffectDescriptorPool
            {
                Device  = mPartition.Device,
                DescriptorPool = descriptorPool,
                PipelineLayout = mPipelineLayout,
                MaxNoOfDescriptorSets = maxNoOfDescriptorSets,
                DescriptorSetLayout = mDescriptorSetLayout,
            };
        }

        #region Private methods

        private void PopulateVariantSeeds(EffectVariantSeed[] seeds)
        {
            // use same shaders
            var vertShaderId = new AssetIdentifier { AssetId = 1 };
            var fragShaderBaseId = new AssetIdentifier { AssetId = 2 };

            // shader files are laid out based off fragShaderBaseId + NoOfTextureSlots
            var fragShaderId = fragShaderBaseId.Add(NoOfTextureSlots);

            foreach (var seed in seeds)
            {
                seed.VertexShader = vertShaderId;
                seed.FragmentShader = fragShaderId;
            }
        }

        private EffectVariant[] CreateEffectVariants(EffectVariantSeed[] seeds)
        {
            IMgPipeline[] variantPipelines = GenerateVariantPipelines(seeds);

            /// output - effect variants    
            var noOfSeeds = seeds.Length;
            var outputs = new EffectVariant[seeds.Length];
            for (var i = 0; i < noOfSeeds; i += 1)
            {
                outputs[i] = new EffectVariant
                {
                    GraphicsDevice = seeds[i].GraphicsDevice,
                    Pipeline = variantPipelines[i],
                };
            }
            return outputs;
        }

        private void SetupPipelineLayout()
        {
            var pLayoutCreateInfo = new MgPipelineLayoutCreateInfo
            {
                SetLayouts = new[] { mDescriptorSetLayout },
            };

            var err = mPartition.Device.CreatePipelineLayout(pLayoutCreateInfo, null, out mPipelineLayout);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");
        }

        private void SetupDescriptorSetLayout()
        {
            // store the descriptor layout for new EffectDescriptorSet(s)
            var dslCreateInfo = new MgDescriptorSetLayoutCreateInfo
            {
                Bindings = new[] {
                        new MgDescriptorSetLayoutBinding {
                            Binding = 0,
                            DescriptorType = MgDescriptorType.COMBINED_IMAGE_SAMPLER,
                            StageFlags = MgShaderStageFlagBits.FRAGMENT_BIT,
                            DescriptorCount = NoOfTextureSlots,
                        },
                        new MgDescriptorSetLayoutBinding
                        {
                            Binding = 1,
                            DescriptorType = MgDescriptorType.STORAGE_BUFFER,
                            StageFlags = MgShaderStageFlagBits.VERTEX_BIT,
                            DescriptorCount = 1,
                        },
                    },
            };
            var err = mPartition.Device.CreateDescriptorSetLayout(dslCreateInfo, null, out mDescriptorSetLayout);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");
        }

        private IMgPipeline[] GenerateVariantPipelinesFromSamePair(EffectVariantSeed[] seeds, AssetIdentifier vertShaderId, AssetIdentifier fragShaderId)
        {
            var noOfSeeds = seeds.Length;

            IMgPipeline[] variantPipelines;           

            using (var vs = mContent.Load(vertShaderId))
            using (var fs = mContent.Load(fragShaderId))
            {
                IMgShaderModule vertSM = SetupShaderModule(vs);
                IMgShaderModule fragSM = SetupShaderModule(fs);

                var pipelineParameters = new MgGraphicsPipelineCreateInfo[noOfSeeds];
                for (var i = 0; i < noOfSeeds; i += 1)
                {
                    pipelineParameters[i] = Generate(seeds[i], mPipelineLayout, vertSM, fragSM);
                }

                var err = mPartition.Device.CreateGraphicsPipelines(null, pipelineParameters, null, out variantPipelines);
                Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

                vertSM.DestroyShaderModule(mPartition.Device, null);
                fragSM.DestroyShaderModule(mPartition.Device, null);
            }

            return variantPipelines;
        }

        private IMgPipeline[] GenerateVariantPipelines(EffectVariantSeed[] seeds)
        {
            var noOfSeeds = seeds.Length;

            IMgPipeline[] output = new IMgPipeline[seeds.Length];

            var index = 0;
            foreach (var seed in seeds)
            {
                using (var vs = mContent.Load(seed.VertexShader))
                using (var fs = mContent.Load(seed.FragmentShader))
                {
                    IMgShaderModule vertSM = SetupShaderModule(vs);
                    IMgShaderModule fragSM = SetupShaderModule(fs);

                    var pipelineParameters = Generate(seed, mPipelineLayout, vertSM, fragSM);

                    IMgPipeline[] singlePipeline;
                    var err = mPartition.Device.CreateGraphicsPipelines(null, new[] { pipelineParameters }, null, out singlePipeline);
                    Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

                    output[index] = singlePipeline[0];

                    vertSM.DestroyShaderModule(mPartition.Device, null);
                    fragSM.DestroyShaderModule(mPartition.Device, null);
                }
                index += 1;
            }

            return output;
        }

        private IMgShaderModule SetupShaderModule(System.IO.Stream vs)
        {
            var vertCreateInfo = new MgShaderModuleCreateInfo
            {
                Code = vs,
                CodeSize = new UIntPtr((ulong)vs.Length),
            };

            IMgShaderModule vertSM;
            var err = mPartition.Device.CreateShaderModule(vertCreateInfo, null, out vertSM);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            return vertSM;
        }

        private static MgGraphicsPipelineCreateInfo Generate(EffectVariantSeed seed, IMgPipelineLayout layout, IMgShaderModule vert, IMgShaderModule frag)
        {
            var graphicsDevice = seed.GraphicsDevice;
            Debug.Assert(graphicsDevice != null);

            // Create effect / pass / sub pass / pipeline tree
            return new MgGraphicsPipelineCreateInfo {
                Layout = layout,
                Stages = new[] {
                    new MgPipelineShaderStageCreateInfo {
                        Module = vert,
                        Name = "vertMain",
                        Stage = MgShaderStageFlagBits.VERTEX_BIT,
                    },
                    new MgPipelineShaderStageCreateInfo {
                        Module = frag,
                        Name = "fragMain",
                        Stage = MgShaderStageFlagBits.FRAGMENT_BIT,
                    },
                },
                RenderPass = graphicsDevice.Renderpass,
                ColorBlendState = new MgPipelineColorBlendStateCreateInfo
                {
                    Attachments = new MgPipelineColorBlendAttachmentState[]
                    {
                        new MgPipelineColorBlendAttachmentState
                        {
                            // WORKS NOW, 
							BlendEnable = false,
                            ColorWriteMask = MgColorComponentFlagBits.R_BIT | MgColorComponentFlagBits.G_BIT | MgColorComponentFlagBits.B_BIT | MgColorComponentFlagBits.A_BIT,
                            SrcColorBlendFactor = MgBlendFactor.SRC_COLOR,
                            SrcAlphaBlendFactor = MgBlendFactor.SRC_ALPHA,
                            AlphaBlendOp = MgBlendOp.ADD,
                            ColorBlendOp = MgBlendOp.ADD,
                            DstColorBlendFactor = MgBlendFactor.ZERO,
                            DstAlphaBlendFactor = MgBlendFactor.ZERO,
                        }
                    },
                },
                DynamicState = new MgPipelineDynamicStateCreateInfo
                {
                    DynamicStates = new MgDynamicState[]
                    {
                        MgDynamicState.STENCIL_COMPARE_MASK,
                        MgDynamicState.STENCIL_REFERENCE,
                        MgDynamicState.STENCIL_WRITE_MASK,
                        MgDynamicState.LINE_WIDTH,
                        MgDynamicState.DEPTH_BOUNDS,
                        MgDynamicState.DEPTH_BIAS,
                        MgDynamicState.BLEND_CONSTANTS,
                    }
                },
                DepthStencilState = new MgPipelineDepthStencilStateCreateInfo {
                    Front = new MgStencilOpState {
                        WriteMask = ~0U,
                        Reference = ~0U,
                        CompareMask = int.MaxValue,
                        CompareOp = MgCompareOp.ALWAYS,
                        PassOp = MgStencilOp.KEEP,
                        FailOp = MgStencilOp.KEEP,
                        DepthFailOp = MgStencilOp.KEEP,
                    },
                    Back = new MgStencilOpState {
                        WriteMask = ~0U,
                        Reference = ~0U,
                        CompareMask = int.MaxValue,
                        CompareOp = MgCompareOp.ALWAYS,
                        PassOp = MgStencilOp.KEEP,
                        FailOp = MgStencilOp.KEEP,
                        DepthFailOp = MgStencilOp.KEEP,
                    },
                    StencilTestEnable = false,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = MgCompareOp.LESS,
                    MinDepthBounds = 0f,
                    MaxDepthBounds = 1000f,
                },                       
                InputAssemblyState = new MgPipelineInputAssemblyStateCreateInfo {
                    Topology = MgPrimitiveTopology.TRIANGLE_LIST,
                },
                
                // TODO : Figure this out for multi-sampling
                MultisampleState = new MgPipelineMultisampleStateCreateInfo {
                    RasterizationSamples = MgSampleCountFlagBits.COUNT_1_BIT,
                },

                RasterizationState = new MgPipelineRasterizationStateCreateInfo {
                    PolygonMode = MgPolygonMode.FILL,
                    CullMode = MgCullModeFlagBits.NONE,
                    FrontFace = MgFrontFace.COUNTER_CLOCKWISE,
                    Flags = 0,
                    DepthClampEnable = true,
                    LineWidth = 1f,
                },
                VertexInputState = new MgPipelineVertexInputStateCreateInfo {
                    VertexBindingDescriptions = new[] {
                        // VERTEX DATA
						new MgVertexInputBindingDescription {
                            Binding = 0,
                            Stride = (uint) Marshal.SizeOf(typeof(MgSpriteVertexData)),
                            InputRate = MgVertexInputRate.VERTEX
                        },
                        // INSTANCE INDEX
                        new MgVertexInputBindingDescription
                        {
                            Binding = 1,
                            Stride = (uint) Marshal.SizeOf(typeof(MgSpriteInstanceData)),
                            InputRate = MgVertexInputRate.INSTANCE,
                        }
                    },
                    VertexAttributeDescriptions = new[] {
                        new MgVertexInputAttributeDescription {
                            Binding = 0,
                            Location = 0,
                            Format = MgFormat.R32G32B32_SFLOAT,
                            Offset = 0,
                        },
                        new MgVertexInputAttributeDescription {
                            Binding = 0,
                            Location = 1,
                            Format = MgFormat.R32G32_SFLOAT,
                            Offset = (uint) Marshal.SizeOf(typeof(Vector3)),
                        },
                        new MgVertexInputAttributeDescription {
                            Binding = 1,
                            Location = 0,
                            Format = MgFormat.R32_UINT,
                            Offset = 0,
                        },
                    },
                },
                // USE DEFAULT HERE HMMM
                ViewportState = new MgPipelineViewportStateCreateInfo {
                    Scissors = new[] {
                        graphicsDevice.Scissor
                    },
                    Viewports = new[] {
                        graphicsDevice.CurrentViewport
                    },
                },                
            };            
        }

        public void Begin(EffectVariant variant, EffectPipelineDescriptorSet descriptorSet)
        {
            throw new NotImplementedException();
        }

        public void Draw(IMgTexture texture, Vector2 position, object p, Color color, float v1, Vector2 origin, float v2, SpriteEffects none, float v3)
        {
            throw new NotImplementedException();
        }

        public void Draw(IMgTexture texture, Vector2 zero, Color white)
        {
            throw new NotImplementedException();
        }

        public void Draw(uint textureSlot, Vector2 zero, Color white)
        {
            throw new NotImplementedException();
        }

        public void Draw(uint textureSlot, Vector2 position, object p, Color color, float v1, Vector2 origin, float v2, SpriteEffects none, float v3)
        {
            throw new NotImplementedException();
        }

        public void DrawString(SpriteFont font, string value, Vector2 vector2, Color black)
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public void Draw(IMgTexture texture, Vector2 position, Rectangle? p, Color color, float v1, Vector2 origin, float v2, SpriteEffects flip, float v3)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}