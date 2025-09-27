using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class OrderItem
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? OrderId { get; set; }

    [DataMember]
    public long? ProductId { get; set; }

    [DataMember]
    public int Quantity { get; set; }

    [DataMember]
    public decimal? UnitPrice { get; set; }

    [DataMember]
    public decimal? TotalPrice { get; set; }

    [DataMember]
    public Product? Product { get; set; }

    [DataMember]
    public Order? Order { get; set; }
}