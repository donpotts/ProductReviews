using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ShippingRate
{
    [Key]
    [DataMember]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [DataMember]
    public string Name { get; set; } = "Standard Shipping";

    [DataMember]
    public decimal Amount { get; set; }

    [DataMember]
    public bool IsDefault { get; set; } = true;

    [DataMember]
    public bool IsActive { get; set; } = true;

    [DataMember]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [MaxLength(500)]
    [DataMember]
    public string? Notes { get; set; }
}
