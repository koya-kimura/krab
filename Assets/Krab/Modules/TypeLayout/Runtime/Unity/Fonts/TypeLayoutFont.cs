using System;
using System.IO;
using Unite.Text.Fonts;
using UnityEngine;

namespace Krab.TypeLayout.Unity.Fonts
{
    /// <summary>
    /// Loads a font file from StreamingAssets once. Loading is synchronous, which suits desktop players;
    /// platforms with packed StreamingAssets (Android, WebGL) need the asynchronous loader instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TypeLayoutFont : MonoBehaviour
    {
        public const string DefaultPath = "Krab/TypeLayout/Fonts/Jost-Regular.ttf";

        [SerializeField] private string _streamingAssetsPath = DefaultPath;

        private FontFace _face;
        private bool _failed;

        private void Awake()
        {
            Load();
        }

        private void OnDestroy()
        {
            _face?.Dispose();
            _face = null;
        }

        /// <summary>The loaded font, or null when loading failed. Reloads after a script reload during Play.</summary>
        public FontFace Face
        {
            get
            {
                if (_face == null && !_failed) Load();

                return _face;
            }
        }

        private void Load()
        {
            try
            {
                _face = FontFace.LoadFont(_streamingAssetsPath);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException
                                                  or UnauthorizedAccessException or ArgumentException)
            {
                _failed = true;
                Debug.LogError($"{nameof(TypeLayoutFont)} could not load '{_streamingAssetsPath}': {exception.Message}",
                    this);
            }
        }
    }
}
