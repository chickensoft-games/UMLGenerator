namespace Chickensoft.UMLGenerator.Helpers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class NodeExtensions
{
	public static string GetScriptPath(this BaseNode node, bool useVSCodePaths, int depth)
	{
		var hasScript = !string.IsNullOrEmpty(node.ScriptPath);
		string filePath;
		string fullFilePath;
		
		if (hasScript)
		{
			filePath = node.ScriptPath;
			fullFilePath = node.FullScriptPath;
		}
		else
		{
			filePath = node.FilePath;
			fullFilePath =  node.FullFilePath;
		}

		return useVSCodePaths ? GetVSCodePath(fullFilePath) : GetPathWithDepth(filePath, depth);
	}

	public static string GetPathWithDepth(string path, int depth)
	{
		if (string.IsNullOrWhiteSpace(path)) 
			return string.Empty;
		
		var depthString = string.Join("", Enumerable.Repeat("../", depth));
		return depthString + path;
	}

	public static string GetVSCodePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		return $"vscode://file/{path}";
	}

	public static BaseNode? FindNodeByType(
		this TypeSyntax? typeSyntax,
		SemanticModel? semanticModel,
		IDictionary<string, BaseNode> nodes)
	{
		if (typeSyntax == null || semanticModel == null || nodes.Count == 0)
			return null;

		var fqn = semanticModel.GetTypeInfo(typeSyntax).Type?.ToDisplayString();
		if (fqn == null)
			return null;

		if (nodes.TryGetValue(fqn, out var exact))
			return exact;

		var lastDot = fqn.LastIndexOf('.');
		var typePart = lastDot >= 0 ? fqn.Substring(lastDot + 1) : fqn;
		if (!typePart.StartsWith("I") || typePart.Length <= 1)
			return null;

		var strippedFqn = lastDot >= 0
			? fqn.Substring(0, lastDot + 1) + typePart.Substring(1)
			: typePart.Substring(1);

		return nodes.TryGetValue(strippedFqn, out var node) ? node : null;
	}
}