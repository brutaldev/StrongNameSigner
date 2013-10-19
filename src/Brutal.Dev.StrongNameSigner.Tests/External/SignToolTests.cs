using Brutal.Dev.StrongNameSigner.External;
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.Tests.External
{
  [TestFixture]
  public class SignToolTests
  {
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
    public void SignTool_Should_Execute_Correctly()
    {
      using (var signTool = new SignTool())
      {
        signTool.Run(s => output.Append(s)).ShouldBe(true);

        signTool.Output.ShouldContain(".NET Framework Strong Name Utility");
      }
    }

    [Test]
    public void SignTool_Should_Not_Create_Key_File_Until_Run()
    {
      using (var signTool = new SignTool())
      {
        File.Exists(signTool.KeyFilePath).ShouldBe(false);

        signTool.Run(s => output.Append(s)).ShouldBe(true);

        File.Exists(signTool.KeyFilePath).ShouldBe(true);
      }
    }

    [Test]
    public void SignTool_Key_File_Should_Be_Removed_When_Disposed()
    {
      string keyFile = string.Empty;

      using (var signTool = new SignTool())
      {
        keyFile = signTool.KeyFilePath;
        signTool.Run(s => output.Append(s)).ShouldBe(true);

        File.Exists(keyFile).ShouldBe(true);
      }

      File.Exists(keyFile).ShouldBe(false);
    }
  }
}
