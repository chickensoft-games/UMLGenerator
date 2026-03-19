namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class DiagramNode(GenerationData data)
{
	private readonly Dictionary<Type, List<ModuleItem>> _moduleItems = [];
	public IReadOnlyDictionary<Type, List<ModuleItem>> ModuleItems => _moduleItems;
	public ScriptComponent? Script { get; private set; }
	public SceneComponent? Scene { get; private set; }

	public GeneratorSyntaxContext? Context => Script?.Context;
	public SemanticModel? SemanticModel => Context?.SemanticModel;
	public TypeDeclarationSyntax? TypeSyntax => Context?.Node as TypeDeclarationSyntax;

	public InterfaceDeclarationSyntax? InterfaceSyntax => TypeSyntax is null ? null :
		SemanticModel.GetDeclaredSymbol(TypeSyntax)?.AllInterfaces
		.FirstOrDefault(x => x.Name == "I" + TypeSyntax.Identifier.Text)
		?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as InterfaceDeclarationSyntax;

	public string? FullyQualifiedName => TypeSyntax is null ? null :
		SemanticModel?.GetDeclaredSymbol(TypeSyntax)?.ToDisplayString();

	public Node? SceneNode => Scene?.Node;

	public string FullFilePath => Scene?.FullScenePath ?? Script?.FullScriptPath ?? throw new NullReferenceException();
	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");

	public string? FullScriptPath => Script?.FullScriptPath ?? Scene?.FullScriptPath ?? null;
	public string? ScriptPath => FullScriptPath?.Replace($"{data.ProjectDir}", "");

	public string Name => Path.GetFileNameWithoutExtension(FilePath);
	public void AddModule(Type type, List<ModuleItem> moduleItems) => _moduleItems.Add(type, moduleItems);

	public void SetScriptComponent(GeneratorSyntaxContext context, string fullScriptPath)
	{
		Script = new ScriptComponent(context, fullScriptPath);
	}

	public void SetSceneComponent(Node node, string fullScenePath, string? fullScriptPath)
	{
		Scene = new SceneComponent(node, fullScenePath, fullScriptPath);
	}

	public bool HasScript => Script != null;
	public bool HasScene => Scene != null;
}

/// <summary>
/// Component for nodes that have a C# script/class definition
/// </summary>
public class ScriptComponent(GeneratorSyntaxContext context, string fullScriptPath)
{
	public GeneratorSyntaxContext Context { get; } = context;
	public string FullScriptPath { get; } = fullScriptPath;
}

/// <summary>
/// Component for nodes that have a Godot scene file (.tscn)
/// </summary>
public class SceneComponent(Node node, string fullScenePath, string? fullScriptPath)
{
	public Node Node { get; } = node;
	public string FullScenePath { get; } = fullScenePath;
	public string? FullScriptPath { get; } = fullScriptPath;
}