using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;

namespace TokenVoting.Tests;

public class GetStatusTests
{
    private readonly VotingAccount _votingAccount;

    public GetStatusTests(ITestOutputHelper testOutputHelper)
    {
        _votingAccount = new VotingAccount(testOutputHelper);
    }

    [Fact]
    public void GetVotingStatus_OneVoter_QuorumIsNotReached_VotingIsNotOver()
    {
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 100L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 300L },
            { "vote_decreaseA", 300L },
            { "vote_increaseB", 300L },
            { $"balance_{PrivateNode.FakeAddress1}", 300L },
            { $"last_vote_{PrivateNode.FakeAddress1}", "decreaseA:increaseB" },
            { $"voting_power_{PrivateNode.FakeAddress1}", 300L },
        });

        var votingStatus = _votingAccount.GetVotingStatus();

        using (new AssertionScope())
        {
            votingStatus.GetProperty("assetId").GetString().Should().Be(PrivateNode.FakeAssetId);
            votingStatus.GetProperty("startHeight").GetInt64().Should().Be(100);
            votingStatus.GetProperty("endHeight").GetInt64().Should().Be(10000000);
            votingStatus.GetProperty("total").GetInt64().Should().Be(1000);
            votingStatus.GetProperty("quorumPercent").GetInt64().Should().Be(50);
            votingStatus.GetProperty("quorum").GetInt64().Should().Be(500);
            votingStatus.GetProperty("voted").GetInt64().Should().Be(300);
            votingStatus.GetProperty("isQuorumReached").GetBoolean().Should().BeFalse();
            votingStatus.GetProperty("isVotingOver").GetBoolean().Should().BeFalse();
            votingStatus.GetProperty("votes").GetRawText().Should().Be("{\"increaseA\":0,\"increaseB\":300,\"decreaseB\":0,\"decreaseA\":300}");
        }
    }

    [Fact]
    public void GetVotingStatus_TwoVoters_QuorumIsReached_VotingIsOver()
    {
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 1L },
            { "end_height", 1L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 800L },
            { "vote_increaseA", 300L },
            { "vote_decreaseA", 500L },
            { "vote_increaseB", 300L },
            { "vote_decreaseB", 500L },
            { $"balance_{PrivateNode.FakeAddress1}", 300L },
            { $"balance_{PrivateNode.FakeAddress2}", 500L },
            { $"last_vote_{PrivateNode.FakeAddress1}", "increaseA:increaseB" },
            { $"last_vote_{PrivateNode.FakeAddress2}", "decreaseA:decreaseB" },
            { $"voting_power_{PrivateNode.FakeAddress1}", 300L },
            { $"voting_power_{PrivateNode.FakeAddress2}", 500L },
        });

        var votingStatus = _votingAccount.GetVotingStatus();

        using (new AssertionScope())
        {
            votingStatus.GetProperty("assetId").GetString().Should().Be(PrivateNode.FakeAssetId);
            votingStatus.GetProperty("startHeight").GetInt64().Should().Be(1);
            votingStatus.GetProperty("endHeight").GetInt64().Should().Be(1);
            votingStatus.GetProperty("total").GetInt64().Should().Be(1000);
            votingStatus.GetProperty("quorumPercent").GetInt64().Should().Be(50);
            votingStatus.GetProperty("quorum").GetInt64().Should().Be(500);
            votingStatus.GetProperty("voted").GetInt64().Should().Be(800);
            votingStatus.GetProperty("isQuorumReached").GetBoolean().Should().BeTrue();
            votingStatus.GetProperty("isVotingOver").GetBoolean().Should().BeTrue();
            votingStatus.GetProperty("votes").GetRawText().Should().Be("{\"increaseA\":300,\"increaseB\":300,\"decreaseB\":500,\"decreaseA\":500}");
        }
    }
}