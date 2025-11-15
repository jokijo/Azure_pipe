using System.Text.Json.Serialization;

namespace AzureCliLoginApp.Models;

public class VirtualMachine
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("resourceGroup")]
    public string ResourceGroup { get; set; } = string.Empty;
    
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
