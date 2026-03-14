using System;
using Virabis.Core.Nexus.Manifest;

namespace Virabis.Core.Nexus;

public class FeatureMetadata
{
    public Type Type { get; init; }
    public FeatureManifest? Manifest { get; init; }
    public VirabisFeatureAttribute Attribute { get; init; }

    public FeatureMetadata(Type type, VirabisFeatureAttribute attribute, FeatureManifest? manifest = null)
    {
        Type = type;
        Attribute = attribute;
        Manifest = manifest;
    }
}
