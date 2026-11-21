using System;
using System.Collections.Generic;
using Krab.ImageLayer.Core.Geometry;
using Krab.ImageLayer.Core.Placement;
using Krab.ImageLayer.Unity.Library;
using UnityEngine;

namespace Krab.ImageLayer.Unity.Rendering
{
    /// <summary>
    /// p5.js-style image drawing on one camera, in output pixels (top-left origin, Y down). Each Image() call is
    /// drawn this frame in call order; call it every frame from Update. Angles are degrees, clockwise on screen.
    /// </summary>
    public sealed class ImageCanvas : IDisposable
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int PixelMatrixId = Shader.PropertyToID("_PixelMatrix");
        private static readonly int UvScaleId = Shader.PropertyToID("_UvScale");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int DarkId = Shader.PropertyToID("_Dark");
        private static readonly int LightId = Shader.PropertyToID("_Light");
        private static readonly int DuotoneId = Shader.PropertyToID("_Duotone");
        private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");

        private readonly Material _material;
        private readonly Mesh _quad;
        private readonly List<MaterialPropertyBlock> _propertyBlocks = new();
        private readonly Stack<(Matrix4x4 Matrix, ImageStyle Style, ImageMode Mode)> _stack = new();

        private Matrix4x4 _matrix = Matrix4x4.identity;
        private ImageStyle _style = ImageStyle.Default;
        private int _frame = -1;
        private int _submitted;

        public ImageCanvas(Camera camera, Shader shader)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (shader == null) throw new ArgumentNullException(nameof(shader));

