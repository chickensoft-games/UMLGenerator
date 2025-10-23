namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using Helpers;
using Models;

public class NodeModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Nodes]";

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var items = new List<ModuleItem>();
		if (node is SceneNode { Node.AllChildren: not null } sceneNode)
		{
			foreach (var nodeDefinition in sceneNode.Node.AllChildren)
			{
				if (!sceneNodeList.TryGetValue(nodeDefinition.Type, out var childSceneNode))
					continue;

				items.Add(new ModuleItem
				{
					Node = childSceneNode,
					Name = nodeDefinition.Name,
					TypeName = nodeDefinition.Type
				});
			}
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