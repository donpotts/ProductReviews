using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class TaxRate
{
    [Key]
    [DataMember]
    public long? Id { get; set; }

    [DataMember]
    public string? State { get; set; }

    [DataMember]
    public string? StateCode { get; set; }

    [DataMember]
    public decimal? StateTaxRate { get; set; }

    [DataMember]
    public decimal? LocalTaxRate { get; set; }

    [DataMember]
    public decimal? CombinedTaxRate { get; set; }

    [DataMember]
    public bool IsActive { get; set; }

    [DataMember]
    public DateTime? CreatedDate { get; set; }

    [DataMember]
    public DateTime? ModifiedDate { get; set; }
}

[DataContract]
public class TaxCalculationRequest
{
    [DataMember]
    public decimal Subtotal { get; set; }
    
    [DataMember]
    public string StateCode { get; set; } = "";
}

[DataContract]
public class TaxCalculationResult
{
    [DataMember]
    public decimal Subtotal { get; set; }
    
    [DataMember]
    public string StateCode { get; set; } = "";
    
    [DataMember]
    public decimal? TaxRate { get; set; }
    
    [DataMember]
    public decimal? TaxAmount { get; set; }
    
    [DataMember]
    public decimal? Total { get; set; }
}