using FluentAssertions.Execution;
using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests;

public class ConstructorTests
{
    private readonly VotingAccount _votingAccount;

    public ConstructorTests()
    {
        _votingAccount = new VotingAccount();
    }

    [Fact]
    public void InvokeFromRandomAccount_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(1, 0);
        var randomAccount = PrivateNode.GenerateAccount();
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(randomAccount, options);

        invoke.Should().Throw<Exception>().WithMessage("*Access denied");
    }

    [Fact]
    public void InvokeWithNotExistAsset_ThrowException()
    {
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = "7KsnSZrrvdAuwvdPi8nVEXJMPqZZJKwtcQJ2TssdouKm",
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Asset not exist");
    }

    [Fact]
    public void InvokeWithStartHeightLargerEndHeight_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(1, 0);
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 200,
            EndHeight = 100,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Start height can't be larger than end height");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(101)]
    public void InvokeWithWrongQuorumPercentValue_ThrowException(long quorumPercent)
    {
        var votingAsset = PrivateNode.IssueAsset(1, 0);
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = quorumPercent,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Quorum percent should be in range [1, 99]");
    }

    [Fact]
    public void InvokeSecondTime_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        _votingAccount.InvokeConstructor(options);

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Already initialized");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(8, 6);
        var options = new ConstructorOptions
        {
            AvailableOptions = "option:yes,option:no",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var expectedData = new Dictionary<string, object>
        {
            { "initialized", true },
            { "available_options", "option:yes,option:no" },
            { "voting_asset", votingAsset.Id },
            { "start_height", 100L },
            { "end_height", 200L },
            { "total", 8000000L },
            { "quorum_percent", 50L },
            { "quorum", 4000000L },
            { "voted", 0L },
        };

        var transactionId = _votingAccount.InvokeConstructor(options);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            var actualData = PrivateNode.Instance.GetAddressData(_votingAccount.Account.Address);
            actualData.Should().BeEquivalentTo(expectedData);
        }
    }
}