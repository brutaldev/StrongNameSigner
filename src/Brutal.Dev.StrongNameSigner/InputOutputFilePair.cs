using System.IO;

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
      OutputFilePath = outputFilePath;
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
    public string OutputFilePath { get; }

    /// <summary>
    /// Gets the path to the .PDB file of the target assembly.
    /// </summary>
    /// <remarks>
    /// Does not check for existence of the file, use <see cref="HasSymbols"/> for existence check.
    /// </remarks>
    public string InputPdbPath => Path.ChangeExtension(InputFilePath, ".pdb");

    /// <summary>
    /// Gets if both input and output file paths are the same file.
    /// </summary>
    /// <value>
    /// <c>true</c> if the paths are the same, <c>false</c> if they are not.
    /// </value>
    public bool IsSameFile => InputFilePath == OutputFilePath;

    /// <summary>
    /// Gets the path to an unsigned backup of the input assembly.
    /// </summary>
    public string BackupAssemblyPath => InputFilePath + ".unsigned";

    /// <summary>
    /// Gets the path to where a backup of the source .PDB file.
    /// </summary>
    public string BackupPdbPath => InputPdbPath + ".unsigned";

    /// <summary>
    /// Gets a value indicating whether the input assembly has a matching PDB file.
    /// </summary>
    public bool HasSymbols => File.Exists(InputPdbPath);
  }
}
