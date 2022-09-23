namespace WebApi.Tests.HttpIntTests.Common;

using System;
using System.Linq;
using System.Collections.Generic;
using global::Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.HttpIntTests.Common;
using WebApi.BackgroundServices;

[TestFixture]
public abstract class WebApiHttpIntTest : HttpIntegrationTestG<Program>
{
}

