using UnityEngine;

namespace Krab.ImageLayer.Unity.Rendering
{
    /// <summary>Owns an <see cref="ImageCanvas"/> drawing on this GameObject's camera.</summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ImageLayerCanvas : MonoBehaviour
    {
        private const string ShaderName = "Hidden/Krab/ImageLayer/Image";

        // Referenced so player builds include the shader.
        [SerializeField] private Shader _shader;

        private ImageCanvas _canvas;

        private void OnDestroy()
        {
            _canvas?.Dispose();
            _canvas = null;
        }

        /// <summary>Created on first use, including after a script reload during Play.</summary>
        public ImageCanvas Canvas
        {
            get
            {
                if (_canvas != null) return _canvas;

                var shader = _shader ? _shader : Shader.Find(ShaderName);
                if (!shader)
                {
                    Debug.LogError($"{nameof(ImageLayerCanvas)} could not find the shader {ShaderName}.", this);
                    return null;
                }

                _canvas = new ImageCanvas(GetComponent<Camera>(), shader);
                return _canvas;
            }
        }

        public Shader Shader
        {
            get => _shader;
            set => _shader = value;
        }
    }
}
