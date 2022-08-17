namespace WebApi.HttpIntTests;

using NUnit.Framework;
using WebApi.Tests.HttpIntTests.Common;

[TestFixture]
internal class MiscWebApiHttpIntTests : WebApiHttpIntTest
{
    [Test]
    public void CanGetHealth()
    {
        HttpResponseMessage res = HttpClient.GetAsync("/v1/health").Result;
        res.EnsureSuccessStatusCode();
    }
}
