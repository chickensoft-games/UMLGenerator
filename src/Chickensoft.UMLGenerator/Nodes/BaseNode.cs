namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class BaseNode(GenerationData data)
{
	private readonly Dictionary<Type, List<ModuleItem>> _moduleItems = [];
	public IReadOnlyDictionary<Type, List<ModuleItem>> ModuleItems => _moduleItems;
	public virtual GeneratorSyntaxContext? Context { get; set; }
	public SemanticModel? SemanticModel => Context?.SemanticModel;

	public TypeDeclarationSyntax? TypeSyntax => Context?.Node as TypeDeclarationSyntax;

	public InterfaceDeclarationSyntax? InterfaceSyntax => TypeSyntax is null ? null :
		SemanticModel.GetDeclaredSymbol(TypeSyntax)?.AllInterfaces
		.FirstOrDefault(x => x.Name == "I" + TypeSyntax.Identifier.Text)
		?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as InterfaceDeclarationSyntax;

	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");
	public abstract string FullFilePath { get; }
	public string ScriptPath => FullScriptPath.Replace($"{data.ProjectDir}", "");
	public abstract string FullScriptPath { get; }
	public string Name => Path.GetFileNameWithoutExtension(FilePath);
	public void AddModule(Type type, List<ModuleItem> moduleItems) => _moduleItems.Add(type, moduleItems);
}