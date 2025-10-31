using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OptimaJet.Workflow.Core.Model;

namespace WorkflowLib;

public sealed class WeeklyReportData : IDynamicParameterCompatible
{
    public Guid Id { get; set; }
    public string SubmittedBy { get; set; }
    public DateTime SubmittedOn { get; set; }
    public string Name { get; set; }
    public string? ReviewedBy { get; set; }
    public string Details { get; set; } = "{}";
    public int Version { get; set; }

    public string? StateName { get; set; }

    public WeeklyReportData()
    {
        Id = Guid.NewGuid();
        SubmittedOn = DateTime.UtcNow;
    }

    public IDictionary<string, object?> GetPropertiesAsDictionary()
    {
        var fixedProperties = new Dictionary<string, object?>
        {
            { nameof(Id), Id },
            { nameof(SubmittedBy), SubmittedBy },
            { nameof(SubmittedOn), SubmittedOn },
            { nameof(Name), Name },
            { nameof(ReviewedBy), ReviewedBy },
            { nameof(Version), Version },
            { nameof(StateName), StateName }
        };

        Dictionary<string, object?> dynamicProperties =
            JsonConvert.DeserializeObject<Dictionary<string, object?>>(Details) ?? new Dictionary<string, object?>();
        return fixedProperties.Concat(dynamicProperties).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void SetPropertiesFromDictionary(IDictionary<string, object> properties)
    {
        SubmittedBy = GetValue<string>(properties,nameof(SubmittedBy));
        SubmittedOn =  GetValue<DateTime>(properties,nameof(SubmittedOn)); 
        Name = GetValue<string>(properties, nameof(Name));
        ReviewedBy = GetValueOrNull<string>(properties, nameof(ReviewedBy));
        Version = GetValue<int>(properties,nameof(Version));
        StateName = GetValueOrNull<string>(properties, nameof(StateName));
        Details = JsonConvert.SerializeObject(properties.Where(kvp =>
                kvp.Key != nameof(Id) &&
                kvp.Key != nameof(SubmittedBy) &&
                kvp.Key != nameof(SubmittedOn) &&
                kvp.Key != nameof(Name) &&
                kvp.Key != nameof(ReviewedBy) &&
                kvp.Key != nameof(Version) &&
                kvp.Key != nameof(StateName))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    private T GetValue<T>(IDictionary<string, object> dict, string key)
    {
        return GetValueOrNull<T>(dict, key) ?? throw new ArgumentException($"Property {key} is not set");
    }

    private T? GetValueOrNull<T>(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out object? value))
        {
            return default;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (value == null)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
