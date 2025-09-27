using System.ComponentModel.DataAnnotations.Schema;

namespace AppProduct.Data;

[Table("CategoryProduct")]
public class CategoryProductLink
{
    public long CategoryId { get; set; }
    public long ProductId { get; set; }
}
