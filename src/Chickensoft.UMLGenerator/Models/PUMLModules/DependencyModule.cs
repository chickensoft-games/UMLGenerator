namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class DependencyModule : IModule
{
	public int Order => (int)ModuleOrder.First;
	public string Title => "[Dependencies]";
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
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
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy) &&
			    !nodeHierarchyList.TryGetValue(typeWithoutInterface, out childNodeHierarchy))
				continue;

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

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(DependencyModule)] ?? [];
		foreach (var module in items)
		{
			var childScript =  module.Hierarchy?.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}
}