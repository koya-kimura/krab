using System;
using System.Collections.Generic;

namespace Krab.ShapeSynth.Core.Evaluation
{
    /// <summary>Voice seed and nested occurrence path; random offsets are hashed from both.</summary>
    public sealed class RandomContext
    {
        private readonly int[] _path;
        private readonly IReadOnlyList<int> _readOnlyPath;

        public int VoiceSeed { get; }
        public int LocalIndex => _path.Length == 0 ? 0 : _path[_path.Length - 1];
        public IReadOnlyList<int> Path => _readOnlyPath;

        public RandomContext(int voiceSeed = 0, params int[] path)
        {
            VoiceSeed = voiceSeed;
            if (path == null) throw new ArgumentNullException(nameof(path));

            _path = (int[])path.Clone();
            for (var i = 0; i < _path.Length; i++)
            {
                if (_path[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(path), "Path indices must be nonnegative.");
            }

            _readOnlyPath = Array.AsReadOnly(_path);
        }

        public RandomContext Child(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Path indices must be nonnegative.");

            var childPath = new int[_path.Length + 1];
            Array.Copy(_path, childPath, _path.Length);
            childPath[_path.Length] = index;
            return new RandomContext(VoiceSeed, childPath);
        }
    }
}
