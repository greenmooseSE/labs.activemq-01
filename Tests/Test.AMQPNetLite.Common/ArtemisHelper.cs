namespace Test.AMQPNetLite.Common;

using ActiveMQ.Artemis.Client;

public class ArtemisHelper
{
    #region Public members

    public static async Task DeleteAllQueuesAsync(string queuePrefix)
    {
        await using ITopologyManager? artemisToplogyMgr = await ArtemisConnectionSingleton.Instance
            .ArtemisConnection.CreateTopologyManagerAsync();

        IReadOnlyList<string> addresses =
            await artemisToplogyMgr.GetQueueNamesAsync() ?? Array.Empty<string>();

        foreach (var address in addresses.Where(a => a.StartsWith(queuePrefix)))
        {
            //To avoid error AMQ229205: Address AutoTestQueue-7452c193-32f1-4853-9812-fad11ef6bc00 has bindings
            try
            {
                // await topologyManager.DeleteQueueAsync(_topicName, removeConsumers:true, autoDeleteAddress:true);
                Console.WriteLine($"Deleting queue '{address}'");
                await artemisToplogyMgr.DeleteAddressAsync(address, true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Swallowing exception '{e.Message}'.");
            }
        }
    }

    #endregion
}
