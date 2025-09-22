using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Tag
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Color { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public List<Product>? Product { get; set; }
}
