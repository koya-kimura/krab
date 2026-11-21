using System;
using System.IO;
using Krab.Common.Output;
using Krab.ImageLayer.Unity.Rendering;
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
            var sample = new GameObject("Image Layer Smoke Test");
            var smokeTest = sample.AddComponent<ImageLayerSmokeTest>();
            var serialized = new SerializedObject(smokeTest);
            serialized.FindProperty("_outputCamera").objectReferenceValue = rig.OutputCamera;
            serialized.FindProperty("_background").objectReferenceValue = rig.Background;
            serialized.FindProperty("_images").objectReferenceValue = rig.Images;
            serialized.FindProperty("_foreground").objectReferenceValue = rig.Foreground;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

            var background = CreateCanvasObject("Background Canvas", outputCamera);

            var imageCamera = CreateOverlayCamera("Image Layer Camera", outputCamera, 0);
            var images = imageCamera.gameObject.AddComponent<ImageLayerCanvas>();
            images.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!images.Shader) throw new FileNotFoundException("ImageLayer shader is missing.", ShaderPath);

            var foregroundCamera = CreateOverlayCamera("Foreground Camera", outputCamera, 10);
            var foreground = CreateCanvasObject("Foreground Canvas", foregroundCamera);
            return new Rig(outputCamera, background, images, foreground);
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
            public Rig(Camera outputCamera, TypeLayoutCanvas background, ImageLayerCanvas images,
                TypeLayoutCanvas foreground)
            {
                OutputCamera = outputCamera;
                Background = background;
                Images = images;
                Foreground = foreground;
            }

            public Camera OutputCamera { get; }

            public TypeLayoutCanvas Background { get; }

            public ImageLayerCanvas Images { get; }

            public TypeLayoutCanvas Foreground { get; }
        }
    }
}
