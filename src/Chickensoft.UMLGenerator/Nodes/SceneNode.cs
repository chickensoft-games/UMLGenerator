namespace Chickensoft.UMLGenerator.Models;

using System.IO;
using Godot;
using Helpers;
using Microsoft.CodeAnalysis;

public class SceneNode(TscnListener listener, AdditionalText additionalText, GenerationData data) : BaseNode(data)
{
	public Node? Node { get; } = listener.RootNode;
	public override string? FullFilePath { get; } = additionalText.Path.NormalizePath();
	public override string? FullScriptPath { get; } = Path.Combine(data.ProjectDir ?? "", listener.Script?.Path.Replace("res://", "") ?? "");
}