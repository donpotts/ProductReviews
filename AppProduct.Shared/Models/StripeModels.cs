using System.Runtime.Serialization;

namespace AppProduct.Shared.Models;

[DataContract]
public class CreateStripeSessionResponse
{
    [DataMember]
    public string SessionId { get; set; } = "";
    
    [DataMember]
    public string? Url { get; set; }
}

[DataContract]
public class CreateStripeSessionRequest
{
    [DataMember]
    public string BaseUrl { get; set; } = "";
    
    [DataMember]
    public string? ShippingAddress { get; set; }
    
    [DataMember]
    public string? BillingAddress { get; set; }
    
    [DataMember]
    public string? BillingStateCode { get; set; }
    
    [DataMember]
    public decimal ShippingAmount { get; set; }
    
    [DataMember]
    public string? Notes { get; set; }
}