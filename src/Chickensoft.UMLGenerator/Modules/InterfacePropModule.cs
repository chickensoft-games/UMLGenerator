namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class InterfaceModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Interface Properties]";
	public bool ShouldDrawChildren => true;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var propertyDeclarations =
			from interfaceMember in node.InterfaceSyntax?.Members ?? []
			from typeMember in node.TypeSyntax?.Members ?? []
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
			BaseNode? childClassNode = null;
			if(sceneNodeList.TryGetValue(typeName, out var value) ||
			   sceneNodeList.TryGetValue(typeWithoutInterface, out value))
				childClassNode = value;

			items.Add(new ModuleItem
			{
				Node = childClassNode,
				Name = ctx.Identifier.ToString(),
				TypeName = typeName,
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = node.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			var result = $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]]";

			var childScript =  module.Node?.GetScriptPath(useVSCodePaths, depth);
			if(childScript != null)
				result += $" - [[{childScript} Script]]";

			yield return result;
		}
	}
}