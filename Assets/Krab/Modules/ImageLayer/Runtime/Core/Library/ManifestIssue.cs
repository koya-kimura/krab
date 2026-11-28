namespace Krab.ImageLayer.Core.Library
{
    /// <summary>A problem found while reading a library. Path is relative to the library root.</summary>
    public readonly struct ManifestIssue
    {
        public ManifestIssue(IssueSeverity severity, string path, string message)
        {
            Severity = severity;
            Path = path;
            Message = message;
        }

        public IssueSeverity Severity { get; }

        public string Path { get; }

        public string Message { get; }

        public override string ToString() => $"{Severity}: {Path}: {Message}";
    }
}
