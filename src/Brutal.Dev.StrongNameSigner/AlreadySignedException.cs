using System;
using System.Runtime.Serialization;

namespace Brutal.Dev.StrongNameSigner
{
  [Serializable]
  public class AlreadySignedException : Exception, ISerializable
  {
    public AlreadySignedException()
      : base()
    {
    }

    public AlreadySignedException(string message)
      : base(message)
    {
    }

    public AlreadySignedException(string message, Exception inner)
      : base(message, inner)
    {
    }

    // This constructor is needed for serialization.
    protected AlreadySignedException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}
