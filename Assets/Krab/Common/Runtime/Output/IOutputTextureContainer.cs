using System;
using UnityEngine;

namespace Krab.Common.Output
{
    public interface IOutputTextureContainer
    {
        public RenderTexture Texture { get; }
        public event Action TextureChanged;
    }
}