namespace TokenVoting.Tests.Models;

public class ConstructorOptions
{
    public string AvailableOptions { get; set; } = string.Empty;

    public string VotingAssetId { get; set; } = string.Empty;

    public long StartHeight { get; set; }

    public long EndHeight { get; set; }

    public long QuorumPercent { get; set; }
}