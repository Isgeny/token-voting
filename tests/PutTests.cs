using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests;

public class PutTests
{
    private readonly VotingAccount _votingAccount;

    public PutTests()
    {
        _votingAccount = new VotingAccount();
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
        var votingAsset = PrivateNode.IssueAsset(10, 0);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 1, account);

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 1);

        invoke.Should().Throw<Exception>().WithMessage("*Not initialized");
    }

    [Fact]
    public void InvokeWithMultiplePayments_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var anotherAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 1, account);
        PrivateNode.TransferAsset(anotherAsset, 1, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        });
        var payments = new Dictionary<Asset, decimal>
        {
            { votingAsset, 1 },
            { anotherAsset, 1 },
        };

        var invoke = () => _votingAccount.InvokePut(account, payments);

        invoke.Should().Throw<Exception>().WithMessage("*Only one payment is allowed");
    }

    [Fact]
    public void InvokeWithWrongPaymentAsset_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var anotherAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(anotherAsset, 1, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokePut(account, anotherAsset, 1);

        invoke.Should().Throw<Exception>().WithMessage("*Wrong asset");
    }

    [Fact]
    public void InvokeWhenVotingIsNotStarted_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 1, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 10000000,
            EndHeight = 20000000,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 1);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is not started");
    }

    [Fact]
    public void InvokeWhenVotingIsOver_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 1, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 1,
            QuorumPercent = 50,
        });

        var invoke = () => _votingAccount.InvokePut(account, votingAsset, 1);

        invoke.Should().Throw<Exception>().WithMessage("*Voting is over");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 10000000,
            QuorumPercent = 50,
        });

        var transactionId = _votingAccount.InvokePut(account, votingAsset, 3);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            var actualData = PrivateNode.Instance.GetAddressData(_votingAccount.Account.Address);
            actualData.Should().ContainKey($"balance_{account.Address}").WhoseValue.Should().Be(3000000L);
        }
    }

    [Fact]
    public void InvokeSecondTime_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 10000000,
            QuorumPercent = 50,
        });

        _votingAccount.InvokePut(account, votingAsset, 3);
        var transactionId = _votingAccount.InvokePut(account, votingAsset, 1);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            var actualData = PrivateNode.Instance.GetAddressData(_votingAccount.Account.Address);
            actualData.Should().ContainKey($"balance_{account.Address}").WhoseValue.Should().Be(4000000L);
        }
    }

    [Fact]
    public void InvokeByDifferentAccounts_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var account1 = PrivateNode.GenerateAccount();
        var account2 = PrivateNode.GenerateAccount();
        PrivateNode.TransferAsset(votingAsset, 4, account1);
        PrivateNode.TransferAsset(votingAsset, 2, account2);
        _votingAccount.InvokeConstructor(new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 1,
            EndHeight = 10000000,
            QuorumPercent = 50,
        });

        var transactionId1 = _votingAccount.InvokePut(account1, votingAsset, 4);
        var transactionId2 = _votingAccount.InvokePut(account2, votingAsset, 2);

        using (new AssertionScope())
        {
            transactionId1.Should().NotBeEmpty();
            transactionId2.Should().NotBeEmpty();

            var actualData = PrivateNode.Instance.GetAddressData(_votingAccount.Account.Address);
            actualData.Should().ContainKey($"balance_{account1.Address}").WhoseValue.Should().Be(4000000L);
            actualData.Should().ContainKey($"balance_{account2.Address}").WhoseValue.Should().Be(2000000L);
        }
    }
}