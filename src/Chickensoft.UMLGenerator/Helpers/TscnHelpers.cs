namespace Chickensoft.UMLGenerator.Helpers;

using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Exceptions;
using Microsoft.CodeAnalysis;
using Righthand.GodotTscnParser.Engine.Grammar;

public static class TscnHelpers
{
	public static TscnListener RunTscnBaseListener(string text, Action<Diagnostic> reportDiagnostic, string filePath)
	{
		try
		{
			var input = new AntlrInputStream(text);
			var lexer = new TscnLexer(input);
			lexer.AddErrorListener(new SyntaxErrorListener());
			var tokens = new CommonTokenStream(lexer);
			var parser = new TscnParser(tokens)
			{
				BuildParseTree = true
			};
			parser.AddErrorListener(new ErrorListener());
			var tree = parser.file();
			var listener = new TscnListener(reportDiagnostic, filePath);
			ParseTreeWalker.Default.Walk(listener, tree);
			return listener;
		}
		catch (ParserException ex)
		{
			var msg = ex.GetMessageWithFilePath(filePath);
			throw new Exception(msg, ex);
		}
	}

	private class SyntaxErrorListener : IAntlrErrorListener<int>
	{
		public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
			int line, int charPositionInLine, string msg, RecognitionException e)
		{
			throw new ParserException(msg, line, charPositionInLine, e);
		}
	}

	private class ErrorListener : BaseErrorListener
	{
		public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
			int line, int charPositionInLine, string msg, RecognitionException e)
		{
			throw new ParserException(msg, line, charPositionInLine, e);
		}
	}
}