using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceFeature
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? IconClass { get; set; }

    [DataMember]
    public bool IsActive { get; set; } = true;

    [DataMember]
    public FeatureType Type { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public List<Service> Service { get; set; } = new();
}

[DataContract]
public enum FeatureType
{
    [EnumMember] Included = 0,
    [EnumMember] Optional = 1,
    [EnumMember] Premium = 2
}