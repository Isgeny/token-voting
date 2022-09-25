using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests;

public class WithdrawTests
{
    private readonly VotingAccount _votingAccount;

    public WithdrawTests()
    {
        _votingAccount = new VotingAccount();
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
        var votingAsset = PrivateNode.IssueAsset(10, 0);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 1, account);

        var invoke = () => _votingAccount.InvokeWithdraw(account, new Dictionary<Asset, decimal> { { votingAsset, 1 } });

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
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 10000000,
            EndHeight = 20000000,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokeWithdraw(account);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not started");
    }

    [Fact]
    public void InvokeWhenVotingIsNotOver_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 10000000,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokeWithdraw(account);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not over");
    }

    [Fact]
    public void InvokeWhenAccountDoesntPut_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 1,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokeWithdraw(account);

        invoke.Should().Throw<Exception>().WithMessage("*Key not exist");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 1,
            QuorumPercent = 50,
        });
        PrivateNode.TransferAsset(votingAsset, 2, _votingAccount.Account);
        PrivateNode.SetData(_votingAccount.Account, $"balance_{account.Address}", 2000000L);

        var transactionId = _votingAccount.InvokeWithdraw(account);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            var actualData = PrivateNode.Instance.GetAddressData(_votingAccount.Account.Address);
            actualData.Should().NotContainKey($"balance_{account.Address}");

            var accountBalance = PrivateNode.Instance.GetBalance(account.Address, votingAsset);
            accountBalance.Should().Be(2);
        }
    }
}