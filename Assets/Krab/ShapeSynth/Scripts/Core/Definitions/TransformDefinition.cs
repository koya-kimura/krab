namespace Krab.ShapeSynth.Core.Definitions
{
    /// <summary>Immutable local T * R * S recipe. Positive rotation is counterclockwise, in degrees.</summary>
    public sealed class TransformDefinition
    {
        public ParameterDefinition PositionX { get; }
        public ParameterDefinition PositionY { get; }
        public ParameterDefinition RotationDegrees { get; }
        public ParameterDefinition ScaleX { get; }
        public ParameterDefinition ScaleY { get; }

        public TransformDefinition(ParameterDefinition positionX = null, ParameterDefinition positionY = null,
            ParameterDefinition rotationDegrees = null, ParameterDefinition scaleX = null, ParameterDefinition scaleY = null)
        {
            PositionX = positionX ?? ParameterDefinition.Constant(0);
            PositionY = positionY ?? ParameterDefinition.Constant(0);
            RotationDegrees = rotationDegrees ?? ParameterDefinition.Constant(0);
            ScaleX = scaleX ?? ParameterDefinition.Constant(1);
            ScaleY = scaleY ?? ParameterDefinition.Constant(1);
        }
    }
}
