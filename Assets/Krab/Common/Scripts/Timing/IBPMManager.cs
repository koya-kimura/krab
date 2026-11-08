using System;

namespace Krab.Common.Timing
{
    public interface IBPMManager
    {
        public event Action OnBeat;
        public double Beat { get; }
        public float CurrentBpm { get; }
        public void SetBpm(float bpm);
    }
}
