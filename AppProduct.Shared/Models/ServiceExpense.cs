using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceExpense
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? ServiceOrderId { get; set; }

    [DataMember]
    public ServiceExpenseType ExpenseType { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public decimal Amount { get; set; }

    [DataMember]
    public DateTime? ExpenseDate { get; set; }

    [DataMember]
    public string? ReceiptUrl { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public ServiceExpenseStatus ApprovalStatus { get; set; } = ServiceExpenseStatus.Pending;

    [DataMember]
    public DateTime? ApprovedDate { get; set; }

    [DataMember]
    public string? ApprovedBy { get; set; }

    [DataMember]
    public string? RejectionReason { get; set; }

    [DataMember]
    public bool IsReimbursable { get; set; } = true;

    [DataMember]
    public string? Vendor { get; set; }

    [DataMember]
    public string? Location { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public ServiceOrder? ServiceOrder { get; set; }
}

[DataContract]
public enum ServiceExpenseType
{
    [EnumMember] Meals = 0,
    [EnumMember] Hotel = 1,
    [EnumMember] CarRental = 2,
    [EnumMember] Airfare = 3,
    [EnumMember] LocalTransport = 4,
    [EnumMember] Parking = 5,
    [EnumMember] Materials = 6,
    [EnumMember] Other = 7
}

[DataContract]
public enum ServiceExpenseStatus
{
    [EnumMember] Pending = 0,
    [EnumMember] Approved = 1,
    [EnumMember] Rejected = 2
}