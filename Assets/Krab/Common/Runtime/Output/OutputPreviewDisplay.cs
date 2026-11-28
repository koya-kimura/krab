using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnitySimpleContainer;

namespace Krab.Common.Output
{
    public class OutputPreviewDisplay : MonoBehaviour
    {
        [SerializeField] private AspectRatioFitter _aspectRatioFitter;
        [SerializeField] private RawImage _rawImage;
        private bool _subscribed;

        private IOutputTextureContainer _textureContainer;

        private void OnEnable()
        {
            if (_textureContainer == null) return;

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        [Inject]
        public void Construct(IEnumerable<IOutputTextureContainer> containers)
        {
            _textureContainer = OutputTextureContainerResolver.ResolveFirst<OutputPreviewDisplay>(containers);
        }

        private void Subscribe()
        {
            if (_subscribed || _textureContainer == null) return;

            _textureContainer.TextureChanged += OnTextureChanged;
            ApplyTexture();
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _textureContainer == null) return;

            _textureContainer.TextureChanged -= OnTextureChanged;
            _subscribed = false;
        }

        private void OnTextureChanged()
        {
            ApplyTexture();
        }

        private void ApplyTexture()
        {
            if (_rawImage == null)
            {
                Debug.LogError($"{nameof(OutputPreviewDisplay)} requires {nameof(RawImage)}.", this);
                return;
            }

            if (_textureContainer == null) return;

            var texture = _textureContainer.Texture;
            _rawImage.texture = texture;
            UpdateAspectRatio(texture);
        }

        private void UpdateAspectRatio(Texture texture)
        {
            if (_aspectRatioFitter == null) _aspectRatioFitter = _rawImage.GetComponent<AspectRatioFitter>();

            if (_aspectRatioFitter == null || texture == null) return;

            _aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
        }
    }
}