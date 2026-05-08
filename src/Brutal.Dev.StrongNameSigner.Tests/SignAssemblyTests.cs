using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Mono.Cecil;
using Shouldly;
using Xunit;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [Collection("FileSystem")]
  public class SignAssemblyTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestAssemblies");

    [Fact]
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

    [Fact]
    public void SignAssembly_Public_API_Invalid_Path_Should_Throw_Exception()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.doesnotexist")).Dispose();
      act.ShouldThrow<FileNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Password_Not_Provided()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx")).Dispose();
      act.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Throw_Exception_When_Wrong_Password_Provided()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "oops").Dispose();
      act.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Password_Protected_Key_Path_Should_Work_With_Correct_Password()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), Path.Combine(TestAssemblyDirectory, "PasswordTest.pfx"), Path.Combine(TestAssemblyDirectory, "Signed"), "password123").Dispose();
      act.ShouldNotThrow();
    }

    [Fact]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_Directory()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), "C:\\DoesNotExist\\KeyFile.snk");
      act.ShouldThrow<DirectoryNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Public_API_Invalid_Key_Path_Should_Throw_Exception_For_Missing_File()
    {
      Action act = () => SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), "C:\\KeyFileThatDoesNotExist.snk");
      act.ShouldThrow<FileNotFoundException>();
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("2.0.50727");
        info.IsAnyCpu.ShouldBe(true);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("4.0.30319");
        info.IsAnyCpu.ShouldBe(true);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_45_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET45.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("4.0.30319");
        info.IsAnyCpu.ShouldBe(true);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(true);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_x86_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("2.0.50727");
        info.IsAnyCpu.ShouldBe(false);
        info.Is32BitOnly.ShouldBe(true);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_x86_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x86.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("4.0.30319");
        info.IsAnyCpu.ShouldBe(false);
        info.Is32BitOnly.ShouldBe(true);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_20_x64_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("2.0.50727");
        info.IsAnyCpu.ShouldBe(false);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(true);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_NET_40_x64_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-x64.exe"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("4.0.30319");
        info.IsAnyCpu.ShouldBe(false);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(true);
        info.IsSigned.ShouldBe(true);
      }
    }

    [Fact]
    public void SignAssembly_Should_Reassemble_Core_5_Assembly_Correctly()
    {
      using (var info = SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.Core5.dll"), string.Empty, Path.Combine(TestAssemblyDirectory, "Signed")))
      {
        info.DotNetVersion.ShouldBe("4.0.30319");
        info.IsAnyCpu.ShouldBe(true);
        info.Is32BitOnly.ShouldBe(false);
        info.Is32BitPreferred.ShouldBe(false);
        info.Is64BitOnly.ShouldBe(false);
        info.IsSigned.ShouldBe(true);
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
          info.IsSigned.ShouldBeTrue();
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
          File.Exists(outAssembly).ShouldBeTrue();
          File.Exists(Path.ChangeExtension(outAssembly, ".pdb")).ShouldBeTrue();
          info.IsSigned.ShouldBeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnAssembly_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnAssembly());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnTypes_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnTypes());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnMethods_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnMethods());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnMethodParameters_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnParameters());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnMethodReturn_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnReturnParameters());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnFields_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnFields());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnProperties_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnProperties());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnEvents_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnEvents());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnEventMethods_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnEventMethods());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnEventFields_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnEventFields());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnConstructors_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnConstructors());
    }

    [Fact]
    public void SignAssembly_CustomAttributesOnConstructorParameters_Should_Succeed()
    {
      SignAssemblyAndRunTestInAppDomain(assemblyTester => assemblyTester.TestCustomAttributesOnConstructorParameters());
    }

    private void SignAssemblyAndRunTestInAppDomain(Action<AppDomainAssemblyTester> testAction)
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      var outDir = Path.Combine(tempDir, "out");
      Directory.CreateDirectory(outDir);
      try
      {
        string sourceAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), sourceAssemblyPath);
        string dependingAssemblyPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), dependingAssemblyPath);

        var pairs = new List<InputOutputFilePair>
        {
          new InputOutputFilePair(sourceAssemblyPath, Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath))),
          new InputOutputFilePair(dependingAssemblyPath, Path.Combine(outDir, Path.GetFileName(dependingAssemblyPath)))
        };

        SigningHelper.SignAssemblies(pairs).ShouldBeTrue();

        string outAssembly = Path.Combine(outDir, Path.GetFileName(sourceAssemblyPath));
        File.Exists(outAssembly).ShouldBeTrue();

        // Run test in separate AppDomain so that we can unload it after the test and delete the assembly
        var appDomain = AppDomain.CreateDomain("TestDomain", null, new AppDomainSetup() { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory });
        try
        {
          var assemblyTester = (AppDomainAssemblyTester)appDomain.CreateInstanceAndUnwrap(
            typeof(AppDomainAssemblyTester).Assembly.FullName,
            typeof(AppDomainAssemblyTester).FullName);

          assemblyTester.LoadAssembly(Path.Combine(outDir, Path.GetFileName(dependingAssemblyPath)));
          assemblyTester.LoadAssembly(outAssembly);

          testAction(assemblyTester);
        }
        finally
        {
          AppDomain.Unload(appDomain);
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
          File.Exists(outAssembly).ShouldBeTrue();
          info.IsSigned.ShouldBeTrue();
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
          info.IsSigned.ShouldBeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void SignAssembly_InParallel_Should_Succeed()
    {
      Parallel.Invoke(
        SignAssembly_Public_API_Test,
        SignAssembly_Should_Reassemble_NET_45_Assembly_Correctly
      );
    }
#pragma warning restore S2699 // Tests should include assertions

    [Fact]
    public void SignAssembly_Null_Path_Should_Throw_ArgumentNullException()
    {
      Action act = () => SigningHelper.SignAssembly(null);
      act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void SignAssembly_Empty_Path_Should_Throw_ArgumentNullException()
    {
      Action act = () => SigningHelper.SignAssembly(string.Empty);
      act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void SignAssembly_WhiteSpace_Path_Should_Throw_ArgumentNullException()
    {
      Action act = () => SigningHelper.SignAssembly("   ");
      act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void SignAssembly_Already_Signed_Assembly_Should_Succeed_And_Remain_Signed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        // Copy so we sign in-place; already-signed assemblies are not re-written to a
        // separate output path, so we must use the same directory as input/output.
        var targetPath = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Signed.exe"), targetPath);

        using (var info = SigningHelper.SignAssembly(targetPath))
        {
          info.IsSigned.ShouldBeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_Obfuscated_Assembly_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        using (var info = SigningHelper.SignAssembly(
          Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET40-Obfuscated.exe"),
          string.Empty,
          Path.Combine(tempDir, "out")))
        {
          info.IsSigned.ShouldBeTrue();
          info.IsAnyCpu.ShouldBeTrue();
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_Output_To_Deeply_Nested_NonExistent_Directory_Should_Succeed()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      try
      {
        var deepOutputDir = Path.Combine(tempDir, "a", "b", "c", "d");

        using (var info = SigningHelper.SignAssembly(
          Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"),
          string.Empty,
          deepOutputDir))
        {
          Directory.Exists(deepOutputDir).ShouldBeTrue();
          info.IsSigned.ShouldBeTrue();
        }
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    [Fact]
    public void SignAssemblies_Empty_Collection_Should_Return_False()
    {
      var result = SigningHelper.SignAssemblies(new List<string>());
      result.ShouldBeFalse();
    }

    [Fact]
    public void SignAssemblies_Missing_Input_File_Should_Throw_FileNotFoundException()
    {
      Action act = () => SigningHelper.SignAssemblies(new[] { Path.Combine(TestAssemblyDirectory, "DoesNotExist.dll") });
      act.ShouldThrow<FileNotFoundException>();
    }

    [Fact]
    public void SignAssemblies_StringPaths_Overload_Should_Sign_All_Assemblies()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        var assemblyA = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        var assemblyB = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), assemblyA);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), assemblyB);

        var result = SigningHelper.SignAssemblies(new[] { assemblyA, assemblyB });
        result.ShouldBeTrue();

        new AssemblyInfo(assemblyA).IsSigned.ShouldBeTrue();
        new AssemblyInfo(assemblyB).IsSigned.ShouldBeTrue();
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssemblies_Two_Unsigned_Assemblies_Without_Key_Should_Share_Same_PublicKey()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      try
      {
        var assemblyA = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        var assemblyB = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), assemblyA);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), assemblyB);

        SigningHelper.SignAssemblies(new[] { assemblyA, assemblyB });

        using (var infoA = new AssemblyInfo(assemblyA))
        using (var infoB = new AssemblyInfo(assemblyB))
        {
          // Both must carry the same public key token so cross-references remain valid.
          var tokenA = BitConverter.ToString(infoA.Definition.Name.PublicKeyToken).Replace("-", string.Empty);
          var tokenB = BitConverter.ToString(infoB.Definition.Name.PublicKeyToken).Replace("-", string.Empty);
          tokenA.ShouldNotBeNullOrEmpty();
          tokenA.ShouldBe(tokenB);
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssembly_Log_Callback_Is_Invoked_During_Signing()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      var logMessages = new List<string>();
      var previousLog = SigningHelper.Log;
      try
      {
        SigningHelper.Log = msg => logMessages.Add(msg);

        SigningHelper.SignAssembly(
          Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.NET20.exe"),
          string.Empty,
          Path.Combine(tempDir, "out"));

        logMessages.ShouldNotBeEmpty();
      }
      finally
      {
        SigningHelper.Log = previousLog;
        Directory.Delete(tempDir, true);
      }
    }

    [Fact]
    public void SignAssemblies_InternalsVisibleTo_Reference_Should_Be_Updated_With_PublicKey()
    {
      var tempDir = Path.Combine(TestAssemblyDirectory, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tempDir);
      var outDir = Path.Combine(tempDir, "out");
      Directory.CreateDirectory(outDir);
      try
      {
        var assemblyA = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
        var assemblyB = Path.Combine(tempDir, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll");
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), assemblyA);
        File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), assemblyB);

        var pairs = new List<InputOutputFilePair>
        {
          new InputOutputFilePair(assemblyA, Path.Combine(outDir, Path.GetFileName(assemblyA))),
          new InputOutputFilePair(assemblyB, Path.Combine(outDir, Path.GetFileName(assemblyB)))
        };

        var result = SigningHelper.SignAssemblies(pairs);
        result.ShouldBeTrue();

        // The signed Assembly A should have an InternalsVisibleTo attribute containing a PublicKey token.
        var signedA = new AssemblyInfo(Path.Combine(outDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"));
        signedA.IsSigned.ShouldBeTrue();

        // Load and inspect the raw attribute to verify PublicKey was injected.
        using (var rawDef = AssemblyDefinition.ReadAssembly(Path.Combine(outDir, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll")))
        {
          var ivtAttr = rawDef.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName);

          if (ivtAttr != null)
          {
            var attrValue = ivtAttr.ConstructorArguments[0].Value.ToString();
            attrValue.ShouldContain("PublicKey=");
          }
        }
      }
      finally
      {
        Directory.Delete(tempDir, true);
      }
    }
  }
}
