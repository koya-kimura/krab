using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Krab.TypeLayout.Unity.Rendering
{
    /// <summary>
    /// UCanvas pinned to one camera in screen pixels (Y down). Keeps drawing to that camera while the Scene View
    /// is focused, so the output texture does not change with Editor window focus.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TypeLayoutCanvas : UCanvas
    {
        // Serialized-only option of UCanvas; JsonUtility reaches it without reflection.
        private const string DisableSceneViewJson = "{\"_drawInSceneView\":false}";

        // UCanvas has a private _targetCamera of its own; Unity rejects the same serialized name twice.
        [SerializeField, FormerlySerializedAs("_targetCamera")] private Camera _pinnedCamera;

        public Camera TargetCamera
        {
            get => _pinnedCamera;
            set => _pinnedCamera = value;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(Camera camera)
        {
            _pinnedCamera = camera;
            JsonUtility.FromJsonOverwrite(DisableSceneViewJson, this);
            CreateCanvas(DrawingMode.ScreenOverlay, targetCamera: camera);
            IsInitialized = true;
        }
    }
}
