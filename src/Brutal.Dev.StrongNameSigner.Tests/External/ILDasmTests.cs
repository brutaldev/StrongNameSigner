using Brutal.Dev.StrongNameSigner.External;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests.External
{
  [TestFixture]
  public class ILDasmTests
  {
    private const string TestAssemblyDirectory = "TestAssemblies";

    private readonly StringBuilder output = new StringBuilder();

    [SetUp]
    public void SetUp()
    {
      output.Clear();
    }

    [TearDown]
    public void TearDown()
    {
      if (TestContext.CurrentContext.Result.Status == TestStatus.Failed)
      {
        //Assert.Fail(output.ToString());
      }
    }

    [Test]
    public void ILDasm_Should_Execute_Correctly()
    {
      using (var ildasm = new ILDasm())
      {
        ildasm.Run(s => output.Append(s)).ShouldBe(true);

        output.ToString().ShouldContain(".NET Framework IL Disassembler");
      }
    }

    [Test]
    public void ILDasm_Output_Directory_Should_Be_Removed_When_Disposed()
    {
      string outputPath = string.Empty;

      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);
        using (var ildasm = new ILDasm(corFlags.AssemblyInfo))
        {
          outputPath = ildasm.WorkingPath;
          ildasm.Run(s => output.Append(s)).ShouldBe(true);

          Directory.Exists(outputPath).ShouldBe(true);
        }

        Directory.Exists(outputPath).ShouldBe(false);
      }
    }

    [Test]
    public void ILDasm_Should_Disassemble_All_Assembly_Types()
    {
      foreach (var assembly in Directory.GetFiles(TestAssemblyDirectory, "*.exe", SearchOption.TopDirectoryOnly).Where(a => !a.Contains("Obfuscated")))
      {
        using (var corFlags = new CorFlags(assembly))
        {
          corFlags.Run(s => output.Append(s)).ShouldBe(true);

          using (var ildasm = new ILDasm(corFlags.AssemblyInfo))
          {
            ildasm.Run(s => output.Append(s)).ShouldBe(true);

            Directory.Exists(ildasm.WorkingPath).ShouldBe(true);
            Directory.GetFiles(ildasm.WorkingPath).ShouldContain(f => f.EndsWith(".il"));
            Directory.GetFiles(ildasm.WorkingPath).ShouldContain(f => f.EndsWith(".binary.il"));
          }
        }
      }
    }

    [Test]
    public void ILDasm_Should_Fail_To_Disassemble_Obfuscated_Assemblies()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        using (var ildasm = new ILDasm(corFlags.AssemblyInfo))
        {
          ildasm.Run(s => output.Append(s)).ShouldBe(false);
          ildasm.Output.ShouldContain("cannot disassemble");
        }
      }
    }
  }
}
