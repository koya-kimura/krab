using System;
using System.Linq;
using Krab.Common.Output;
using Krab.ShapeSynth.Samples;
using Krab.ShapeSynth.Unity.Rendering;
using Unite.Canvas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Krab.ShapeSynth.Editor.Scenes
{
    public static class RendererSmokeSceneSetup
    {
        // Upgrade only the dedicated, saved sample; never rebuild a performance Scene.
        public static void Upgrade()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || scene.path != ShapeSynthSmokeSceneBuilder.ScenePath
                || scene.isDirty)
            {
                throw new InvalidOperationException("Open the saved smoke Scene in Edit Mode before upgrading.");
            }

            var synth = scene.GetRootGameObjects().Single(root => root.name == "ShapeSynth");
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true))
                .Single()
                .GetComponent<Camera>();
            UpgradeCanvas<ShapeSynthSmokeTest>(synth, camera);
            UpgradeCanvas<HierarchySmokeTest>(synth, camera);

            if (!synth.GetComponentInChildren<RendererSmokeTest>(true))
            {
                var go = new GameObject("Renderer Checks", typeof(TargetCameraCanvas));
                go.transform.SetParent(synth.transform, false);
                var demo = go.AddComponent<RendererSmokeTest>();
                var settings = new SerializedObject(demo);
                settings.FindProperty("_outputCamera").objectReferenceValue = camera;
                settings.FindProperty("_invertTargetY").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                go.GetComponent<TargetCameraCanvas>().TargetCamera = camera;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Could not save smoke Scene.");
            }
        }

        private static void SetOutputCamera(MonoBehaviour demo, Camera camera)
        {
            var settings = new SerializedObject(demo);
            settings.FindProperty("_outputCamera").objectReferenceValue = camera;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpgradeCanvas<T>(GameObject synth, Camera camera) where T : MonoBehaviour
        {
            var demo = synth.GetComponentInChildren<T>(true);
            var go = demo.gameObject;
            var canvas = go.GetComponent<UCanvas>();
            if (canvas is TargetCameraCanvas target)
            {
                target.TargetCamera = camera;
                SetOutputCamera(demo, camera);
                return;
            }

            var state = EditorJsonUtility.ToJson(demo);
            var shaders = canvas.ShaderLibrary;
            UnityEngine.Object.DestroyImmediate(demo);
            UnityEngine.Object.DestroyImmediate(canvas);
            var replacement = go.AddComponent<TargetCameraCanvas>();
            replacement.ShaderLibrary = shaders;
            replacement.TargetCamera = camera;
            var restored = go.AddComponent<T>();
            EditorJsonUtility.FromJsonOverwrite(state, restored);
            SetOutputCamera(restored, camera);
        }
    }
}
