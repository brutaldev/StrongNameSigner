using Brutal.Dev.StrongNameSigner.External;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Reflection;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests.External
{
  [TestFixture]
  public class ILAsmTests
  {
    private const string TestAssemblyDirectory = "TestAssemblies";

    private readonly StringBuilder output = new StringBuilder();

    private SignTool signTool = new SignTool();

    [TestFixtureSetUp]
    public void TestFixtureSetUp()
    {
      signTool.Run(null);
    }

    [TestFixtureTearDown]
    public void TestFixtureTearDown()
    {
      signTool.Dispose();
    }

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
    public void ILAsm_Should_Execute_Correctly()
    {
      using (var ilasm = new ILAsm())
      {
        ilasm.Run(s => output.Append(s)).ShouldBe(true);

        ilasm.Output.ShouldContain(".NET Framework IL Assembler");
      }
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_20_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe", true);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_40_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe", true);
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_45_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe", true);
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(true);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_20_x86_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe", true);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_40_x86_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe", true);
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_20_x64_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe", true);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void ILAsm_Should_Reassemble_NET_40_x64_Assembly_Correctly()
    {
      var info = RoundTrip("Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe", true);
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
      info.IsSigned.ShouldBe(true);
    }

    private AssemblyInfo RoundTrip(string fileName, bool signFile)
    {
      // Disassemble first
      var info = SigningHelper.GetAssemblyInfo(Path.Combine(TestAssemblyDirectory, fileName));
      using (var ildasm = new ILDasm(info))
      {
        ildasm.Run(s => output.Append(s)).ShouldBe(true);

        using (var ilasm = new ILAsm(info, ildasm.BinaryILFilePath, signFile ? signTool.KeyFilePath : string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
        {
          ilasm.Run(s => output.Append(s)).ShouldBe(true);

          return SigningHelper.GetAssemblyInfo(ilasm.SignedAssemblyPath);
        }
      }
    }
  }
}
