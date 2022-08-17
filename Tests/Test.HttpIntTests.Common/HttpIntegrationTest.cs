namespace Test.HttpIntTests.Common;

using NUnit.Framework;
using Tests.Common;
using Tests.Common.EnsureExtension;

public abstract class HttpIntegrationTest : UnitTest
{
    #region Public members

    protected HttpClient HttpClient => _httpClient.EnsureNotNull();

    [SetUp]
    public void HttpIntegrationTestSetUp()
    {
        if (_httpClient == null)
        {
            _httpClient = NewHttpClient();
        }
    }

    #endregion

    #region Non-Public members

    private static HttpClient? _httpClient;

    protected abstract HttpClient NewHttpClient();

    #endregion
}
