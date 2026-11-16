using System;
using System.IO;
using Krab.Common.Output;
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
    public static class TypeLayoutSmokeSceneBuilder
    {
        public const string ScenePath = "Assets/Krab/TypeLayout/Samples/TypeLayoutSmokeTest.unity";

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
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath))
                throw new InvalidOperationException("Smoke Test Scene must not exist; create in Edit Mode.");

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = CreateOutputCamera();
                CreateCommon();
                CreateEventSystem();
                CreateTypeLayout(camera);

                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Scene.");
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

        private static void CreateTypeLayout(Camera camera)
        {
            var typeLayout = new GameObject("Type Layout", typeof(TypeLayoutCanvas), typeof(TypeLayoutFont),
                typeof(TypeLayoutSmokeTest));
            typeLayout.GetComponent<TypeLayoutCanvas>().TargetCamera = camera;
            var smokeTest = new SerializedObject(typeLayout.GetComponent<TypeLayoutSmokeTest>());
            smokeTest.FindProperty("_outputCamera").objectReferenceValue = camera;
            smokeTest.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
