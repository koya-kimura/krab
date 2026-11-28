using System;
using System.IO;
using System.Linq;
using Krab.Common.Midi.Devices.ApcMiniMk2;
using Krab.Common.Midi.Devices.Mpd218;
using Krab.Common.Midi.Input;
using Krab.Common.Midi.Output;
using Krab.Common.Midi.Views;
using Krab.Common.Output;
using Krab.Common.Output.Layers;
using Krab.ImageLayer.Performance;
using Krab.ImageLayer.Unity.Rendering;
using Krab.ShapeSynth.Performance;
using Krab.ShapeSynth.Unity;
using Krab.ShapeSynth.Unity.Rendering;
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

namespace Krab.Shows.ShirtMeetsLausbub.Editor
{
    /// <summary>
    /// Puts the TypeLayout, ShapeSynth and ImageLayer layers on the output camera, each on its own overlay camera,
    /// under one <see cref="LayerMixer"/>. Builds the sample Scene and sets up performance Scenes the same way.
    /// </summary>
    public static class LayerSceneSetup
    {
        public const string SampleScenePath = "Assets/Krab/Shows/2612_shirt-meets-lausbub/Samples/LayerMixer.unity";

        private const string ImageShaderPath = "Assets/Krab/Modules/ImageLayer/Runtime/Unity/Shaders/ImageLayerImage.shader";
        private const int OrderStep = 10;

        private static readonly string[] CommonPrefabs =
        {
            "Assets/Krab/Common/Prefabs/SceneContainer.prefab",
            "Assets/Krab/Common/Prefabs/UiBuilder.prefab",
            "Assets/Krab/Common/Prefabs/OutputSender.prefab",
            "Assets/Krab/Common/Prefabs/OutputPreview.prefab",
            "Assets/Krab/Common/Prefabs/ApplicationFrameSetting.prefab",
            "Assets/Krab/Common/Prefabs/BPMManager.prefab"
        };

        [MenuItem("Krab/Layers/Create Sample Scene")]
        public static void CreateSampleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(SampleScenePath))
                throw new InvalidOperationException($"{SampleScenePath} must not exist; create in Edit Mode.");

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var outputCamera = CreateOutputCamera();
                CreateCommon();
                AddLayers(outputCamera);

                Directory.CreateDirectory(Path.GetDirectoryName(SampleScenePath)!);
                if (!EditorSceneManager.SaveScene(scene, SampleScenePath)) throw new IOException("Could not save Scene.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Krab/Layers/Add Layers To Active Scene")]
        public static void AddLayersToActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            var outputs = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OutputTextureProvider>(true))
                .ToArray();
            if (outputs.Length != 1)
                throw new InvalidOperationException($"{scene.path} needs exactly one {nameof(OutputTextureProvider)}.");

            AddLayers(outputs[0].GetComponent<Camera>());
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Front to back: TypeLayout, ShapeSynth, ImageLayer. The output camera only clears to black.</summary>
        public static LayerMixer AddLayers(Camera outputCamera)
        {
            if (!outputCamera) throw new ArgumentNullException(nameof(outputCamera));

            var roots = outputCamera.gameObject.scene.GetRootGameObjects();
            if (roots.Any(root => root.GetComponentInChildren<LayerMixer>(true)))
                throw new InvalidOperationException($"The Scene already has a {nameof(LayerMixer)}.");

            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.GetUniversalAdditionalCameraData().cameraStack.Clear();

            var root = new GameObject("Layers");
            var mixer = root.AddComponent<LayerMixer>();
            var layers = new[]
            {
                ("Type Layout", CreateLayer("Type Layout", root.transform, outputCamera, AddTypeLayout)),
                ("Shape Synth", CreateLayer("Shape Synth", root.transform, outputCamera, AddShapeSynth)),
                ("Image Layer", CreateLayer("Image Layer", root.transform, outputCamera, AddImageLayer))
            };

            var serialized = new SerializedObject(mixer);
            var entries = serialized.FindProperty("_layers");
            entries.arraySize = layers.Length;
            for (int index = 0; index < layers.Length; index++)
            {
                var (name, layer) = layers[index];
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_name").stringValue = name;
                entry.FindPropertyRelative("_camera").objectReferenceValue = layer;

                // The mixer assigns the same orders at runtime; saving them shows the stack in Edit Mode too.
                layer.Order = (layers.Length - 1 - index) * OrderStep;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            var stack = outputCamera.GetUniversalAdditionalCameraData().cameraStack;
            foreach (var (_, layer) in layers.Reverse())
                stack.Add(layer.GetComponent<Camera>());

            return mixer;
        }

        private static OverlayLayerCamera CreateLayer(string name, Transform parent, Camera outputCamera,
            Action<GameObject, Camera> addContent)
        {
            var layerObject = new GameObject(name, typeof(Camera));
            layerObject.transform.SetParent(parent, false);
            var camera = layerObject.GetComponent<Camera>();
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            var layer = layerObject.AddComponent<OverlayLayerCamera>();
            layer.BaseCamera = outputCamera;
            addContent(layerObject, camera);
            return layer;
        }

        private static void AddTypeLayout(GameObject layerObject, Camera camera)
        {
            layerObject.AddComponent<TypeLayoutCanvas>().TargetCamera = camera;
            layerObject.AddComponent<TypeLayoutFont>();
            SetReference(layerObject.AddComponent<TypeLayoutPerformance>(), "_outputCamera", camera);
        }

        private static void AddShapeSynth(GameObject layerObject, Camera camera)
        {
            layerObject.AddComponent<TargetCameraCanvas>().TargetCamera = camera;
            SetReference(layerObject.AddComponent<ShapeSynthController>(), "_outputCamera", camera);
            layerObject.AddComponent<ShapeSynthPerformance>();
        }

        private static void AddImageLayer(GameObject layerObject, Camera camera)
        {
            var images = layerObject.AddComponent<ImageLayerCanvas>();
            images.Shader = AssetDatabase.LoadAssetAtPath<Shader>(ImageShaderPath);
            if (!images.Shader) throw new FileNotFoundException("ImageLayer shader is missing.", ImageShaderPath);

            SetReference(layerObject.AddComponent<ImageLayerPerformance>(), "_images", images);
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Camera CreateOutputCamera()
        {
            var outputObject = new GameObject("Main Camera", typeof(Camera));
            outputObject.tag = "MainCamera";
            outputObject.AddComponent<OutputTextureProvider>();
            return outputObject.GetComponent<Camera>();
        }

        // Shared services, MIDI with on-screen stand-ins for the pads, and an EventSystem for the settings UI.
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

            _ = new GameObject("MIDI", typeof(MidiOutput), typeof(MidiInputManager), typeof(ApcMiniMk2Simulator),
                typeof(Mpd218Simulator), typeof(MidiBindingUi));
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
