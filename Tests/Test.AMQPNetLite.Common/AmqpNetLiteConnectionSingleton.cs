namespace Test.AMQPNetLite.Common;

using Amqp;
using RestApi.Common.EnsureExtension;

internal class AmqpNetLiteConnectionSingleton
{
    #region Public members

    public Connection AmqpNetLiteConnection { get; }

    public static AmqpNetLiteConnectionSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance =
                            new AmqpNetLiteConnectionSingleton(
                                new Connection(_address.EnsureNotNull(nameof(_address))));
                    }
                }
            }

            return _instance;
        }
    }

    public static void Configure(string host = "localhost",
        int port = 5672,
        string user = "admin",
        string pwd = "admin")
    {
        if (_address == null)
        {
            lock (_addressLock)
            {
                if (_address == null)
                {
                    _address = new Address($"amqp://{user}:{pwd}@{host}:{port}");
                }
            }
        }
    }

    #endregion

    #region Non-Public members

    private AmqpNetLiteConnectionSingleton(Connection connection)
    {
        AmqpNetLiteConnection = connection;
    }

    private static volatile Address? _address;
    private static readonly object _addressLock = new();

    private static volatile AmqpNetLiteConnectionSingleton? _instance;
    private static readonly object _instanceLock = new();

    #endregion
}
