namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Models;

public class NodeModule : IModule
{
	public int Order => (int)ModuleOrder.Middle;
	public string Title => "[Nodes]";

	public void SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var items = new List<ModuleItem>();
		if (hierarchy is NodeHierarchy { Node.AllChildren: not null } nodeHierarchy)
		{
			foreach (var nodeDefinition in nodeHierarchy.Node.AllChildren)
			{
				if (!nodeHierarchyList.TryGetValue(nodeDefinition.Type, out var childNodeHierarchy))
					continue;

				items.Add(new ModuleItem
				{
					Hierarchy = childNodeHierarchy,
					Name = nodeDefinition.Name,
					TypeName = nodeDefinition.Type
				});
			}
		}
		if(items.Any())
			hierarchy.AddModule<NodeModule>(items);
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		var items = hierarchy.ModuleItems[typeof(NodeModule)] ?? [];
		foreach (var module in items)
		{
			var childScript =  module.Hierarchy?.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}
}