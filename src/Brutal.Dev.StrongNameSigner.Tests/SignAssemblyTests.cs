using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  public class SignAssemblyTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestAssemblies");

    [Fact]
    public void SignAssembly_Public_API_Test()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.Should().NotBe(null);
      info.DotNetVersion.Should().Be("2.0.50727");
      info.Is32BitOnly.Should().Be(false);
      info.Is32BitPreferred.Should().Be(false);
      info.Is64BitOnly.Should().Be(false);
      info.IsAnyCpu.Should().Be(true);
      info.IsSigned.Should().Be(true);
    }

    [Fact]
    public void SignAssembly_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist")).Dispose();
      act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Password_Not_Provided()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx")).Dispose();
      act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Wrong_Password_Provided()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "oops").Dispose();
      act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Work_With_Correct_Password()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "password123").Dispose();
      act.Should().NotThrow<CryptographicException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_Directory()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), "C:\\DoesNotExist\\KeyFile.snk");
      act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_File()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), "C:\\KeyFileThatDoesNotExist.snk");
      act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("2.0.50727");
        info.IsAnyCpu.Should().Be(true);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("4.0.30319");
        info.IsAnyCpu.Should().Be(true);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_45_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("4.0.30319");
        info.IsAnyCpu.Should().Be(true);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(true);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_x86_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("2.0.50727");
        info.IsAnyCpu.Should().Be(false);
        info.Is32BitOnly.Should().Be(true);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_x86_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("4.0.30319");
        info.IsAnyCpu.Should().Be(false);
        info.Is32BitOnly.Should().Be(true);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_x64_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("2.0.50727");
        info.IsAnyCpu.Should().Be(false);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(true);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_x64_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("4.0.30319");
        info.IsAnyCpu.Should().Be(false);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(true);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_Core_5_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.Core5.dll"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.Should().Be("4.0.30319");
        info.IsAnyCpu.Should().Be(true);
        info.Is32BitOnly.Should().Be(false);
        info.Is32BitPreferred.Should().Be(false);
        info.Is64BitOnly.Should().Be(false);
        info.IsSigned.Should().Be(true);
      }
    }

    [Fact]
    public void SignAssembly_InPlaceWithPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        string targetAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), targetAssemblyPath);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"), Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"));

        using (var info = SigningHelper.SignAssembly(targetAssemblyPath))
        {
          info.IsSigned.Should().BeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_NewLocationWithPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      var outDir = Path.Combine(tempDir, "out");
      Directory.CreateDirectory(outDir);
      try
      {
        string sourceAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), sourceAssemblyPath);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"), Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"));

        using (var info = SigningHelper.SignAssembly(sourceAssemblyPath, null, outDir))
        {
          string outAssembly = Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath));
          File.Exists(outAssembly).Should().BeTrue();
          File.Exists(Path.ChangeExtension(outAssembly, ".pdb")).Should().BeTrue();
          info.IsSigned.Should().BeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_NewLocationWithoutPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      var outDir = Path.Combine(tempDir, "out");
      Directory.CreateDirectory(outDir);
      try
      {
        string sourceAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), sourceAssemblyPath);

        using (var info = SigningHelper.SignAssembly(sourceAssemblyPath, null, outDir))
        {
          string outAssembly = Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath));
          File.Exists(outAssembly).Should().BeTrue();
          info.IsSigned.Should().BeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_InPlaceWithoutPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        string targetAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), targetAssemblyPath);

        using (var info = SigningHelper.SignAssembly(targetAssemblyPath))
        {
          info.IsSigned.Should().BeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void SignAssembly_InParralel_Should_Succeed()
    {
      Parallel.Invoke(
        SignAssembly_Public_API_Test,
        SignAssembly_Should_Reassemble_NET_45_Assembly_Correctly
      );
    }
#pragma warning restore S2699 // Tests should include assertions
  }
}
