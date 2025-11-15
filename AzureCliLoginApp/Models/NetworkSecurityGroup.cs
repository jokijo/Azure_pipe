namespace AzureCliLoginApp.Models;

public class NetworkSecurityGroup
{
    public string Name { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}
