namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// An input/output pair of file paths used for providing the option to write files to another location.
  /// </summary>
  public class InputOutputFilePair
  {
    /// <summary>
    /// Initializes a new instance of the InputOutputPair class.
    /// </summary>
    /// <param name="inputFilePath">Full file path of the input file.</param>
    /// <param name="outputFilePath">Full file path of the output file.</param>
    public InputOutputFilePair(string inputFilePath, string outputFilePath)
    {
      InputFilePath = inputFilePath;
      OutFilePath = outputFilePath;
    }

    /// <summary>
    /// Gets the full path of the file to process.
    /// </summary>
    /// <value>
    /// The full pathname of the input file.
    /// </value>
    public string InputFilePath { get; }

    /// <summary>
    /// Gets the full path of where to write the file.
    /// </summary>
    /// <value>
    /// The full pathname of the out file.
    /// </value>
    public string OutFilePath { get; }
  }
}
