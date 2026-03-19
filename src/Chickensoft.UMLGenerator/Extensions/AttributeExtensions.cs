namespace Chickensoft.UMLGenerator;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class AttributeExtensions
{
	public static ClassDiagramAttribute GetClassDiagramAttribute(this DiagramNode node)
	{
		var attribute = node.GetClassDiagramAttributeSyntax();
		var arguments = attribute?.ArgumentList?.Arguments;
		return new ClassDiagramAttribute()
		{
			UseVSCodePaths = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.UseVSCodePaths)),
			GetAllTopLevelNodes = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.GetAllTopLevelNodes))
		};
	}

	private static AttributeSyntax? GetClassDiagramAttributeSyntax(this DiagramNode node)
	{
		var attributeName = nameof(ClassDiagramAttribute).Replace("Attribute", "");
		var classDiagramAttribute = node.TypeSyntax?
			.AttributeLists.SelectMany(x => x.Attributes)
			.FirstOrDefault(x => x.Name.ToString() == attributeName);

		return classDiagramAttribute;
	}

	public static bool HasClassDiagramAttribute(this DiagramNode node) => node.GetClassDiagramAttributeSyntax() != null;

	private static bool GetAttributeBooleanValue(SeparatedSyntaxList<AttributeArgumentSyntax>? arguments, string attributeName)
	{
		return arguments?.Any(arg =>
			arg.NameEquals is { } nameEquals &&
			nameEquals.Name.ToString() == attributeName &&
			arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;
	}
}