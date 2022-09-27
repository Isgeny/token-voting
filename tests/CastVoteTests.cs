using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;

namespace TokenVoting.Tests;

public class CastVoteTests
{
    private readonly VotingAccount _votingAccount;

    public CastVoteTests(ITestOutputHelper testOutputHelper)
    {
        _votingAccount = new VotingAccount(testOutputHelper);
    }

    [Fact]
    public void InvokeFromAdminAccount_ThrowException()
    {
        var invoke = () => _votingAccount.InvokeCastVote(_votingAccount.Account, "increaseA");

        invoke.Should().Throw<Exception>().WithMessage("*Access denied");
    }

    [Fact]
    public void InvokeWithPayments_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();

        var invoke = () => _votingAccount.InvokeCastVote(account, "increaseA", new Dictionary<Asset, decimal> { { Assets.WAVES, 0.1M } });

        invoke.Should().Throw<Exception>().WithMessage("*Payments are prohibited");
    }

    [Fact]
    public void InvokeWhenNotInitialized_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();

        var invoke = () => _votingAccount.InvokeCastVote(account, "increaseA");

        invoke.Should().Throw<Exception>().WithMessage("*Not initialized");
    }

    [Fact]
    public void InvokeWhenVotingIsNotStarted_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 10000000L },
            { "end_height", 20000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokeCastVote(account, "increaseA");

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not started");
    }

    [Fact]
    public void InvokeWhenVotingIsOver_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 1L },
            { "end_height", 1L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokeCastVote(account, "increaseA");

        invoke.Should().Throw<Exception>().WithMessage("*Voting is over");
    }

    [Theory]
    [InlineData("increaseA,decreaseA")]
    [InlineData("increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB")]
    public void InvokeWithIncorrectOption_ThrowException(string availableOptions)
    {
        var account = PrivateNode.GenerateAccount();
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", availableOptions },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokeCastVote(account, "hello");

        invoke.Should().Throw<Exception>().WithMessage("*Incorrect voting option");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { $"balance_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "increaseA");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 400),
                new KeyValuePair<string, object>("vote_increaseA", 400),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "increaseA"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 400));
        }
    }

    [Fact]
    public void InvokeSecondTimeWithOtherOption_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 6, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 400L },
            { "vote_increaseA", 400L },
            { $"balance_{account.Address}", 400L },
            { $"last_vote_{account.Address}", "increaseA" },
            { $"voting_power_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "decreaseA");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 400),
                new KeyValuePair<string, object>("vote_increaseA", 0),
                new KeyValuePair<string, object>("vote_decreaseA", 400),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "decreaseA"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 400));
        }
    }

    [Fact]
    public void InvokeSecondTimeWithHigherPower_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 6, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 400L },
            { "vote_increaseA", 400L },
            { $"balance_{account.Address}", 600L },
            { $"last_vote_{account.Address}", "increaseA" },
            { $"voting_power_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "increaseA");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 600),
                new KeyValuePair<string, object>("vote_increaseA", 600),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "increaseA"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 600));
        }
    }

    [Fact]
    public void InvokeWithSecondAccount_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account1 = PrivateNode.GenerateAccount();
        var account2 = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 6, _votingAccount.Account);
        PrivateNode.TransferAsset(votingAsset, 4, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 600L },
            { "vote_decreaseA", 600L },
            { $"balance_{account1.Address}", 600L },
            { $"balance_{account2.Address}", 400L },
            { $"last_vote_{account1.Address}", "decreaseA" },
            { $"voting_power_{account1.Address}", 600L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account2, "decreaseA");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 1000),
                new KeyValuePair<string, object>("vote_decreaseA", 1000),
                new KeyValuePair<string, object>($"last_vote_{account1.Address}", "decreaseA"),
                new KeyValuePair<string, object>($"last_vote_{account2.Address}", "decreaseA"),
                new KeyValuePair<string, object>($"voting_power_{account1.Address}", 600),
                new KeyValuePair<string, object>($"voting_power_{account2.Address}", 400));
        }
    }

    [Fact]
    public void InvokeWhenTokenQuantityChanged_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(30, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 20, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 600L },
            { "vote_decreaseA", 600L },
            { $"balance_{account.Address}", 2000L },
            { $"last_vote_{account.Address}", "decreaseA" },
            { $"voting_power_{account.Address}", 600L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "decreaseA");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("total", 3000),
                new KeyValuePair<string, object>("quorum", 1500),
                new KeyValuePair<string, object>("voted", 2000),
                new KeyValuePair<string, object>("vote_decreaseA", 2000),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "decreaseA"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 2000));
        }
    }

    [Fact]
    public void Invoke_TwoGroups_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { $"balance_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "increaseA:decreaseB");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 400),
                new KeyValuePair<string, object>("vote_increaseA", 400),
                new KeyValuePair<string, object>("vote_decreaseB", 400),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "increaseA:decreaseB"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 400));
        }
    }

    [Fact]
    public void InvokeSecondTimeWithOtherOption_TwoGroups_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 6, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 400L },
            { "vote_increaseA", 400L },
            { "vote_decreaseB", 400L },
            { $"balance_{account.Address}", 400L },
            { $"last_vote_{account.Address}", "increaseA:decreaseB" },
            { $"voting_power_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "decreaseA:increaseB");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 400),
                new KeyValuePair<string, object>("vote_increaseA", 0),
                new KeyValuePair<string, object>("vote_decreaseA", 400),
                new KeyValuePair<string, object>("vote_increaseB", 400),
                new KeyValuePair<string, object>("vote_decreaseB", 0),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "decreaseA:increaseB"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 400));
        }
    }

    [Fact]
    public void InvokeSecondTimeWithHigherPower_TwoGroups_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 6, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 400L },
            { "vote_increaseA", 400L },
            { "vote_decreaseB", 400L },
            { $"balance_{account.Address}", 600L },
            { $"last_vote_{account.Address}", "increaseA:decreaseB" },
            { $"voting_power_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account, "increaseA:decreaseB");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 600),
                new KeyValuePair<string, object>("vote_increaseA", 600),
                new KeyValuePair<string, object>("vote_decreaseB", 600),
                new KeyValuePair<string, object>($"last_vote_{account.Address}", "increaseA:decreaseB"),
                new KeyValuePair<string, object>($"voting_power_{account.Address}", 600));
        }
    }

    [Fact]
    public void InvokeWithSecondAccount_TwoGroups_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account1 = PrivateNode.GenerateAccount();
        var account2 = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 10, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA:increaseB,increaseA:decreaseB,decreaseA:increaseB,decreaseA:decreaseB" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { "voted", 600L },
            { "vote_increaseA", 600L },
            { "vote_decreaseB", 600L },
            { $"balance_{account1.Address}", 600L },
            { $"balance_{account2.Address}", 400L },
            { $"last_vote_{account1.Address}", "increaseA:decreaseB" },
            { $"voting_power_{account1.Address}", 600L },
        });

        var transactionId = _votingAccount.InvokeCastVote(account2, "increaseA:increaseB");

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().Contain(
                new KeyValuePair<string, object>("voted", 1000),
                new KeyValuePair<string, object>("vote_increaseA", 1000),
                new KeyValuePair<string, object>("vote_increaseB", 400),
                new KeyValuePair<string, object>("vote_decreaseB", 600),
                new KeyValuePair<string, object>($"last_vote_{account1.Address}", "increaseA:decreaseB"),
                new KeyValuePair<string, object>($"last_vote_{account2.Address}", "increaseA:increaseB"),
                new KeyValuePair<string, object>($"voting_power_{account1.Address}", 600),
                new KeyValuePair<string, object>($"voting_power_{account2.Address}", 400));
        }
    }
}