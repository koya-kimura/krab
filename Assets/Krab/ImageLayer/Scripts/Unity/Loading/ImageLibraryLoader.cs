using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Krab.ImageLayer.Core.Library;
using Krab.ImageLayer.Unity.Library;
using UnityEngine;

namespace Krab.ImageLayer.Unity.Loading
{
    /// <summary>
    /// Reads a library folder and decodes its frames over several Unity frames, so the output keeps running while
    /// it loads. Problems are collected in <see cref="ImageLibrary.Issues"/> instead of stopping the load.
    /// </summary>
    public static class ImageLibraryLoader
    {
        /// <param name="onProgress">Receives 0..1 as frames finish; called on the main thread.</param>
        public static async Awaitable<ImageLibrary> LoadAsync(
            string libraryName, ImageLoadOptions options, Action<float> onProgress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(libraryName)) throw new ArgumentException("Library name is empty.");

            var total = Stopwatch.StartNew();
            string folder = ImageLibraryPaths.LibraryFolder(libraryName);
            if (!Directory.Exists(folder))
            {
                var missing = new ManifestIssue(IssueSeverity.Error, libraryName, $"Library folder not found: {folder}");
                return new ImageLibrary(libraryName, Array.Empty<ImageSet>(), new[] { missing }, 0, 0);
            }

            var manifest = ManifestBuilder.Build(libraryName, ImageLibraryPaths.ListFiles(folder));
            var issues = new List<ManifestIssue>(manifest.Issues);
            var sets = new List<ImageSet>();
            var loader = new FrameLoader(folder, options, manifest.FrameCount, onProgress, issues);
            try
            {
                foreach (var setManifest in manifest.Sets)
                {
                    var clips = new List<ImageClip>();
                    foreach (var clipManifest in setManifest.Clips)
                    {
                        var clip = await loader.LoadClipAsync(setManifest.Name, clipManifest, cancellationToken);
                        if (clip != null) clips.Add(clip);
                    }

                    if (clips.Count > 0) sets.Add(new ImageSet(setManifest.Name, setManifest.Kind, clips));
                }
            }
            catch
            {
                new ImageLibrary(libraryName, sets, issues, 0, 0).Dispose();
                loader.ReleasePending();
                throw;
            }

            onProgress?.Invoke(1f);
            return new ImageLibrary(libraryName, sets, issues, loader.TextureBytes, total.Elapsed.TotalSeconds);
        }

        private sealed class FrameLoader
        {
            private readonly string _folder;
            private readonly ImageLoadOptions _options;
            private readonly int _frameCount;
            private readonly Action<float> _onProgress;
            private readonly List<ManifestIssue> _issues;
            private readonly Stopwatch _slice = Stopwatch.StartNew();
            private readonly List<ImageFrame> _pending = new();

            private int _loadedCount;

            public FrameLoader(string folder, ImageLoadOptions options, int frameCount, Action<float> onProgress,
                List<ManifestIssue> issues)
            {
                _folder = folder;
                _options = options;
                _frameCount = Math.Max(1, frameCount);
                _onProgress = onProgress;
                _issues = issues;
            }

            public long TextureBytes { get; private set; }

            public async Awaitable<ImageClip> LoadClipAsync(
                string setName, ClipManifest manifest, CancellationToken cancellationToken)
            {
                _pending.Clear();
                foreach (var relativePath in manifest.FramePaths)
                {
                    if (_slice.Elapsed.TotalMilliseconds >= _options.FrameBudgetMilliseconds)
                    {
                        await Awaitable.NextFrameAsync(cancellationToken);
                        _slice.Restart();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var frame = TryDecode(relativePath);
                    if (frame != null) AddFrame(setName, manifest.Name, relativePath, frame);

                    _loadedCount++;
                    _onProgress?.Invoke((float)_loadedCount / _frameCount);
                }

                if (_pending.Count == 0) return null;

                var clip = new ImageClip(manifest.Name, _pending.ToArray());
                _pending.Clear();
                return clip;
            }

            public void ReleasePending()
            {
                foreach (var frame in _pending)
                    frame.Release();

                _pending.Clear();
            }

            private ImageFrame TryDecode(string relativePath)
            {
                try
                {
                    var frame = FrameDecoder.Decode(Path.Combine(_folder, relativePath), _options, out long bytes);
                    TextureBytes += bytes;
                    return frame;
                }
                catch (Exception exception) when (exception is IOException || exception is InvalidDataException ||
                                                  exception is UnauthorizedAccessException)
                {
                    _issues.Add(new ManifestIssue(IssueSeverity.Error, relativePath,
                        $"Could not load; skipped. {exception.Message}"));
                    return null;
                }
            }

            private void AddFrame(string setName, string clipName, string relativePath, ImageFrame frame)
            {
                // Mixed sizes would make the clip jump; keep the frame but say so once per clip.
                if (_pending.Count > 0 && (frame.SourceWidth != _pending[0].SourceWidth ||
                                           frame.SourceHeight != _pending[0].SourceHeight))
                {
                    if (!HasSizeIssue(setName, clipName))
                    {
                        _issues.Add(new ManifestIssue(IssueSeverity.Warning, $"{setName}/{clipName}",
                            $"Frame sizes differ ({relativePath} is {frame.SourceWidth}×{frame.SourceHeight}, " +
                            $"first is {_pending[0].SourceWidth}×{_pending[0].SourceHeight})."));
                    }
                }

                _pending.Add(frame);
            }

            private bool HasSizeIssue(string setName, string clipName)
            {
                string path = $"{setName}/{clipName}";
                foreach (var issue in _issues)
                {
                    if (issue.Path == path && issue.Message.StartsWith("Frame sizes differ", StringComparison.Ordinal))
                        return true;
                }

                return false;
            }
        }
    }
}
