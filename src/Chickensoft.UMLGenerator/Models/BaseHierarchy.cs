namespace Chickensoft.UMLGenerator.Models;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public abstract class BaseHierarchy(GenerationData data)
{
	public virtual List<GeneratorSyntaxContext> ContextList { get; } = [];
	
	public ClassDeclarationSyntax? ClassSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is ClassDeclarationSyntax) as ClassDeclarationSyntax;
	public InterfaceDeclarationSyntax? InterfaceSyntax => ContextList.Select(x => x.Node)
		.FirstOrDefault(x => x is InterfaceDeclarationSyntax ctx && ctx.Identifier.ValueText == $"I{Name}") as InterfaceDeclarationSyntax;
	
	public string FilePath => FullFilePath.Replace($"{data.ProjectDir}", "");
	public abstract string FullFilePath { get; }
	public string ScriptPath => FullScriptPath.Replace($"{data.ProjectDir}", "");
	public abstract string FullScriptPath { get; }
	public string Name => Path.GetFileNameWithoutExtension(FilePath);
	
	private Dictionary<string, BaseHierarchy> _dictOfChildren = [];
	private Dictionary<string, BaseHierarchy> _dictOfParents = [];
	
	public IReadOnlyDictionary<string, BaseHierarchy> DictOfChildren => _dictOfChildren;
	public IReadOnlyDictionary<string, BaseHierarchy> DictOfParents => _dictOfParents;

	public virtual void GenerateHierarchy(IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var propertyDeclarations = GetSyntaxContextForPropertyDeclarations();
		foreach (var ctx in propertyDeclarations)
		{
			var className = Path.GetFileNameWithoutExtension(ctx.SemanticModel.SyntaxTree.FilePath);
			if (!nodeHierarchyList.TryGetValue(className, out var childNodeHierarchy)) 
				continue;
			
			AddChild(childNodeHierarchy);
			childNodeHierarchy.AddParent(this);
		}
	}

	private AttributeSyntax? GetClassDiagramAttribute()
	{
		var attributeName = nameof(ClassDiagramAttribute).Replace("Attribute", "");
		var classDiagramAttribute = ContextList
			.Select(x => (x.Node as TypeDeclarationSyntax)?.AttributeLists.SelectMany(x => x.Attributes))
			.SelectMany(x => x)
			.FirstOrDefault(x => x.Name.ToString() == attributeName);
		
		return classDiagramAttribute;
	}

	public bool ShouldUseVSCode()
	{
		var attribute = GetClassDiagramAttribute();
		return attribute?.ArgumentList?.Arguments.Any(arg =>
			arg.NameEquals is NameEqualsSyntax nameEquals &&
			nameEquals.Name.ToString() == nameof(ClassDiagramAttribute.UseVSCodePaths) &&
			arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;
	}
	
	public bool HasClassDiagramAttribute() => GetClassDiagramAttribute() != null;

	internal void AddChild(BaseHierarchy node)
	{
		if(!_dictOfChildren.ContainsKey(node.Name))
			_dictOfChildren.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate {node.Name} in {Name}");
	}

	internal void AddParent(BaseHierarchy node)
	{
		if(!_dictOfParents.ContainsKey(node.Name))
			_dictOfParents.Add(node.Name, node);
		else
			Console.WriteLine($"Found duplicate {node.Name} in {Name}");
	}

	public void AddContextList(List<GeneratorSyntaxContext> list)
	{
		ContextList.AddRange(list);
	}

	/// <summary>
	/// Returns all SyntaxContexts for properties which don't have a Dependency attribute
	/// </summary>
	/// <returns></returns>
	private IList<GeneratorSyntaxContext> GetSyntaxContextForPropertyDeclarations()
	{
		if (ClassSyntax == null)
			return ImmutableList<GeneratorSyntaxContext>.Empty;
		
		var listOfChildContexts = new List<GeneratorSyntaxContext>();
			
		var properties = ClassSyntax
			.Members.OfType<PropertyDeclarationSyntax>()
			.Where(x => 
				!x.AttributeLists.SelectMany(x => x.Attributes)
					.Any(x => x.Name.ToString() == "Dependency"));
	
		foreach (var property in properties)
		{
			var type = property.Type.ToString();
			var childContexts = data.SyntaxContexts
				.Where(x =>
				{
					var typeSyntax = x.Node as TypeDeclarationSyntax;
					var sourceFileName = typeSyntax?.Identifier.ValueText;
					return sourceFileName == type && !DictOfChildren.ContainsKey(type);
				});
			
			listOfChildContexts.AddRange(childContexts);
		}

		return listOfChildContexts;
	}

	/// <summary>
	/// This will return class properties which exist in the interface
	/// </summary>
	/// <returns></returns>
	private IEnumerable<PropertyDeclarationSyntax> GetInterfacePropertyDeclarations()
	{
		if (InterfaceSyntax == null) return [];
		return from interfaceMember in InterfaceSyntax.Members
			from classMember in ClassSyntax.Members
			where classMember is PropertyDeclarationSyntax classProperty &&
			      interfaceMember is PropertyDeclarationSyntax interfaceProperty &&
			      classProperty.Identifier.Value == interfaceProperty.Identifier.Value
			orderby (classMember as PropertyDeclarationSyntax).Identifier.ValueText
			select classMember as PropertyDeclarationSyntax;
	}
	
	/// <summary>
	/// This will return class methods which exist in the interface
	/// </summary>
	/// <returns></returns>
	private IEnumerable<MethodDeclarationSyntax> GetInterfaceMethodDeclarations()
	{
		if (InterfaceSyntax == null) return [];
		return from interfaceMember in InterfaceSyntax!.Members
			from classMember in ClassSyntax.Members
			where classMember is MethodDeclarationSyntax classMethod &&
			      interfaceMember is MethodDeclarationSyntax interfaceMethod &&
			      classMethod.Identifier.Value == interfaceMethod.Identifier.Value
			orderby (classMember as MethodDeclarationSyntax).Identifier.ValueText
			select classMember as MethodDeclarationSyntax;
	}
	
	internal string GetDiagram(int depth, bool useVSCodePaths)
	{
		var classDefinition = GetTypeDefinition(depth, useVSCodePaths, out var properties);
		
		var childrenToDraw = DictOfChildren.Values
			.Where(x => x.DictOfChildren.Count != 0 ||
			            x.GetInterfacePropertyDeclarations().Any() ||
			            x.GetInterfaceMethodDeclarations().Any() ||
						!properties.Values.Contains(x)
			).ToList();
		
		if (childrenToDraw.Count == 0)
			return classDefinition;
		
		var newFilePath = useVSCodePaths ? GetVSCodePath(FullFilePath) : GetPathWithDepth(FilePath, depth);
		
		var childrenDefinitions = string.Join("\n\t",
			childrenToDraw.Select(x =>
				x.GetDiagram(depth, useVSCodePaths)
			)
		);

		var childrenRelationships = string.Join("\n\t",
			childrenToDraw.Select(x =>
			{
				var memberName = properties.FirstOrDefault(prop => x == prop.Value).Key ?? x.Name;
				return $"{Name}::{memberName} {(x.DictOfChildren.Count == 0 ? string.Empty : "-")}--> {x.Name}";
			})
		);

		var packageType = this switch
		{
			ClassHierarchy => "Class",
			NodeHierarchy => "Scene",
			_ => throw new NotImplementedException()
		};
			
		return 
			$$"""

			  package {{Name}}-{{packageType}} [[{{newFilePath}}]] {
			  	{{classDefinition}}
			  	{{childrenDefinitions}}
			  	{{childrenRelationships}}
			  }

			  """;
	}

	private string GetTypeDefinition(int depth, bool useVSCodePaths, out IDictionary<string, BaseHierarchy> children)
	{
		children = ImmutableDictionary<string, BaseHierarchy>.Empty;
		
		var hasScript = !string.IsNullOrEmpty(ScriptPath);
		var filePath = hasScript ? ScriptPath : FilePath;
		var fullFilePath = hasScript ? FullScriptPath : FullFilePath;
		
		var interfaceMethodsString = string.Empty;
		var interfacePropertiesString = string.Empty;

		var newScriptPath = useVSCodePaths ? GetVSCodePath(fullFilePath) : GetPathWithDepth(filePath, depth);
		
		var classPropertiesFromInterface = GetInterfacePropertyDeclarations().ToList();
		var allClassProperties = ClassSyntax?.Members.OfType<PropertyDeclarationSyntax>() ?? [];
			
		var props = 
			from child in DictOfChildren
			from prop in allClassProperties
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
			children.Where(x => classPropertiesFromInterface
					.All(y => y.Identifier.ValueText != x.Key))
				.Select(x =>
				{
					var scriptDefinitions = string.Empty;
					var propName = x.Key;
					var propertyDeclarationSyntax = ClassSyntax?
						.Members
						.OfType<PropertyDeclarationSyntax>()
						.FirstOrDefault(x => x.Identifier.ValueText == propName);
					
					var value = propertyDeclarationSyntax != null ? $"[[{newScriptPath}:{propertyDeclarationSyntax.GetLineNumber()} {propName}]]" : propName;
					
					var scriptPath = useVSCodePaths ? GetVSCodePath(x.Value.FullScriptPath) : GetPathWithDepth(x.Value.ScriptPath, depth);
					
					if(!string.IsNullOrWhiteSpace(scriptPath))
						scriptDefinitions = $" - [[{scriptPath} Script]]";
					
					return value + scriptDefinitions;
				})
		);
		
		if (!string.IsNullOrWhiteSpace(externalChildrenString))
			externalChildrenString = "\n--\n" + externalChildrenString;
		
		if(InterfaceSyntax != null)
		{
			interfacePropertiesString = string.Join("\n\t",
				classPropertiesFromInterface.Select(x =>
				{
					var propName = x?.Identifier.ValueText;
					var value = $"+ [[{newScriptPath}:{x?.GetLineNumber()} {propName}]]";
					
					if(!insideProp.TryGetValue(propName!, out var child))
						return value;
					
					var scriptPath = useVSCodePaths ? GetVSCodePath(child.FullScriptPath) : GetPathWithDepth(child.ScriptPath, depth);
					
					var scriptDefinitions = $" - [[{scriptPath} Script]]";
					
					return value + scriptDefinitions;
				})
			);

			if (!string.IsNullOrWhiteSpace(interfacePropertiesString))
				interfacePropertiesString = "\n--\n" + interfacePropertiesString;

			var classMethodsFromInterface = GetInterfaceMethodDeclarations();

			interfaceMethodsString = string.Join("\n\t",
				classMethodsFromInterface.Select(x =>
					$"[[{newScriptPath}:{x?.GetLineNumber()} {x?.Identifier.Value}()]]"
				)
			);

			if (!string.IsNullOrWhiteSpace(interfaceMethodsString))
				interfaceMethodsString = "\n--\n" + interfaceMethodsString;
		}
		
		var fileType = hasScript ? "Script" : "Scene";

		return 
		$$"""

		class {{Name}} {
			[[{{newScriptPath}} {{fileType}}File]]{{interfacePropertiesString}}{{interfaceMethodsString}}{{externalChildrenString}}
		}

		""";
	}

	private string GetPathWithDepth(string path, int depth)
	{
		if (string.IsNullOrWhiteSpace(path)) 
			return string.Empty;
		
		var depthString = string.Join("", Enumerable.Repeat("../", depth));
		return depthString + path;
	}

	private string GetVSCodePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) 
			return string.Empty;

		return $"vscode://file/{path}";
	}
}