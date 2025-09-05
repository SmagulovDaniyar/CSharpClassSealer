using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpClassSealer;

public static class Parser
{
    private static IEnumerable<string> GetAllFilesInDirectory(string directory, IEnumerable<string> exclude) =>
        GetAllFilesInDirectory(directory, exclude.Distinct().ToHashSet());

    private static IEnumerable<string> GetAllFilesInDirectory(string directory, HashSet<string> exclude) =>
        Directory
            .GetFiles(directory)
            .Concat(Directory
                .GetDirectories(directory)
                .Where(d => !exclude.Contains(d))
                .SelectMany(d => Directory.GetFiles(d)))
            .Where(p => !exclude.Contains(p));

    public static IEnumerable<SyntaxTree> GetAllSyntaxTreesInDirectory(string directory, IEnumerable<string> exclude) =>
        GetAllFilesInDirectory(directory, exclude)
        .Where(f => Path.GetExtension(f) == ".cs")
        .Select(f => CSharpSyntaxTree
            .ParseText(text: File.ReadAllText(f), path: f));

    public static Compilation CompileSyntaxTrees(IEnumerable<SyntaxTree> syntaxTrees) =>
        CSharpCompilation.Create(
            assemblyName: "",
            syntaxTrees: syntaxTrees,
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            ],
            options: null
        );
}
