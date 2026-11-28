namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>The minimal common surface of the supported immutable Shape and Group definitions.</summary>
    public interface INodeDefinition
    {
        TransformDefinition Transform { get; }
    }
}
