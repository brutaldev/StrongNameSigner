using System.IO;
using System.Reflection;
using NUnit.Framework;
using Shouldly;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  [TestFixture]
  public class FixAssemblyReferenceTests
  {
    private static readonly string TestAssemblyDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestAssemblies");

    [Test]
    public void FixAssemblyReference_Public_API_Invalid_Path1_Should_Throw_Exception()
    {
      Assert.Throws<FileNotFoundException>(() => SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.doesnotexist"), Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll")));
    }

    [Test]
    public void FixAssemblyReference_Public_API_Invalid_Path2_Should_Throw_Exception()
    {
      Assert.Throws<FileNotFoundException>(() => SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.doesnotexist")));
    }

    [Test]
    public void FixAssemblyReference_Should_Fix_InternalsVisbileTo()
    {
      // Sign assembly A.
      SigningHelper.SignAssembly(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), null, Path.Combine(TestAssemblyDirectory, "IVT"));
      // Copy unsigned assembly B and just fix the references (has none but will fix InternalsVisibleTo).
      File.Copy(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), Path.Combine(TestAssemblyDirectory, "IVT", "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll"), true);
      SigningHelper.FixAssemblyReference(Path.Combine(TestAssemblyDirectory, "IVT", "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll"), Path.Combine(TestAssemblyDirectory, "IVT", "Brutal.Dev.StrongNameSigner.TestAssembly.B.dll")).ShouldBe(true);
    }
  }
}
