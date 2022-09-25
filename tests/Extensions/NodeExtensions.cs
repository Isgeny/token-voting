namespace TokenVoting.Tests.Extensions;

public static class NodeExtensions
{
    public static string BroadcastTransaction(this Node node, PrivateKeyAccount sender, Transaction transaction)
    {
        transaction.Sign(sender);
        var response = node.Broadcast(transaction);
        var transactionId = response.ParseJsonObject().GetString("id");

        for (var i = 0; i < 50; i++)
        {
            var writtenTransaction = node.GetTransactionByIdOrNull(transactionId);
            if (writtenTransaction is not null)
            {
                return transactionId;
            }

            Thread.Sleep(100);
        }

        throw new Exception("Transaction was not written");
    }
}