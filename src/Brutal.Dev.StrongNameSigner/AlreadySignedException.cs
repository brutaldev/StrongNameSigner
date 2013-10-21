using System;
using System.Runtime.Serialization;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Exception that gets throw when an attempt is made to strong name sign an assembly that already has a strong name signature.
  /// </summary>
  [Serializable]
  public class AlreadySignedException : Exception, ISerializable
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="AlreadySignedException"/> class.
    /// </summary>
    public AlreadySignedException()
      : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlreadySignedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AlreadySignedException(string message)
      : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlreadySignedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception or a null reference if no inner exception is specified.</param>
    public AlreadySignedException(string message, Exception inner)
      : base(message, inner)
    {
    }

    // This constructor is needed for serialization.
    /// <summary>
    /// Initializes a new instance of the <see cref="AlreadySignedException"/> class.
    /// </summary>
    /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
    protected AlreadySignedException(SerializationInfo info, StreamingContext context)
      : base(info, context)
    {
    }
  }
}
