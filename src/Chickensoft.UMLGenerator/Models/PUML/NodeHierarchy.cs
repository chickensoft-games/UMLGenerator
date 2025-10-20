namespace Chickensoft.UMLGenerator.Models;

using Godot;
using Helpers;
using Microsoft.CodeAnalysis;

public class NodeHierarchy(TscnListener listener, AdditionalText additionalText, GenerationData data) : BaseHierarchy(data)
{
	public Node? Node { get; } = listener.RootNode;
	public override string? FullFilePath { get; } = additionalText.Path.NormalizePath();
	public override string? FullScriptPath { get; } = data.ProjectDir + listener.Script?.Path.Replace("res://", "");
}