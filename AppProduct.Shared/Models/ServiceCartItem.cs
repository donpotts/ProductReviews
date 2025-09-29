using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceCartItem
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? ServiceCartId { get; set; }

    [DataMember]
    public long? ServiceId { get; set; }

    [DataMember]
    public decimal EstimatedHours { get; set; } = 1;

    [DataMember]
    public decimal? UnitPrice { get; set; }

    [DataMember]
    public ServicePricingType PricingType { get; set; }

    [DataMember]
    public DateTime? PreferredStartDate { get; set; }

    [DataMember]
    public string? SpecialRequirements { get; set; }

    [DataMember]
    public bool RequiresOnsite { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public Service? Service { get; set; }

    [DataMember]
    public ServiceCart? ServiceCart { get; set; }
}