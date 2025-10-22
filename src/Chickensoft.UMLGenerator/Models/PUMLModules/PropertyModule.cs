namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class PropertyModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Properties]";
	public void SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return;

		var propertyDeclarations = baseTypeSyntax
			.Members.OfType<PropertyDeclarationSyntax>()
			.Where(syntax => syntax.AttributeLists.SelectMany(x => x.Attributes)
				.All(x => x.Name.ToString() != "Dependency")).ToList();
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
		if(items.Any())
			hierarchy.AddModule<PropertyModule>(items);
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(PropertyModule)] ?? [];
		foreach (var module in items)
		{
			var result = $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]]";

			var childScript =  module.Hierarchy?.GetScriptPath(useVSCodePaths, depth);
			if(childScript != null)
				result += $" - [[{childScript} Script]]";

			yield return result;
		}
	}
}