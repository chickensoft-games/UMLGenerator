namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class PrimaryConstructorModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Primary Constructors]";
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var parameterList = baseTypeSyntax?.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>();
		var items = new List<ModuleItem>();

		foreach (var ctx in parameterList)
		{
			var typeName = ctx.Type?.ToFullString().Trim();
			var typeWithoutInterface = typeName?.TrimStart('I').Trim();
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy) &&
			    !nodeHierarchyList.TryGetValue(typeWithoutInterface, out childNodeHierarchy))
				continue;

			items.Add(new ModuleItem
			{
				Hierarchy = childNodeHierarchy,
				Name = ctx.Identifier.ToString(),
				TypeName = typeName
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(PrimaryConstructorModule)] ?? [];
		foreach (var module in items)
		{
			var childScript =  module.Hierarchy?.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}
}