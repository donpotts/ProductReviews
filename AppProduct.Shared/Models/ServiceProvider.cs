using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class ServiceProviderLegacy
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? ContactEmail { get; set; }

    [DataMember]
    public string? ContactPhone { get; set; }

    [DataMember]
    public string? Website { get; set; }

    [DataMember]
    public string? LogoUrl { get; set; }

    [DataMember]
    public string? Address { get; set; }

    [DataMember]
    public bool IsActive { get; set; } = true;

    [DataMember]
    public bool IsCertified { get; set; }

    [DataMember]
    public string? Certifications { get; set; }

    [DataMember]
    public int? YearsOfExperience { get; set; }

    [DataMember]
    public string? Specialties { get; set; }

    [DataMember]
    public decimal? AverageRating { get; set; }

    [DataMember]
    public int? TotalReviews { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    // Legacy navigation list kept for potential backward compatibility
    [DataMember]
    public List<Service> Service { get; set; } = new();
}