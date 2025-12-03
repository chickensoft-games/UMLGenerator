namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class DependencyInjectionModule : IModule
{
	public int Order => (int)ModuleOrder.Ignore;
	public string Title => string.Empty;
	public bool ShouldDrawChildren => true;

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var items = new List<ModuleItem>();

		foreach (var otherNode in sceneNodeList.Values)
		{
			var constructorParameterList = otherNode.TypeSyntax?
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Select(x => x.ParameterList)
				.Concat([otherNode.TypeSyntax?.ParameterList]);

			if (constructorParameterList?.Any(paramList => paramList?.Parameters.
				    Any(paramSyntax => paramSyntax.Type?.ToString() == baseTypeSyntax.Identifier.ToString()) is true) is true)
			{
				items.Add(new ModuleItem
				{
					Node = otherNode
				});
			}
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		return [];
	}
}