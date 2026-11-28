using System.Collections.Generic;
using Klak.Spout;
using Krab.Common.Views;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace Krab.Common.Output
{
    [RequireComponent(typeof(SpoutSender))]
    [UiTarget("Common/Output", "Spout", 41)]
    public sealed class SpoutController : MonoBehaviour, IUiTarget
    {
        // SyphonController already owns the "SpoutName" key, so Spout uses its own key.
        private readonly PrefsString _spoutName = new("SpoutSenderName", "krab");
        private bool _subscribed;
        private SpoutSender _spoutSender;

        private IOutputTextureContainer _textureContainer;

        // Spout is Windows only. The prefab keeps SpoutSender disabled so other platforms never load the plugin.
        private static bool IsSupported =>
            Application.platform is RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer;

        private void Awake()
        {
            _spoutSender = GetComponent<SpoutSender>();
            _spoutSender.captureMethod = CaptureMethod.Texture;
            _spoutSender.enabled = IsSupported;
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
            if (!IsSupported) return UI.Label("Spout is available on Windows only.");

            return UI.Column(_spoutName.CreateElement(), UI.Button("Apply", ApplyName));
        }

        [Inject]
        public void Construct(IEnumerable<IOutputTextureContainer> containers)
        {
            _textureContainer = OutputTextureContainerResolver.ResolveFirst<SpoutController>(containers);
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
            _spoutSender.spoutName = _spoutName;
        }

        private void OnProviderTextureChanged()
        {
            ApplyTexture();
        }

        private void ApplyTexture()
        {
            if (_textureContainer == null) return;
            _spoutSender.sourceTexture = _textureContainer.Texture;
        }
    }
}
