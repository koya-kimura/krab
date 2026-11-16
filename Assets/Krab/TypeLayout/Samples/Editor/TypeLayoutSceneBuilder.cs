using System;
using System.IO;
using Krab.Common.Output;
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

namespace Krab.TypeLayout.Samples.Editor
{
    public static class TypeLayoutSceneBuilder
    {
        public const string SmokeTestScenePath = "Assets/Krab/TypeLayout/Samples/TypeLayoutSmokeTest.unity";
        public const string PerformanceScenePath = "Assets/Krab/TypeLayout/Samples/TypeLayoutPerformance.unity";

        private static readonly string[] CommonPrefabs =
        {
            "Assets/Krab/Common/Prefabs/SceneContainer.prefab",
            "Assets/Krab/Common/Prefabs/UiBuilder.prefab",
            "Assets/Krab/Common/Prefabs/OutputSender.prefab",
            "Assets/Krab/Common/Prefabs/OutputPreview.prefab",
            "Assets/Krab/Common/Prefabs/ApplicationFrameSetting.prefab",
            "Assets/Krab/Common/Prefabs/BPMManager.prefab"
        };

        [MenuItem("Krab/Type Layout/Create Smoke Test Scene")]
        public static void CreateSmokeTest()
        {
            Create(SmokeTestScenePath, typeof(TypeLayoutSmokeTest));
        }

        [MenuItem("Krab/Type Layout/Create Performance Scene")]
        public static void CreatePerformance()
        {
            Create(PerformanceScenePath, typeof(TypeLayoutPerformance));
        }

        private static void Create(string scenePath, Type controllerType)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(scenePath))
                throw new InvalidOperationException($"{scenePath} must not exist; create in Edit Mode.");

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = CreateOutputCamera();
                CreateCommon();
                CreateEventSystem();
                CreateTypeLayout(camera, controllerType);

                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Could not save Scene.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Camera CreateOutputCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.GetUniversalAdditionalCameraData();
            cameraObject.AddComponent<OutputTextureProvider>();
            return camera;
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

        private static void CreateTypeLayout(Camera camera, Type controllerType)
        {
            var typeLayout = new GameObject("Type Layout", typeof(TypeLayoutCanvas), typeof(TypeLayoutFont),
                controllerType);
            typeLayout.GetComponent<TypeLayoutCanvas>().TargetCamera = camera;
            var controller = new SerializedObject(typeLayout.GetComponent(controllerType));
            controller.FindProperty("_outputCamera").objectReferenceValue = camera;
            controller.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
