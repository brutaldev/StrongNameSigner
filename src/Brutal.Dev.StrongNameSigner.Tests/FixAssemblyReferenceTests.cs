using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class FixAssemblyReferenceTests
  {
    private const string TestAssemblyDirectory = @"TestAssemblies";
    
    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void FixAssemblyReference_Public_API_Invalid_Path1_Should_Throw_Exception()
    {
      SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.doesnotexist"), Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"));
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void FixAssemblyReference_Public_API_Invalid_Path2_Should_Throw_Exception()
    {
      SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.doesnotexist"));
    }
  }
}

