using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

namespace Krab.ShapeSynth.Unity
{
    /// <summary>
    /// UCanvas with an explicit SRP camera boundary. Owns the Unity messages instead of
    /// UCanvas's all-camera callbacks. Uses only public p5.Unite drawing/context APIs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TargetCameraCanvas : UCanvas
    {
        [SerializeField] private Camera _targetCamera;
        private DrawingContext _context;
        public Camera TargetCamera { get => _targetCamera; set => _targetCamera = value; }
        public int SubmittedCameraFrames { get; private set; }
        public int IgnoredCameraFrames { get; private set; }

        public void Initialize(Camera camera)
        {
            _targetCamera = camera;
            _context = CreateCanvas(DrawingMode.ScreenOverlay);
        }

        // These messages deliberately replace UCanvas's private lifecycle messages.
        private void OnEnable() => RenderPipelineManager.beginCameraRendering += RenderCamera;
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= RenderCamera;
            _context?.CaptureCommands();
            _context?.CaptureCommands(); // Discard pending and captured commands on disable.
            ResetMatrix();
        }
        private void LateUpdate() => _context?.CaptureCommands();
        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= RenderCamera;
            _context?.Dispose();
            _context = null;
        }
        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!_targetCamera || camera != _targetCamera)
            {
                IgnoredCameraFrames++;
                return;
            }
            if (_context == null) return;
            _context.Draw(camera);
            SubmittedCameraFrames++;
        }
    }
}
