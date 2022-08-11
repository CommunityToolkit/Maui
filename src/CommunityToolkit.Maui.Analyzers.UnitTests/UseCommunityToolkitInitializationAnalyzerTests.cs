using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CommunityToolkit.Maui.Analyzers.UnitTests;

public class UseCommunityToolkitInitializationAnalyzerTests
{
	[Fact]
	public void UseCommunityToolkitInitializationAnalyzerId()
	{
		Assert.Equal("MCT001", UseCommunityToolkitInitializationAnalyzer.DiagnosticId);
	}
}