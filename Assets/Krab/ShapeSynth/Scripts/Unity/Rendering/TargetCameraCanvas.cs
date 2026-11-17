using System;
using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;

namespace Krab.ShapeSynth.Unity.Rendering
{
    /// <summary>
    /// UCanvas pinned to one camera. Keeps drawing to that camera even while the Scene View is focused,
    /// so the output texture does not change with Editor window focus.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("Krab.ShapeSynth.Unity")]
    public sealed class TargetCameraCanvas : UCanvas
    {
        // Serialized-only option of UCanvas; JsonUtility reaches it without reflection.
        private const string DisableSceneViewJson = "{\"_drawInSceneView\":false}";

        [SerializeField] private Camera _targetCamera;

        // UCanvas disposes its drawing context before every assembly reload, including one during Play, and Awake
        // does not run again afterwards. NonSerialized makes the reload reset this flag together with the context.
        [NonSerialized] private bool _initialized;

        public Camera TargetCamera
        {
            get => _targetCamera;
            set => _targetCamera = value;
        }

        public void Initialize(Camera camera)
        {
            _targetCamera = camera;
            JsonUtility.FromJsonOverwrite(DisableSceneViewJson, this);
            CreateCanvas(DrawingMode.ScreenOverlay, targetCamera: camera);
            _initialized = true;
        }

        /// <summary>Recreates the canvas for the same camera after a script reload; call before drawing.</summary>
        public void EnsureInitialized()
        {
            if (!_initialized && _targetCamera) Initialize(_targetCamera);
        }
    }
}
