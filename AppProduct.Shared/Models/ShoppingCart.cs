using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ShoppingCart
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? UserId { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
}