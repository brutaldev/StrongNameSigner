namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Defines the type of strong-name signing that is currently in place.
  /// </summary>
  public enum StrongNameType
  {
    /// <summary>
    /// The assembly contains a public key and is correctly signed.
    /// </summary>
    Signed,

    /// <summary>
    /// The assembly contains a public key but is not actually signed.
    /// </summary>
    /// <remarks><see href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/publicsign-compiler-option"/>
    /// Additional documentation on delay-signing.
    /// </remarks>
    DelaySigned,

    /// <summary>
    /// The assembly contains no public key and is not signed.
    /// </summary>
    NotSigned
  }
}
