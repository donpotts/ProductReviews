using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class Product
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? Name { get; set; }

    [DataMember]
    public string? Description { get; set; }

    [DataMember]
    public string? DetailedSpecs { get; set; }

    [DataMember]
    public decimal? Price { get; set; }

    [DataMember]
    public long? CategoryId { get; set; }

    [DataMember]
    public long? BrandId { get; set; }

    [DataMember]
    public string? ImageUrl { get; set; }

    [DataMember]
    public bool InStock { get; set; }

    [DataMember]
    public int? StockQuantity { get; set; }

    [DataMember]
    public int? ReorderLevel { get; set; }

    [DataMember]
    public decimal? Weight { get; set; }

    [DataMember]
    public string? SKU { get; set; }

    [DataMember]
    public DateTime? ReleaseDate { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }

    [DataMember]
    public string? Notes { get; set; }

    [DataMember]
    public long? UserId { get; set; }

    [DataMember]
    public List<Category>? Category { get; set; }

    [DataMember]
    public List<Brand>? Brand { get; set; }

    [DataMember]
    public List<Feature>? Feature { get; set; }

    [DataMember]
    public List<Tag>? Tag { get; set; }

    [DataMember]
    public List<ProductReview>? ProductReview { get; set; }
}
