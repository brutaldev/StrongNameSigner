using Brutal.Dev.StrongNameSigner.External;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests.External
{
  [TestFixture]
  public class CorFlagsTests
  {
    private const string TestAssemblyDirectory = @"TestAssemblies";

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
    public void CorFlags_Should_Execute_Correctly()
    {
      using (var corFlags = new CorFlags())
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.Output.ShouldContain(".NET Framework CorFlags Conversion Tool");
      }
    }

    [Test]
    public void CorFlags_Default_Values()
    {
      using (var corFlags = new CorFlags())
      {
        corFlags.AssemblyInfo.ShouldBe(null);
      }
    }

    [Test]
    public void CorFlags_Detects_Signed_20_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-Signed.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(true);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
      }
    }

    [Test]
    public void CorFlags_Detects_Signed_40_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(true);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
      }
    }

    [Test]
    public void CorFlags_Detects_AnyCPU_20_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_AnyCPU_40_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_AnyCPU_45_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(true);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_64bit_20_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(true);
      }
    }

    [Test]
    public void CorFlags_Detects_64bit_40_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(true);
      }
    }

    [Test]
    public void CorFlags_Detects_32bit_20_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_32bit_40_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_Obfuscated_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsSigned.ShouldBe(false);
        corFlags.AssemblyInfo.IsAnyCpu.ShouldBe(true);
        corFlags.AssemblyInfo.Is32BitOnly.ShouldBe(false);
        corFlags.AssemblyInfo.Is32BitPreferred.ShouldBe(false);
        corFlags.AssemblyInfo.Is64BitOnly.ShouldBe(false);
      }
    }

    [Test]
    public void CorFlags_Detects_Correct_Version_20_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsManagedAssembly.ShouldBe(true);
        corFlags.AssemblyInfo.DotNetVersion.ShouldBe("2.0.50727");
      }
    }

    [Test]
    public void CorFlags_Detects_Correct_Version_40_Assembly()
    {
      using (var corFlags = new CorFlags(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe")))
      {
        corFlags.Run(s => output.Append(s)).ShouldBe(true);

        corFlags.AssemblyInfo.IsManagedAssembly.ShouldBe(true);
        corFlags.AssemblyInfo.DotNetVersion.ShouldBe("4.0.30319");
      }
    }
  }
}

