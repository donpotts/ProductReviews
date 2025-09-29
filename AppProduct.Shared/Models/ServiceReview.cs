using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceReview
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public long? ServiceId { get; set; }

    [DataMember]
    public string? CustomerName { get; set; }

    [DataMember]
    public string? CustomerEmail { get; set; }

    [DataMember]
    public decimal? Rating { get; set; }

    [DataMember]
    public string? Title { get; set; }

    [DataMember]
    public string? ReviewText { get; set; }

    [DataMember]
    public DateTime? ReviewDate { get; set; }

    [DataMember]
    public bool IsVerifiedCustomer { get; set; }

    [DataMember]
    public bool IsApproved { get; set; } = true;

    [DataMember]
    public string? Response { get; set; }

    [DataMember]
    public DateTime? ResponseDate { get; set; }

    [DataMember]
    public string? ResponseBy { get; set; }

    [DataMember]
    public int? HelpfulVotes { get; set; }

    [DataMember]
    public int? TotalVotes { get; set; }

    [DataMember]
    public ServiceReviewCategory ReviewCategory { get; set; }

    [DataMember]
    public decimal? QualityRating { get; set; }

    [DataMember]
    public decimal? TimelinessRating { get; set; }

    [DataMember]
    public decimal? CommunicationRating { get; set; }

    [DataMember]
    public decimal? ValueRating { get; set; }

    [DataMember]
    public bool WouldRecommend { get; set; }

    [DataMember]
    public string? ProjectType { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public Service? Service { get; set; }
}

[DataContract]
public enum ServiceReviewCategory
{
    [EnumMember] General = 0,
    [EnumMember] Support = 1,
    [EnumMember] Consulting = 2,
    [EnumMember] Implementation = 3,
    [EnumMember] Maintenance = 4,
    [EnumMember] Training = 5
}