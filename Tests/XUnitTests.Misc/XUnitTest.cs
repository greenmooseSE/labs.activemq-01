namespace XUnitTests.Misc;

using System;
using System.Linq;
using ActiveMqLabs01.Common;
using Tests.Common;
using Xunit;

public class XUnitTest : UnitTest, ICommonTest
{
    [Fact]
    public void CanResolveMessageProducer()
    {
        var inst = Resolve<MessageProducer>();
        Assert.NotNull(inst);
    }

}
