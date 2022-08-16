namespace Test.AMQPNetLite.Common;

using ActiveMQ.Artemis.Client;
using Tests.Common.EnsureExtension;

internal class ArtemisConnectionSingleton
{
    #region Public members

    public IConnection ArtemisConnection { get; }

    public static ArtemisConnectionSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ArtemisConnectionSingleton(_connectionFactory
                            .CreateAsync(_endpoint.EnsureNotNull(nameof(_endpoint)))
                            .Result);
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
        if (_endpoint == null)
        {
            lock (_endpointLock)
            {
                if (_endpoint == null)
                {
                    _endpoint = Endpoint.Create(host, port, user, pwd).EnsureNotNull();
                }
            }
        }
    }

    #endregion

    #region Non-Public members

    private ArtemisConnectionSingleton(IConnection connection)
    {
        ArtemisConnection = connection;
    }

    private static readonly ConnectionFactory _connectionFactory = new();

    private static volatile Endpoint? _endpoint;
    private static readonly object _endpointLock = new();

    private static ArtemisConnectionSingleton? _instance;


    private static readonly object _instanceLock = new();

    #endregion
}
