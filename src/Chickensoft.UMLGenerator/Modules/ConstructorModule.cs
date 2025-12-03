namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class ConstructorModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Constructors]";
	public bool ShouldDrawChildren => false;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var constructorDeclarations = baseTypeSyntax
			.Members.OfType<ConstructorDeclarationSyntax>().Select(x => x.ParameterList).ToList();

		var items = new List<ModuleItem>();

		foreach (var ctxList in constructorDeclarations)
		{
			foreach (var ctx in ctxList.Parameters)
			{
				var typeName = ctx.Type?.ToFullString().Trim();
				var typeWithoutInterface = typeName?.TrimStart('I').Trim();
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

			if (ctxList.Parameters.Count == 0)
			{
				items.Add(new ModuleItem
				{
					Name = "",
					LineNumber = ctxList.GetLineNumber()
				});
			}
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = node.GetScriptPath(useVSCodePaths, depth);
		var typeName = node.TypeSyntax?.Identifier.ToString();

		var constructorGroups = moduleItems.GroupBy(x => x.LineNumber);

		foreach (var constructorGroup in constructorGroups)
		{
			var constructorItems = constructorGroup.ToList();

			var stringList =
				from module in constructorItems
				where !string.IsNullOrEmpty(module.Name)
				let childScript = module.Node?.GetScriptPath(useVSCodePaths, depth)
				select $"[[{childScript} {module.TypeName}]] {module.Name}";

			var parameters = string.Join(", ", stringList);
			var lineNumber = constructorGroup.Key;

			yield return $"[[{parentScriptPath}:{lineNumber} {typeName}]]({parameters})";
		}
	}
}