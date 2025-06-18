namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Godot;
using Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class BaseHierarchy(GenerationData data)
{
	private Dictionary<string, BaseHierarchy> _dictOfChildren = [];
	private Dictionary<string, BaseHierarchy> _dictOfParents = [];
	
	public IReadOnlyDictionary<string, BaseHierarchy> DictOfChildren => _dictOfChildren;
	public IReadOnlyDictionary<string, BaseHierarchy> DictOfParents => _dictOfParents;
	
	public virtual List<GeneratorSyntaxContext> ContextList { get; } = [];
	
	public TypeDeclarationSyntax? TypeSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax) as TypeDeclarationSyntax;
	public InterfaceDeclarationSyntax? InterfaceSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is InterfaceDeclarationSyntax ctx && ctx.Identifier.ValueText == $"I{Name}") as InterfaceDeclarationSyntax;
	
	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");
	public abstract string FullFilePath { get; }
	public string ScriptPath => FullScriptPath.Replace($"{data.ProjectDir}", "");
	public abstract string FullScriptPath { get; }
	public string ScriptPathFromParent => FullScriptPathFromParent?.Replace($"{data.ProjectDir}", "") ?? "";
	public string FullScriptPathFromParent { get; private set; } = null!;
	public string Name => Path.GetFileNameWithoutExtension(FilePath);
	
	public virtual void GenerateHierarchy(IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var propertyDeclarations = this.GetSyntaxContextForPropertyDeclarations(data.SyntaxContexts);
		foreach (var ctx in propertyDeclarations)
		{
			var typeName = Path.GetFileNameWithoutExtension(ctx.SemanticModel.SyntaxTree.FilePath);
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy)) 
				continue;
			
			AddChild(childNodeHierarchy);
			childNodeHierarchy.AddParent(this);
		}

		var parameterList = TypeSyntax?.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>();
		foreach (var ctx in parameterList)
		{
			var typeName = ctx.Type?.ToFullString().Trim();
			var typeWithoutInterface = ctx.Type?.ToFullString().TrimStart('I').Trim();
			BaseHierarchy? childNodeHierarchy;
			if (!nodeHierarchyList.TryGetValue(typeName, out childNodeHierarchy) && 
			    !nodeHierarchyList.TryGetValue(typeWithoutInterface, out childNodeHierarchy)) 
				continue;
			
			AddChild(childNodeHierarchy);
			childNodeHierarchy.AddParent(this);
		}
	}

	internal void AddChild(BaseHierarchy node)
	{
		if(!_dictOfChildren.ContainsKey(node.Name))
			_dictOfChildren.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate {node.Name} in {Name}");
	}

	internal void AddParent(BaseHierarchy node, Node? nodeDefinition = null)
	{
		if (!_dictOfParents.ContainsKey(node.Name))
		{
			_dictOfParents.Add(node.Name, node);
			if (!string.IsNullOrEmpty(nodeDefinition?.Script?.Path))
			{
				var scriptPath = nodeDefinition!.Script!.Path.Replace("res://", "");;
				FullScriptPathFromParent = data.ProjectDir + scriptPath;
			}
		}
		else
			Console.WriteLine($"Found duplicate {node.Name} in {Name}");
	}
	
	internal string GetDiagram(int depth, bool useVSCodePaths)
	{
		var typeDefinition = GetTypeDefinition(depth, useVSCodePaths, out var properties);
		
		var childrenToDraw = DictOfChildren.Values
			.Where(x => x.DictOfChildren.Count != 0 ||
			            x.GetInterfacePropertyDeclarations().Any() ||
			            x.GetInterfaceMethodDeclarations().Any() ||
						!properties.Values.Contains(x)
			).ToList();
		
		if (childrenToDraw.Count == 0)
			return typeDefinition;
		
		var newFilePath = useVSCodePaths ? HierarchyHelpers.GetVSCodePath(FullFilePath) : HierarchyHelpers.GetPathWithDepth(FilePath, depth);
		
		var childrenDefinitions = string.Join("\n\t",
			childrenToDraw.Select(x =>
				x.GetDiagram(depth, useVSCodePaths)
			)
		);

		var childrenRelationships = string.Join("\n\t",
			childrenToDraw.Select(x =>
			{
				var memberName = (this as NodeHierarchy)?
				                 .Node?
				                 .AllChildren
				                 .FirstOrDefault(node => node.Type == x.Name)?.Name
				                 ?? properties.FirstOrDefault(prop => x == prop.Value).Key 
				                 ?? x.Name;
				
				return $"{Name}::{memberName} {(x.DictOfChildren.Count == 0 ? string.Empty : "-")}--> {x.Name}";
			})
		);

		var packageType = this switch
		{
			TypeHierarchy => "Type",
			NodeHierarchy => "Scene",
			_ => throw new NotImplementedException()
		};
			
		return 
			$$"""

			  package {{Name}}-{{packageType}} [[{{newFilePath}}]] {
			  	{{typeDefinition}}
			  	{{childrenDefinitions}}
			  	{{childrenRelationships}}
			  }

			  """;
	}

	private string GetTypeDefinition(int depth, bool useVSCodePaths, out IDictionary<string, BaseHierarchy> children)
	{
		children = ImmutableDictionary<string, BaseHierarchy>.Empty;
		
		var interfaceMethodsString = string.Empty;
		var interfacePropertiesString = string.Empty;

		var newScriptPath = this.GetScriptPath(useVSCodePaths, depth, out var hasScript);
		
		var propertiesFromInterface = this.GetInterfacePropertyDeclarations().ToList();
		var allProperties = TypeSyntax?.Members.OfType<PropertyDeclarationSyntax>() ?? [];
			
		var props = 
			from child in DictOfChildren
			from prop in allProperties
			where prop.Type.ToString() == child.Key || prop.Type.ToString() == $"I{child.Key}"
			select (prop.Identifier.ValueText, child.Value);

		children =
			(from child in DictOfChildren
				join prop in props on child.Key equals prop.Value.Name into grouping
				from prop in grouping.DefaultIfEmpty()
				orderby prop.Item1, child.Key
				select (prop.Item1 ?? child.Key, child.Value))
			.ToDictionary(x => x.Item1, x => x.Value);

		var insideProp = children;
			
		var externalChildrenString = string.Join("\n\t",
			children.Where(x => propertiesFromInterface
					.All(y => y.Identifier.ValueText != x.Key))
				.Select(x =>
				{
					var scriptDefinitions = string.Empty;
					var propName = x.Key;
					var value = string.Empty;

					var scriptPath = x.Value.GetScriptPath(useVSCodePaths, depth, out var hasScript);
					
					var propertyDeclarationSyntax = TypeSyntax?
						.Members
						.OfType<PropertyDeclarationSyntax>()
						.FirstOrDefault(x => x.Identifier.ValueText == propName);
					
					//Get direct link to property declaration
					if (propertyDeclarationSyntax != null)
						value = $"[[{newScriptPath}:{propertyDeclarationSyntax.GetLineNumber()} {propName}]]";
					else if (this is NodeHierarchy nodeHierarchy)
					{
						value = nodeHierarchy
							.Node?
							.AllChildren
							.FirstOrDefault(node => node.Type == propName)?.Name;
					}
					
					if(string.IsNullOrEmpty(value))
						value = propName;
					
					var fileType = hasScript ? "Script" : "Scene";
					
					if(!string.IsNullOrWhiteSpace(scriptPath))
						scriptDefinitions = $" - [[{scriptPath} {fileType}]]";
					
					return value + scriptDefinitions;
				})
		);
		
		if (!string.IsNullOrWhiteSpace(externalChildrenString))
			externalChildrenString = "\n--\n" + externalChildrenString;
		
		if(InterfaceSyntax != null)
		{
			interfacePropertiesString = string.Join("\n\t",
				propertiesFromInterface.Select(x =>
				{
					var propName = x?.Identifier.ValueText;
					var value = $"+ [[{newScriptPath}:{x?.GetLineNumber()} {propName}]]";
					
					if(!insideProp.TryGetValue(propName!, out var child))
						return value;
					
					var scriptPath = useVSCodePaths ? HierarchyHelpers.GetVSCodePath(child.FullScriptPath) : HierarchyHelpers.GetPathWithDepth(child.ScriptPath, depth);
					
					var scriptDefinitions = $" - [[{scriptPath} Script]]";
					
					return value + scriptDefinitions;
				})
			);

			if (!string.IsNullOrWhiteSpace(interfacePropertiesString))
				interfacePropertiesString = "\n--\n" + interfacePropertiesString;

			var methodsFromInterface = this.GetInterfaceMethodDeclarations();

			interfaceMethodsString = string.Join("\n\t",
				methodsFromInterface.Select(x =>
					$"[[{newScriptPath}:{x?.GetLineNumber()} {x?.Identifier.Value}()]]"
				)
			);

			if (!string.IsNullOrWhiteSpace(interfaceMethodsString))
				interfaceMethodsString = "\n--\n" + interfaceMethodsString;
		}
		
		var fileType = hasScript ? "Script" : "Scene";

		var spotCharacter = hasScript ? "" : "<< (S,black) >>";

		return 
		$$"""

		class {{Name}} {{spotCharacter}} {
			[[{{newScriptPath}} {{fileType}}File]]{{interfacePropertiesString}}{{interfaceMethodsString}}{{externalChildrenString}}
		}

		""";
	}
}