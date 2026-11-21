using System;
using Krab.Common.Utils;
using Krab.TypeLayout.Core.Geometry;

namespace Krab.TypeLayout.Performance.Patterns
{
    /// <summary>
    /// Keeps each flicker block on one spot for its whole cycle and, when its cycle changes, picks a seeded spot on
    /// the grid that keeps clear of the spots other blocks hold. Blocks therefore never overlap; a block that finds
    /// no free spot stays hidden until its next cycle.
    /// </summary>
    internal sealed class FlickerPlacer
    {
        private const int Attempts = 32;
        private const uint PositionSalt = 0x9E37u;

        private readonly long[] _cycles;
        private readonly Rect2[] _rects;
        private readonly bool[] _placed;

        private Rect2 _area;
        private double _width;
        private double _height;
        private double _gridX;
        private double _gridY;
        private double _gap;
        private uint _seed;
        private int _count = -1;

        public FlickerPlacer(int capacity)
        {
            _cycles = new long[capacity];
            _rects = new Rect2[capacity];
            _placed = new bool[capacity];
        }

        /// <summary>
        /// Sets this frame's block size and grid. Any change other than a small width change (such as a ticking
        /// {time}) forgets every spot, so blocks re-pick on the new grid.
        /// </summary>
        public void Prepare(Rect2 area, double width, double height, double gridX, double gridY, double gap,
            uint seed, int count)
        {
            var changed = !area.Equals(_area) || height != _height || gridX != _gridX || gridY != _gridY ||
                          gap != _gap || seed != _seed || count != _count || Math.Abs(width - _width) > gap;
            _area = area;
            _width = width;
            _height = height;
            _gridX = gridX;
            _gridY = gridY;
            _gap = gap;
            _seed = seed;
            _count = count;
            if (changed) Array.Fill(_cycles, long.MinValue);
        }

        /// <summary>Top-left of the block for this cycle, or false while it has no free spot.</summary>
        public bool TryGetOrigin(int block, long cycle, out Point2 origin)
        {
            if (_cycles[block] != cycle)
            {
                _cycles[block] = cycle;
                _placed[block] = TryPick(block, cycle, out _rects[block]);
            }

            origin = new Point2(_rects[block].X, _rects[block].Y);
            return _placed[block];
        }

        private bool TryPick(int block, long cycle, out Rect2 rect)
        {
            var columns = (int)(Math.Max(0, _area.Width - _width) / _gridX) + 1;
            var rows = (int)(Math.Max(0, _area.Height - _height) / _gridY) + 1;
            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                var random = Pcg.Pcg01(new UInt4(_seed, (uint)block, unchecked((uint)cycle),
                    PositionSalt + (uint)attempt));
                var column = Math.Min(columns - 1, (int)(random.x * columns));
                var row = Math.Min(rows - 1, (int)(random.y * rows));
                rect = new Rect2(_area.Left + column * _gridX, _area.Top + row * _gridY, _width, _height);
                if (IsFree(block, rect)) return true;
            }

            rect = default;
            return false;
        }

        private bool IsFree(int block, Rect2 rect)
        {
            for (var other = 0; other < _count; other++)
            {
                if (other == block || !_placed[other] || _cycles[other] == long.MinValue) continue;
                if (Overlaps(rect, _rects[other], _gap)) return false;
            }

            return true;
        }

        private static bool Overlaps(Rect2 a, Rect2 b, double gap)
        {
            return a.Left < b.Right + gap && b.Left < a.Right + gap && a.Top < b.Bottom + gap &&
                   b.Top < a.Bottom + gap;
        }
    }
}
