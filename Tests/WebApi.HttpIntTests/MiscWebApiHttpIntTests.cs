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

    [Test]
    public void CanGetSwaggerJson()
    {
        HttpResponseMessage res = HttpClient.GetAsync("/swagger/v1/swagger.json").Result;
        res.EnsureSuccessStatusCode();


    }
}
