using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Text;
using Mono.Cecil;

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
      string downstreamAssemblyPath = Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.doesnotexist");
      string upstreamAssemblyPath = Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll");
      SigningHelper.FixAssemblyReference(AssemblyDefinition.ReadAssembly(downstreamAssemblyPath, SigningHelper.GetReadParameters(downstreamAssemblyPath)), AssemblyDefinition.ReadAssembly(upstreamAssemblyPath, SigningHelper.GetReadParameters(upstreamAssemblyPath)));
    }

    [Test]
    [ExpectedException(typeof(FileNotFoundException))]
    public void FixAssemblyReference_Public_API_Invalid_Path2_Should_Throw_Exception()
    {
      string downstreamAssemblyPath = Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll");
      string upstreamAssemblyPath = Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.doesnotexist");
      SigningHelper.FixAssemblyReference(AssemblyDefinition.ReadAssembly(downstreamAssemblyPath, SigningHelper.GetReadParameters(downstreamAssemblyPath)), AssemblyDefinition.ReadAssembly(upstreamAssemblyPath, SigningHelper.GetReadParameters(upstreamAssemblyPath)));
    }

    /*[Test]
    public void FixAssemblyReference_Should_Fix_InternalsVisbileTo()
    {
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), Path.Combine(TestAssemblyDirectory, "StrongNameSigner.snk"));
      SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll")).ShouldBe(true);
    }*/
  }
}

