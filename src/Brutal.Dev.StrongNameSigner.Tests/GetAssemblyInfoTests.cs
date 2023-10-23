using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  public class GetAssemblyInfoTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestAssemblies");

    [Fact]
    public void GetAssemblyInfo_Public_API_Test()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.Should().NotBe(null);
      info.DotNetVersion.Should().Be("2.0.50727");
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.IsSigned.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      Action act = () => new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist"));
      act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void GetAssemblyInfo_Public_API_Invalid_File_Should_Throw_Exception()
    {
      Action act = () => new AssemblyInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe"));
      act.Should().Throw<BadImageFormatException>();
    }

    [Fact]
    public void GetAssemblyInfo_Detects_Signed_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-Signed.exe"));
      info.Should().NotBe(null);
      info.IsSigned.Should().Be(true);
      info.IsAnyCpu.Should().Be(true);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_Signed_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe"));
      info.IsSigned.Should().Be(true);
      info.IsAnyCpu.Should().Be(true);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_AnyCPU_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_AnyCPU_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_AnyCPU_45_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(true);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_64bit_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(false);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(true);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_64bit_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(false);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(true);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_32bit_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(false);
      info.Is32BitOnly.Should().Be(true);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_32bit_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(false);
      info.Is32BitOnly.Should().Be(true);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_Obfuscated_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe"));
      info.IsSigned.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
    }

    [Fact]
    public void GetAssemblyInfo_Detects_Correct_Version_20_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"));
      info.IsManagedAssembly.Should().Be(true);
      info.DotNetVersion.Should().Be("2.0.50727");
    }

    [Fact]
    public void GetAssemblyInfo_Detects_Correct_Version_40_Assembly()
    {
      var info = new AssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"));
      info.IsManagedAssembly.Should().Be(true);
      info.DotNetVersion.Should().Be("4.0.30319");
    }
  }
}
