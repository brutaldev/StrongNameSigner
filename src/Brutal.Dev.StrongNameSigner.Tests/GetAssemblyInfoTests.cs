using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Reflection;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class GetAssemblyInfoTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestAssemblies");

    [Test]
    public void GetAssemblyInfo_Public_API_Test()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.ShouldNotBe(null);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.IsSigned.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      Assert.Throws<FileNotFoundException>(() => new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist")));
    }

    [Test]
    public void GetAssemblyInfo_Public_API_Invalid_File_Should_Throw_Exception()
    {
      Assert.Throws<BadImageFormatException>(() => new AssemblyInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe")));
    }

    [Test]
    public void GetAssemblyInfo_Detects_Signed_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-Signed.exe"));
      info.ShouldNotBe(null);
      info.IsSigned.ShouldBe(true);
      info.IsAnyCpu.ShouldBe(true);
    }

    [Test]
    public void GetAssemblyInfo_Detects_Signed_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe"));
      info.IsSigned.ShouldBe(true);
      info.IsAnyCpu.ShouldBe(true);
    }

    [Test]
    public void GetAssemblyInfo_Detects_AnyCPU_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_AnyCPU_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_AnyCPU_45_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(true);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_64bit_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
    }

    [Test]
    public void GetAssemblyInfo_Detects_64bit_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
    }

    [Test]
    public void GetAssemblyInfo_Detects_32bit_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_32bit_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe"));
      info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_Obfuscated_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe")); info.IsSigned.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
    }

    [Test]
    public void GetAssemblyInfo_Detects_Correct_Version_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.IsManagedAssembly.ShouldBe(true);
      info.DotNetVersion.ShouldBe("2.0.50727");
    }

    [Test]
    public void GetAssemblyInfo_Detects_Correct_Version_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"));
      info.IsManagedAssembly.ShouldBe(true);
      info.DotNetVersion.ShouldBe("4.0.30319");
    }
  }
}
