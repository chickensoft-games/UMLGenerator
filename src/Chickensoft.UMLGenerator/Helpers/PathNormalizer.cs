namespace Chickensoft.UMLGenerator.Helpers;

public static class PathNormalizer
{
	public static string NormalizePath(this string input)
	{
		return input.Replace("\\", "/");
	}
}