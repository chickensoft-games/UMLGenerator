namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;
using Godot;
using Helpers;
using Microsoft.CodeAnalysis;

public class NodeHierarchy(TscnListener listener, AdditionalText additionalText, GenerationData data) : BaseHierarchy(data)
{
	public Node? Node { get; } = listener.RootNode;
	public override string? FullFilePath { get; } = additionalText.Path.NormalizePath();
	public override string? FullScriptPath { get; } = data.ProjectDir + listener.Script?.Path.Replace("res://", "");

	public override void GenerateHierarchy(IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		if (Node?.AllChildren != null)
			foreach (var nodeDefinition in Node.AllChildren)
			{
				if (!nodeHierarchyList.TryGetValue(nodeDefinition.Type, out var childNodeHierarchy))
					continue;

				AddChild(childNodeHierarchy);
				childNodeHierarchy.AddParent(this);
			}

		base.GenerateHierarchy(nodeHierarchyList);
	}
}