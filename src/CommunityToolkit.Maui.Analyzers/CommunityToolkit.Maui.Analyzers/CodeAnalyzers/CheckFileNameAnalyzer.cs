using CommunityToolkit.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static CommunityToolkit.Maui.Analyzers.Constants;

namespace CommunityToolkit.Maui.Analyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CheckFileNameAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "CheckFileNameAnalyzer";

		static readonly LocalizableString title = new LocalizableResourceString(nameof(Resources.CheckFileNameAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		//static readonly LocalizableString messageFormat = new LocalizableResourceString(nameof(Resources.NotImplementedMessageFormat), Resources.ResourceManager, typeof(Resources));
		static readonly LocalizableString description = new LocalizableResourceString(nameof(Resources.CheckFileNameAnalyzerDescription), Resources.ResourceManager, typeof(Resources));


		static readonly DiagnosticDescriptor rule = new(DiagnosticId, title, string.Empty, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: description);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(FileNameAnalyzer);
		}

		static void FileNameAnalyzer(SyntaxTreeAnalysisContext context)
		{
			var filePath = context.Tree.FilePath;

			if (string.IsNullOrWhiteSpace(filePath) && filePath.EndsWith(".cs"))
			{
				return;
			}

			if (!(filePath.Contains(".android.") 
				|| filePath.Contains(".ios.")
				|| filePath.Contains(".shared.")
				|| filePath.Contains(".windows.")
				|| filePath.Contains(".macios.")
				|| filePath.Contains(".macos.")))
			{
				var diagnostic = Diagnostic.Create(rule, context.Tree.GetRoot(context.CancellationToken).GetLocation());
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
