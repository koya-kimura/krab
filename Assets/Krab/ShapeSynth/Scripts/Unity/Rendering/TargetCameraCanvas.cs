using Unite.Canvas;
using Unite.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

namespace Krab.ShapeSynth.Unity.Rendering
{
    /// <summary>
    /// UCanvas with an explicit SRP camera boundary. Owns the Unity messages instead of
    /// UCanvas's all-camera callbacks. Uses only public p5.Unite drawing/context APIs.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom("Krab.ShapeSynth.Unity")]
    public sealed class TargetCameraCanvas : UCanvas
    {
        [SerializeField] private Camera _targetCamera;

        private DrawingContext _context;

        // These messages deliberately replace UCanvas's private lifecycle messages.
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += RenderCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= RenderCamera;

            // Discard pending and captured commands on disable.
            _context?.CaptureCommands();
            _context?.CaptureCommands();
            ResetMatrix();
        }

        private void LateUpdate()
        {
            _context?.CaptureCommands();
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= RenderCamera;
            _context?.Dispose();
            _context = null;
        }

        public Camera TargetCamera
        {
            get => _targetCamera;
            set => _targetCamera = value;
        }

        public int SubmittedCameraFrames { get; private set; }

        public int IgnoredCameraFrames { get; private set; }

        public void Initialize(Camera camera)
        {
            _targetCamera = camera;
            _context = CreateCanvas(DrawingMode.ScreenOverlay);
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
