using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Order
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? OrderNumber { get; set; }

    [DataMember]
    public string? UserId { get; set; }

    [DataMember]
    public DateTime? OrderDate { get; set; }

    [DataMember]
    public OrderStatus Status { get; set; }

    [DataMember]
    public decimal? Subtotal { get; set; }

    [DataMember]
    public decimal? TaxAmount { get; set; }

    [DataMember]
    public decimal? ShippingAmount { get; set; }

    [DataMember]
    public decimal? TotalAmount { get; set; }

    [DataMember]
    public PaymentMethod PaymentMethod { get; set; }

    [DataMember]
    public string? PaymentIntentId { get; set; }

    [DataMember]
    public string? ShippingAddress { get; set; }

    [DataMember]
    public string? BillingAddress { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [DataMember]
    public DateTime? ActualDeliveryDate { get; set; }

    [DataMember]
    public string? TrackingNumber { get; set; }

    [DataMember]
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

[DataContract]
public enum OrderStatus
{
    [EnumMember] Pending = 0,
    [EnumMember] Confirmed = 1,
    [EnumMember] Processing = 2,
    [EnumMember] Shipped = 3,
    [EnumMember] Delivered = 4,
    [EnumMember] Cancelled = 5,
    [EnumMember] Refunded = 6
}

[DataContract]
public enum PaymentMethod
{
    [EnumMember] CreditCard = 0,
    [EnumMember] Cash = 1,
    [EnumMember] PurchaseOrder = 2
}