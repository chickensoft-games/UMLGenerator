namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class PrimaryConstructorModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Primary Constructor]";
	public bool ShouldDrawChildren => false;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var parameterList = baseTypeSyntax?.ParameterList?.Parameters ?? Enumerable.Empty<ParameterSyntax>();
		var items = new List<ModuleItem>();

		foreach (var ctx in parameterList)
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

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = node.GetScriptPath(useVSCodePaths, depth);
		var typeName = node.TypeSyntax?.Identifier.ToString();

		var stringList = new List<string>();

		foreach (var module in moduleItems)
		{
			var childScript =  module.Node?.GetScriptPath(useVSCodePaths, depth);
			stringList.Add($"[[{childScript} {module.TypeName}]] {module.Name}");
		}

		var parameters = string.Join(", ", stringList);
		var lineNumber = moduleItems.First().LineNumber;

		return [$"[[{parentScriptPath}:{lineNumber} {typeName}]]({parameters})"];
	}
}