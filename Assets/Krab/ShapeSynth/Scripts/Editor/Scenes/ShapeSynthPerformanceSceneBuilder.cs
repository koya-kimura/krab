using System;
using System.IO;
using System.Linq;
using Krab.Common.Output;
using Krab.ShapeSynth.Performance;
using Krab.ShapeSynth.Unity.Rendering;
using Krab.ShapeSynth.Unity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Krab.ShapeSynth.Editor.Scenes
{
    public static class ShapeSynthPerformanceSceneBuilder
    {
        public const string ScenePath = "Assets/Krab/ShapeSynth/Samples/ShapeSynthPerformance.unity";

        [MenuItem("Krab/Shape Synth/Create Performance Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath))
            {
                throw new InvalidOperationException("Performance Scene must not exist; create in Edit Mode.");
            }

            var source = SceneManager.GetSceneByPath(ShapeSynthSmokeSceneBuilder.ScenePath);
            var opened = !source.IsValid() || !source.isLoaded;
            var previous = SceneManager.GetActiveScene();
            var scratchPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Krab/ShapeSynth/Samples/PerformanceScratch.unity");
            var scratch = default(Scene);
            try
            {
                if (opened)
                {
                    source = EditorSceneManager.OpenScene(
                        ShapeSynthSmokeSceneBuilder.ScenePath, OpenSceneMode.Additive);
                }

                if (source.isDirty)
                {
                    throw new InvalidOperationException("Smoke Scene has unsaved changes.");
                }

                if (!EditorSceneManager.SaveScene(source, scratchPath, true))
                {
                    throw new IOException("Could not copy smoke Scene.");
                }


                scratch = EditorSceneManager.OpenScene(scratchPath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scratch);

                // Only the scratch copy loses the test demos; Common, DI, Camera and Output stay intact.
                var synth = scratch.GetRootGameObjects().Single(root => root.name == "ShapeSynth");
                UnityEngine.Object.DestroyImmediate(synth);
                synth = new GameObject("ShapeSynth Performance");
                SceneManager.MoveGameObjectToScene(synth, scratch);
                var output = scratch.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true))
                    .Single();
                var camera = output.GetComponent<Camera>();
                camera.backgroundColor = Color.black;
                var canvas = synth.AddComponent<TargetCameraCanvas>();
                canvas.TargetCamera = camera;
                var controller = synth.AddComponent<ShapeSynthController>();
                var settings = new SerializedObject(controller);
                settings.FindProperty("_outputCamera").objectReferenceValue = camera;
                settings.FindProperty("_invertTargetY").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                synth.AddComponent<ShapeSynthPerformance>();

                if (!EditorSceneManager.SaveScene(scratch, ScenePath))
                {
                    throw new IOException("Could not save performance Scene.");
                }
            }
            finally
            {
                if (scratch.IsValid() && scratch.isLoaded)
                {
                    EditorSceneManager.CloseScene(scratch, true);
                }

                if (opened && source.IsValid() && source.isLoaded)
                {
                    EditorSceneManager.CloseScene(source, true);
                }

                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }

                AssetDatabase.DeleteAsset(scratchPath);
            }
        }
    }
}
