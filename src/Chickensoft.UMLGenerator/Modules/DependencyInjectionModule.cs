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

	private readonly Dictionary<BaseNode, HashSet<BaseNode>> _items = new();

	public void InitModule(IDictionary<string, BaseNode> sceneNodeList)
	{
		foreach (var otherNode in sceneNodeList.Values)
		{
			var constructorParameterList = otherNode.TypeSyntax?
				.Members.OfType<ConstructorDeclarationSyntax>()
				.Select(x => x.ParameterList)
				.Concat([otherNode.TypeSyntax?.ParameterList]);

			var nodesInConstructor = constructorParameterList?.SelectMany(paramList =>
				paramList?.Parameters.Select(paramSyntax => paramSyntax.Type.FindNodeByType(otherNode.SemanticModel, sceneNodeList)) ?? [])
				.OfType<BaseNode>()
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

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
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

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		return [];
	}
}