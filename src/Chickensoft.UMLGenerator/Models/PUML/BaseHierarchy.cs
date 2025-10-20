namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class BaseHierarchy(GenerationData data)
{
	private readonly Dictionary<string, BaseHierarchy> _children = [];
	private readonly Dictionary<string, BaseHierarchy> _parents = [];
	private readonly Dictionary<string, BaseHierarchy> _provisions = [];
	private readonly Dictionary<string, BaseHierarchy> _dependencies = [];

	public IReadOnlyDictionary<string, BaseHierarchy> Children => _children;
	public IReadOnlyDictionary<string, BaseHierarchy> Parents => _parents;
	public IReadOnlyDictionary<string, BaseHierarchy> Provisions => _provisions;
	public IReadOnlyDictionary<string, BaseHierarchy> Dependencies => _dependencies;

	public virtual List<GeneratorSyntaxContext> ContextList { get; } = [];

	public TypeDeclarationSyntax? TypeSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax) as TypeDeclarationSyntax;
	public InterfaceDeclarationSyntax? InterfaceSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is InterfaceDeclarationSyntax ctx && ctx.Identifier.ValueText == $"I{Name}") as InterfaceDeclarationSyntax;

	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");
	public abstract string FullFilePath { get; }
	public string ScriptPath => FullScriptPath.Replace($"{data.ProjectDir}", "");
	public abstract string FullScriptPath { get; }
	public string Name => Path.GetFileNameWithoutExtension(FilePath);


	internal void AddChild(BaseHierarchy node)
	{
		if(!_children.ContainsKey(node.Name))
			_children.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate child {node.Name} in {Name}");
	}

	internal void AddParent(BaseHierarchy node)
	{
		if (!_parents.ContainsKey(node.Name))
		{
			_parents.Add(node.Name, node);
		}
		else
			Console.WriteLine($"Found duplicate parent {node.Name} in {Name}");
	}

	internal void AddDependent(BaseHierarchy node)
	{
		if(!_dependencies.ContainsKey(node.Name))
			_dependencies.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate dependent {node.Name} in {Name}");
	}

	internal void AddProvision(BaseHierarchy node)
	{
		if(!_provisions.ContainsKey(node.Name))
			_provisions.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate provision {node.Name} in {Name}");
	}
}