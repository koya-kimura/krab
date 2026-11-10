using System;
using System.Linq;
using Krab.Common.Output;
using Krab.ShapeSynth.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Krab.ShapeSynth.Samples.Editor
{
    public static class VariationSmokeSceneSetup
    {
        public static void Upgrade()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ShapeSynthSmokeSceneBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Open the saved smoke Scene in Edit Mode before upgrading.");
            var synth = scene.GetRootGameObjects().Single(root => root.name == "ShapeSynth");
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true))
                .Single().GetComponent<Camera>();
            Configure(synth, camera);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save smoke Scene.");
        }

        public static void Configure(GameObject synth, Camera camera)
        {
            if (synth.GetComponentInChildren<VariationSmokeTest>(true))
                throw new InvalidOperationException("Variation demo already exists; settings are preserved.");
            var go = new GameObject("Variation", typeof(TargetCameraCanvas));
            go.transform.SetParent(synth.transform, false);
            go.GetComponent<TargetCameraCanvas>().TargetCamera = camera;
            var controller = go.AddComponent<ShapeSynthController>();
            var settings = new SerializedObject(controller);
            settings.FindProperty("_outputCamera").objectReferenceValue = camera;
            settings.FindProperty("_invertTargetY").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var demo = go.AddComponent<VariationSmokeTest>();
            var demoSettings = new SerializedObject(demo);
            demoSettings.FindProperty("_seed").intValue = 1234;
            demoSettings.FindProperty("_lifetimeBeats").floatValue = 128;
            demoSettings.FindProperty("_showInstructions").boolValue = true;
            demoSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
