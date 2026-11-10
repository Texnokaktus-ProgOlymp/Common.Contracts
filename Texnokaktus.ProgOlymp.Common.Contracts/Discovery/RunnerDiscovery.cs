namespace Texnokaktus.ProgOlymp.Common.Contracts.Discovery;

public static class RunnerDiscovery
{
    public const int Version = 1;
    public const string InstanceIdMetadata = "runner-instance-id";

    public const RunnerDiscoveryEvent.Types.RunnerDiscoveryEventType Registered =
        RunnerDiscoveryEvent.Types.RunnerDiscoveryEventType.Registered;

    public const RunnerDiscoveryEvent.Types.RunnerDiscoveryEventType Removed =
        RunnerDiscoveryEvent.Types.RunnerDiscoveryEventType.Removed;

    public static string Prefix(string environment) => $"progolymp:runner-discovery:v1:{environment}";

    public static string RegistrationKey(string environment, string instanceId) =>
        $"{Prefix(environment)}:instances:{instanceId}";

    public static string EventsChannel(string environment) => $"{Prefix(environment)}:events";
}
