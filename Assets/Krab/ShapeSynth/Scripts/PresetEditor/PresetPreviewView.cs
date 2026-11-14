using System;
using RosettaUI;
using RosettaUI.UIToolkit.Builder;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Interactive viewport for the preview texture. Texture ownership remains with its provider.</summary>
    public sealed class PresetPreviewView
    {
        private readonly Func<Texture> _textureProvider;
        private ImageElement _imageElement;
        private VisualElement _viewport;
        private VisualElement _imageView;
        private int _lastTextureWidth = -1;
        private int _lastTextureHeight = -1;
        private bool _hasViewportGeometry;
        private bool _isDragging;
        private int _pointerId = -1;
        private Vector2 _lastPointerPosition;

        public float Zoom { get; private set; } = 1f;
        public Vector2 Pan { get; private set; }

        public PresetPreviewView(Func<Texture> textureProvider)
        {
            _textureProvider = textureProvider ?? throw new ArgumentNullException(nameof(textureProvider));
        }

        /// <summary>The zoomable viewport only. Toolbar and footer belong to the editor shell.</summary>
        public Element CreateElement()
        {
            _imageElement = UI.Image(_textureProvider);
            var viewport = UI.Box(_imageElement).SetMinHeight(120).SetFlexGrow(1);
            viewport.RegisterUpdateCallback(_ => CheckTextureSize());
            return PresetWorkspaceLayout.BindView(viewport, Attach).SetFlexGrow(1).SetMinHeight(0);
        }

        public void Fit()
        {
            Zoom = 1f;
            Pan = Vector2.zero;
            ApplyTransform();
        }

        private void CheckTextureSize()
        {
            var texture = _textureProvider();
            if (texture == null) return;
            if (_lastTextureWidth == texture.width && _lastTextureHeight == texture.height) return;

            var hadTexture = _lastTextureWidth >= 0;
            _lastTextureWidth = texture.width;
            _lastTextureHeight = texture.height;
            if (hadTexture) Fit();
        }

        public string ZoomAndTextureSize()
        {
            var texture = _textureProvider();
            return texture == null
                ? $"no texture · Zoom {Zoom:P0}"
                : $"{texture.width} × {texture.height} · Zoom {Zoom:P0}";
        }

        private Action Attach(VisualElement viewport)
        {
            _viewport = viewport;
            viewport.name = "preset-preview-viewport";
            viewport.style.overflow = Overflow.Hidden;
            viewport.style.backgroundColor = PresetEditorTheme.Center;
            PresetEditorTheme.Border(viewport.style, PresetEditorTheme.Center, 0);
            PresetEditorTheme.Margin(viewport.style, 0, 0);
            FindImageView();
            viewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            viewport.RegisterCallback<WheelEvent>(OnWheel);
            viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
            viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            viewport.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            ApplyTransform();

            return () =>
            {
                viewport.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                viewport.UnregisterCallback<WheelEvent>(OnWheel);
                viewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                viewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                viewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                viewport.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                if (_isDragging && viewport.HasPointerCapture(_pointerId)) viewport.ReleasePointer(_pointerId);
                _isDragging = false;
                _pointerId = -1;
                if (_viewport == viewport) _viewport = null;
                _imageView = null;
            };
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width <= 0 || evt.newRect.height <= 0) return;
            if (_imageView == null) FindImageView();
            if (_hasViewportGeometry) return;
            _hasViewportGeometry = true;
            Fit();
        }

        private void OnWheel(WheelEvent evt)
        {
            var factor = Mathf.Pow(1.1f, -evt.delta.y);
            var nextZoom = Mathf.Clamp(Zoom * factor, 0.05f, 32f);
            if (Mathf.Approximately(nextZoom, Zoom)) return;

            var center = _viewport.contentRect.center;
            var cursor = evt.localMousePosition - center;
            var ratio = nextZoom / Zoom;
            Pan = cursor - (cursor - Pan) * ratio;
            Zoom = nextZoom;
            ApplyTransform();
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.MiddleMouse) return;
            _isDragging = true;
            _pointerId = evt.pointerId;
            _lastPointerPosition = (Vector2)evt.localPosition;
            _viewport.CapturePointer(_pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || evt.pointerId != _pointerId) return;
            var pointerPosition = (Vector2)evt.localPosition;
            Pan += pointerPosition - _lastPointerPosition;
            _lastPointerPosition = pointerPosition;
            ApplyTransform();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || evt.pointerId != _pointerId || evt.button != (int)MouseButton.MiddleMouse) return;
            _viewport.ReleasePointer(_pointerId);
            _isDragging = false;
            _pointerId = -1;
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            _isDragging = false;
            _pointerId = -1;
        }

        private void ApplyTransform()
        {
            if (_imageView == null) return;
            _imageView.style.scale = new Scale(new Vector2(Zoom * 0.92f, Zoom * 0.92f));
            _imageView.style.translate = new Translate(Pan.x, Pan.y);
        }

        private void FindImageView()
        {
            _imageView = UIToolkitBuilder.Instance.GetUIObj(_imageElement);
            if (_imageView == null) return;
            _imageView.name = "preset-preview-image";
            if (_imageView is Image image) image.scaleMode = ScaleMode.ScaleToFit;
            _imageView.style.position = Position.Absolute;
            _imageView.style.left = 0;
            _imageView.style.right = 0;
            _imageView.style.top = 0;
            _imageView.style.bottom = 0;
            ApplyTransform();
        }
    }
}
