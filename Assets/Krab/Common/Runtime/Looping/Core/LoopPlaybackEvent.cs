namespace Krab.Common.Looping.Core
{
    public readonly struct LoopPlaybackEvent<T>
    {
        public LoopPlaybackEvent(T payload, double position)
        {
            Payload = payload;
            Position = position;
        }

        public T Payload { get; }

        public double Position { get; }
    }
}
