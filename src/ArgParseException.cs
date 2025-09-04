namespace CSharpClassSealer;

public sealed class ArgParseException(string message) : Exception(AddHelpMessage(message))
{
    private static string AddHelpMessage(string message) =>
        message + Environment.NewLine +
        Environment.NewLine + "Usage:" +
        Environment.NewLine + "  CSharpSealer.exe <path> [options]" +
        Environment.NewLine +
        Environment.NewLine + "Arguments:" +
        Environment.NewLine + "  path                Path to the root directory of the project" +
        Environment.NewLine +
        Environment.NewLine + "Options:" +
        Environment.NewLine + "  -e, --exclude       One or more directories/files to exclude" +
        Environment.NewLine;
}