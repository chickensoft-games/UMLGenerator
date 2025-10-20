namespace Chickensoft.UMLGenerator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class PumlWriter(IDictionary<string, BaseHierarchy> nodeHierarchyList, GenerationData data)
{
	public void GenerateHierarchy(BaseHierarchy hierarchy)
	{
		if (hierarchy is NodeHierarchy { Node.AllChildren: not null } nodeHierarchy)
			foreach (var nodeDefinition in nodeHierarchy.Node.AllChildren)
			{
				if (!nodeHierarchyList.TryGetValue(nodeDefinition.Type, out var childNodeHierarchy))
					continue;

				hierarchy.AddChild(childNodeHierarchy);
				childNodeHierarchy.AddParent(nodeHierarchy);
			}

		var propertyDeclarations = hierarchy.GetSyntaxContextForPropertyDeclarations(data.SyntaxContexts);
		foreach (var ctx in propertyDeclarations)
		{
			var typeName = Path.GetFileNameWithoutExtension(ctx.SemanticModel.SyntaxTree.FilePath);
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy))
				continue;

			if(childNodeHierarchy == hierarchy)
				continue;

			hierarchy.AddChild(childNodeHierarchy);
			childNodeHierarchy.AddParent(hierarchy);
		}

		var dependentDeclarations = hierarchy.GetSyntaxContextForDependentPropertyDeclarations(data.SyntaxContexts);
		foreach (var ctx in dependentDeclarations)
		{
			var typeName = Path.GetFileNameWithoutExtension(ctx.SemanticModel.SyntaxTree.FilePath);
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy))
				continue;

			hierarchy.AddDependent(childNodeHierarchy);
		}

		var provisionDeclarations = hierarchy.GetSyntaxContextForProvisionedMethodDeclarations(data.SyntaxContexts);
		foreach (var ctx in provisionDeclarations)
		{
			var typeName = Path.GetFileNameWithoutExtension(ctx.SemanticModel.SyntaxTree.FilePath);
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy))
				continue;

			hierarchy.AddProvision(childNodeHierarchy);
		}

		var parameterList = hierarchy.TypeSyntax?.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>();
		foreach (var ctx in parameterList)
		{
			var typeName = ctx.Type?.ToFullString().Trim();
			var typeWithoutInterface = ctx.Type?.ToFullString().TrimStart('I').Trim();
			BaseHierarchy? childNodeHierarchy;
			if (!nodeHierarchyList.TryGetValue(typeName, out childNodeHierarchy) &&
			    !nodeHierarchyList.TryGetValue(typeWithoutInterface, out childNodeHierarchy))
				continue;

			hierarchy.AddChild(childNodeHierarchy);
			childNodeHierarchy.AddParent(hierarchy);
		}
	}

	public string GetDiagram(BaseHierarchy hierarchy, int depth, ClassDiagramAttribute attribute)
	{
		var typeDefinition = GetTypeDefinition(hierarchy, depth, attribute, out var properties);

		var childrenToDraw = hierarchy.Children.Values
			.Where(x => x.Children.Count != 0 ||
			            x.Dependencies.Count != 0 ||
			            x.Provisions.Count != 0 ||
			            x.GetPropertyDeclarations(attribute.ShowAllProperties).Any() ||
			            x.GetMethodDeclarations(attribute.ShowAllMethods).Any() ||
			            !properties.Values.Contains(x)).ToList();

		if (childrenToDraw.Count == 0)
			return typeDefinition;

		var newFilePath = attribute.UseVSCodePaths ?
			HierarchyExtensions.GetVSCodePath(hierarchy.FullFilePath) :
			HierarchyExtensions.GetPathWithDepth(hierarchy.FilePath, depth);

		var childrenDefinitions = string.Join("\n\t",
			childrenToDraw.Select(x =>
				GetDiagram(x, depth, attribute)
			)
		);

		var childrenRelationships = string.Join("\n\t",
			childrenToDraw.Select(x =>
			{
				var memberName = (hierarchy as NodeHierarchy)?
				                 .Node?
				                 .AllChildren
				                 .FirstOrDefault(node => node.Type == x.Name)?.Name
				                 ?? properties.FirstOrDefault(prop => x == prop.Value).Key
				                 ?? x.Name;

				return $"{hierarchy.Name}::{memberName} {(x.Children.Count == 0 ? string.Empty : "-")}--> {x.Name}";
			})
		);

		var packageType = hierarchy switch
		{
			TypeHierarchy => "Type",
			NodeHierarchy => "Scene",
			_ => throw new NotImplementedException()
		};

		return
			$$"""

			  package {{hierarchy.Name}}-{{packageType}} [[{{newFilePath}}]] {
			  	{{typeDefinition}}
			  	{{childrenDefinitions}}
			  	{{childrenRelationships}}
			  }

			  """;
	}

	private string GetTypeDefinition(BaseHierarchy hierarchy, int depth, ClassDiagramAttribute classDiagramAttribute, out IDictionary<string, BaseHierarchy> children)
	{
		var useVsCodePaths = classDiagramAttribute.UseVSCodePaths;
		children = ImmutableDictionary<string, BaseHierarchy>.Empty;

		var externalChildrenString = string.Empty;
		var interfaceMethodsString = string.Empty;
		var dependencyPropertiesString = string.Empty;
		var provisionMethodsString = string.Empty;
		var propertiesString = string.Empty;

		var newScriptPath = hierarchy.GetScriptPath(useVsCodePaths, depth, out var hasScript);

		var propertyDeclarations = hierarchy.GetPropertyDeclarations(classDiagramAttribute.ShowAllProperties).ToList();
		var allProperties = hierarchy.TypeSyntax?.Members.OfType<PropertyDeclarationSyntax>().ToList() ?? [];

		//Get all the names of the properties that exist as a child to the current node
		var props =
			from child in hierarchy.Children
			from prop in allProperties
			where prop.Type.ToString() == child.Key || prop.Type.ToString() == $"I{child.Key}"
			select (
				PropertyName: prop.Identifier.ValueText,
				Hierarchy: child.Value
			);

		//Get all the names of the children which exist as a child to the current node
		//If a property exists, return the property name, otherwise return the type name
		children =
			(from child in hierarchy.Children
				join prop in props on child.Key equals prop.Hierarchy.Name into grouping
				from prop in grouping.DefaultIfEmpty()
				orderby prop.PropertyName, child.Key
				select (Name: prop.PropertyName ?? child.Key, Hierarchy: child.Value))
			.ToDictionary(x => x.Name, x => x.Hierarchy);

		var insideProp = children;

		externalChildrenString = string.Join("\n\t",
			children.Where(x =>  propertyDeclarations
					.All(y => y.Identifier.ValueText != x.Key))
				.Select(x =>
				{
					var scriptDefinitions = string.Empty;
					var propName = x.Key;
					var value = string.Empty;

					var scriptPath = x.Value.GetScriptPath(useVsCodePaths, depth, out var childHasScript);

					var propertyDeclarationSyntax = hierarchy.TypeSyntax?
						.Members
						.OfType<PropertyDeclarationSyntax>()
						.FirstOrDefault(x => x.Identifier.ValueText == propName);

					//Get a direct link to property declaration
					if (propertyDeclarationSyntax != null)
						value = $"[[{newScriptPath}:{propertyDeclarationSyntax.GetLineNumber()} {propName}]]";
					else if (hierarchy is NodeHierarchy nodeHierarchy)
					{
						value = nodeHierarchy
							.Node?
							.AllChildren
							.FirstOrDefault(node => node.Type == propName)?.Name;
					}
					else
						value = propName;

					var fileType = childHasScript ? "Script" : "Scene";

					if(!string.IsNullOrWhiteSpace(scriptPath))
						scriptDefinitions = $" - [[{scriptPath} {fileType}]]";

					return value + scriptDefinitions;
				})
		);

		if (!string.IsNullOrWhiteSpace(externalChildrenString))
			externalChildrenString = "\n--\n" + externalChildrenString;

		if (hierarchy.InterfaceSyntax != null || classDiagramAttribute.ShowAllProperties)
		{
			propertiesString = string.Join("\n\t",
				propertyDeclarations.Select(x =>
				{
					var propName = x?.Identifier.ValueText;
					var value = $"+ [[{newScriptPath}:{x?.GetLineNumber()} {propName}]]";

					if (!insideProp.TryGetValue(propName!, out var child))
						return value;

					var scriptPath = useVsCodePaths
						? HierarchyExtensions.GetVSCodePath(child.FullScriptPath)
						: HierarchyExtensions.GetPathWithDepth(child.ScriptPath, depth);

					var scriptDefinitions = $" - [[{scriptPath} Script]]";

					return value + scriptDefinitions;
				})
			);

			if (!string.IsNullOrWhiteSpace(propertiesString))
				propertiesString = "\n--\n" + propertiesString;
		}

		if(hierarchy.InterfaceSyntax != null || classDiagramAttribute.ShowAllMethods)
		{
			var methodString = hierarchy.GetMethodDeclarations(classDiagramAttribute.ShowAllMethods);

			interfaceMethodsString = string.Join("\n\t",
				methodString.Select(x =>
				{
					var expIntIdent = string.Empty;
					if (x?.ExplicitInterfaceSpecifier?.Name != null)
						expIntIdent = x?.ExplicitInterfaceSpecifier?.Name + ".";

					return $"[[{newScriptPath}:{x?.GetLineNumber()} {expIntIdent}{x?.Identifier.Value}()]]";
				})
			);

			if (!string.IsNullOrWhiteSpace(interfaceMethodsString))
				interfaceMethodsString = "\n--\n" + interfaceMethodsString;
		}

		var dependencies = hierarchy.Dependencies;
		if (dependencies.Count != 0)
		{
			var dependentPropertiesFromClass = hierarchy.GetClassDependentPropertyDeclarations().ToList();
			dependencyPropertiesString = "\n\t" + "[Dependencies]" + "\n\t" + string.Join("\n\t",
				dependentPropertiesFromClass.Select(x =>
				{
					var propName = x?.Type.ToString();
					var value = $"[[{newScriptPath}:{x?.GetLineNumber()} {propName}]]";

					if(!dependencies.TryGetValue(propName!, out var child) && !dependencies.TryGetValue(propName!.Substring(1), out child))
						return value;

					var scriptPath = useVsCodePaths ? HierarchyExtensions.GetVSCodePath(child.FullScriptPath) : HierarchyExtensions.GetPathWithDepth(child.ScriptPath, depth);

					return $"{value} - [[{scriptPath} Script]]";
				})
			);
		}

		var provisions = hierarchy.Provisions;
		if (provisions.Count != 0)
		{
			var provisionMethodsFromClass = hierarchy.GetProvisionMethodDeclarations().ToList();
			provisionMethodsString = "\n\t" + "[Provisions]" + "\n\t" + string.Join("\n\t",
				provisionMethodsFromClass.Select(x =>
				{
					var provisionName = (x.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.TypeArgumentList.Arguments[0].ToString();
					var value = $"[[{newScriptPath}:{x.GetLineNumber()} {provisionName}]]";

					if(!provisions.TryGetValue(provisionName!, out var child) && !provisions.TryGetValue(provisionName!.Substring(1), out child))
						return value;

					var scriptPath = useVsCodePaths ? HierarchyExtensions.GetVSCodePath(child.FullScriptPath) : HierarchyExtensions.GetPathWithDepth(child.ScriptPath, depth);

					return $"{value} - [[{scriptPath} Script]]";
				})
			);
		}

		var fileType = hasScript ? "Script" : "Scene";

		var spotCharacter = hasScript ? "" : "<< (S,black) >>";

		return
		$$"""

		class {{hierarchy.Name}} {{spotCharacter}} {
			[[{{newScriptPath}} {{fileType}}File]]{{dependencyPropertiesString}}{{provisionMethodsString}}{{propertiesString}}{{interfaceMethodsString}}{{externalChildrenString}}
		}

		""";
	}
}