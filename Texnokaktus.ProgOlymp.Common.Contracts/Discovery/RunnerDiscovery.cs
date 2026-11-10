using System.Text.Json.Serialization;

namespace Texnokaktus.ProgOlymp.Common.Contracts.Discovery;

public static class RunnerDiscovery
{
    public const int Version = 1;
    public const string InstanceIdMetadata = "runner-instance-id";
    public const string Registered = "registered";
    public const string Removed = "removed";

    public static string Prefix(string environment) => $"progolymp:runner-discovery:v1:{environment}";
    public static string RegistrationKey(string environment, string instanceId) => $"{Prefix(environment)}:instances:{instanceId}";
    public static string EventsChannel(string environment) => $"{Prefix(environment)}:events";
}

public sealed record RunnerRegistration(
    [property: JsonPropertyName("instance_id")] string InstanceId,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("language_version")] string LanguageVersion,
    [property: JsonPropertyName("endpoint")] string Endpoint
);

public sealed record RunnerDiscoveryEvent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("instance_id")] string InstanceId
);
