using UnityEngine;

namespace Krab.ImageLayer.Unity
{
    internal static class UnityObjects
    {
        /// <summary>Destroy that also works in Edit Mode, where tools and tests load libraries.</summary>
        public static void Destroy(Object target)
        {
            if (target == null) return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
