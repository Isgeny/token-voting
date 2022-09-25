using TokenVoting.Tests.Extensions;

namespace TokenVoting.Tests.Fixture;

public class NodeFixture
{
    private readonly Node _node;
    private const char ChainId = 'R';
    private const string NodeUrl = "http://localhost:6869";
    private const string MainAccountPrivateKey = "83M4HnCQxrDMzUQqwmxfTVJPTE9WdE7zjAooZZm2jCyV";
    private const string ScriptPath = @"..\..\..\..\scripts\token-voting.ride";

    public NodeFixture()
    {
        _node = new Node(NodeUrl, ChainId);
        MainAccount = PrivateKeyAccount.CreateFromPrivateKey(MainAccountPrivateKey, ChainId);
        PingNode();
    }

    public PrivateKeyAccount MainAccount { get; }

    public void InvokeConstructor(PrivateKeyAccount callerAccount, string votingAddress, Asset votingAsset, long startHeight, long endHeight, long quorumPercent)
    {
        var arguments = new List<object> { votingAsset.Id, startHeight, endHeight, quorumPercent };
        var invokeScriptTransaction = new InvokeScriptTransaction(ChainId, callerAccount.PublicKey, votingAddress, "constructor", arguments, null, 0.005M, Assets.WAVES);
        _node.BroadcastTransaction(callerAccount, invokeScriptTransaction);
    }

    public void InvokeConstructor(PrivateKeyAccount votingAccount, Asset votingAsset, long startHeight, long endHeight, long quorumPercent)
        => InvokeConstructor(votingAccount, votingAccount.Address, votingAsset, startHeight, endHeight, quorumPercent);

    public void SetVotingScript(PrivateKeyAccount account)
    {
        var scriptText = File.ReadAllText(ScriptPath);
        var compiledScript = _node.CompileScript(scriptText);
        var setScriptTransaction = new SetScriptTransaction(account.PublicKey, compiledScript, ChainId);
        _node.BroadcastTransaction(account, setScriptTransaction);
    }

    public Asset IssueVotingAsset()
    {
        var issueTransaction = new IssueTransaction(MainAccount.PublicKey, "Test", null, 30000, 6, true, ChainId);
        var assetId = _node.BroadcastTransaction(MainAccount, issueTransaction);
        return _node.GetAsset(assetId);
    }

    public PrivateKeyAccount GenerateAccount()
    {
        var account = PrivateKeyAccount.CreateFromSeed(PrivateKeyAccount.GenerateSeed(), ChainId);
        var transferTransaction = new TransferTransaction(ChainId, MainAccount.PublicKey, account.Address, Assets.WAVES, 1, 0.005M, Assets.WAVES);
        _node.BroadcastTransaction(MainAccount, transferTransaction);
        return account;
    }

    private void PingNode()
    {
        try
        {
            _node.GetHeight();
        }
        catch (WebException)
        {
            throw new Exception("Run the following docker image: https://github.com/wavesplatform/private-node-docker-image");
        }
    }
}