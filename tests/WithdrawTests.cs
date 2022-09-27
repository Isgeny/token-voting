using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;

namespace TokenVoting.Tests;

public class WithdrawTests
{
    private readonly VotingAccount _votingAccount;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _votingAccount = new VotingAccount(testOutputHelper);
    }

    [Fact]
    public void InvokeFromAdminAccount_ThrowException()
    {
        var invoke = () => _votingAccount.InvokeWithdraw(_votingAccount.Account);

        invoke.Should().Throw<Exception>().WithMessage("*Access denied");
    }

    [Fact]
    public void InvokeWithPayments_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();

        var invoke = () => _votingAccount.InvokeWithdraw(account, new Dictionary<Asset, decimal> { { Assets.WAVES, 0.1M } });

        invoke.Should().Throw<Exception>().WithMessage("*Payments are prohibited");
    }

    [Fact]
    public void InvokeWhenNotInitialized_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();

        var invoke = () => _votingAccount.InvokeWithdraw(account);

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

        var invoke = () => _votingAccount.InvokeWithdraw(account);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not started");
    }

    [Fact]
    public void InvokeWhenVotingIsNotOver_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", PrivateNode.FakeAssetId },
            { "start_height", 1L },
            { "end_height", 10000000L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
        });

        var invoke = () => _votingAccount.InvokeWithdraw(account);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not over");
    }

    [Fact]
    public void InvokeWhenAccountDoesntPut_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var account1 = PrivateNode.GenerateAccount();
        var account2 = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, _votingAccount.Account);
        _votingAccount.SetData(new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "increaseA,decreaseA" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 1L },
            { "end_height", 1L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { $"balance_{account2.Address}", 400L },
        });

        var invoke = () => _votingAccount.InvokeWithdraw(account1);

        invoke.Should().Throw<Exception>().WithMessage("*Key not exist");
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
            { "end_height", 1L },
            { "total", 1000L },
            { "quorum_percent", 50L },
            { "quorum", 500L },
            { $"balance_{account.Address}", 400L },
        });

        var transactionId = _votingAccount.InvokeWithdraw(account);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().NotContainKey($"balance_{account.Address}");

            PrivateNode.Instance.GetBalance(account.Address, votingAsset).Should().Be(4);
        }
    }
}