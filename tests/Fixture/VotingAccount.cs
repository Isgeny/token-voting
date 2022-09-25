using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests.Fixture;

public class VotingAccount
{
    private const string ScriptPath = "../../../../scripts/token-voting.ride";

    public VotingAccount()
    {
        Account = PrivateNode.GenerateAccount();
        var scriptText = File.ReadAllText(ScriptPath);
        var compiledScript = PrivateNode.Instance.CompileScript(scriptText);
        var setScriptTransaction = new SetScriptTransaction(Account.PublicKey, compiledScript, PrivateNode.ChainId);
        PrivateNode.Instance.Broadcast(Account, setScriptTransaction);
    }

    public PrivateKeyAccount Account { get; }

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

    public string InvokeWithdraw(PrivateKeyAccount callerAccount, Dictionary<Asset, decimal>? payment = null)
    {
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, Account.Address, "withdraw", null, payment, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }
}