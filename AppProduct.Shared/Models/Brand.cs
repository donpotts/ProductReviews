using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Brand
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? LogoUrl { get; set; }

    [DataMember]
    public string? Website { get; set; }

    [DataMember]
    public DateTime? EstablishedDate { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public long? UserId { get; set; }
}
