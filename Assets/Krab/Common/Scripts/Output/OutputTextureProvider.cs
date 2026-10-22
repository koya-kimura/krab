using System;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;

namespace Krab.Common.Output
{
    [RequireComponent(typeof(Camera))]
    public class OutputTextureProvider : MonoBehaviour, IOutputTextureContainer, IUiTarget
    {
        private readonly PrefsVector2Int _resolution = new("OutputResolution", new Vector2Int(1920, 1080));
        private Camera _camera;
        private RenderTexture _outputTexture;

        private void Awake()
        {
            TryEnsureCamera();
        }

        private void Start()
        {
            ApplyResolution();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        public RenderTexture Texture
        {
            get
            {
                if (!TryEnsureCamera()) return null;

                return _camera.targetTexture;
            }
        }

        public event Action TextureChanged;

        public Element CreateElement()
        {
            return UI.Column(_resolution.CreateElement(), UI.Button("Apply", ApplyResolution));
        }

        private bool TryEnsureCamera()
        {
            if (_camera != null) return true;

            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError($"{nameof(OutputTextureProvider)} requires {nameof(Camera)} on the same GameObject.",
                    this);
                return false;
            }

            return true;
        }

        private void ApplyResolution()
        {
            if (!TryEnsureCamera()) return;

            var resolution = _resolution.Get();
            if (resolution.x <= 0 || resolution.y <= 0)
            {
                Debug.LogWarning($"{nameof(OutputTextureProvider)} requires a value greater than zero.");
                return;
            }

            if (_outputTexture != null && resolution.x == _outputTexture.width && resolution.y == _outputTexture.height)
                return;

            ReleaseTexture();
            _outputTexture = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            _camera.targetTexture = _outputTexture;

            TextureChanged?.Invoke();
        }

        private void ReleaseTexture()
        {
            if (_outputTexture == null) return;

            if (_camera != null) _camera.targetTexture = null;
            _outputTexture.Release();
            Destroy(_outputTexture);
            _outputTexture = null;
        }
    }
}