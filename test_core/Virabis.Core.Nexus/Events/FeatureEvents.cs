using System;

namespace Virabis.Core.Nexus.Events;

public struct FeatureRegisteredEvent
{
    public string Name;
    public DateTime Timestamp;
}

public struct FeatureInitializedEvent
{
    public string Name;
    public int InitTimeMs;
    public DateTime Timestamp;
}

public struct FeatureShutdownEvent
{
    public string Name;
    public DateTime Timestamp;
}

public struct FeatureUnregisteredEvent
{
    public string Name;
    public DateTime Timestamp;
}

public struct FeatureEnabledEvent
{
    public string Name;
    public DateTime Timestamp;
}

public struct FeatureDisabledEvent
{
    public string Name;
    public string Reason;
    public DateTime Timestamp;
}

public struct HealthStatusChangedEvent
{
    public string Name;
    public bool IsHealthy;
    public string Status;
    public DateTime Timestamp;
}
