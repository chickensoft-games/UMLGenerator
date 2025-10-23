namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class PrimaryConstructorModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Primary Constructors]";
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
				TypeName = typeName
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