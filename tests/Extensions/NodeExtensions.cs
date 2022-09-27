namespace TokenVoting.Tests.Extensions;

public static class PrivateNode
{
    private static readonly PrivateKeyAccount MainAccount;
    private static readonly object Lock = new();

    public const char ChainId = 'R';
    public const string FakeAssetId = "7KsnSZrrvdAuwvdPi8nVEXJMPqZZJKwtcQJ2TssdouKm";
    public static readonly Node Instance;

    static PrivateNode()
    {
        MainAccount = PrivateKeyAccount.CreateFromPrivateKey("83M4HnCQxrDMzUQqwmxfTVJPTE9WdE7zjAooZZm2jCyV", ChainId);
        Instance = new Node("http://localhost:6869", ChainId);
        try
        {
            Instance.GetHeight();
        }
        catch (WebException)
        {
            throw new Exception("Run the following docker image: https://github.com/wavesplatform/private-node-docker-image");
        }
    }

    public static PrivateKeyAccount GenerateAccount()
    {
        var seed = PrivateKeyAccount.GenerateSeed();

        // Something wrong with multi-thread in library
        PrivateKeyAccount account;
        lock (Lock)
        {
            account = PrivateKeyAccount.CreateFromSeed(seed, ChainId);
        }

        var transferTransaction = new TransferTransaction(ChainId, MainAccount.PublicKey, account.Address, Assets.WAVES, 1, 0.005M, Assets.WAVES);
        Instance.Broadcast(MainAccount, transferTransaction);
        return account;
    }

    public static Asset IssueAsset(decimal quantity, byte decimals, string? name = "Test", bool reissuable = true)
    {
        var issueTransaction = new IssueTransaction(MainAccount.PublicKey, name, null, quantity, decimals, reissuable, ChainId);
        var assetId = Instance.Broadcast(MainAccount, issueTransaction);
        return Instance.GetAsset(assetId);
    }

    public static void TransferAsset(Asset asset, decimal quantity, PrivateKeyAccount recipient)
    {
        var transferTransaction = new TransferTransaction(ChainId, MainAccount.PublicKey, recipient.Address, asset, quantity, 0.005M, Assets.WAVES);
        Instance.Broadcast(MainAccount, transferTransaction);
    }

    public static void SetData(PrivateKeyAccount account, Dictionary<string, object> entries)
    {
        var dataTransaction = new DataTransaction(ChainId, account.PublicKey, entries);
        Instance.Broadcast(account, dataTransaction);
    }

    public static string Broadcast(this Node node, PrivateKeyAccount sender, Transaction transaction)
    {
        transaction.Sign(sender);
        var response = node.Broadcast(transaction);
        var transactionId = response.ParseJsonObject().GetString("id");

        for (var i = 0; i < 100; i++)
        {
            var writtenTransaction = node.GetTransactionByIdOrNull(transactionId);
            if (writtenTransaction is not null)
            {
                return transactionId;
            }

            Thread.Sleep(500);
        }

        throw new Exception("Transaction was not written");
    }

    public static Dictionary<string, object> Compile(this Node node, string script) => node.Post("/utils/script/compile", script).ParseJsonObject();
}