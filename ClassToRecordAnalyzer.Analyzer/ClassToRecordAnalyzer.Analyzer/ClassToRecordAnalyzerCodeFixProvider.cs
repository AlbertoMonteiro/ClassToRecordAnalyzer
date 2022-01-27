using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace ClassToRecordAnalyzer.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassToRecordAnalyzerCodeFixProvider)), Shared]
    public class ClassToRecordAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ClassToRecordAnalyzer.DiagnosticId);

        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => GenerateRecord(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> GenerateRecord(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sm = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var tokens = new List<SyntaxNodeOrToken>();
            foreach (var p in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (tokens.Count >= 1)
                    tokens.Add(Token(SyntaxKind.CommaToken));
                tokens.Add(Parameter(Identifier(p.Identifier.ValueText)).WithType(p.Type)) ;
            }

            var record = RecordDeclaration(
                                Token(SyntaxKind.RecordKeyword),
                                Identifier(classDeclaration.Identifier.ValueText))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithParameterList(
                                ParameterList(
                                    SeparatedList<ParameterSyntax>(
                                        tokens)))
                            .WithSemicolonToken(
                                Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace();

            var newRoot = root.ReplaceNode(classDeclaration, record).NormalizeWhitespace();
            return document.WithSyntaxRoot(newRoot);
        }

        private static FieldDeclarationSyntax GenerateField(string typeName, string fieldName)
        {
            var modifiers = SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) });
            return SyntaxFactory.FieldDeclaration(
                                            SyntaxFactory.VariableDeclaration(
                                                SyntaxFactory.IdentifierName(typeName))
                                            .WithVariables(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier($"_{fieldName}"))
                                                    .WithInitializer(
                                                        SyntaxFactory.EqualsValueClause(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.StringLiteralExpression,
                                                                SyntaxFactory.Literal("oi")))))))
                .WithModifiers(modifiers);
        }

        //private async Task<Solution> GenerateSubstitutes(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        //{
        //    // Compute new uppercase name.
        //    var identifierToken = typeDecl.Identifier;
        //    var newName = identifierToken.Text.ToUpperInvariant();

        //    // Get the symbol representing the type to be renamed.
        //    var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        //    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

        //    // Produce a new solution that has all references to that type renamed, including the declaration.
        //    var originalSolution = document.Project.Solution;
        //    var optionSet = originalSolution.Workspace.Options;
        //    var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

        //    // Return the new solution with the now-uppercase type name.
        //    return newSolution;
        //}
    }
}
