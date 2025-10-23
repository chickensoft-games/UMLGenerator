namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class BaseNode(GenerationData data)
{
	private readonly Dictionary<Type, List<ModuleItem>> _moduleItems = [];
	public IReadOnlyDictionary<Type, List<ModuleItem>> ModuleItems => _moduleItems;
	public virtual List<GeneratorSyntaxContext> ContextList { get; } = [];

	public SemanticModel? SemanticModel => ContextList
		.Where(x => x.Node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax)
		.Select(x => x.SemanticModel).FirstOrDefault();

	public TypeDeclarationSyntax? TypeSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax) as TypeDeclarationSyntax;

	public InterfaceDeclarationSyntax? InterfaceSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is InterfaceDeclarationSyntax ctx && ctx.Identifier.ValueText == $"I{Name}") as InterfaceDeclarationSyntax;

	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");
	public abstract string FullFilePath { get; }
	public string ScriptPath => FullScriptPath.Replace($"{data.ProjectDir}", "");
	public abstract string FullScriptPath { get; }
	public string Name => Path.GetFileNameWithoutExtension(FilePath);
	public void AddModule(Type type, List<ModuleItem> moduleItems) => _moduleItems.Add(type, moduleItems);
}