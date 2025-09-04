using System.CommandLine;
using Microsoft.CodeAnalysis;

namespace CSharpClassSealer;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var pathArgument = new Argument<string>("path")
            {
                Description = "Path to the root directory of the project to process"
            };

            var excludeOptions = new Option<string[]>(name: "exclude", aliases: ["--exclude", "-e"])
            {
                Description = "List of directories or files to exclude (separated by space)"
            };

            var rootCommand = new RootCommand("CSharpClassSealer — utility to automatically add 'sealed' modifier to all classes in C# project.");
            rootCommand.Arguments.Add(pathArgument);
            rootCommand.Options.Add(excludeOptions);
            ParseResult parseResult = rootCommand.Parse(args);

            if (parseResult.Errors.Count > 0) throw new ArgParseException(string.Join(Environment.NewLine, parseResult.Errors.Select(e => e.Message)));
            if (parseResult.GetValue(pathArgument) is not string path) throw new ArgParseException("Couldn't parse path");
            if (parseResult.GetValue(excludeOptions) is not string[] exclude) throw new ArgParseException("Couldn't parse exclude");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgParseException("Path is required");
            if (!Directory.Exists(path)) throw new ArgParseException($"Directory not found: {path}");

            Compilation compilation = Parser.CompileSyntaxTrees(
                Parser.GetAllSyntaxTreesInDirectory(path, exclude));

            SealedRewriter sealedRewriter = new(compilation);

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SyntaxNode sealedNode = sealedRewriter.Visit(syntaxTree.GetRoot());
                if (sealedNode is null) continue;
                File.WriteAllText(syntaxTree.FilePath, sealedNode.ToFullString());
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Visited nodes: {sealedRewriter.VisitedCount}");
            Console.WriteLine($"Sealed classes: {sealedRewriter.SealedCount}");
            Console.ResetColor();
        }
        catch (ArgParseException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }
    }
}