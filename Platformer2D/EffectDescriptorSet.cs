using Magnesium;
using MonoGame.Graphics;
using System.Collections.Generic;

namespace Platformer2D
{
    // HOLDS ALL UPDATES 
    public class EffectDescriptorSet
    {
        public EffectDescriptorSet(IMgDescriptorSet dSet)
        {
            mDescriptorSet = dSet;
            mWrites = new List<MgWriteDescriptorSet>();
        }

        private List<MgWriteDescriptorSet> mWrites;

        private IMgDescriptorSet mDescriptorSet;
        public IMgDescriptorSet DescriptorSet
        {
            get
            {
                return mDescriptorSet;
            }
        }

        public void Begin()
        {
            mWrites.Clear();
        }

        public void SetTextures(uint binding, uint first, MgTexture[] textures)
        {
            if (textures != null)
            {
                var writeItem = new MgWriteDescriptorSet
                {
                    DescriptorCount = (uint) textures.Length,
                    DescriptorType = MgDescriptorType.COMBINED_IMAGE_SAMPLER,
                    DstArrayElement = first,
                    DstBinding = binding,   
                    DstSet = mDescriptorSet,
                };

                var imageItems = new List<MgDescriptorImageInfo>();
                foreach(var texture in textures)
                {
                    var image = new MgDescriptorImageInfo
                    {
                        // ASSUME IT ALWAYS GOING TO BE LIKE THIS
                        ImageLayout = MgImageLayout.SHADER_READ_ONLY_OPTIMAL,
                        ImageView = texture.View,
                        Sampler = texture.Sampler,
                    };
                    imageItems.Add(image);
                }
                writeItem.ImageInfo = imageItems.ToArray();

                mWrites.Add(writeItem);
            }
        }

        public void SetConstantBuffers(uint binding, uint first, MgDescriptorBufferInfo[] buffers)
        {
            if (buffers != null)
            {
                var writeItem = new MgWriteDescriptorSet
                {
                    DescriptorCount = (uint)buffers.Length,
                    DescriptorType = MgDescriptorType.COMBINED_IMAGE_SAMPLER,
                    DstArrayElement = first,
                    DstBinding = binding,
                    DstSet = mDescriptorSet,
                    BufferInfo = buffers,
                };

                mWrites.Add(writeItem);
            }
        }

        public void End(IMgDevice device)
        {
            device.UpdateDescriptorSets(mWrites.ToArray(), null);
        }
    }
}
