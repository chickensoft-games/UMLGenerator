namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;

public class ModuleItem
{
	public BaseNode? Node { get; set; }
	public string TypeName { get; set; }
	public string? Name { get; set; }
	public int LineNumber { get; set; }
}

public interface IModule
{
	int Order { get; }
	string Title { get; }
	List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList);
	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth);
}

public enum ModuleOrder
{
	First = 0,
	Middle = 100,
	Last = 200,
}