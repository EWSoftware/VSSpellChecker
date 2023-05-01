//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckCodeFixProvider.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/30/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a class used to provide the spell check code fixes
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 01/29/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

using VisualStudio.SpellChecker.CodeAnalyzer;

namespace VisualStudio.SpellChecker.CodeFixes
{
    /// <summary>
    /// This is used to provide the spell check code fixes
    /// </summary>
    // TODO: Separate code fixes for VB and F# are likely needed since they uses different SyntaxNode types among
    // other things.
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SpellCheckCodeFixProvider)), Shared]
    public class SpellCheckCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc />
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            CSharpSpellCheckCodeAnalyzer.SpellingDiagnosticId);

        /// <summary>
        /// This fix provider does not use a fix all provider
        /// </summary>
        /// <returns>Always returns null</returns>
        /// <remarks>It seems to work well enough without out it so the batch fix all provider is not used here</remarks>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        /// <inheritdoc />
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach(var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the identifier for the diagnostic
                var syntaxToken = root.FindToken(diagnosticSpan.Start);

                if(diagnostic.Properties.TryGetValue("Suggestions", out string suggestions))
                {
                    // If the misspelling is a sub-span, the prefix and suffix will contain the surrounding text
                    // used to create the full identifier.
                    _ = diagnostic.Properties.TryGetValue("Prefix", out string prefix);
                    _ = diagnostic.Properties.TryGetValue("Suffix", out string suffix);

                    ImmutableArray<CodeAction> replacements;

                    if(!String.IsNullOrWhiteSpace(suggestions))
                    {
                        replacements = suggestions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(
                            s =>
                            {
                                string replacement = s;

                                if(!String.IsNullOrWhiteSpace(prefix) || !String.IsNullOrWhiteSpace(suffix))
                                    replacement = prefix + replacement + suffix;

                                return CodeAction.Create(replacement, c => CorrectSpellingAsync(context.Document, syntaxToken,
                                    replacement, c), nameof(CodeFixResources.CodeFixTitle));

                            }).ToImmutableArray();
                    }
                    else
                    {
                        replacements = new[] { CodeAction.Create("(No Suggestions)",
                            c => CorrectSpellingAsync(context.Document, syntaxToken, null, c),
                            nameof(CodeFixResources.CodeFixTitle)) }.ToImmutableArray();
                    }

                    // Register a code action that will invoke the fix and offer the various suggested replacements
#pragma warning disable RS1010
                    context.RegisterCodeFix(CodeAction.Create(diagnostic.Descriptor.MessageFormat.ToString(null),
                        replacements, false), diagnostic);
#pragma warning restore RS1010
                }
            }
        }

        /// <summary>
        /// Create the solution used to correct a spelling error
        /// </summary>
        /// <param name="document">The document containing the misspelling</param>
        /// <param name="token">The token for the misspelling's location</param>
        /// <param name="replacement">The replacement text</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The solution task used to correct the misspelling or null if not supported</returns>
        private static async Task<Solution> CorrectSpellingAsync(Document document, SyntaxToken token, string replacement,
            CancellationToken cancellationToken)
        {
            if(document == null || replacement == null)
                return null;

            // Get the symbol representing the identifier to be renamed
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            ISymbol symbol = null;
            bool renameFile = false;

            // I'm not sure we'll see all of these but we'll play it safe
            switch(token.Parent)
            {
                case CompilationUnitSyntax cus:
                    symbol = semanticModel.GetDeclaredSymbol(cus, cancellationToken);
                    break;

                case NamespaceDeclarationSyntax nds:
                    symbol = semanticModel.GetDeclaredSymbol(nds, cancellationToken);
                    break;
#if !VS2017AND2019
                case FileScopedNamespaceDeclarationSyntax fsnds:
                    symbol = semanticModel.GetDeclaredSymbol(fsnds, cancellationToken);
                    break;
#endif
                case BaseTypeDeclarationSyntax btds:
                    symbol = semanticModel.GetDeclaredSymbol(btds, cancellationToken);

                    // Rename the file only if the type is not nested within another type
                    renameFile = !(token.Parent?.Parent is ClassDeclarationSyntax);
                    break;

                case DelegateDeclarationSyntax dds:
                    symbol = semanticModel.GetDeclaredSymbol(dds, cancellationToken);
                    break;

                case EnumMemberDeclarationSyntax emds:
                    symbol = semanticModel.GetDeclaredSymbol(emds, cancellationToken);
                    break;

                case BaseMethodDeclarationSyntax bmds:
                    symbol = semanticModel.GetDeclaredSymbol(bmds, cancellationToken);
                    break;

                case PropertyDeclarationSyntax pds:
                    symbol = semanticModel.GetDeclaredSymbol(pds, cancellationToken);
                    break;

                case IndexerDeclarationSyntax ids:
                    symbol = semanticModel.GetDeclaredSymbol(ids, cancellationToken);
                    break;

                case EventDeclarationSyntax eds:
                    symbol = semanticModel.GetDeclaredSymbol(eds, cancellationToken);
                    break;

                case BasePropertyDeclarationSyntax bpds:
                    symbol = semanticModel.GetDeclaredSymbol(bpds, cancellationToken);
                    break;

                case MemberDeclarationSyntax mds:
                    symbol = semanticModel.GetDeclaredSymbol(mds, cancellationToken);
                    break;

                case AnonymousObjectMemberDeclaratorSyntax aomds:
                    symbol = semanticModel.GetDeclaredSymbol(aomds, cancellationToken);
                    break;

                case AnonymousObjectCreationExpressionSyntax aoces:
                    symbol = semanticModel.GetDeclaredSymbol(aoces, cancellationToken);
                    break;

                case TupleExpressionSyntax tes:
                    symbol = semanticModel.GetDeclaredSymbol(tes, cancellationToken);
                    break;

                case ArgumentSyntax argsyn:
                    symbol = semanticModel.GetDeclaredSymbol(argsyn, cancellationToken);
                    break;

                case AccessorDeclarationSyntax ads:
                    symbol = semanticModel.GetDeclaredSymbol(ads, cancellationToken);
                    break;

                case SingleVariableDesignationSyntax svds:
                    symbol = semanticModel.GetDeclaredSymbol(svds, cancellationToken);
                    break;

                case VariableDeclaratorSyntax vds:
                    symbol = semanticModel.GetDeclaredSymbol(vds, cancellationToken);
                    break;

                case TupleElementSyntax tes:
                    symbol = semanticModel.GetDeclaredSymbol(tes, cancellationToken);
                    break;

                case LabeledStatementSyntax lss:
                    symbol = semanticModel.GetDeclaredSymbol(lss, cancellationToken);
                    break;

                case SwitchLabelSyntax sls:
                    symbol = semanticModel.GetDeclaredSymbol(sls, cancellationToken);
                    break;

                case UsingDirectiveSyntax uds:
                    symbol = semanticModel.GetDeclaredSymbol(uds, cancellationToken);
                    break;

                case ExternAliasDirectiveSyntax eads:
                    symbol = semanticModel.GetDeclaredSymbol(eads, cancellationToken);
                    break;

                case ParameterSyntax ps:
                    symbol = semanticModel.GetDeclaredSymbol(ps, cancellationToken);
                    break;

                case TypeParameterSyntax tps:
                    symbol = semanticModel.GetDeclaredSymbol(tps, cancellationToken);
                    break;

                case ForEachStatementSyntax fess:
                    symbol = semanticModel.GetDeclaredSymbol(fess, cancellationToken);
                    break;

                case CatchDeclarationSyntax cds:
                    symbol = semanticModel.GetDeclaredSymbol(cds, cancellationToken);
                    break;

                case QueryClauseSyntax qcs:
                    symbol = semanticModel.GetDeclaredSymbol(qcs, cancellationToken);
                    break;

                case JoinIntoClauseSyntax jics:
                    symbol = semanticModel.GetDeclaredSymbol(jics, cancellationToken);
                    break;

                case QueryContinuationSyntax qcs:
                    symbol = semanticModel.GetDeclaredSymbol(qcs, cancellationToken);
                    break;

                case IdentifierNameSyntax _:
                    // TODO: Renaming namespace parts needs some work.  It's fine for the last part but renaming
                    // an earlier part puts the change on the end (e.g. If renaming "Namespace1" in
                    // Root.Namespace1.SubNamespace it becomes Root.Namespace1.NewNamespace rather than
                    // Root.NewNamespace.SubNamespace).  Not sure how to fix that yet.  As such, we'll limit it
                    // to only allowing fixing the misspelling on single namespaces or the last part of
                    // multi-part namespaces.  For others, it will show suggestions but won't allow a change
                    // and it will have to be fixed manually.
                    if(token.Parent?.Parent is NamespaceDeclarationSyntax singleNamespace)
                        symbol = semanticModel.GetDeclaredSymbol(singleNamespace, cancellationToken);
                    else
                    {
                        if(token.Parent?.Parent?.Parent is NamespaceDeclarationSyntax lastPart)
                            symbol = semanticModel.GetDeclaredSymbol(lastPart, cancellationToken);
                    }
                    break;

                default:
                    symbol = semanticModel.GetDeclaredSymbol(token.Parent, cancellationToken);

                    if(symbol == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Unhandled token parent type: " + token.Parent.GetType().FullName);
                        System.Diagnostics.Debugger.Break();
                    }
                    break;
            }

#if VS2017AND2019
            if(symbol != null)
            {
                // Produce a new solution that has all references to the identifier renamed, including the
                // declaration.
                var originalSolution = document.Project.Solution;
                var optionSet = originalSolution.Workspace.Options;
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, replacement,
                    optionSet, cancellationToken).ConfigureAwait(false);

                return newSolution;
            }
#else
            if(symbol != null)
            {
                // Produce a new solution that has all references to the identifier renamed, including the
                // declaration.  Only rename the file if it's a non-nested type.
                var renameOptions = new SymbolRenameOptions(true, true, true, renameFile);

                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol,
                    renameOptions, replacement, cancellationToken).ConfigureAwait(false);

                return newSolution;
            }
#endif
            return null;
        }
    }
}
