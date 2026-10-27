using Newtonsoft.Json;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;
using OptimaJet.Workflow.Core.Model;

namespace WorkflowApi.Data;

public sealed class WeeklyReportData : IDynamicParameterCompatible
{
    public WeeklyReportData()
    {
        Id = Guid.NewGuid();
        SubmittedOn = DateTime.UtcNow;
    }

    public Guid Id { get; set; }

    public string SubmittedBy { get; set; } = String.Empty;

    public DateTime SubmittedOn { get; set; }

    public string Name { get; set; } = String.Empty;

    public string? ReviewedBy { get; set; }

    public string Details { get; set; } = "{}";

    public int Version { get; set; }

    public string? StateName { get; set; }

    public IDictionary<string, object?> GetPropertiesAsDictionary()
    {
        var fixedProperties = new Dictionary<string, object?>
        {
            [nameof(Id)] = Id,
            [nameof(SubmittedBy)] = SubmittedBy,
            [nameof(SubmittedOn)] = SubmittedOn,
            [nameof(Name)] = Name,
            [nameof(ReviewedBy)] = ReviewedBy,
            [nameof(Version)] = Version,
            [nameof(StateName)] = StateName
        };

        var dynamicProperties = SystemTextJsonSerializer.Deserialize<Dictionary<string, object?>>(Details) ?? [];
        return fixedProperties.Concat(dynamicProperties).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public void SetPropertiesFromDictionary(IDictionary<string, object> properties)
    {
        SubmittedBy = GetValue<string>(properties, nameof(SubmittedBy));
        SubmittedOn = GetValue<DateTime>(properties, nameof(SubmittedOn));
        Name = GetValue<string>(properties, nameof(Name));
        ReviewedBy = GetValueOrDefault<string>(properties, nameof(ReviewedBy));
        Version = GetValue<int>(properties, nameof(Version));
        StateName = GetValueOrDefault<string>(properties, nameof(StateName));
        Details = JsonConvert.SerializeObject(properties
            .Where(pair => !FixedPropertyNames.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static T GetValue<T>(IDictionary<string, object> properties, string key)
    {
        return GetValueOrDefault<T>(properties, key)
            ?? throw new ArgumentException($"Property {key} is not set.", nameof(properties));
    }

    private static T? GetValueOrDefault<T>(IDictionary<string, object> properties, string key)
    {
        if (!properties.TryGetValue(key, out var value) || value is null)
        {
            return default;
        }

        return value is T typed ? typed : (T)Convert.ChangeType(value, typeof(T));
    }

    private static readonly HashSet<string> FixedPropertyNames =
    [
        nameof(Id),
        nameof(SubmittedBy),
        nameof(SubmittedOn),
        nameof(Name),
        nameof(ReviewedBy),
        nameof(Version),
        nameof(StateName)
    ];
}
