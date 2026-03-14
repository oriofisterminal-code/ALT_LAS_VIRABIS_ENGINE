using System;

namespace Virabis.Core.Nexus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class VirabisFeatureAttribute : Attribute
{
    public string ManifestPath { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public int InitTimeMs { get; set; }
    public bool LazyLoad { get; set; }
    public int Priority { get; set; }
}
