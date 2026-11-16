using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;

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

        [SerializeField] private Camera _targetCamera;

        public Camera TargetCamera
        {
            get => _targetCamera;
            set => _targetCamera = value;
        }

        public bool IsInitialized { get; private set; }

        public void Initialize(Camera camera)
        {
            _targetCamera = camera;
            JsonUtility.FromJsonOverwrite(DisableSceneViewJson, this);
            CreateCanvas(DrawingMode.ScreenOverlay, targetCamera: camera);
            IsInitialized = true;
        }
    }
}
