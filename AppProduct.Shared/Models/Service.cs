using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Service
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? DetailedDescription { get; set; }

    [DataMember]
    public decimal? HourlyRate { get; set; }

    [DataMember]
    public decimal? DailyRate { get; set; }

    [DataMember]
    public decimal? ProjectRate { get; set; }

    [DataMember]
    public ServicePricingType PricingType { get; set; }

    [DataMember]
    public long? ServiceCategoryId { get; set; }

    [DataMember]
    public long? ServiceCompanyId { get; set; }

    [DataMember]
    public string? ImageUrl { get; set; }

    [DataMember]
    public bool IsAvailable { get; set; } = true;

    [DataMember]
    public int? EstimatedDurationHours { get; set; }

    [DataMember]
    public bool RequiresOnsite { get; set; }

    [DataMember]
    public bool IncludesTravel { get; set; }

    [DataMember]
    public string? Requirements { get; set; }

    [DataMember]
    public string? Deliverables { get; set; }

    [DataMember]
    public ServiceComplexity Complexity { get; set; }

    [DataMember]
    public string? SKU { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public List<ServiceCategory> ServiceCategory { get; set; } = new();

    [DataMember]
    public List<ServiceCompany> ServiceCompany { get; set; } = new();

    [DataMember]
    public List<ServiceFeature> ServiceFeature { get; set; } = new();

    [DataMember]
    public List<ServiceTag> ServiceTag { get; set; } = new();

    [DataMember]
    public List<ServiceReview> ServiceReview { get; set; } = new();

    [NotMapped]
    public string DisplayName => Name ?? string.Empty;

    [NotMapped]
    public string PricingDisplay => PricingType switch
    {
        ServicePricingType.Hourly => HourlyRate.HasValue ? $"${HourlyRate:F2}/hour" : "Contact for pricing",
        ServicePricingType.Daily => DailyRate.HasValue ? $"${DailyRate:F2}/day" : "Contact for pricing",
        ServicePricingType.Project => ProjectRate.HasValue ? $"${ProjectRate:F2}/project" : "Contact for pricing",
        ServicePricingType.Custom => "Custom pricing",
        _ => "Contact for pricing"
    };

    public decimal? GetBasePrice() => PricingType switch
    {
        ServicePricingType.Hourly => HourlyRate,
        ServicePricingType.Daily => DailyRate,
        ServicePricingType.Project => ProjectRate,
        _ => null
    };
}

[DataContract]
public enum ServicePricingType
{
    [EnumMember] Hourly = 0,
    [EnumMember] Daily = 1,
    [EnumMember] Project = 2,
    [EnumMember] Custom = 3
}

[DataContract]
public enum ServiceComplexity
{
    [EnumMember] Basic = 0,
    [EnumMember] Intermediate = 1,
    [EnumMember] Advanced = 2,
    [EnumMember] Expert = 3
}