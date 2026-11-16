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

        private void Awake()
        {
            try
            {
                Face = FontFace.LoadFont(_streamingAssetsPath);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException
                                                  or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogError($"{nameof(TypeLayoutFont)} could not load '{_streamingAssetsPath}': {exception.Message}",
                    this);
            }
        }

        private void OnDestroy()
        {
            Face?.Dispose();
            Face = null;
        }

        public FontFace Face { get; private set; }
    }
}
