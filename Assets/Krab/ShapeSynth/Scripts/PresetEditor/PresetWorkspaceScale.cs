using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Krab.ShapeSynth.PresetEditor
{
    /// <summary>Session-only UI density. Never edits the scene's PanelSettings asset.</summary>
    public sealed class PresetWorkspaceScale : IDisposable
    {
        private readonly UIDocument _document;
        private readonly PanelSettings _original;
        private readonly PanelSettings _settings;
        private VisualElement _root;
        private float _pixelWidth;
        public int Percent { get; private set; } = 140;

        public PresetWorkspaceScale(UIDocument document)
        {
            _document = document;
            _original = document.panelSettings;
            _settings = UnityEngine.Object.Instantiate(_original);
            _settings.name = "Preset Workspace UI (session)";
            _settings.hideFlags = HideFlags.DontSave;
            _settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _settings.scale = 1;
            document.panelSettings = _settings;
        }

        public void Update()
        {
            var root = _document ? _document.rootVisualElement?.panel?.visualTree : null;
            if (ReferenceEquals(root, _root)) return;
            if (_root != null) _root.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _root = root;
            if (root == null) return;
            root.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            if (root.layout.width > 0) SetPixelWidth(root.layout.width * _settings.scale);
        }

        public void SetPercent(int percent)
        {
            Percent = Mathf.Clamp(percent, 100, 200);
            Apply();
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            if (!ReferenceEquals(evt.target, _root) || evt.newRect.width <= 0) return;
            SetPixelWidth(evt.newRect.width * _settings.scale);
        }
        private void SetPixelWidth(float width) { _pixelWidth = width; Apply(); }
        private void Apply()
        {
            if (_pixelWidth <= 0) return;
            // Keep the same readable density on a 4K Game View instead of halving the controls.
            var scale = Percent / 100f * Mathf.Max(1, _pixelWidth / 1920f);
            if (!Mathf.Approximately(_settings.scale, scale)) _settings.scale = scale;
        }

        public void Dispose()
        {
            if (_root != null) _root.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            _root = null;
            if (_document && _document.panelSettings == _settings) _document.panelSettings = _original;
            if (_settings) UnityEngine.Object.Destroy(_settings);
        }
    }
}
