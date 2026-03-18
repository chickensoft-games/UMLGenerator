namespace Chickensoft.UMLGenerator.Modules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class ProvisionModule : IModule
{
	public int Order => (int)ModuleOrder.First;
	public string Title => "[Provisions]";
	public bool ShouldDrawChildren => false;

	public void InitModule(IDictionary<string, BaseNode> sceneNodeList) { }

	public List<ModuleItem> SetupModule(BaseNode node, IDictionary<string, BaseNode> sceneNodeList)
	{
		var baseTypeSyntax = node.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var provisionDeclarations = baseTypeSyntax
			.Members.OfType<MethodDeclarationSyntax>()
			.Where(syntax => (syntax.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)
				?.Identifier.Text == "IProvide").ToList();

		var items = new List<ModuleItem>();
		foreach (var ctx in provisionDeclarations)
		{
			var typeArgSyntax = (ctx.ExplicitInterfaceSpecifier?.Name as GenericNameSyntax)?.TypeArgumentList.Arguments[0];
			var typeName = typeArgSyntax?.ToString();
			var childClassNode = typeArgSyntax.FindNodeByType(node.SemanticModel, sceneNodeList);
			if (childClassNode == null)
				continue;

			items.Add(new ModuleItem
			{
				Node = childClassNode,
				Name = typeName,
				TypeName = typeName,
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = node.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			var childScript = module.Node.GetScriptPath(useVSCodePaths, depth);
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}]] - [[{childScript} Script]]";
		}
	}
}