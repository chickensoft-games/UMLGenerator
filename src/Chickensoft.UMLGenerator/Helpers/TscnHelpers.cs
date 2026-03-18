namespace Chickensoft.UMLGenerator.Helpers;

using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.CodeAnalysis;
using Righthand.GodotTscnParser.Engine.Grammar;

public static class TscnHelpers
{
	public static TscnListener RunTscnBaseListener(string text, Action<Diagnostic> reportDiagnostic, string filePath)
	{
		var input = new AntlrInputStream(text);
		var lexer = new TscnLexer(input);
		var tokens = new CommonTokenStream(lexer);
		var parser = new TscnParser(tokens)
		{
			BuildParseTree = true
		};
		var tree = parser.file();
		var listener = new TscnListener(reportDiagnostic, filePath);
		ParseTreeWalker.Default.Walk(listener, tree);
		return listener;
	}
}