namespace AmqpNetLite.Common;

using Amqp;

public class ReceiverLinkWrapper
{
    #region Public members

    public int Credit { get; private set; }
    public CreditMode CreditMode { get; private set; }
    public ReceiverLink ReceiverLink { get; }

    public void SetCredit(int credit, CreditMode creditMode)
    {
        Credit = 0;
        CreditMode = creditMode;
        ReceiverLink.SetCredit(credit, CreditMode);
    }

    public ReceiverLinkWrapper(ReceiverLink receiverLink)
    {
        ReceiverLink = receiverLink;
    }

    #endregion
}

public abstract class Base
{
    public abstract string Foo();
}
