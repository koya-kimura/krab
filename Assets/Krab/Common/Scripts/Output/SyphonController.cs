using System.Collections.Generic;
using Klak.Syphon;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.Common.Output
{
    [RequireComponent(typeof(SyphonServer))]
    public class SyphonController : MonoBehaviour, IUiTarget
    {
        private readonly PrefsString _syphonName = new("SpoutName", "krab");
        private bool _subscribed;
        private SyphonServer _syphonServer;

        private IOutputTextureContainer _textureContainer;

        private void Awake()
        {
            _syphonServer = GetComponent<SyphonServer>();
        }

        private void Start()
        {
            ApplyName();
        }

        private void OnEnable()
        {
            if (_textureContainer == null) return;

            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public Element CreateElement()
        {
            return UI.Column(_syphonName.CreateElement(), UI.Button("Apply", ApplyName));
        }

        [Inject]
        public void Construct(IEnumerable<IOutputTextureContainer> containers)
        {
            _textureContainer = OutputTextureContainerResolver.ResolveFirst<SyphonController>(containers);
        }

        private void Subscribe()
        {
            if (_subscribed || _textureContainer == null) return;

            _textureContainer.TextureChanged += OnProviderTextureChanged;
            ApplyTexture();
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _textureContainer == null) return;

            _textureContainer.TextureChanged -= OnProviderTextureChanged;
            _subscribed = false;
        }

        private void ApplyName()
        {
            if (_syphonServer.ServerName == _syphonName) return;
            _syphonServer.ServerName = _syphonName;
        }

        private void OnProviderTextureChanged()
        {
            ApplyTexture();
        }

        private void ApplyTexture()
        {
            if (_textureContainer == null) return;
            _syphonServer.SourceTexture = _textureContainer.Texture;
        }
    }
}