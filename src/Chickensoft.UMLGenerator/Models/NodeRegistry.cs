namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Registry for managing node lookups by FQN and scene name.
/// </summary>
public class NodeRegistry
{
	private readonly Dictionary<string, DiagramNode> _nodesByFQN;
	private readonly Dictionary<string, DiagramNode> _nodesBySceneName;
	private readonly Dictionary<string, DiagramNode> _nodesByFilePath;

	/// <summary>
	/// All nodes in the registry.
	/// </summary>
	public IEnumerable<DiagramNode> AllNodes
	{
		get
		{
			var seen = new HashSet<DiagramNode>();
			foreach (var node in _nodesByFQN.Values)
			{
				if (seen.Add(node))
					yield return node;
			}
			foreach (var node in _nodesBySceneName.Values)
			{
				if (seen.Add(node))
					yield return node;
			}
		}
	}

	public NodeRegistry()
	{
		_nodesByFQN = new Dictionary<string, DiagramNode>(StringComparer.OrdinalIgnoreCase);
		_nodesBySceneName = new Dictionary<string, DiagramNode>(StringComparer.OrdinalIgnoreCase);
		_nodesByFilePath = new Dictionary<string, DiagramNode>(StringComparer.OrdinalIgnoreCase);
	}

	public void AddByFQN(string fqn, DiagramNode node)
	{
		if (!string.IsNullOrWhiteSpace(fqn))
			_nodesByFQN[fqn] = node;

		_nodesByFilePath[node.FullScriptPath!] = node;
	}

	public void AddBySceneName(string sceneName, DiagramNode node)
	{
		if (!string.IsNullOrWhiteSpace(sceneName))
			_nodesBySceneName[sceneName] = node;

		_nodesByFilePath[node.FullFilePath] = node;

		if(node.FullScriptPath != null)
			_nodesByFilePath[node.FullScriptPath] = node;
	}

	public bool TryGetByFQN(string fqn, out DiagramNode node)
	{
		return _nodesByFQN.TryGetValue(fqn, out node);
	}

	public bool TryGetBySceneName(string sceneName, out DiagramNode node)
	{
		return _nodesBySceneName.TryGetValue(sceneName, out node);
	}

	public bool TryGetByFilePath(string filePath, out DiagramNode node)
	{
		return _nodesByFilePath.TryGetValue(filePath, out node);
	}

	public DiagramNode? FindNodeByType(
		TypeSyntax? typeSyntax,
		SemanticModel? semanticModel)
	{
		if (typeSyntax == null || semanticModel == null)
			return null;

		var fqn = semanticModel.GetTypeInfo(typeSyntax).Type?.ToDisplayString();
		if (fqn == null)
			return null;

		if (TryGetByFQN(fqn, out var exact))
			return exact;

		var lastDot = fqn.LastIndexOf('.');
		var typePart = lastDot >= 0 ? fqn.Substring(lastDot + 1) : fqn;
		if (!typePart.StartsWith("I") || typePart.Length <= 1)
			return null;

		var strippedFqn = lastDot >= 0
			? fqn.Substring(0, lastDot + 1) + typePart.Substring(1)
			: typePart.Substring(1);

		return TryGetByFQN(strippedFqn, out var node) ? node : null;
	}
}
