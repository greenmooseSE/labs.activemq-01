namespace NUnitTests.Misc;

using System;
using System.Linq;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMqLabs01.Common;
using NUnit.Framework;
using Tests.Common;

[TestFixture]
internal class MiscTest : NUnitTest, ICommonTest
{
 
    [Test]
    public void CanResolveMesageProducer()
    {
        var inst = Resolve<MessageProducer>();
        Assert.IsNotNull(inst);
    }
}
