using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class CartProduct
{
    [DataMember]
    public long? Id { get; set; }
    
    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Model { get; set; }
    
    [DataMember]
    public string? Description { get; set; }
    
    [DataMember]
    public decimal? Price { get; set; }
    
    [DataMember]
    public int Quantity { get; set; }

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(Model)
        ? Name ?? string.Empty
        : string.Join(' ', new[] { Model?.Trim(), Name?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
}