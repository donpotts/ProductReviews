using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceOrderItem
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? ServiceOrderId { get; set; }

    [DataMember]
    public long? ServiceId { get; set; }

    [DataMember]
    public decimal? HoursEstimated { get; set; }

    [DataMember]
    public decimal? HoursActual { get; set; }

    [DataMember]
    public decimal? UnitPrice { get; set; }

    [DataMember]
    public decimal? TotalPrice { get; set; }

    [DataMember]
    public ServicePricingType PricingType { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public DateTime? StartDate { get; set; }

    [DataMember]
    public DateTime? EndDate { get; set; }

    [DataMember]
    public ServiceItemStatus Status { get; set; }

    [DataMember]
    public string? Deliverables { get; set; }

    [DataMember]
    public int? ProgressPercentage { get; set; }

    [DataMember]
    public Service? Service { get; set; }

    [DataMember]
    public ServiceOrder? ServiceOrder { get; set; }
}

[DataContract]
public enum ServiceItemStatus
{
    [EnumMember] NotStarted = 0,
    [EnumMember] InProgress = 1,
    [EnumMember] OnHold = 2,
    [EnumMember] Completed = 3,
    [EnumMember] Cancelled = 4
}