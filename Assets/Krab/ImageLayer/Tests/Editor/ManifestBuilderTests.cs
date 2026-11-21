using System.Linq;
using Krab.ImageLayer.Core.Library;
using NUnit.Framework;

namespace Krab.ImageLayer.Tests.Editor
{
    public sealed class ManifestBuilderTests
    {
        [Test]
        public void PngsInASetFolderBecomeOneFrameClips()
        {
            var manifest = ManifestBuilder.Build("Lib", new[] { "animal/2.png", "animal/10.png", "animal/1.png" });

            Assert.That(manifest.TryGetSet("animal", out var set), Is.True);
            Assert.That(set.Kind, Is.EqualTo(SetKind.Stills));
            Assert.That(set.Clips.Select(clip => clip.Name), Is.EqualTo(new[] { "1", "2", "10" }));
            Assert.That(set[2].FramePaths, Is.EqualTo(new[] { "animal/10.png" }));
            Assert.That(manifest.Issues, Is.Empty);
        }

        [Test]
        public void SubfoldersBecomeSequenceClipsWithFramesInNaturalOrder()
        {
            var manifest = ManifestBuilder.Build("Lib", new[]
            {
                "walk/2/1.png", "walk/1/10.png", "walk/1/2.png", "walk/1/1.png", "walk/10/1.png",
                "walk/1/3.png", "walk/1/4.png", "walk/1/5.png", "walk/1/6.png", "walk/1/7.png", "walk/1/8.png",
                "walk/1/9.png",
            });

            manifest.TryGetSet("walk", out var set);
            Assert.That(set.Kind, Is.EqualTo(SetKind.Sequences));
            Assert.That(set.Clips.Select(clip => clip.Name), Is.EqualTo(new[] { "1", "2", "10" }));
            Assert.That(set[0].FrameCount, Is.EqualTo(10));
            Assert.That(set[0].FramePaths[1], Is.EqualTo("walk/1/2.png"));
            Assert.That(set[0].FramePaths[9], Is.EqualTo("walk/1/10.png"));
            Assert.That(manifest.FrameCount, Is.EqualTo(12));
            Assert.That(manifest.Issues, Is.Empty);
        }

        [Test]
        public void SetsAreSortedAndLookedUpByExactName()
        {
            var manifest = ManifestBuilder.Build("Lib", new[] { "walk/a/1.png", "car/a/1.png", "animal/1.png" });

            Assert.That(manifest.Name, Is.EqualTo("Lib"));
            Assert.That(manifest.Sets.Select(set => set.Name), Is.EqualTo(new[] { "animal", "car", "walk" }));
            Assert.That(manifest.TryGetSet("Walk", out _), Is.False);
        }

        [Test]
        public void HiddenAndMetaFilesAreIgnoredSilentlyAndBackslashesAreAccepted()
        {
            var manifest = ManifestBuilder.Build("Lib", new[]
            {
                "animal/1.png", "animal/1.png.meta", "animal/.DS_Store", ".DS_Store", "walk.meta",
                "walk\\1\\1.png", "/walk/1/2.png/",
            });

            Assert.That(manifest.Issues, Is.Empty);
            manifest.TryGetSet("walk", out var walk);
            Assert.That(walk[0].FramePaths, Is.EqualTo(new[] { "walk/1/1.png", "walk/1/2.png" }));
        }

        [Test]
        public void MisplacedAndUnsupportedFilesAreReportedAndSkipped()
        {
            var manifest = ManifestBuilder.Build("Lib", new[]
            {
                "loose.png", "walk/1/deep/1.png", "animal/photo.jpg", "animal/1.png",
            });

            Assert.That(manifest.Sets.Select(set => set.Name), Is.EqualTo(new[] { "animal" }));
            Assert.That(manifest.Issues.Select(issue => issue.Path),
                Is.EquivalentTo(new[] { "loose.png", "walk/1/deep/1.png", "animal/photo.jpg" }));
            Assert.That(manifest.Issues.All(issue => issue.Severity == IssueSeverity.Warning), Is.True);
        }

        [Test]
        public void ASetMixingStillsAndClipsIsSkippedWithAnError()
        {
            var manifest = ManifestBuilder.Build("Lib", new[] { "hand/1.png", "hand/1/1.png", "car/1/1.png" });

            Assert.That(manifest.TryGetSet("hand", out _), Is.False);
            Assert.That(manifest.TryGetSet("car", out _), Is.True);
            var issue = manifest.Issues.Single();
            Assert.That(issue.Severity, Is.EqualTo(IssueSeverity.Error));
            Assert.That(issue.Path, Is.EqualTo("hand"));
        }

        [Test]
        public void SkippedOrRepeatedFrameNumbersAreWarnedButLoaded()
        {
            var manifest = ManifestBuilder.Build("Lib", new[]
            {
                "gap/a/1.png", "gap/a/2.png", "gap/a/4.png",
                "dup/a/1.png", "dup/a/01.png", "dup/a/2.png",
                "named/a/start.png", "named/a/end.png",
                "padded/a/001.png", "padded/a/002.png",
            });

            Assert.That(manifest.Issues.Select(issue => issue.Path), Is.EquivalentTo(new[] { "gap/a", "dup/a" }));
            manifest.TryGetSet("gap", out var gap);
            Assert.That(gap[0].FrameCount, Is.EqualTo(3));
        }

        [Test]
        public void AnEmptyListingGivesAnEmptyLibrary()
        {
            var manifest = ManifestBuilder.Build("Lib", new string[0]);
            Assert.That(manifest.Sets, Is.Empty);
            Assert.That(manifest.FrameCount, Is.EqualTo(0));
        }
    }
}
