namespace WebApi.HttpIntHostServiceTests;

using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using RestApi.Common.EnsureExtension;
using WebApi.BackgroundServices;
using WebApi.Models;
using WebApi.Tests.HttpIntTests.Common;

[TestFixture]
internal class MiscWebApiHttpIntHostServiceTests : WebApiHttpIntTest
{
    /// <inheritdoc />
    protected override bool DoRegisterWebApiHostServices
    {
        get
        {
            StatisticsBackgroundService.IterationDelay = TimeSpan.FromMilliseconds(10);
            return true;
        }
    }

    [SetUp]
    public void MiscWebApiHttpIntHostServiceTestsSetUp()
    {
        StatisticsBackgroundService.IterationDelay = TimeSpan.FromMilliseconds(10);
    }

    [TearDown]
    public void MiscWebApiHttpIntHostServiceTestsTearDown()
    {
        StatisticsBackgroundService.IterationDelay = TimeSpan.FromSeconds(1);
    }

    [Test]
    public void CanFetchHealthEndpoint()
    {
        //Verifies setup is working properly
        HttpResponseMessage res = HttpClient.GetAsync("/v1/health").Result;
        res.EnsureSuccessStatusCode();
    }

    ///<summary>For whatever reason we cannot rerun this test in a process. Investigate it some rainy day.</summary>
    [Test]
    [NCrunch.Framework.Isolated]
    public void StatisticsServiceIsRefreshingIterationCount()
    {
        HealthVm res = HttpClient.GetFromJsonAsync<HealthVm>("/v1/health").Result.EnsureNotNull();

        LogInfo($"Received: {JsonSerializer.Serialize(res)}");

        var cancellationToken = new CancellationTokenSource();
        void OnIterated()
        {
            cancellationToken.Cancel();
        }

        StatisticsBackgroundService.OnIteration += OnIterated;
        try
        {
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await Task.Delay(1000, cancellationToken.Token));

            HealthVm res2 = HttpClient.GetFromJsonAsync<HealthVm>("/v1/health").Result.EnsureNotNull();
            LogInfo($"Received: {JsonSerializer.Serialize(res2)}");

            res2.StatisticsServiceWorkIterationCount.Should()
                .BeGreaterThan(res.StatisticsServiceWorkIterationCount);
        }
        finally
        {
            StatisticsBackgroundService.OnIteration -= OnIterated;
        }
    }
}
