namespace Chickensoft.UMLGenerator.Helpers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class HierarchyHelpers
{
	public static bool ShouldUseVSCode(this BaseHierarchy hierarchy)
	{
		var attribute = hierarchy.GetClassDiagramAttribute();
		return attribute?.ArgumentList?.Arguments.Any(arg =>
			arg.NameEquals is { } nameEquals &&
			nameEquals.Name.ToString() == nameof(ClassDiagramAttribute.UseVSCodePaths) &&
			arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;
	}

	public static bool HasClassDiagramAttribute(this BaseHierarchy hierarchy) => hierarchy.GetClassDiagramAttribute() != null;
	
	private static AttributeSyntax? GetClassDiagramAttribute(this BaseHierarchy hierarchy)
	{
		var attributeName = nameof(ClassDiagramAttribute).Replace("Attribute", "");
		var classDiagramAttribute = hierarchy.ContextList
			.Select(x => (x.Node as TypeDeclarationSyntax)?.AttributeLists.SelectMany(x => x.Attributes))
			.SelectMany(x => x)
			.FirstOrDefault(x => x.Name.ToString() == attributeName);
		
		return classDiagramAttribute;
	}

	/// <summary>
	/// This will return type properties which exist in the interface
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<PropertyDeclarationSyntax> GetInterfacePropertyDeclarations(this BaseHierarchy hierarchy)
	{
		if (hierarchy.InterfaceSyntax == null) return [];
		return from interfaceMember in hierarchy.InterfaceSyntax.Members
			from typeMember in hierarchy.TypeSyntax.Members
			where typeMember is PropertyDeclarationSyntax property &&
			      interfaceMember is PropertyDeclarationSyntax interfaceProperty &&
			      property.Identifier.Value == interfaceProperty.Identifier.Value
			orderby (typeMember as PropertyDeclarationSyntax).Identifier.ValueText
			select typeMember as PropertyDeclarationSyntax;
	}
	
	/// <summary>
	/// This will return type methods which exist in the interface
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<MethodDeclarationSyntax> GetInterfaceMethodDeclarations(this BaseHierarchy hierarchy)
	{
		if (hierarchy.InterfaceSyntax == null) return [];
		return from interfaceMember in hierarchy.InterfaceSyntax!.Members
			from typeMember in hierarchy.TypeSyntax.Members
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      interfaceMember is MethodDeclarationSyntax interfaceMethod &&
			      typeMethod.Identifier.Value == interfaceMethod.Identifier.Value
			orderby (typeMember as MethodDeclarationSyntax).Identifier.ValueText
			select typeMember as MethodDeclarationSyntax;
	}

	/// <summary>
	/// Returns all SyntaxContexts for properties which do have a Dependency attribute
	/// </summary>
	/// <returns></returns>
	public static IList<GeneratorSyntaxContext> GetSyntaxContextForDependantPropertyDeclarations(this BaseHierarchy hierarchy, IEnumerable<GeneratorSyntaxContext> allSyntaxContexts)
	{
		var typeSyntax = hierarchy.TypeSyntax;
		if (typeSyntax == null)
			return ImmutableList<GeneratorSyntaxContext>.Empty;

		var allSyntaxContextList = allSyntaxContexts.ToImmutableList();
		var listOfDependantContexts = new List<GeneratorSyntaxContext>();

		var properties = typeSyntax
			.Members.OfType<PropertyDeclarationSyntax>()
			.Where(x =>
				x.AttributeLists.SelectMany(x => x.Attributes)
					.Any(x => x.Name.ToString() == "Dependency"));

		foreach (var property in properties)
		{
			var type = property.Type.ToString();
			var childContexts = allSyntaxContextList
				.Where(x =>
				{
					var typeSyntax = x.Node as TypeDeclarationSyntax;
					var sourceFileName = typeSyntax?.Identifier.ValueText;
					return sourceFileName == type;
				});

			listOfDependantContexts.AddRange(childContexts);
		}

		return listOfDependantContexts;
	}

	/// <summary>
	/// Returns all SyntaxContexts for properties which don't have a Dependency attribute
	/// </summary>
	/// <returns></returns>
	public static IList<GeneratorSyntaxContext> GetSyntaxContextForPropertyDeclarations(this BaseHierarchy hierarchy, IEnumerable<GeneratorSyntaxContext> allSyntaxContexts)
	{
		var typeSyntax = hierarchy.TypeSyntax;
		if (typeSyntax == null)
			return ImmutableList<GeneratorSyntaxContext>.Empty;

		var allSyntaxContextList = allSyntaxContexts.ToImmutableList();
		var listOfChildContexts = new List<GeneratorSyntaxContext>();
			
		var properties = typeSyntax
			.Members.OfType<PropertyDeclarationSyntax>()
			.Where(x => 
				!x.AttributeLists.SelectMany(x => x.Attributes)
					.Any(x => x.Name.ToString() == "Dependency"));
	
		foreach (var property in properties)
		{
			var type = property.Type.ToString();
			var childContexts = allSyntaxContextList
				.Where(x =>
				{
					var typeSyntax = x.Node as TypeDeclarationSyntax;
					var sourceFileName = typeSyntax?.Identifier.ValueText;
					return sourceFileName == type && !hierarchy.DictOfChildren.ContainsKey(type);
				});
			
			listOfChildContexts.AddRange(childContexts);
		}

		return listOfChildContexts;
	}
	
	public static string GetScriptPath(this BaseHierarchy hierarchy, bool useVSCodePaths, int depth, out bool hasScript)
	{
		hasScript = !string.IsNullOrEmpty(hierarchy.ScriptPath) || !string.IsNullOrEmpty(hierarchy.ScriptPathFromParent);
		string filePath;
		string fullFilePath;
		
		if (hasScript)
		{
			if (!string.IsNullOrEmpty(hierarchy.ScriptPath))
			{
				filePath = hierarchy.ScriptPath;
				fullFilePath = hierarchy.FullScriptPath;
			}
			else
			{
				filePath = hierarchy.ScriptPathFromParent;
				fullFilePath = hierarchy.FullScriptPathFromParent;
			}
		}
		else
		{
			filePath = hierarchy.FilePath;
			fullFilePath =  hierarchy.FullFilePath;
		}

		return useVSCodePaths ? GetVSCodePath(fullFilePath) : GetPathWithDepth(filePath, depth);
	}

	public static string GetPathWithDepth(string path, int depth)
	{
		if (string.IsNullOrWhiteSpace(path)) 
			return string.Empty;
		
		var depthString = string.Join("", Enumerable.Repeat("../", depth));
		return depthString + path;
	}

	public static string GetVSCodePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) 
			return string.Empty;

		return $"vscode://file/{path}";
	}
}