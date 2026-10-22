using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Krab.Common.Output
{
    public static class OutputTextureContainerResolver
    {
        public static IOutputTextureContainer ResolveFirst<TConsumer>(IEnumerable<IOutputTextureContainer> containers)
        {
            var consumerName = typeof(TConsumer).Name;
            var list = containers?.ToList();
            if (list == null || list.Count == 0)
            {
                Debug.LogError($"{consumerName} requires at least one {nameof(IOutputTextureContainer)}.");
                return null;
            }

            if (list.Count > 1)
                Debug.LogWarning(
                    $"{consumerName}: Multiple {nameof(IOutputTextureContainer)} registered. Using the first.");

            return list[0];
        }
    }
}