using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceOrder
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
    public ServiceOrderStatus Status { get; set; }

    [DataMember]
    public decimal? Subtotal { get; set; }

    [DataMember]
    public decimal? TaxAmount { get; set; }

    [DataMember]
    public decimal? ExpenseAmount { get; set; }

    [DataMember]
    public decimal? TotalAmount { get; set; }

    [DataMember]
    public PaymentMethod PaymentMethod { get; set; }

    [DataMember]
    public string? PaymentIntentId { get; set; }

    [DataMember]
    public string? CustomerAddress { get; set; }

    [DataMember]
    public string? BillingAddress { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public DateTime? ScheduledStartDate { get; set; }

    [DataMember]
    public DateTime? ScheduledEndDate { get; set; }

    [DataMember]
    public DateTime? ActualStartDate { get; set; }

    [DataMember]
    public DateTime? ActualEndDate { get; set; }

    [DataMember]
    public bool RequiresOnsite { get; set; }

    [DataMember]
    public string? OnsiteAddress { get; set; }

    [DataMember]
    public string? ContactPerson { get; set; }

    [DataMember]
    public string? ContactPhone { get; set; }

    [DataMember]
    public string? ContactEmail { get; set; }

    [DataMember]
    public string? CompletionNotes { get; set; }

    [DataMember]
    public string? CustomerSignature { get; set; }

    [DataMember]
    public DateTime? SignatureDate { get; set; }

    [DataMember]
    public ICollection<ServiceOrderItem> Items { get; set; } = new List<ServiceOrderItem>();

    [DataMember]
    public ICollection<ServiceExpense> Expenses { get; set; } = new List<ServiceExpense>();
}

[DataContract]
public enum ServiceOrderStatus
{
    [EnumMember] Pending = 0,
    [EnumMember] Confirmed = 1,
    [EnumMember] Scheduled = 2,
    [EnumMember] InProgress = 3,
    [EnumMember] OnHold = 4,
    [EnumMember] Completed = 5,
    [EnumMember] Cancelled = 6,
    [EnumMember] Invoiced = 7
}