using TokenVoting.Tests.Extensions;
using TokenVoting.Tests.Models;

namespace TokenVoting.Tests.Fixture;

public class VotingAccount
{
    private const string ScriptPath = "../../../../scripts/token-voting.ride";
    private readonly PrivateKeyAccount _account;

    public VotingAccount()
    {
        _account = PrivateNode.GenerateAccount();
        var scriptText = File.ReadAllText(ScriptPath);
        var compiledScript = PrivateNode.Instance.CompileScript(scriptText);
        var setScriptTransaction = new SetScriptTransaction(_account.PublicKey, compiledScript, PrivateNode.ChainId);
        PrivateNode.Instance.Broadcast(_account, setScriptTransaction);
    }

    public string Address => _account.Address;

    public string InvokeConstructor(ConstructorOptions options) => InvokeConstructor(_account, options);

    public string InvokeConstructor(PrivateKeyAccount callerAccount, ConstructorOptions options)
    {
        var arguments = new List<object> { options.AvailableOptions, options.VotingAssetId, options.StartHeight, options.EndHeight, options.QuorumPercent };
        var invokeScriptTransaction = new InvokeScriptTransaction(PrivateNode.ChainId, callerAccount.PublicKey, _account.Address, "constructor", arguments, null, 0.005M, Assets.WAVES);
        return PrivateNode.Instance.Broadcast(callerAccount, invokeScriptTransaction);
    }
}