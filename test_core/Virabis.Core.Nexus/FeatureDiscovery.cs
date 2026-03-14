using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Virabis.Core.Nexus.Manifest;

namespace Virabis.Core.Nexus;

public static class FeatureDiscovery
{
    public static IEnumerable<FeatureMetadata> DiscoverFeatures(Assembly assembly)
    {
        var featureTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IFeature).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<VirabisFeatureAttribute>() != null);

        foreach (var type in featureTypes)
        {
            var metadata = GetMetadata(type);
            if (metadata != null)
            {
                yield return metadata;
            }
        }
    }

    public static FeatureMetadata? GetMetadata(Type type)
    {
        var attribute = type.GetCustomAttribute<VirabisFeatureAttribute>();
        if (attribute == null)
        {
            return null;
        }

        FeatureManifest? manifest = null;
        if (!string.IsNullOrEmpty(attribute.ManifestPath))
        {
            try
            {
                manifest = ManifestLoader.Load(attribute.ManifestPath);
            }
            catch
            {
            }
        }

        return new FeatureMetadata(type, attribute, manifest);
    }
}
