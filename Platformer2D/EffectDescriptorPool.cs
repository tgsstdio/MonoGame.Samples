using Magnesium;
using System.Diagnostics;

namespace Platformer2D
{
    public class EffectDescriptorPool
    {
        public IMgDescriptorPool DescriptorPool { get; internal set; }
        public IMgDescriptorSetLayout DescriptorSetLayout { get; internal set; }
        public IMgPipelineLayout PipelineLayout { get; internal set; }
        public IMgDevice Device { get; internal set; }
        public uint MaxNoOfDescriptorSets { get; internal set; }

        public EffectPipelineDescriptorSet CreateDescriptorSet()
        {
            IMgDescriptorSet[] dSets;
            var allocateInfo = new MgDescriptorSetAllocateInfo
            {
                DescriptorPool = DescriptorPool,
                DescriptorSetCount = 1,
                SetLayouts = new[]
                {
                    DescriptorSetLayout,
                }
            };
            var err = Device.AllocateDescriptorSets(allocateInfo, out dSets);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            var effectSet = new EffectPipelineDescriptorSet(Device, dSets[0], PipelineLayout);
            return effectSet;
        }
    }
}
