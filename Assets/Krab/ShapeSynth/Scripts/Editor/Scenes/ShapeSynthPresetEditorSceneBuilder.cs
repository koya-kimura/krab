using System;
using System.IO;
using System.Linq;
using Krab.Common.Output;
using Krab.ShapeSynth.PresetEditor;
using Krab.ShapeSynth.Unity.Rendering;
using Krab.ShapeSynth.Unity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.Editor.Scenes
{
    public static class ShapeSynthPresetEditorSceneBuilder
    {
        public const string ScenePath = "Assets/Krab/ShapeSynth/Samples/ShapeSynthPresetEditor.unity";

        public static void ConfigureWorkspace(Scene scene)
        {
            var common = scene.GetRootGameObjects().Single(root => root.name == "Common");
            var diagnostics = common.transform.Find("[Graphy]");
            if (diagnostics) diagnostics.gameObject.SetActive(false);
            var document = common.GetComponentInChildren<UIDocument>(true);
            const string settingsPath = "Assets/Krab/ShapeSynth/Samples/Settings/WorkspacePanelSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            if (!settings)
            {
                settings = UnityEngine.Object.Instantiate(document.panelSettings);
                settings.name = "ShapeSynth Preset Workspace";
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1;
                AssetDatabase.CreateAsset(settings, settingsPath);
            }
            document.panelSettings = settings;
            PrefabUtility.RecordPrefabInstancePropertyModifications(document);
            if (diagnostics) PrefabUtility.RecordPrefabInstancePropertyModifications(diagnostics.gameObject);
        }

        [MenuItem("Krab/Shape Synth/Create Preset Editor Scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath))
                throw new InvalidOperationException("Preset Editor Scene must not exist; create in Edit Mode.");
            var source = SceneManager.GetSceneByPath(ShapeSynthSmokeSceneBuilder.ScenePath);
            var opened = !source.IsValid() || !source.isLoaded;
            var previous = SceneManager.GetActiveScene();
            var scratchPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Krab/ShapeSynth/Samples/PresetEditorScratch.unity");
            var scratch = default(Scene);
            try
            {
                if (opened) source = EditorSceneManager.OpenScene(ShapeSynthSmokeSceneBuilder.ScenePath, OpenSceneMode.Additive);
                if (source.isDirty) throw new InvalidOperationException("Smoke Scene has unsaved changes.");
                if (!EditorSceneManager.SaveScene(source, scratchPath, true)) throw new IOException("Could not copy smoke Scene.");
                scratch = EditorSceneManager.OpenScene(scratchPath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(scratch);
                // Only the scratch copy loses the test demos; Common, DI, Camera and Output stay intact.
                var synth = scratch.GetRootGameObjects().Single(root => root.name == "ShapeSynth");
                UnityEngine.Object.DestroyImmediate(synth);
                synth = new GameObject("ShapeSynth Preset Editor");
                SceneManager.MoveGameObjectToScene(synth, scratch);
                var output = scratch.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true)).Single();
                var camera = output.GetComponent<Camera>();
                camera.backgroundColor = Color.black;
                var canvas = synth.AddComponent<TargetCameraCanvas>();
                canvas.TargetCamera = camera;
                var controller = synth.AddComponent<ShapeSynthController>();
                var settings = new SerializedObject(controller);
                settings.FindProperty("_outputCamera").objectReferenceValue = camera;
                settings.FindProperty("_invertTargetY").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                synth.AddComponent<ShapeSynthPresetEditor>();
                // The production diagnostics overlay obscures this dedicated authoring workspace.
                ConfigureWorkspace(scratch);
                if (!EditorSceneManager.SaveScene(scratch, ScenePath)) throw new IOException("Could not save Preset Editor Scene.");
            }
            finally
            {
                if (scratch.IsValid() && scratch.isLoaded) EditorSceneManager.CloseScene(scratch, true);
                if (opened && source.IsValid() && source.isLoaded) EditorSceneManager.CloseScene(source, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                AssetDatabase.DeleteAsset(scratchPath);
            }
        }
    }
}
