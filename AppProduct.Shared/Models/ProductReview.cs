using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ProductReview
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? ProductId { get; set; }

    [DataMember]
    public string? CustomerName { get; set; }

    [DataMember]
    public string? CustomerEmail { get; set; }

    [DataMember]
    public long? Rating { get; set; }

    [DataMember]
    public string? Title { get; set; }

    [DataMember]
    public string? ReviewText { get; set; }

    [DataMember]
    public DateTime? ReviewDate { get; set; }

    [DataMember]
    public bool IsVerifiedPurchase { get; set; }

    [DataMember]
    public long? HelpfulVotes { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public long? UserId { get; set; }
}