            Camera = camera;
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _quad = CreateUnitQuad();
        }

        public Camera Camera { get; }

        public int Width => Camera.pixelWidth;

        public int Height => Camera.pixelHeight;

        /// <summary>How the numbers of Image() are read, as in p5.js imageMode(). Default Corner.</summary>
        public ImageMode Mode { get; set; } = ImageMode.Corner;

        /// <summary>Tint, duotone and threshold applied to the next Image() calls.</summary>
        public ImageStyle Style
        {
            get => _style;
            set => _style = value;
        }

        /// <summary>Image() calls drawn so far in this frame.</summary>
        public int DrawCount => _frame == Time.frameCount ? _submitted : 0;

        public void Push()
        {
            _stack.Push((_matrix, _style, Mode));
        }

        public void Pop()
        {
            if (_stack.Count == 0) throw new InvalidOperationException("Pop without a matching Push.");

            (_matrix, _style, Mode) = _stack.Pop();
        }

        /// <summary>Clears transforms, styles and the Push stack. Call at the start of a frame's drawing.</summary>
        public void Reset()
        {
            _stack.Clear();
            _matrix = Matrix4x4.identity;
            _style = ImageStyle.Default;
            Mode = ImageMode.Corner;
        }

        public void Translate(float x, float y)
        {
            _matrix *= Matrix4x4.Translate(new Vector3(x, y, 0f));
        }

        public void Rotate(float degrees)
        {
            _matrix *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, degrees));
        }

        public void Scale(float scale) => Scale(scale, scale);

        public void Scale(float x, float y)
        {
            _matrix *= Matrix4x4.Scale(new Vector3(x, y, 1f));
        }

        public void Tint(Color color) => _style.Tint = color;

        public void NoTint() => _style.Tint = Color.white;

        /// <summary>Maps lightness onto dark..light. amount blends with the original colors.</summary>
        public void Duotone(Color dark, Color light, float amount = 1f)
        {
            _style.Dark = dark;
            _style.Light = light;
            _style.Duotone = Mathf.Clamp01(amount);
        }

        public void NoDuotone() => _style.Duotone = 0f;

        /// <summary>Splits the duotone at a lightness level (0..1) for a hard two-color cut. Needs Duotone.</summary>
        public void Threshold(float level, float softness = 0.02f)
        {
            _style.ThresholdEnabled = true;
            _style.ThresholdLevel = Mathf.Clamp01(level);
            _style.ThresholdSoftness = Mathf.Max(0f, softness);
        }

        public void NoThreshold() => _style.ThresholdEnabled = false;

        /// <summary>Draws the frame at its original pixel size.</summary>
        public void Image(ImageFrame frame, float x, float y)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            float width = frame.SourceWidth;
            float height = frame.SourceHeight;
            var rect = Mode == ImageMode.Corners
                ? new DrawRect(x, y, width, height)
                : ImagePlacement.Resolve(Mode, x, y, width, height);
            DrawCanvasRect(frame, rect);
        }

        /// <summary>Draws the whole original canvas into the rectangle given by <see cref="Mode"/>.</summary>
        public void Image(ImageFrame frame, float a, float b, float c, float d)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            DrawCanvasRect(frame, ImagePlacement.Resolve(Mode, a, b, c, d));
        }

        /// <summary>
        /// Scales the clip so its content (all frames together) fits the box given by <see cref="Mode"/>, then
        /// draws the frame. The size stays the same from frame to frame. align: 0 = left/top, 1 = right/bottom.
        /// </summary>
        public void ImageFit(ImageClip clip, ImageFrame frame, float a, float b, float c, float d,
            FitMode fit = FitMode.Contain, float alignX = 0.5f, float alignY = 0.5f)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var box = ImagePlacement.Resolve(Mode, a, b, c, d);
            var rect = ImagePlacement.Fit(box, clip.SourceWidth, clip.SourceHeight, clip.ContentBounds, fit, alignX,
                alignY);
            DrawCanvasRect(frame, rect);
        }

        public void Dispose()
        {
            UnityObjects.Destroy(_material);
            UnityObjects.Destroy(_quad);
        }

        private void DrawCanvasRect(ImageFrame frame, DrawRect canvasRect)
        {
            if (frame.IsEmpty || _style.Tint.a <= 0f) return;

            var content = ImagePlacement.MapContent(canvasRect, frame.Trim);
            if (content.Width <= 0 || content.Height <= 0) return;

            BeginFrameIfNeeded();
            var pixelMatrix = _matrix *
                              Matrix4x4.Translate(new Vector3((float)content.X, (float)content.Y, 0f)) *
                              Matrix4x4.Scale(new Vector3((float)content.Width, (float)content.Height, 1f));

            var properties = RentPropertyBlock();
            properties.SetTexture(MainTexId, frame.Texture);
            properties.SetMatrix(PixelMatrixId, pixelMatrix);
            properties.SetVector(UvScaleId, frame.UvScale);
            properties.SetColor(TintId, _style.Tint);
            properties.SetColor(DarkId, _style.Dark);
            properties.SetColor(LightId, _style.Light);
            properties.SetFloat(DuotoneId, _style.Duotone);
            properties.SetVector(ThresholdId, new Vector4(_style.ThresholdLevel, _style.ThresholdSoftness,
                _style.ThresholdEnabled ? 1f : 0f, 0f));

            // Transparent draws are sorted back to front by bounds; each later call sits a little closer to the
            // camera so the sort keeps call order (same approach as p5.Unite).
            var cameraTransform = Camera.transform;
            float sortDistance = 1000f - 0.01f * _submitted++;
            var renderParams = new RenderParams(_material)
            {
                camera = Camera,
                layer = Camera.gameObject.layer,
                matProps = properties,
                worldBounds = new Bounds(cameraTransform.position + cameraTransform.forward * sortDistance,
                    Vector3.one * 10000f)
            };
            Graphics.RenderMesh(renderParams, _quad, 0, Matrix4x4.identity);
        }

        private void BeginFrameIfNeeded()
        {
            if (_frame == Time.frameCount) return;

            _frame = Time.frameCount;
            _submitted = 0;
        }

        // Property blocks are read when the camera renders, so each draw this frame keeps its own.
        private MaterialPropertyBlock RentPropertyBlock()
        {
            while (_propertyBlocks.Count <= _submitted)
                _propertyBlocks.Add(new MaterialPropertyBlock());

            var block = _propertyBlocks[_submitted];
            block.Clear();
            return block;
        }

        private static Mesh CreateUnitQuad()
        {
            var corners = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            var mesh = new Mesh
            {
                name = "ImageLayer Quad",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = Array.ConvertAll(corners, corner => (Vector3)corner),
                uv = corners,
                triangles = new[] { 0, 1, 2, 2, 1, 3 }
            };

            // Positions are placed in the shader; the bounds only need to keep the quad from being culled.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return mesh;
        }
    }
}
