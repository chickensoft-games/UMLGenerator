namespace Chickensoft.UMLGenerator.Helpers;

using System.Linq;
using Models;

public static class NodeExtensions
{
	public static string GetScriptPath(this DiagramNode node, bool useVSCodePaths, int depth)
	{
		string filePath;
		string fullFilePath;
		
		if (node.HasScript)
		{
			filePath = node.ScriptPath!;
			fullFilePath = node.FullScriptPath!;
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
}