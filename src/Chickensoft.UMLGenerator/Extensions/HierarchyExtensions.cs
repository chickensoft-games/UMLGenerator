namespace Chickensoft.UMLGenerator.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class HierarchyExtensions
{
	/// <summary>
	/// This will return properties which exist in the interface
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<PropertyDeclarationSyntax> GetPropertyDeclarations(this BaseHierarchy hierarchy, bool getAllProperties)
	{
		if (getAllProperties && hierarchy.TypeSyntax != null)
			return from typeMember in hierarchy.TypeSyntax.Members
				where typeMember is PropertyDeclarationSyntax property &&
				      property.AttributeLists.SelectMany(x => x.Attributes)
					      .All(x => x.Name.ToString() != "Dependency")
				orderby (typeMember as PropertyDeclarationSyntax)?.Identifier.ValueText
				select typeMember as PropertyDeclarationSyntax;

		if (hierarchy.InterfaceSyntax == null) return [];
		return from interfaceMember in hierarchy.InterfaceSyntax.Members
			from typeMember in hierarchy.TypeSyntax?.Members ?? []
			where typeMember is PropertyDeclarationSyntax property &&
			      interfaceMember is PropertyDeclarationSyntax interfaceProperty &&
			      property.Identifier.Value == interfaceProperty.Identifier.Value
			orderby (typeMember as PropertyDeclarationSyntax)?.Identifier.ValueText
			select typeMember as PropertyDeclarationSyntax;
	}
	
	/// <summary>
	/// This will return type methods which exist in the interface
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<MethodDeclarationSyntax> GetMethodDeclarations(this BaseHierarchy hierarchy, bool getAllMethods)
	{
		if (getAllMethods && hierarchy.TypeSyntax != null)
			return hierarchy.TypeSyntax.Members.OfType<MethodDeclarationSyntax>()
				.Where(x => (x.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.Identifier.Text != "IProvide");

		if (hierarchy.InterfaceSyntax == null) return [];
		return from interfaceMember in hierarchy.InterfaceSyntax!.Members
			from typeMember in hierarchy.TypeSyntax?.Members ?? []
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      interfaceMember is MethodDeclarationSyntax interfaceMethod &&
			      typeMethod.Identifier.Value == interfaceMethod.Identifier.Value
			orderby (typeMember as MethodDeclarationSyntax)?.Identifier.ValueText
			select typeMember as MethodDeclarationSyntax;
	}

	/// <summary>
	/// This will return properties with the [Dependency] attribute which exist in the class
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<PropertyDeclarationSyntax> GetClassDependentPropertyDeclarations(this BaseHierarchy hierarchy)
	{
		if (hierarchy.TypeSyntax == null) return [];
		return from typeMember in hierarchy.TypeSyntax.Members
			where typeMember is PropertyDeclarationSyntax property &&
			      property.AttributeLists.SelectMany(x => x.Attributes)
				      .Any(x => x.Name.ToString() == "Dependency")
			orderby (typeMember as PropertyDeclarationSyntax)?.Identifier.ValueText
			select typeMember as PropertyDeclarationSyntax;
	}

	/// <summary>
	/// This will return methods with the IProvide ExplicitInterfaceSpecifier which exist in the class
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<MethodDeclarationSyntax> GetProvisionMethodDeclarations(this BaseHierarchy hierarchy)
	{
		if (hierarchy.TypeSyntax == null) return [];
		return from typeMember in hierarchy.TypeSyntax.Members
			where typeMember is MethodDeclarationSyntax method &&
			      (method.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.Identifier.Text == "IProvide"
			orderby (typeMember as MethodDeclarationSyntax)?.Identifier.ValueText
			select typeMember as MethodDeclarationSyntax;
	}

	/// <summary>
	/// Finds all syntax contexts for classes of declared properties which have the Dependency attribute
	/// </summary>
	/// <returns></returns>
	public static IList<GeneratorSyntaxContext> GetSyntaxContextForDependentPropertyDeclarations(this BaseHierarchy hierarchy, IEnumerable<GeneratorSyntaxContext> allSyntaxContexts)
	{
		return GetSyntaxContextForMethodDeclarations<PropertyDeclarationSyntax>(
			hierarchy, allSyntaxContexts,
			syntax => syntax.AttributeLists.SelectMany(x => x.Attributes)
				.Any(x => x.Name.ToString() == "Dependency"),
			(property) => property.Type.ToString()
		);
	}

	/// <summary>
	/// Finds all syntax contexts for classes of declared properties which don't have the Dependency attribute
	/// </summary>
	/// <returns></returns>
	public static IList<GeneratorSyntaxContext> GetSyntaxContextForPropertyDeclarations(this BaseHierarchy hierarchy, IEnumerable<GeneratorSyntaxContext> allSyntaxContexts)
	{
		return GetSyntaxContextForMethodDeclarations<PropertyDeclarationSyntax>(
			hierarchy, allSyntaxContexts,
			syntax => syntax.AttributeLists.SelectMany(x => x.Attributes)
				.All(x => x.Name.ToString() != "Dependency"),
			(property) => property.Type.ToString());
	}

	/// <summary>
	/// Finds all syntax contexts for class argument T of the IProvide{T}.Value() method
	/// </summary>
	/// <returns></returns>
	public static IList<GeneratorSyntaxContext> GetSyntaxContextForProvisionedMethodDeclarations(this BaseHierarchy hierarchy, IEnumerable<GeneratorSyntaxContext> allSyntaxContexts)
	{
		return GetSyntaxContextForMethodDeclarations<MethodDeclarationSyntax>(
			hierarchy, allSyntaxContexts,
			syntax => (syntax.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)
				?.Identifier.Text == "IProvide",
			(syntax) =>
				(syntax.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?
				.TypeArgumentList.Arguments[0].ToString() ?? string.Empty);
	}

	private static IList<GeneratorSyntaxContext> GetSyntaxContextForMethodDeclarations<TSyntax>(
		this BaseHierarchy hierarchy,
		IEnumerable<GeneratorSyntaxContext> allSyntaxContexts,
		Func<TSyntax, bool> memberFilter,
		Func<TSyntax, string> nameFilter) where TSyntax : MemberDeclarationSyntax
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return ImmutableList<GeneratorSyntaxContext>.Empty;

		var allSyntaxContextList = allSyntaxContexts.ToImmutableList();
		var listOfDependentContexts = new List<GeneratorSyntaxContext>();

		//Find all members that match the filter
		var syntaxes = baseTypeSyntax
			.Members.OfType<TSyntax>()
			.Where(memberFilter).ToList();

		foreach (var syntax in syntaxes)
		{
			//Get the name of the type, then find all syntax contexts that match that name
			var typeName = nameFilter(syntax);
			var childContexts = allSyntaxContextList
				.Where(x =>
				{
					var typeSyntax = x.Node as TypeDeclarationSyntax;
					var sourceFileName = typeSyntax?.Identifier.ValueText;
					return sourceFileName == typeName;
				});
			listOfDependentContexts.AddRange(childContexts);
		}

		return listOfDependentContexts;
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