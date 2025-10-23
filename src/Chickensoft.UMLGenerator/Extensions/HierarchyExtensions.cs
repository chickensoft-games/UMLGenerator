namespace Chickensoft.UMLGenerator.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class HierarchyExtensions
{
	public static string GetScriptPath(this BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var hasScript = !string.IsNullOrEmpty(hierarchy.ScriptPath);
		string filePath;
		string fullFilePath;
		
		if (hasScript)
		{
			filePath = hierarchy.ScriptPath;
			fullFilePath = hierarchy.FullScriptPath;
		}
		else
		{
			filePath = hierarchy.FilePath;
			fullFilePath =  hierarchy.FullFilePath;
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
}