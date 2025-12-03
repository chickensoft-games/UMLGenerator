namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class DependencyModule : IModule
{
	public int Order => (int)ModuleOrder.First;
	public string Title => "[Dependencies]";
	public bool ShouldDrawChildren => false;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var dependentDeclarations = baseTypeSyntax
			.Members.OfType<PropertyDeclarationSyntax>()
			.Where(syntax => syntax.AttributeLists.SelectMany(x => x.Attributes)
				.Any(x => x.Name.ToString() == "Dependency")).ToList();

		var items = new List<ModuleItem>();

		foreach (var ctx in dependentDeclarations)
		{
			var typeName = ctx.Type.ToString();
			var typeWithoutInterface = typeName.TrimStart('I').Trim();
			if (!sceneNodeList.TryGetValue(typeName, out var childClassNode) &&
			    !sceneNodeList.TryGetValue(typeWithoutInterface, out childClassNode))
				continue;

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
			var childScript =  module.Node?.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}
}