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

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

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

            var tokens = new List<SyntaxNodeOrToken>();
            foreach (var property in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (tokens.Count >= 1)
                    tokens.Add(Token(SyntaxKind.CommaToken));
                var parameter = Parameter(Identifier(property.Identifier.ValueText)).WithType(property.Type);
                if (property.AttributeLists.Any())
                {
                    var newAttributeList = AttributeList(SeparatedList(property.AttributeLists.SelectMany(x => x.Attributes)));
                    parameter = parameter.WithAttributeLists(SingletonList(newAttributeList.WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.PropertyKeyword)))));
                }
                tokens.Add(parameter);
            }

            var record = RecordDeclaration(Token(SyntaxKind.RecordKeyword), classDeclaration.Identifier.ValueText)
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(tokens)))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace();

            var newRoot = root.ReplaceNode(classDeclaration, record).NormalizeWhitespace();
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
