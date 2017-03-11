using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Core;
using MonoGame.Graphics;
using Magnesium;

namespace Platformer2D
{
    public class DefaultSpriteBatcher : IMgSpriteBatch
    {
        private readonly MgSpriteBatchBuffer mBatchBuffer;
        private readonly MgDrawIndexedIndirectCommand mCommand;
        private readonly MgSpriteBatchRenderer mRenderer;
        private readonly MgIndirectBufferSpriteInfo mIndirectBuffer;
        private List<IMgTexture> mTextures;
        private IMgIndexedIndirectCommandSerializer mSerializer;

        public DefaultSpriteBatcher(
            IMgGraphicsConfiguration graphicsConfiguration,
            MgSpriteBatchBuffer batchBuffer,
            MgSpriteBatchRenderer renderer,
            MgIndirectBufferSpriteInfo indirectBuffer,
            IMgIndexedIndirectCommandSerializer serializer
            )
        {
            mBatchBuffer = batchBuffer;
            mRenderer = renderer;
            mIndirectBuffer = indirectBuffer;
            mTextures = new List<IMgTexture>();
            mSerializer = serializer;

            mCommand = new MgDrawIndexedIndirectCommand
            {
                FirstIndex = 0,
                FirstInstance = 0,
                IndexCount = 0,
                InstanceCount = 0,
                VertexOffset = 0,
            };

        }

        private MgSpriteRenderPageInfo mRenderPage;
        private EffectPipelineDescriptorSet mDescriptorSet;

        public void Begin(EffectVariant variant, EffectPipelineDescriptorSet descriptorSet)
        {
            SetupDescriptorSet(descriptorSet);

            SetupRenderer(variant, descriptorSet);
        }

        private void SetupRenderer(EffectVariant variant, EffectPipelineDescriptorSet descriptorSet)
        {
            var graphicsDevice = variant.GraphicsDevice;
            var CLEAR_COLOR = Color.AliceBlue;

            var passInfo = new MgClearRenderPassInfo(graphicsDevice.RenderpassInfo);
            var clearValues = ExtractClearValues(CLEAR_COLOR, passInfo);

            mRenderPage = new MgSpriteRenderPageInfo(mBatchBuffer, mIndirectBuffer)
            {
                ClearValues = clearValues.ToArray(),
                DescriptorSet = descriptorSet.DescriptorSet,
                Layout = descriptorSet.PipelineLayout,
                Pipeline = variant.Pipeline,
                RenderArea = graphicsDevice.Scissor,
                RenderPass = graphicsDevice.Renderpass,
            };

            mRenderer.Viewport = graphicsDevice.CurrentViewport;
        }

        private void SetupDescriptorSet(EffectPipelineDescriptorSet descriptorSet)
        {
            mDescriptorSet = descriptorSet;

            var constantBufferInfo = new MgDescriptorBufferInfo[]
            {
                new MgDescriptorBufferInfo
                {
                    Buffer = mBatchBuffer.Buffer,
                    Offset = mBatchBuffer.Materials.Offset,
                    Range = mBatchBuffer.Materials.ArraySize,
                }
            };

            const uint FIRST_CONSTANT_BUFFER_BINDING = 1U;
            descriptorSet.Begin();
            descriptorSet.SetConstantBuffers(FIRST_CONSTANT_BUFFER_BINDING, 0, constantBufferInfo);
        }

        private static List<MgClearValue> ExtractClearValues(Color clearColor, MgClearRenderPassInfo passInfo)
        {
            var clearValues = new List<MgClearValue>();
            foreach (var attachment in passInfo.Attachments)
            {
                if (attachment.Usage == MgImageAspectFlagBits.COLOR_BIT)
                {
                    var clear = attachment.AsColor(ExtractColor(clearColor));
                    clearValues.Add(clear);
                }
                else if ((attachment.Usage | MgImageAspectFlagBits.DEPTH_BIT) > 0 || (attachment.Usage | MgImageAspectFlagBits.STENCIL_BIT) > 0)
                {
                    var depthStencil = attachment.AsDepthAndStencil(0f, 0);
                    clearValues.Add(depthStencil);
                }
            }

            return clearValues;
        }

        private static MgColor4f ExtractColor(Color value)
        {
            var color = value.ToVector4();
            return new MgColor4f { R = color.X, G = color.Y, B = color.Z, A = color.W };
        }

        private static Vector4 AsVector4(Color value)
        {
            var color = value.ToVector4();
            return new Vector4(color.X, color.Y, color.Z, color.W );
        }

        public void Draw(IMgTexture texture, Vector2 position, Rectangle? sourceRect, Color color, float rotation, Vector2 origin, float scale, SpriteEffects flip, float depth)
        {
            var slotIndex = (uint) mTextures.Count;
            mTextures.Add(texture);

            // SHOULD BE Transform -> Rotate - Scale

            var scaleM = Matrix.CreateScale(scale);
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, depth));          
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var w = texture.Width * scale;
            var h = texture.Height * scale;
            if (sourceRect.HasValue)
            {
                w = sourceRect.Value.Width * scale;
                h = sourceRect.Value.Height * scale;
            }

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = slotIndex,
                Width = texture.Width,
                Height = texture.Height,
                SourceRect = sourceRect,
                DestinationRect = new Vector4(position.X, position.Y, w, h),
                Color = AsVector4(color),
                Origin = origin,
                Transform = final,
                Scale = scale,
                Depth = depth,
                Rotation = rotation,                
                SpriteEffect = flip,                
            };
            mRenderer.DrawInternal(item);
        }

        public void Draw(IMgTexture texture, Vector2 position, Color mask)
        {
            var slotIndex = (uint)mTextures.Count;
            mTextures.Add(texture);

            // SHOULD BE Transform -> Rotate - Scale

            var scaleM = Matrix.Identity;
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, 0));
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var w = texture.Width;
            var h = texture.Height;

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = slotIndex,
                Width = texture.Width,
                Height = texture.Height,
                DestinationRect = new Vector4(position.X, position.Y, w, h),
                SourceRect = null,
                TopLeft = new Vector2(0,0),
                BottomRight = new Vector2(1, 1),
                Color = AsVector4(mask),
                Origin = Vector2.Zero,
                Transform = final,
                Scale = 1f,
                Depth = 0f,
                Rotation = 0f,
                SpriteEffect = 0,
            };
            mRenderer.DrawInternal(item);
        }

        public void Draw(uint textureSlot, Vector2 position, Color mask)
        {
            var scaleM = Matrix.Identity;
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, 0));
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = textureSlot,
                SourceRect = null,
                TopLeft = new Vector2(0, 0),
                BottomRight = new Vector2(1, 1),
                Color = AsVector4(mask),
                Origin = Vector2.Zero,
                Transform = final,
                Scale = 1f,
                Depth = 0f,
                Rotation = 0f,
                SpriteEffect = 0,
            };
            mRenderer.DrawInternal(item);
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
            if (mDescriptorSet == null)
            {
                return;
            }

            const uint FIRST_TEXTURE_BINDING = 0;
            if (mTextures.Count > 0)
            {
                mDescriptorSet.SetTextures(FIRST_TEXTURE_BINDING, 0, mTextures.ToArray());
            }

            mDescriptorSet.End();

            mRenderer.Update();

            if (mIndirectBuffer != null)
            {
                mIndirectBuffer.Update(mCommand);
            }
        }
    }
}
