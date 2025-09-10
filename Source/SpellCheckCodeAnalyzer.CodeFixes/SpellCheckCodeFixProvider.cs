//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SpellCheckCodeFixProvider.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/01/2025
// Note    : Copyright 2023-2025, Eric Woodruff, All rights reserved
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

// Ignore Spelling: welldone

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;

using VisualStudio.SpellChecker.CodeAnalyzer;
using VisualStudio.SpellChecker.Common;

namespace VisualStudio.SpellChecker.CodeFixes
{
    // TODO: Separate code fixes for VB and F# are likely needed since they uses different SyntaxNode types among
    // other things.

    /// <summary>
    /// This is used to provide the spell check code fixes
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SpellCheckCodeFixProvider)), Shared]
    public class SpellCheckCodeFixProvider : CodeFixProvider
    {
        #region Private data members
        //=====================================================================

        private static readonly char[] commaSeparator = [','];
        //// private static readonly char[] pipeSeparator = ['|'];

        #endregion

        #region Property and method overrides
        //=====================================================================

        /// <inheritdoc />
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            [CSharpSpellCheckCodeAnalyzer.SpellingDiagnosticId];

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
                // Find the identifier for the diagnostic
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var syntaxToken = root.FindToken(diagnosticSpan.Start);

                HashSet<string> suggestions = null;
                List<GlobalDictionary> globalDictionaries = null;

                // If suggestions exist, they come from the code analysis dictionary settings and we won't
                // offer to add them to any dictionaries or ignored words files.
                if(!diagnostic.Properties.TryGetValue("Suggestions", out string suggestedReplacements))
                {
                    if(diagnostic.Properties.TryGetValue("Languages", out string languages) &&
                      diagnostic.Properties.TryGetValue("TextToCheck", out suggestedReplacements))
                    {
                        // Getting suggestions is expensive so it's done here when actually needed rather
                        // than in the code analyzer.  Dictionaries should exist at this point since the
                        // code analyzer will have created them.
                        globalDictionaries = [.. languages.Split(commaSeparator,
                        StringSplitOptions.RemoveEmptyEntries).Select(l =>
                            GlobalDictionary.CreateGlobalDictionary(new CultureInfo(l), null, [], false)).Where(
                                d => d != null).Distinct()];

                        if(globalDictionaries.Count != 0)
                        {
                            var dictionary = new SpellingDictionary(globalDictionaries, null);

                            suggestions = CheckSuggestions(dictionary.SuggestCorrections(suggestedReplacements).Select(
                                ss => ss.Suggestion));
                        }
                    }
                }
                else
                    suggestions = [.. suggestedReplacements.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries)];

                if(suggestedReplacements != null)
                {
                    List<CodeAction> replacements;
                    string word = root.GetText().GetSubText(diagnosticSpan).ToString();

                    if((suggestions?.Count ?? 0) != 0)
                    {
                        // If the misspelling is a sub-span, the prefix and suffix will contain the surrounding text
                        // used to create the full identifier.
                        _ = diagnostic.Properties.TryGetValue("Prefix", out string prefix);
                        _ = diagnostic.Properties.TryGetValue("Suffix", out string suffix);

                        replacements = [.. suggestions.Select(
                                s =>
                                {
                                    string replacement = (String.IsNullOrWhiteSpace(prefix) &&
                                        String.IsNullOrWhiteSpace(suffix)) ? s : prefix + s + suffix;

                                    return CodeAction.Create(replacement,
                                        c => CorrectSpellingAsync(context.Document, syntaxToken, replacement, c),
                                        replacement);

                                })];
                    }
                    else
                    {
                        replacements = [ CodeAction.Create(CodeFixResources.NoSuggestions,
                                c => CorrectSpellingAsync(context.Document, syntaxToken, null, c),
                                nameof(CodeFixResources.NoSuggestions)) ];
                    }

                    //// These options are currently not implemented because there is no way to modify a non-code
                    //// file within the context of a code action because they don't have a syntax tree that can be
                    //// modified and applied later if the user choses to execute the action.  You can't just write
                    //// to the file in the called method as it is called to get a preview of the changes and there's
                    //// no way to defer the actual update in those cases.
                    ////
                    //// if(globalDictionaries != null)
                    //// {
                    ////     foreach(var d in globalDictionaries)
                    ////     {
                    ////         replacements.Add(CodeAction.Create(
                    ////             String.Format(CodeFixResources.AddToDictionary, d.Culture.DisplayName),
                    ////             c => AddToDictionaryAsync(d, word), $"{d.Culture.Name}-{word}"));
                    ////     }
                    //// }
                    ////
                    //// if(diagnostic.Properties.TryGetValue("IgnoredWordsFiles", out string ignoredWordsFiles))
                    //// {
                    ////     List<string> ignoredWordsFileList = [.. ignoredWordsFiles.Split(pipeSeparator, StringSplitOptions.RemoveEmptyEntries)];
                    ////     var solution = context.Document.Project.Solution;
                    ////
                    ////     foreach(string f in ignoredWordsFileList)
                    ////     {
                    ////         // This is a pain, but to be usable, it has to be in at least one project file in the solution
                    ////         // with a build action of "Additional Files".
                    ////         var ids = solution.GetDocumentIdsWithFilePath(f);
                    ////
                    ////         if(ids.Length != 0)
                    ////         {
                    ////             replacements.Add(CodeAction.Create(String.Format(CodeFixResources.AddToIgnoredWordsFile, f),
                    ////                 c => AddToIgnoredWordsFileAsync(context.Document, ids[0], word), $"{f}-{word}"));
                    ////         }
                    ////     }
                    //// }

                    replacements.Add(CodeAction.Create(CodeFixResources.IgnoreWordInFile,
                        c => IgnoreWordAsync(context.Document, word, c), word));

                    // Register a code action that will invoke the fix and offer the various suggested replacements
                    context.RegisterCodeFix(CodeAction.Create(diagnostic.Descriptor.MessageFormat.ToString(null),
                        [.. replacements], false), diagnostic);
                }
            }
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// This is used to filter and adjust the suggestions used to fix a misspelling in an identifier
        /// </summary>
        /// <param name="suggestions">The suggestions that should replace the misspelling</param>
        /// <returns>An enumerable list of valid suggestions, if any.</returns>
        /// <remarks>Some suggestions include spaces or punctuation.  Those are altered to remove the punctuation
        /// and return the suggestion in camel case.</remarks>
        private static HashSet<string> CheckSuggestions(IEnumerable<string> suggestions)
        {
            var validSuggestions = new HashSet<string>();

            foreach(string s in suggestions)
            {
                var wordChars = s.ToArray();

                if(wordChars.All(c => Char.IsLetter(c)))
                    validSuggestions.Add(s);
                else
                {
                    // Certain misspellings may return suggestions with spaces or punctuation.  For example:
                    // welldone suggests "well done" and "well-done".  Return those as a camel case suggestion:
                    // wellDone.
                    bool caseChanged = false;

                    for(int idx = 0; idx < wordChars.Length; idx++)
                    {
                        if(!Char.IsLetter(wordChars[idx]))
                        {
                            while(idx < wordChars.Length && !Char.IsLetter(wordChars[idx]))
                                idx++;

                            if(idx < wordChars.Length)
                            {
                                wordChars[idx] = Char.ToUpperInvariant(wordChars[idx]);
                                caseChanged = true;
                            }
                        }
                    }

                    if(caseChanged)
                        validSuggestions.Add(new String([.. wordChars.Where(c => Char.IsLetter(c))]));
                }
            }

            return validSuggestions;
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

                case FileScopedNamespaceDeclarationSyntax fsnds:
                    symbol = semanticModel.GetDeclaredSymbol(fsnds, cancellationToken);
                    break;

                case BaseTypeDeclarationSyntax btds:
                    symbol = semanticModel.GetDeclaredSymbol(btds, cancellationToken);

                    // Rename the file only if the type is not nested within another type and the filename
                    // matches the token text.  GitHub issue #304.
                    renameFile = token.Parent?.Parent is not ClassDeclarationSyntax &&
                        Path.GetFileNameWithoutExtension(token.SyntaxTree.FilePath ?? String.Empty).Equals(
                            token.Text, StringComparison.OrdinalIgnoreCase);
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
                    // Renaming namespace parts needs some work.  It's fine for the last part but renaming
                    // an earlier part puts the change on the end (e.g. If renaming "OldNamespace" in
                    // Root.OldNamespace.SubNamespace it becomes Root.OldNamespace.NewNamespace rather than
                    // Root.NewNamespace.SubNamespace).  I'm not sure if this is a limitation of the code fix
                    // API or my limited understanding of how they work.  As such, for now we'll limit it to
                    // only allowing fixing the misspelling on single namespaces or the last part of multi-part
                    // namespaces.  For others, it will show suggestions but won't allow a change and it will
                    // have to be fixed manually.
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

            if(symbol != null)
            {
                // Produce a new solution that has all references to the identifier renamed, including the
                // declaration.  Only rename the file if it's a non-nested type.
                var renameOptions = new SymbolRenameOptions(true, true, true, renameFile);

                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol,
                    renameOptions, replacement, cancellationToken).ConfigureAwait(false);

                return newSolution;
            }

            return null;
        }

        /// <summary>
        /// Create the solution used to ignore a misspelled word
        /// </summary>
        /// <param name="document">The document containing the misspelling</param>
        /// <param name="ignoredWord">The ignored word syntax token</param>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>The solution task used to add the ignored word to the Ignore Spelling directive</returns>
        private static async Task<Document> IgnoreWordAsync(Document document, string ignoredWord,
          CancellationToken cancellationToken)
        {
            if(document == null || ignoredWord == null)
                return null;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // TODO: This will vary by language
            string newLineText = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine,
                LanguageNames.CSharp);
            char commentChar = '/';

            var newLineTrivia = SyntaxFactory.EndOfLine(newLineText);
            var newLeadingTrivia = SyntaxFactory.ParseLeadingTrivia(String.Empty);

            // We'll try to insert the directive after any leading comments and whitespace so add any existing
            // leading trivia around the directive comment.
            SyntaxTriviaList leadingTrivia = root.GetLeadingTrivia();
            string existingComment = null;
            int triviaIdx = 0, existingCommentIdx = -1, lineCount = 0, blankLineCount = 0;
            bool done = false;

            while(triviaIdx < leadingTrivia.Count && !done)
            {
                switch(leadingTrivia[triviaIdx].Kind())
                {
                    case SyntaxKind.EndOfLineTrivia:
                        triviaIdx++;
                        lineCount++;
                        break;

                    case SyntaxKind.WhitespaceTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        triviaIdx++;
                        lineCount = 0;
                        break;

                    case SyntaxKind.SingleLineCommentTrivia:
                        existingComment = leadingTrivia[triviaIdx].ToString();
                        blankLineCount = lineCount;

                        int skipIdx = 0;

                        while(skipIdx < existingComment.Length && (existingComment[skipIdx] == commentChar ||
                          Char.IsWhiteSpace(existingComment[skipIdx])))
                        {
                            skipIdx++;
                        }

                        if(skipIdx < existingComment.Length && existingComment.Substring(skipIdx).StartsWith(
                          "Ignore Spelling:", StringComparison.OrdinalIgnoreCase))
                        {
                            existingCommentIdx = triviaIdx;
                            triviaIdx--;
                            done = true;
                        }
                        else
                            triviaIdx++;
                        break;

                    default:
                        triviaIdx--;
                        done = true;
                        break;
                }
            }

            if(triviaIdx > 0)
            {
                newLeadingTrivia = newLeadingTrivia.AddRange(leadingTrivia.Take(triviaIdx + 1));

                if(blankLineCount == 0)
                    newLeadingTrivia = newLeadingTrivia.Add(newLineTrivia);
            }

            if(existingCommentIdx != -1)
            {
                // No need for a new line here as there should already be new line trivia after it
                newLeadingTrivia = newLeadingTrivia.AddRange(SyntaxFactory.ParseLeadingTrivia(
                    existingComment + " " + ignoredWord));
                triviaIdx++;
            }
            else
            {
                newLeadingTrivia = newLeadingTrivia.AddRange(SyntaxFactory.ParseLeadingTrivia(
                    $"// Ignore Spelling: {ignoredWord}{newLineText}"));

                if(triviaIdx + 1 >= leadingTrivia.Count || !leadingTrivia[triviaIdx + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                    newLeadingTrivia = newLeadingTrivia.Add(newLineTrivia);
            }

            if(triviaIdx < leadingTrivia.Count)
                newLeadingTrivia = newLeadingTrivia.AddRange(leadingTrivia.Skip(triviaIdx + 1));

            return document.WithSyntaxRoot(root.WithLeadingTrivia(newLeadingTrivia));
        }

        //// Not implemented yet.  See above for the reason why.
        ////
        //// /// <summary>
        //// /// Add an ignored word to an ignored words file
        //// /// </summary>
        //// /// <param name="document">The document containing the ignored word</param>
        //// /// <param name="ignoredWordsDocId">The ignored words file document ID</param>
        //// /// <param name="ignoredWordsFile">The ignored words file to which the word is added</param>
        //// /// <param name="word">The ignored word</param>
        //// /// <returns>Always returns null as we update the ignored words file directly</returns>
        //// private static Task<Solution> AddToIgnoredWordsFileAsync(Document document string ignoredWordsDocId, string word)
        //// {
        ////     /* This sort of works but it removes the file from the project and adds it back which removes
        ////        the "Additional Document" build action so it no longer sees the file to allow adding other
        ////        words.  It also modifies the project which is unnecessary.
        ////     var solution = document.Project.Solution;
        ////     var ignoredWordsDoc = solution.GetAdditionalDocument(ignoredWordsDocId);
        ////
        ////     if(ignoredWordsDoc != null)
        ////     {
        ////         var project = ignoredWordsDoc.Project;
        ////         var text = await ignoredWordsDoc.GetTextAsync().ConfigureAwait(false);
        ////
        ////         // Not the best way but we're just testing for now
        ////         text = text.Replace(0, 0, word + Environment.NewLine);
        ////
        ////         solution = solution.RemoveAdditionalDocument(ignoredWordsDoc.Id);
        ////         solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id), ignoredWordsDoc.Name, text);
        ////     }
        ////
        ////     return solution;*/
        ////
        ////     return Task.FromResult<Solution>(null);
        //// }
        ////
        //// /// <summary>
        //// /// Add a word to a user dictionary
        //// /// </summary>
        //// /// <param name="dictionary">The dictionary to which the word will be added</param>
        //// /// <param name="word">The word to add</param>
        //// /// <returns>Always returns null as we update the user dictionary file directly</returns>
        //// private static Task<Document> AddToDictionaryAsync(GlobalDictionary dictionary, string word)
        //// {
        ////     return Task.FromResult<Document>(null);
        //// }
        #endregion
    }
}
