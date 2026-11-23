using System;
using System.IO;
using Krab.Common.Midi.Devices.ApcMiniMk2;
using Krab.Common.Midi.Input;
using Krab.Common.Midi.Output;
using Krab.Common.Midi.Views;
using Krab.Common.Output;
using Krab.ImageLayer.Performance;
using Krab.ImageLayer.Unity.Rendering;
using Krab.TypeLayout.Performance;
using Krab.TypeLayout.Unity.Fonts;
using Krab.TypeLayout.Unity.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Krab.ImageLayer.Samples.Editor
{
    /// <summary>Builds the ImageLayer sample Scenes: output camera plus one overlay camera per layer.</summary>
    public static class ImageLayerSceneBuilder
    {
        public const string SmokeTestScenePath = "Assets/Krab/ImageLayer/Samples/ImageLayerSmokeTest.unity";
        public const string PerformanceScenePath = "Assets/Krab/ImageLayer/Samples/ImageLayerPerformance.unity";
        public const string ShaderPath = "Assets/Krab/ImageLayer/Scripts/Unity/Shaders/ImageLayerImage.shader";

        private static readonly string[] CommonPrefabs =
        {
            "Assets/Krab/Common/Prefabs/SceneContainer.prefab",
            "Assets/Krab/Common/Prefabs/UiBuilder.prefab",
            "Assets/Krab/Common/Prefabs/OutputSender.prefab",
            "Assets/Krab/Common/Prefabs/OutputPreview.prefab",
            "Assets/Krab/Common/Prefabs/ApplicationFrameSetting.prefab",
            "Assets/Krab/Common/Prefabs/BPMManager.prefab"
        };

        [MenuItem("Krab/Image Layer/Create Smoke Test Scene")]
        public static void CreateSmokeTest()
        {
            Create(SmokeTestScenePath, BuildSmokeTest);
        }

        [MenuItem("Krab/Image Layer/Create Performance Scene")]
        public static void CreatePerformance()
        {
            Create(PerformanceScenePath, BuildPerformance);
        }

        /// <summary>Creates a Scene with the shared rig, lets <paramref name="build"/> add the content, saves it.</summary>
        public static void Create(string scenePath, Action<Rig> build)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(scenePath))
                throw new InvalidOperationException($"{scenePath} must not exist; create in Edit Mode.");

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var rig = CreateRig();
                CreateCommon();
                CreateEventSystem();
                build(rig);

                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Could not save Scene.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildSmokeTest(Rig rig)
        {
            var background = CreateCanvasObject("Background Canvas", rig.OutputCamera);
            var foreground = CreateCanvasObject("Foreground Canvas", rig.ForegroundCamera);
            var sample = new GameObject("Image Layer Smoke Test");
            var smokeTest = sample.AddComponent<ImageLayerSmokeTest>();
            var serialized = new SerializedObject(smokeTest);
            serialized.FindProperty("_outputCamera").objectReferenceValue = rig.OutputCamera;
            serialized.FindProperty("_background").objectReferenceValue = background;
            serialized.FindProperty("_images").objectReferenceValue = rig.Images;
            serialized.FindProperty("_foreground").objectReferenceValue = foreground;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Images and TypeLayout text, each on its own overlay camera above the output camera's clear color.
        private static void BuildPerformance(Rig rig)
        {
            var images = new GameObject("Image Layer Performance");
            var performance = images.AddComponent<ImageLayerPerformance>();
            var serializedImages = new SerializedObject(performance);
            serializedImages.FindProperty("_images").objectReferenceValue = rig.Images;
            serializedImages.ApplyModifiedPropertiesWithoutUndo();

            // MIDI pads switch the deck radios; the on-screen APC mini stands in when no controller is connected.
            _ = new GameObject("MIDI", typeof(MidiOutput), typeof(MidiInputManager), typeof(ApcMiniMk2Simulator),
                typeof(MidiBindingUi));

            var typeLayout = new GameObject("Type Layout", typeof(TypeLayoutCanvas), typeof(TypeLayoutFont),
                typeof(TypeLayoutPerformance));
            typeLayout.GetComponent<TypeLayoutCanvas>().TargetCamera = rig.ForegroundCamera;
            var serializedText = new SerializedObject(typeLayout.GetComponent<TypeLayoutPerformance>());
            serializedText.FindProperty("_outputCamera").objectReferenceValue = rig.ForegroundCamera;
            serializedText.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Rig CreateRig()
        {
            var outputObject = new GameObject("Main Camera", typeof(Camera));
            outputObject.tag = "MainCamera";
            var outputCamera = outputObject.GetComponent<Camera>();
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.GetUniversalAdditionalCameraData();
            outputObject.AddComponent<OutputTextureProvider>();

            var imageCamera = CreateOverlayCamera("Image Layer Camera", outputCamera, 0);
            var images = imageCamera.gameObject.AddComponent<ImageLayerCanvas>();
            images.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!images.Shader) throw new FileNotFoundException("ImageLayer shader is missing.", ShaderPath);

            var foregroundCamera = CreateOverlayCamera("Foreground Camera", outputCamera, 10);
            return new Rig(outputCamera, images, foregroundCamera);
        }

        private static Camera CreateOverlayCamera(string name, Camera outputCamera, int order)
        {
            var cameraObject = new GameObject(name, typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            var layer = cameraObject.AddComponent<OverlayLayerCamera>();
            layer.BaseCamera = outputCamera;
            layer.Order = order;

            // Saved in the stack as well, so the Scene shows the order in Edit Mode before anything enables.
            outputCamera.GetUniversalAdditionalCameraData().cameraStack.Add(camera);
            return camera;
        }

        private static TypeLayoutCanvas CreateCanvasObject(string name, Camera camera)
        {
            var canvasObject = new GameObject(name, typeof(TypeLayoutCanvas));
            var canvas = canvasObject.GetComponent<TypeLayoutCanvas>();
            canvas.TargetCamera = camera;
            return canvas;
        }

        private static void CreateCommon()
        {
            var common = new GameObject("Common");
            foreach (var path in CommonPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) throw new FileNotFoundException("Common prefab is missing.", path);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(common.transform, false);
            }
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public readonly struct Rig
        {
            public Rig(Camera outputCamera, ImageLayerCanvas images, Camera foregroundCamera)
            {
                OutputCamera = outputCamera;
                Images = images;
                ForegroundCamera = foregroundCamera;
            }

            /// <summary>Base camera rendering into the output texture; background drawings go here.</summary>
            public Camera OutputCamera { get; }

            /// <summary>Image layer on its own overlay camera (order 0).</summary>
            public ImageLayerCanvas Images { get; }

            /// <summary>Overlay camera above the images (order 10), for text or shapes in front.</summary>
            public Camera ForegroundCamera { get; }
        }
    }
}
