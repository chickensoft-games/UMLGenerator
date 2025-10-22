namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class ProvisionModule : IModule
{
	public int Order => (int)ModuleOrder.First;
	public string Title => "[Provisions]";
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var provisionDeclarations = baseTypeSyntax
			.Members.OfType<MethodDeclarationSyntax>()
			.Where(syntax => (syntax.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)
				?.Identifier.Text == "IProvide").ToList();

		var items = new List<ModuleItem>();
		foreach (var ctx in  provisionDeclarations)
		{
			var typeName = (ctx.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.TypeArgumentList.Arguments[0].ToString();
			var typeWithoutInterface = typeName?.TrimStart('I').Trim();
			if (!nodeHierarchyList.TryGetValue(typeName, out var childNodeHierarchy) &&
			    !nodeHierarchyList.TryGetValue(typeWithoutInterface, out childNodeHierarchy))
				continue;

			items.Add(new ModuleItem
			{
				Hierarchy = childNodeHierarchy,
				Name = typeName,
				TypeName = typeName,
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(ProvisionModule)] ?? [];
		foreach (var module in items)
		{
			var childScript =  module.Hierarchy.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}

}