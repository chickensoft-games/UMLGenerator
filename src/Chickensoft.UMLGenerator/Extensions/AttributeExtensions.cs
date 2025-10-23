namespace Chickensoft.UMLGenerator;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class AttributeExtensions
{
	public static ClassDiagramAttribute GetClassDiagramAttribute(this BaseNode node)
	{
		var attribute = node.GetClassDiagramAttributeSyntax();
		var arguments = attribute?.ArgumentList?.Arguments;
		return new ClassDiagramAttribute()
		{
			UseVSCodePaths = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.UseVSCodePaths)),
			ShowAllProperties = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.ShowAllProperties)),
			ShowAllMethods = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.ShowAllMethods))
		};
	}

	private static AttributeSyntax? GetClassDiagramAttributeSyntax(this BaseNode node)
	{
		var attributeName = nameof(ClassDiagramAttribute).Replace("Attribute", "");
		var classDiagramAttribute = node.ContextList
			.Select(x => (x.Node as TypeDeclarationSyntax)?.AttributeLists.SelectMany(x => x.Attributes))
			.SelectMany(x => x)
			.FirstOrDefault(x => x.Name.ToString() == attributeName);

		return classDiagramAttribute;
	}

	public static bool HasClassDiagramAttribute(this BaseNode node) => node.GetClassDiagramAttributeSyntax() != null;

	private static bool GetAttributeBooleanValue(SeparatedSyntaxList<AttributeArgumentSyntax>? arguments, string attributeName)
	{
		return arguments?.Any(arg =>
			arg.NameEquals is { } nameEquals &&
			nameEquals.Name.ToString() == attributeName &&
			arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;
	}
}