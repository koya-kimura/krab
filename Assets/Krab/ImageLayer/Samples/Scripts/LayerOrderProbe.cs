using System.Collections;
using System.Text;
using Krab.TypeLayout.Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Krab.ImageLayer.Samples
{
    /// <summary>
    /// Phase 0 probe. Draws a p5.Unite background, an image quad and a p5.Unite foreground, then reads the
    /// output texture to check that they stack in that order. Compares one camera per layer (URP camera stack)
    /// with all layers on a single camera.
    /// </summary>
    public sealed class LayerOrderProbe : MonoBehaviour
    {
        private const string LogPrefix = "[LayerOrderProbe]";
        private const int Width = 512;
        private const int Height = 256;
        private const int WarmupFrames = 5;
        private const int SampleFrames = 30;

        private RenderTexture _output;
        private Camera _baseCamera;
        private Camera _imageCamera;
        private Camera _frontCamera;
        private TypeLayoutCanvas _backCanvas;
        private TypeLayoutCanvas _frontCanvas;
        private Mesh _quad;
        private Material _imageMaterial;
        private Texture2D _imageTexture;
        private Texture2D _readback;
        private Camera _imageTarget;

        private void Start()
        {
            _output = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            _output.Create();
            _readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _quad = CreateQuad();
            _imageTexture = CreateSolidTexture(Color.green);
            _imageMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = _imageTexture };

            _baseCamera = CreateCamera("Probe Base", CameraRenderType.Base);
            _baseCamera.targetTexture = _output;
            _imageCamera = CreateCamera("Probe Image", CameraRenderType.Overlay);
            _frontCamera = CreateCamera("Probe Front", CameraRenderType.Overlay);
            var stack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            stack.Add(_imageCamera);
            stack.Add(_frontCamera);

            StartCoroutine(RunAsync());
        }

        private void Update()
        {
            if (_backCanvas == null || _imageTarget == null) return;

            _backCanvas.NoStroke();
            _backCanvas.Fill(Color.red);
            _backCanvas.Rect(0f, 0f, Width, Height);

            float imageSize = Height * 0.6f;
            var renderParams = new RenderParams(_imageMaterial) { camera = _imageTarget };
            Graphics.RenderMesh(renderParams, _quad, 0, Matrix4x4.Scale(new Vector3(imageSize, imageSize, 1f)));

            _frontCanvas.NoStroke();
            _frontCanvas.Fill(Color.blue);
            _frontCanvas.Circle(Width * 0.5f, Height * 0.5f, Height * 0.3f);
        }

        private void OnDestroy()
        {
            if (_output != null)
            {
                _output.Release();
                Destroy(_output);
            }

            Destroy(_readback);
            Destroy(_quad);
            Destroy(_imageMaterial);
            Destroy(_imageTexture);
        }

        private IEnumerator RunAsync()
        {
            var report = new StringBuilder();
            yield return MeasureAsync("Camera stack", _imageCamera, _frontCamera, report);
            yield return MeasureAsync("Single camera", _baseCamera, _baseCamera, report);
            Debug.Log($"{LogPrefix}\n{report}", this);
        }

        private IEnumerator MeasureAsync(string label, Camera imageCamera, Camera frontCamera, StringBuilder report)
        {
            _backCanvas = CreateCanvas("Back Canvas", _baseCamera);
            _frontCanvas = CreateCanvas("Front Canvas", frontCamera);
            _imageTarget = imageCamera;

            for (int frame = 0; frame < WarmupFrames; frame++)
                yield return null;

            int passed = 0;
            string firstFailure = null;
            for (int frame = 0; frame < SampleFrames; frame++)
            {
                yield return new WaitForEndOfFrame();

                string result = Sample();
                if (result == null)
                    passed++;
                else
                    firstFailure ??= result;
            }

            report.AppendLine($"{label}: {passed}/{SampleFrames} frames in order. {firstFailure}");

            _imageTarget = null;
            Destroy(_backCanvas.gameObject);
            Destroy(_frontCanvas.gameObject);
            _backCanvas = null;
            _frontCanvas = null;
            yield return null;
        }

        // Returns null when center = front (blue), image ring = image (green), corner = background (red).
        private string Sample()
        {
            var previous = RenderTexture.active;
            RenderTexture.active = _output;
            _readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            _readback.Apply(false);
            RenderTexture.active = previous;

            var center = _readback.GetPixel(Width / 2, Height / 2);
            var ring = _readback.GetPixel(Width / 2 + (int)(Height * 0.24f), Height / 2);
            var corner = _readback.GetPixel(4, 4);
            bool ok = Dominant(center) == 'b' && Dominant(ring) == 'g' && Dominant(corner) == 'r';
            return ok ? null : $"First mismatch: center={Dominant(center)} ring={Dominant(ring)} corner={Dominant(corner)}";
        }

        private static char Dominant(Color color)
        {
            if (color.r > color.g && color.r > color.b) return 'r';
            if (color.g > color.b) return 'g';

            return color.b > 0f ? 'b' : '-';
        }

        private Camera CreateCamera(string cameraName, CameraRenderType renderType)
        {
            var cameraObject = new GameObject(cameraName);
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var probeCamera = cameraObject.AddComponent<Camera>();
            probeCamera.orthographic = true;
            probeCamera.orthographicSize = Height * 0.5f;
            probeCamera.aspect = (float)Width / Height;
            probeCamera.clearFlags = CameraClearFlags.SolidColor;
            probeCamera.backgroundColor = Color.black;
            probeCamera.GetUniversalAdditionalCameraData().renderType = renderType;
            return probeCamera;
        }

        private TypeLayoutCanvas CreateCanvas(string canvasName, Camera targetCamera)
        {
            var canvasObject = new GameObject(canvasName);
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<TypeLayoutCanvas>();
            canvas.Initialize(targetCamera);
            return canvas;
        }

        private static Mesh CreateQuad()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
                },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = color;

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }
    }
}
