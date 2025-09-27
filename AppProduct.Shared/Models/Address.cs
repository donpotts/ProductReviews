using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Address
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? UserId { get; set; }

    [DataMember]
    public string? FirstName { get; set; }

    [DataMember]
    public string? LastName { get; set; }

    [DataMember]
    public string? Company { get; set; }

    [DataMember]
    public string? AddressLine1 { get; set; }

    [DataMember]
    public string? AddressLine2 { get; set; }

    [DataMember]
    public string? City { get; set; }

    [DataMember]
    public string? State { get; set; }

    [DataMember]
    public string? PostalCode { get; set; }

    [DataMember]
    public string? Country { get; set; }

    [DataMember]
    public string? Phone { get; set; }

    [DataMember]
    public string? Email { get; set; }

    [DataMember]
    public bool IsDefault { get; set; }

    [DataMember]
    public AddressType Type { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }
}

[DataContract]
public enum AddressType
{
    [EnumMember] Shipping = 0,
    [EnumMember] Billing = 1,
    [EnumMember] Both = 2
}