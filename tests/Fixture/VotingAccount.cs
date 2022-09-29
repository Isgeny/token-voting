using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests.Fixture;

public class VotingAccount
{
    private const string ScriptPath = "../../../../scripts/token-voting.ride";

    public VotingAccount(ITestOutputHelper testOutputHelper)
    {
        Account = PrivateNode.GenerateAccount();
        var scriptText = File.ReadAllText(ScriptPath);
        var compilationResult = PrivateNode.Instance.Compile(scriptText);
        var compiledScript = compilationResult.Get<string>("script").FromBase64();
        var complexity = compilationResult.Get<long>("complexity");
        testOutputHelper.WriteLine($"Complexity: {complexity}");

        var setScriptTransaction = new SetScriptTransaction(Account.PublicKey, compiledScript, PrivateNode.ChainId);
        PrivateNode.Instance.Broadcast(Account, setScriptTransaction);
    }

    public PrivateKeyAccount Account { get; }

    public void SetData(Dictionary<string, object> entries) => PrivateNode.SetData(Account, entries);

    public Dictionary<string, object> GetData() => PrivateNode.Instance.GetAddressData(Account.Address);

    public string InvokeConstructor(ConstructorOptions options, Dictionary<Asset, decimal>? payment = null) => InvokeConstructor(Account, options, payment);

    public string InvokeConstructor(PrivateKeyAccount callerAccount, ConstructorOptions options, Dictionary<Asset, decimal>? payment = null)
    {
        var arguments = new List<object> { options.AvailableOptions, options.VotingAssetId, options.StartHeight, options.EndHeight, options.QuorumPercent };
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, Account.Address, "constructor", arguments, payment, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }

    public string InvokePut(PrivateKeyAccount callerAccount, Asset asset, decimal quantity) => InvokePut(callerAccount, new Dictionary<Asset, decimal> { { asset, quantity } });

    public string InvokePut(PrivateKeyAccount callerAccount, Dictionary<Asset, decimal> payment)
    {
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, Account.Address, "put", null, payment, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }

    public string InvokeCastVote(PrivateKeyAccount callerAccount, string selectedOption, Dictionary<Asset, decimal>? payment = null)
    {
        var arguments = new List<object> { selectedOption };
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, Account.Address, "castVote", arguments, payment, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }

    public string InvokeWithdraw(PrivateKeyAccount callerAccount, Dictionary<Asset, decimal>? payment = null)
    {
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, Account.Address, "withdraw", null, payment, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }

    public JsonElement GetVotingStatus()
    {
        var votingStatus = PrivateNode.Instance.EvaluateString(Account.Address, "getVotingStatusREADONLY()");
        return JsonDocument.Parse(votingStatus).RootElement;
    }
}