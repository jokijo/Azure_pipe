using System.Text.Json.Serialization;

namespace AzureCliLoginApp.Models;

public class InboundSecurityRule
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("priority")]
    public int Priority { get; set; }
    
    [JsonPropertyName("sourceAddressPrefix")]
    public string SourceAddressPrefix { get; set; } = string.Empty;
    
    [JsonPropertyName("sourcePortRange")]
    public string SourcePortRange { get; set; } = string.Empty;
    
    [JsonPropertyName("destinationAddressPrefix")]
    public string DestinationAddressPrefix { get; set; } = string.Empty;
    
    [JsonPropertyName("destinationPortRange")]
    public string DestinationPortRange { get; set; } = string.Empty;
    
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;
    
    [JsonPropertyName("access")]
    public string Access { get; set; } = string.Empty;
    
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;
    
    public string NsgName { get; set; } = string.Empty;
    
    public string ResourceGroup { get; set; } = string.Empty;
}
