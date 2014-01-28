using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class SignAssemblyTests
  {
    private const string TestAssemblyDirectory = @"TestAssemblies";

    [Test]
    public void SignAssembly_Public_API_Test()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.ShouldNotBe(null);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void SignAssembly_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist"));
    }

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Password_Not_Provided()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"));
    }

    [Test]
    [ExpectedException(typeof(CryptographicException))]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Wrong_Password_Provided()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "oops");
    }

    [Test]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Work_With_Correct_Password()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "password123");
    }

    [Test]
    [ExpectedException(typeof(DirectoryNotFoundException))]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_Directory()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), "C:\\DoesNotExist\\KeyFile.snk");
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_File()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), "C:\\KeyFileThatDoesNotExist.snk");
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_20_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_40_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_45_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(true);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(true);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_20_x86_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_40_x86_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(true);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_20_x64_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
      info.IsSigned.ShouldBe(true);
    }

    [Test]
    public void SignAssembly_Should_Reassemble_NET_40_x64_Assembly_Correctly()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"));
      info.DotNetVersion.ShouldBe("4.0.30319");
      info.IsAnyCpu.ShouldBe(false);
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(true);
      info.IsSigned.ShouldBe(true);
    }
  }
}

