using System;
using System.IO;
using System.Linq;
using Krab.Common.Output;
using Krab.Common.Timing;
using Unite.Canvas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Krab.ShapeSynth.Samples.Editor
{
    public static class ShapeSynthSmokeSceneBuilder
    {
        public const string ScenePath = "Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeTest.unity";
        public const string ReferenceScenePath = "Assets/Krab/Scenes/2612_shirt-meets-lausbub.unity";

        [MenuItem("Krab/Shape Synth/Create Smoke Test Scene")]
        public static void Create() => BuildFromReference(false);

        // Call explicitly with overwrite=true when intentionally rebuilding the sample.
        // The reference scene is only saved as a separate scratch copy, never modified.
        public static void BuildFromReference(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating the smoke scene.");
            if (File.Exists(ScenePath) && !overwrite)
                throw new InvalidOperationException("Smoke scene already exists; it will not be overwritten.");

            var previous = SceneManager.GetActiveScene();
            var previousPath = previous.path;
            var target = SceneManager.GetSceneByPath(ScenePath);
            if (target.IsValid() && target.isDirty)
                throw new InvalidOperationException("The smoke scene has unsaved changes.");
            var reference = SceneManager.GetSceneByPath(ReferenceScenePath);
            var openedReference = !reference.IsValid() || !reference.isLoaded;
            if (openedReference) reference = EditorSceneManager.OpenScene(ReferenceScenePath, OpenSceneMode.Additive);
            var scratch = default(Scene);
            var scratchPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Krab/ShapeSynth/Samples/ShapeSynthSmokeScratch.unity");
            try
            {
                if (reference.isDirty)
                    throw new InvalidOperationException("The reference scene has unsaved changes.");
                if (!EditorSceneManager.SaveScene(reference, scratchPath, true))
                    throw new IOException("Could not copy reference scene.");
                scratch = EditorSceneManager.OpenScene(scratchPath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scratch);
                Configure(scratch);
                if (target.IsValid() && target.isLoaded) EditorSceneManager.CloseScene(target, true);
                if (!EditorSceneManager.SaveScene(scratch, ScenePath))
                    throw new IOException("Could not save smoke scene.");
                Debug.Log("Created and saved " + ScenePath + " using the reference scene's Common setup.");
            }
            finally
            {
                if (scratch.IsValid() && scratch.isLoaded) EditorSceneManager.CloseScene(scratch, true);
                if (openedReference && reference.IsValid() && reference.isLoaded)
                    EditorSceneManager.CloseScene(reference, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                else if (previousPath == ScenePath)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                AssetDatabase.DeleteAsset(scratchPath);
            }
        }

        private static void Configure(Scene scene)
        {
            var common = scene.GetRootGameObjects().Single(root => root.name == "Common");
            // The reference contains both these root components and the BPMManager prefab.
            // Keep the prefab's configured tap input and a single IBPMManager registration.
            var duplicateTap = common.GetComponent<TapTempoController>();
            if (duplicateTap) UnityEngine.Object.DestroyImmediate(duplicateTap);
            var duplicateBpm = common.GetComponent<BPMManager>();
            if (duplicateBpm) UnityEngine.Object.DestroyImmediate(duplicateBpm);
            var bpm = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BPMManager>(true)).Single();
            var output = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true)).Single();
            var camera = output.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.05f, 1);
            camera.allowHDR = false;
            camera.allowMSAA = false;

            var synth = new GameObject("ShapeSynth");
            SceneManager.MoveGameObjectToScene(synth, scene);
            var sketchObject = new GameObject("Modulator Comparison", typeof(UCanvas));
            sketchObject.transform.SetParent(synth.transform, false);
            var sketch = sketchObject.AddComponent<ShapeSynthSmokeTest>();
            var serialized = new SerializedObject(sketch);
            serialized.FindProperty("_outputCamera").objectReferenceValue = camera;
            serialized.FindProperty("_lfoCyclesPerBeat").floatValue = 0.25f;
            serialized.FindProperty("_lfoPhaseOffset").floatValue = 0;
            serialized.FindProperty("_lfoWaveform").enumValueIndex = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Resolving Single above also validates the copied reference's BPM/output cardinality.
            if (!bpm) throw new InvalidOperationException("The reference has no BPMManager.");
        }
    }
}
