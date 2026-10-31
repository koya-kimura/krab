namespace Krab.Common.Midi.Actions
{
    public readonly struct MidiActionUpdateContext
    {
        public MidiActionUpdateContext(int frameCount, long beatIndex)
        {
            FrameCount = frameCount;
            BeatIndex = beatIndex;
        }

        public int FrameCount { get; }
        public long BeatIndex { get; }
    }

    public interface IMidiActionRuntime
    {
        void Update(in MidiActionUpdateContext context);
    }
}
