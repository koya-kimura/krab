using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Krab.Common.Output.Layers
{
    /// <summary>
    /// Makes this camera an Overlay in the output camera's URP stack, so its drawings land on the output texture
    /// in <see cref="Order"/> above the base camera. p5.Unite canvases sharing one camera have no defined order
    /// between them, so each layer gets its own camera. Disabling this component hides the layer.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
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

        // Runs before the drawing scripts. An overlay renders into the base camera's target, but without a target of
        // its own its pixelWidth/Height report the Game view, so drawings sized from it would not match the output.
        private void Update()
        {
            SyncTarget();
        }

        private void OnEnable()
        {
            if (!_baseCamera)
            {
                Debug.LogError($"{nameof(OverlayLayerCamera)} requires a base {nameof(Camera)}.", this);
                return;
            }

            _camera.enabled = true;
            SyncTarget();
            var stack = Stack;
            if (!stack.Contains(_camera)) stack.Add(_camera);

            // OnEnable order between GameObjects is not defined, so the stack is sorted by the explicit order.
            SortStack();
        }

        private void OnDisable()
        {
            _camera.enabled = false;
            if (!_baseCamera) return;

            Stack.Remove(_camera);
            _camera.targetTexture = null;
        }

        public Camera BaseCamera
        {
            get => _baseCamera;
            set => _baseCamera = value;
        }

        /// <summary>Changing it while enabled moves the camera in the stack right away.</summary>
        public int Order
        {
            get => _order;
            set
            {
                _order = value;
                if (isActiveAndEnabled && _baseCamera) SortStack();
            }
        }

        public Camera Camera => _camera;

        private List<Camera> Stack => _baseCamera.GetUniversalAdditionalCameraData().cameraStack;

        private void SyncTarget()
        {
            if (!_baseCamera || _camera.targetTexture == _baseCamera.targetTexture) return;

            _camera.targetTexture = _baseCamera.targetTexture;
        }

        // Insertion sort keeps equal orders in their current places; List.Sort is not stable.
        private void SortStack()
        {
            var stack = Stack;
            stack.RemoveAll(camera => !camera);
            for (int index = 1; index < stack.Count; index++)
            {
                var camera = stack[index];
                int order = OrderOf(camera);
                int position = index - 1;
                while (position >= 0 && OrderOf(stack[position]) > order)
                {
                    stack[position + 1] = stack[position];
                    position--;
                }

                stack[position + 1] = camera;
            }
        }

        // Overlays that are not layers stay at the back, below every layer.
        private static int OrderOf(Camera camera)
        {
            return camera.TryGetComponent<OverlayLayerCamera>(out var layer) ? layer._order : int.MinValue;
        }
    }
}
