using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Fixture;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests;

public class ConstructorTests
{
    private readonly VotingAccount _votingAccount;

    public ConstructorTests(ITestOutputHelper testOutputHelper)
    {
        _votingAccount = new VotingAccount(testOutputHelper);
    }

    [Fact]
    public void InvokeFromNonAdminAccount_ThrowException()
    {
        var account = PrivateNode.GenerateAccount();
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
            VotingAssetId = PrivateNode.FakeAssetId,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(account, options);

        invoke.Should().Throw<Exception>().WithMessage("*Access denied");
    }

    [Fact]
    public void InvokeSecondTime_ThrowException()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
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
    public void InvokeWithPayment_ThrowException()
    {
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
            VotingAssetId = PrivateNode.FakeAssetId,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options, new Dictionary<Asset, decimal> { { Assets.WAVES, 0.1M } });

        invoke.Should().Throw<Exception>().WithMessage("*Payments are prohibited");
    }

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData(",,")]
    [InlineData(" , ")]
    [InlineData("a , b")]
    [InlineData(",a,,bc,")]
    [InlineData("abc")]
    [InlineData(":")]
    [InlineData("::")]
    [InlineData(":,:")]
    public void InvokeWithWrongAvailableOptions_ThrowException(string availableOptions)
    {
        var options = new ConstructorOptions
        {
            AvailableOptions = availableOptions,
            VotingAssetId = PrivateNode.FakeAssetId,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Options are not in valid format");
    }

    [Fact]
    public void InvokeWithNotExistAsset_ThrowException()
    {
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
            VotingAssetId = PrivateNode.FakeAssetId,
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
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
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
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = quorumPercent,
        };

        var invoke = () => _votingAccount.InvokeConstructor(options);

        invoke.Should().Throw<Exception>().WithMessage("*Quorum percent should be in range [1, 99]");
    }

    [Fact]
    public void Invoke_Success()
    {
        var votingAsset = PrivateNode.IssueAsset(10, 2);
        var options = new ConstructorOptions
        {
            AvailableOptions = "increaseA,decreaseA",
            VotingAssetId = votingAsset.Id,
            StartHeight = 100,
            EndHeight = 200,
            QuorumPercent = 50,
        };

        var transactionId = _votingAccount.InvokeConstructor(options);

        using (new AssertionScope())
        {
            transactionId.Should().NotBeEmpty();

            _votingAccount.GetData().Should().BeEquivalentTo(new Dictionary<string, object>
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
        }
    }
}