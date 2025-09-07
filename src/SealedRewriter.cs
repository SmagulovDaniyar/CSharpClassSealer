using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpClassSealer;

public sealed class SealedRewriter(Compilation compilation) : CSharpSyntaxRewriter
{
    private readonly Compilation _compilation = compilation;
    private readonly HashSet<ISymbol?> _derivedClasses = GetDerivedClasses(compilation)
            .Distinct(SymbolEqualityComparer.Default)
            .ToHashSet(SymbolEqualityComparer.Default);

    private readonly List<string> _unsealed = [];
    public IEnumerable<string> Unsealed => _unsealed;

    public int VisitedCount { get; private set; } = 0;
    public int SealedCount { get; private set; } = 0;
    public int UnsealedCount { get; private set; } = 0;

    public static readonly SyntaxKind[] AccessModifiers = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.FileKeyword
    ];


    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        ++VisitedCount;

        ClassDeclarationSyntax? visited = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node);

        try
        {
            if (visited is null) return visited;
            if (!CanSeal(visited, visited.Modifiers)) return visited;

            ClassDeclarationSyntax sealedClass = visited.WithModifiers(SyntaxFactory.TokenList(AddSealedSyntaxToken(visited.Modifiers)));

            ++SealedCount;

            return sealedClass;
        }
        catch (Exception)
        {
            if (visited is not null)
            {
                ++UnsealedCount;
                _unsealed.Add(visited.Identifier.Text);
            }

            return visited;
        }
    }

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        ++VisitedCount;

        RecordDeclarationSyntax? visited = (RecordDeclarationSyntax?)base.VisitRecordDeclaration(node);

        try
        {
            if (visited is null) return visited;
            if (!CanSeal(visited, visited.Modifiers)) return visited;
            if (visited.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)) return visited;

            RecordDeclarationSyntax sealedRecord = visited.WithModifiers(SyntaxFactory.TokenList(AddSealedSyntaxToken(visited.Modifiers)));

            ++SealedCount;

            return sealedRecord;
        }
        catch (Exception)
        {
            if (visited is not null)
            {
                ++UnsealedCount;
                _unsealed.Add(visited.Identifier.Text);
            }

            return visited;
        }
    }


    private IEnumerable<SyntaxToken> AddSealedSyntaxToken(SyntaxTokenList syntaxTokens) =>
        syntaxTokens
            .Where(t => AccessModifiers.Contains(t.Kind()))
            .Append(SyntaxFactory
                .Token(SyntaxKind.SealedKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space))
            .Concat(syntaxTokens.Where(t => !AccessModifiers.Contains(t.Kind())));

    private bool CanSeal(TypeDeclarationSyntax syntaxNode, SyntaxTokenList syntaxTokens) =>
        !(IsUnsealableModifier(syntaxTokens) || IsDerivedClass(syntaxNode) || HasVirtualOrAbstractMembers(syntaxNode));

    private static bool IsUnsealableModifier(SyntaxTokenList syntaxTokens) =>
        syntaxTokens
            .Any(m => m.IsKind(SyntaxKind.SealedKeyword)
                   || m.IsKind(SyntaxKind.AbstractKeyword)
                   || m.IsKind(SyntaxKind.StaticKeyword));

    private static bool HasVirtualOrAbstractMembers(TypeDeclarationSyntax typeDecl) =>
        typeDecl.Members.Any(m =>
            (m is MethodDeclarationSyntax method     && method.Modifiers.Any(IsVirtualOrAbstract)) ||
            (m is PropertyDeclarationSyntax property && property.Modifiers.Any(IsVirtualOrAbstract)) ||
            (m is EventDeclarationSyntax ev          && ev.Modifiers.Any(IsVirtualOrAbstract)) ||
            (m is IndexerDeclarationSyntax indexer   && indexer.Modifiers.Any(IsVirtualOrAbstract)));

    private static bool IsVirtualOrAbstract(SyntaxToken token) =>
        token.IsKind(SyntaxKind.VirtualKeyword) || token.IsKind(SyntaxKind.AbstractKeyword);

    private bool IsDerivedClass(SyntaxNode syntaxNode) =>
        _compilation.GetSemanticModel(syntaxNode.SyntaxTree)
            .GetDeclaredSymbol(syntaxNode) is INamedTypeSymbol namedTypeSymbol
            && _derivedClasses.Contains(namedTypeSymbol.OriginalDefinition);

    private static IEnumerable<INamedTypeSymbol> GetDerivedClasses(Compilation compilation) =>
        compilation.SyntaxTrees.SelectMany(t => GetDerivedClasses(t, compilation.GetSemanticModel(t)));

    private static IEnumerable<INamedTypeSymbol> GetDerivedClasses(SyntaxTree syntaxTree, SemanticModel semanticModel) =>
        syntaxTree
            .GetRoot()
            .DescendantNodes()
            .Where(n => n is ClassDeclarationSyntax or RecordDeclarationSyntax)
            .Select(n => semanticModel.GetDeclaredSymbol(n))
            .OfType<INamedTypeSymbol>()
            .Where(s => s.BaseType is not null)
            .Select(s => s.BaseType!.OriginalDefinition);
}