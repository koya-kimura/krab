using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Krab.ImageLayer.Unity.Rendering
{
    /// <summary>
    /// Makes this camera an Overlay in the output camera's URP stack, so its drawings land on the output texture
    /// in a fixed order above the base camera (and above overlays added earlier). p5.Unite canvases sharing one
    /// camera have no defined order between them, so each layer gets its own camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class OverlayLayerCamera : MonoBehaviour
    {
        [SerializeField] private Camera _baseCamera;
        [Tooltip("Stack position among overlay layers: lower draws first (further back).")]
        [SerializeField] private int _order;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
        }

        private void OnEnable()
        {
            if (!_baseCamera)
            {
                Debug.LogError($"{nameof(OverlayLayerCamera)} requires a base {nameof(Camera)}.", this);
                return;
            }

            var stack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            if (stack.Contains(_camera)) return;

            // OnEnable order between GameObjects is not defined, so insert by the explicit order instead.
            int index = stack.Count;
            for (int position = 0; position < stack.Count; position++)
            {
                var layer = stack[position] ? stack[position].GetComponent<OverlayLayerCamera>() : null;
                if (layer == null || layer._order <= _order) continue;

                index = position;
                break;
            }

            stack.Insert(index, _camera);
        }

        private void OnDisable()
        {
            if (!_baseCamera) return;

            _baseCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(_camera);
        }

        public Camera BaseCamera
        {
            get => _baseCamera;
            set => _baseCamera = value;
        }

        public int Order
        {
            get => _order;
            set => _order = value;
        }

        public Camera Camera => _camera;
    }
}
