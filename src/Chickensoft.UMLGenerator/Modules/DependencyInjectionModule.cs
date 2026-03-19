namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class DependencyInjectionModule : IModule
{
	public int Order => (int)ModuleOrder.Ignore;
	public string Title => string.Empty;
	public bool ShouldDrawChildren => true;

	private readonly Dictionary<DiagramNode, HashSet<DiagramNode>> _items = new();

	public void InitModule(NodeRegistry nodeRegistry)
	{
		foreach (var otherNode in nodeRegistry.AllNodes)
		{
			var constructorParameterList = otherNode.TypeSyntax?
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Select(x => x.ParameterList)
				.Concat([otherNode.TypeSyntax?.ParameterList]);

			var nodesInConstructor = constructorParameterList?.SelectMany(paramList =>
				paramList?.Parameters.Select(paramSyntax => nodeRegistry.FindNodeByType(paramSyntax.Type, otherNode.SemanticModel)) ?? [])
				.OfType<DiagramNode>()
				.Distinct() ?? [];

			foreach (var node in nodesInConstructor)
			{
				if (_items.TryGetValue(node, out var list))
				{
					list.Add(otherNode);
				}
				else
				{
					_items[node] = [otherNode];
				}
			}
		}
	}

	public List<ModuleItem> SetupModule(DiagramNode node, NodeRegistry nodeRegistry)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null || !_items.TryGetValue(node, out var nodeList))
			return [];

		var items = new List<ModuleItem>();

		foreach (var otherNode in nodeList)
		{
			items.Add(new ModuleItem
			{
				Node = otherNode
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(DiagramNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		return [];
	}
}