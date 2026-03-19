namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using Helpers;
using Models;

public class NodeModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Nodes]";
	public bool ShouldDrawChildren => true;

	public void InitModule(NodeRegistry nodeRegistry) { }

	public List<ModuleItem> SetupModule(DiagramNode node, NodeRegistry nodeRegistry)
	{
		var items = new List<ModuleItem>();
		if (!node.HasScene || node.SceneNode?.AllChildren == null)
			return items;

		foreach (var childNodeDefinition in node.SceneNode.AllChildren)
		{
			if (!nodeRegistry.TryGetBySceneName(childNodeDefinition.Type, out var childNode))
				continue;

			items.Add(new ModuleItem
			{
				Node = childNode,
				Name = childNodeDefinition.Name,
				TypeName = childNodeDefinition.Type
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(DiagramNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		foreach (var module in moduleItems)
		{
			var childScript = module.Node?.GetScriptPath(useVSCodePaths, depth);
			yield return $"{module.Name} - [[{childScript} Script]]";
		}
	}
}