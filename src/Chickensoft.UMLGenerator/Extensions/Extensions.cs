using System.Text;
using Antlr4.Runtime.Misc;

namespace Chickensoft.UMLGenerator;

using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class Extensions
{
    public static int GetLineNumber(this MemberDeclarationSyntax method)
    {
        var source = method.SyntaxTree.ToString();
        var subSource = source.Substring(0, method.SpanStart);
        var lineNumber = subSource.Split('\n');
        return lineNumber.Length;
    }
    
    public static string? ToPascalCase(this string? text)
    {
        if (text is null)
        {
            return null;
        }
        var sb = new StringBuilder(text.Length);
        bool isUpper = true;
        foreach (var c in text)
        {
            if (!char.IsLetterOrDigit(c))
            {
                isUpper = true;
            }
            else
            {
                if (isUpper)
                {
                    sb.Append(char.ToUpper(c));
                    isUpper = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        return sb.ToString();
    }
}