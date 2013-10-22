using Brutal.Dev.StrongNameSigner.External;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests.External
{
  [TestFixture]
  public class PublicKeyToolTests
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
    public void PublicKeyTool_Should_Execute_Correctly()
    {
      using (var publicKeyTool = new PublicKeyTool())
      {
        publicKeyTool.Run(s => output.Append(s)).ShouldBe(false);

        publicKeyTool.Output.ShouldContain(".NET Framework Strong Name Utility");
      }
    }

    [Test]
    public void PublicKeyTool_Has_No_Token_For_Unsigned_Assembly()
    {
      using (var publicKeyTool = new PublicKeyTool(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.dll")))
      {
        publicKeyTool.Run(s => output.Append(s)).ShouldBe(false);

        publicKeyTool.PublicKeyToken.ShouldBeEmpty();
      }
    }

    [Test]
    public void PublicKeyTool_Has_Token_For_Signed_Assembly()
    {
      using (var publicKeyTool = new PublicKeyTool(Path.Combine(TestAssemblyDirectory, "Brutal.Dev.StrongNameSigner.TestAssembly.A.Signed.dll")))
      {
        publicKeyTool.Run(s => output.Append(s)).ShouldBe(true);

        publicKeyTool.PublicKeyToken.Length.ShouldBe(16);
        publicKeyTool.PublicKeyToken.ShouldBe("A13519261D199C73");
      }
    }
  }
}
