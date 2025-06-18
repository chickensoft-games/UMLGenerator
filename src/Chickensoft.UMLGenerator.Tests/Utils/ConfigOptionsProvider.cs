namespace Chickensoft.UMLGenerator.Tests.Utils;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public class ConfigOptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
{
	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
	{
		return GlobalOptions;
	}

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
	{
		return GlobalOptions;
	}

	public override AnalyzerConfigOptions GlobalOptions { get; } = options;
}

public class ConfigOptions(Dictionary<string, string> optionsDict) : AnalyzerConfigOptions
{
	public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
	{
		return optionsDict.TryGetValue(key, out value);
	}
}