using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class SigningHelperTests
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
    public void SignAssembly_Public_API_Test()
    {
      var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed"), s => output.Append(s));
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
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist"), s => output.Append(s));
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), "C:\\DoesNotExist\\KeyFile.snk", s => output.Append(s));
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void SignAssembly_Public_API_Obfuscated_File_Should_Throw_Exception()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe"), s => output.Append(s));
    }

    [Test]
    [ExpectedException(typeof(AlreadySignedException))]
    public void SignAssembly_Public_API_Signed_File_Should_Throw_Exception()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe"), s => output.Append(s));
    }

    [Test]
    public void GetAssemblyInfo_Public_API_Test()
    {
      var info = SigningHelper.GetAssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), s => output.Append(s));
      info.ShouldNotBe(null);
      info.DotNetVersion.ShouldBe("2.0.50727");
      info.Is32BitOnly.ShouldBe(false);
      info.Is32BitPreferred.ShouldBe(false);
      info.Is64BitOnly.ShouldBe(false);
      info.IsAnyCpu.ShouldBe(true);
      info.IsSigned.ShouldBe(false);
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void GetAssemblyInfo_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      SigningHelper.GetAssemblyInfo(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist"), s => output.Append(s));
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GetAssemblyInfo_Public_API_Invalid_File_Should_Throw_Exception()
    {
      SigningHelper.GetAssemblyInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe"), s => output.Append(s));
    }
  }
}

