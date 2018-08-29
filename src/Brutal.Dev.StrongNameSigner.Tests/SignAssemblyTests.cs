using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class SignAssemblyTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"TestAssemblies");


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
    public void SignAssembly_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      Assert.Throws<FileNotFoundException>(() => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist")));
    }

    [Test]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Password_Not_Provided()
    {
      Assert.Throws<ArgumentException>(() => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx")));
    }

    [Test]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Wrong_Password_Provided()
    {
      Assert.Throws<CryptographicException>(() => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "oops"));
    }

    [Test]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Work_With_Correct_Password()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "password123");
    }

    [Test]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_Directory()
    {
      Assert.Throws<DirectoryNotFoundException>(() => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), "C:\\DoesNotExist\\KeyFile.snk"));
    }

    [Test]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_File()
    {
      Assert.Throws<FileNotFoundException>(() => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), "C:\\KeyFileThatDoesNotExist.snk"));
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


    [Test]
    public void SignAssembly_InPlaceWithPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        string targetAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), targetAssemblyPath);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"), Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.pdb"));

        SigningHelper.SignAssembly(targetAssemblyPath);
        var info = SigningHelper.GetAssemblyInfo(targetAssemblyPath);
        Assert.IsTrue(info.IsSigned);
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Test]
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

        SigningHelper.SignAssembly(sourceAssemblyPath, null, outDir);
        string outAssembly = Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath));
        Assert.IsTrue(File.Exists(outAssembly));
        Assert.IsTrue(File.Exists(Path.ChangeExtension(outAssembly, ".pdb")));
        var info = SigningHelper.GetAssemblyInfo(outAssembly);
        Assert.IsTrue(info.IsSigned);
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Test]
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

        SigningHelper.SignAssembly(sourceAssemblyPath, null, outDir);
        string outAssembly = Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath));
        Assert.IsTrue(File.Exists(outAssembly));
        var info = SigningHelper.GetAssemblyInfo(outAssembly);
        Assert.IsTrue(info.IsSigned);
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Test]
    public void SignAssembly_InPlaceWithoutPdb_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        string targetAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), targetAssemblyPath);

        SigningHelper.SignAssembly(targetAssemblyPath);
        var info = SigningHelper.GetAssemblyInfo(targetAssemblyPath);
        Assert.IsTrue(info.IsSigned);

      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }
  }
}

