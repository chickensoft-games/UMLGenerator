namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class InterfaceModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Interface Properties]";
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var propertyDeclarations =
			from interfaceMember in hierarchy.InterfaceSyntax?.Members ?? []
			from typeMember in hierarchy.TypeSyntax?.Members ?? []
			where typeMember is PropertyDeclarationSyntax property &&
			      interfaceMember is PropertyDeclarationSyntax interfaceProperty &&
			      property.Identifier.Value == interfaceProperty.Identifier.Value
			orderby (typeMember as PropertyDeclarationSyntax)?.Identifier.ValueText
			select typeMember as PropertyDeclarationSyntax;

		var items = new List<ModuleItem>();

		foreach (var ctx in propertyDeclarations)
		{
			var typeName = ctx.Type.ToString();
			var typeWithoutInterface = typeName.TrimStart('I').Trim();
			BaseHierarchy? childNodeHierarchy = null;
			if(nodeHierarchyList.TryGetValue(typeName, out var value) ||
			   nodeHierarchyList.TryGetValue(typeWithoutInterface, out value))
				childNodeHierarchy = value;

			items.Add(new ModuleItem
			{
				Hierarchy = childNodeHierarchy,
				Name = ctx.Identifier.ToString(),
				TypeName = typeName,
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			var result = $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]]";

			var childScript =  module.Hierarchy?.GetScriptPath(useVSCodePaths, depth);
			if(childScript != null)
				result += $" - [[{childScript} Script]]";

			yield return result;
		}
	}
}