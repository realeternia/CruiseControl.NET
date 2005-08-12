using System;
using System.IO;
using Exortech.NetReflector;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Tasks;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;

namespace ThoughtWorks.CruiseControl.UnitTests.Core.Tasks
{
	[TestFixture]
	public class MsBuildTaskTest : ProcessExecutorTestFixtureBase
	{
		private IIntegrationResult result;
		private MsBuildTask task;

		[SetUp]
		protected void SetUp()
		{
			CreateProcessExecutorMock(MsBuildTask.DefaultExecutable);
			result = IntegrationResult();
			result.Label = "1.0";
			result.ArtifactDirectory = @"c:\artifacts";
			task = new MsBuildTask((ProcessExecutor) mockProcessExecutor.MockInstance);
		}

		[TearDown]
		protected void TearDown()
		{
			Verify();
		}

		[Test]
		public void ExecuteSpecifiedProject()
		{
			string args = "/nologo /t:target1;target2 " + IntegrationProperties() + " /p:Configuration=Release myproject.sln" + DefaultLogger();
			ExpectToExecuteArguments(args);

			task.ProjectFile = "myproject.sln";
			task.Targets = "target1;target2";
			task.BuildArgs = "/p:Configuration=Release";
			task.Timeout = 600;
			task.Run(result);

			Assert.AreEqual(1, result.TaskResults.Count);
			Assert.AreEqual(IntegrationStatus.Success, result.Status);
			Assert.AreEqual(ProcessResultOutput, result.TaskOutput);
		}

		private string DefaultLogger()
		{
			return string.Format(@" /l:{0};c:\artifacts\msbuild-results.xml", MsBuildTask.DefaultLogger);
		}

		private string IntegrationProperties()
		{
			return string.Format(@"/p:CCNetIntegrationStatus=Success;CCNetBuildDate={1};CCNetArtifactDirectory=c:\artifacts;CCNetBuildTime=08:45:00;CCNetProject=test;CCNetLabel=1.0;CCNetWorkingDirectory={0};CCNetLastIntegrationStatus=Unknown;CCNetBuildCondition=NoBuild", DefaultWorkingDirectory, new DateTime(2005,6,6).ToShortDateString());
		}

		[Test]
		public void AddQuotesAroundProjectsWithSpacesAndHandleNoSpecifiedTargets()
		{
			ExpectToExecuteArguments(@"/nologo " + IntegrationProperties() + @" ""my project.proj""" + DefaultLogger());
			task.ProjectFile = "my project.proj";
			task.Run(result);
		}

		[Test]
		public void AddQuotesAroundTargetsWithSpaces()
		{
			ExpectToExecuteArguments(@"/nologo ""/t:first;next task"" " + IntegrationProperties() + DefaultLogger());
			task.Targets = "first;next task";
			task.Run(result);
		}

		[Test]
		public void AddQuotesAroundPropertiesWithSpaces()
		{
			string expectedProperties = string.Format(@"/p:CCNetIntegrationStatus=Success;CCNetBuildDate={0};CCNetArtifactDirectory=c:\artifacts;CCNetBuildTime=08:45:00;CCNetProject=test;CCNetLabel=My Label;CCNetWorkingDirectory=c:\source\;CCNetLastIntegrationStatus=Unknown;CCNetBuildCondition=NoBuild",new DateTime(2005,6,6).ToShortDateString());
			ExpectToExecuteArguments(@"/nologo " + @"""" + expectedProperties + @"""" + DefaultLogger());
			result.Label = @"My Label";			
			task.Run(result);
		}


		[Test]
		public void RebaseFromWorkingDirectory()
		{
			ProcessInfo info = NewProcessInfo("/nologo " + IntegrationProperties() + DefaultLogger());
			info.WorkingDirectory = Path.Combine(DefaultWorkingDirectory, "src");
			ExpectToExecute(info);
			task.WorkingDirectory = "src";
			task.Run(result);
		}

		[Test]
		public void TimedOutExecutionShouldFailBuild()
		{
			ExpectToExecuteAndReturn(TimedOutProcessResult());
			task.Run(result);
			Assert.AreEqual(IntegrationStatus.Failure, result.Status);
		}

		[Test]
		public void PopulateFromConfiguration()
		{
			string xml = @"<msbuild>
	<executable>C:\WINDOWS\Microsoft.NET\Framework\v2.0.50215\MSBuild.exe</executable>
	<workingDirectory>C:\dev\ccnet</workingDirectory>
	<projectFile>CCNet.sln</projectFile>
	<buildArgs>/p:Configuration=Debug /v:diag</buildArgs>
	<targets>Build;Test</targets>
	<timeout>15</timeout>
	<logger>Kobush.Build.Logging.XmlLogger,Kobush.MSBuild.dll;buildresult.xml</logger>
</msbuild>";
			task = (MsBuildTask) NetReflector.Read(xml);
			Assert.AreEqual(@"C:\WINDOWS\Microsoft.NET\Framework\v2.0.50215\MSBuild.exe", task.Executable);
			Assert.AreEqual(@"C:\dev\ccnet", task.WorkingDirectory);
			Assert.AreEqual("CCNet.sln", task.ProjectFile);
			Assert.AreEqual("Build;Test", task.Targets);
			Assert.AreEqual("/p:Configuration=Debug /v:diag", task.BuildArgs);
			Assert.AreEqual(15, task.Timeout);
			Assert.AreEqual("Kobush.Build.Logging.XmlLogger,Kobush.MSBuild.dll;buildresult.xml", task.Logger);
		}

		[Test]
		public void PopulateFromMinimalConfiguration()
		{
			task = (MsBuildTask) NetReflector.Read("<msbuild />");
			Assert.AreEqual(defaultExecutable, task.Executable);
			Assert.AreEqual(MsBuildTask.DefaultTimeout, task.Timeout);
			Assert.AreEqual(MsBuildTask.DefaultLogger, task.Logger);
		}
	}
}