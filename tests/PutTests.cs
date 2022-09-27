using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;

namespace TokenVoting.Tests;

public class PutTests
{
    private readonly VotingAccount _votingAccount;

    public PutTests(ITestOutputHelper testOutputHelper)
    {
        _votingAccount = new VotingAccount(testOutputHelper);
    }

    [Fact]
    public void InvokeFromAdminAccount_ThrowException()
    {
        var invoke = () => _votingAccount.InvokePut(_votingAccount.Account, new Dictionary<Asset, decimal>());

        invoke.Should().Throw<Exception>().WithMessage("*Access denied");
    }

    [Fact]
    public void InvokeWithoutPayments_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();

        var invoke = () => _votingAccount.InvokePut(account, new Dictionary<Asset, decimal>());

        invoke.Should().Throw<Exception>().WithMessage("*Only one payment is allowed");
    }

    [Fact]
    public void InvokeWhenNotInitialized_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 4);

        invoke.Should().Throw<Exception>().WithMessage("*Not initialized");
    }

    [Fact]
    public void InvokeWithMultiplePayments_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 100L },
            { "end_height", 200L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });
        var payments = new Dictionary<Asset, decimal>
        {
            { votingAsset, 4 },
            { Assets.WAVES, 0.1M },
        };

        var invoke = () => _votingAccount.InvokePut(account, payments);

        invoke.Should().Throw<Exception>().WithMessage("*Only one payment is allowed");
    }

    [Fact]
    public void InvokeWithWrongPaymentAsset_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 100L },
            { "end_height", 200L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokePut(account, Assets.WAVES, 0.1M);

        invoke.Should().Throw<Exception>().WithMessage("*Wrong asset");
    }

    [Fact]
    public void InvokeWhenVotingIsNotStarted_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 10000000L },
            { "end_height", 20000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 4);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not started");
    }

    [Fact]
    public void InvokeWhenVotingIsOver_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 1L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 4);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is over");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
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
        });

        var transactionId = _votingAccount.InvokePut(account, votingAsset, 3);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().ContainKey($"balance_{account.Address}").WhoseValue.Should().Be(300);
        }
    }

    [Fact]
    public void InvokeSecondTime_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
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
        });

        _votingAccount.InvokePut(account, votingAsset, 3);
        var transactionId = _votingAccount.InvokePut(account, votingAsset, 1);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().ContainKey($"balance_{account.Address}").WhoseValue.Should().Be(400);
        }
    }

    [Fact]
    public void InvokeByDifferentAccounts_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account1 = PrivateNode.GenerateAccount();
        var account2 = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account1);
        PrivateNode.TransferAsset(votingAsset, 2, account2);
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
        });

        var transactionId1 = _votingAccount.InvokePut(account1, votingAsset, 4);
        var transactionId2 = _votingAccount.InvokePut(account2, votingAsset, 2);

        using (new AssertionScope())
        {
            transactionId1.Should().NotBeEmpty();
            transactionId2.Should().NotBeEmpty();

            var actualData = _votingAccount.GetData();
            actualData.Should().ContainKey($"balance_{account1.Address}").WhoseValue.Should().Be(400);
            actualData.Should().ContainKey($"balance_{account2.Address}").WhoseValue.Should().Be(200);
        }
    }
}