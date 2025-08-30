//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckHandler.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/20/2025
// Note    : Copyright 2023-2025, Eric Woodruff, All rights reserved
//
// This file contains the abstract base class used to perform the spell checking process on the code syntax
// elements.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/25/2023  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    /// <summary>
    /// This abstract class is used to perform the spell checking process on the code syntax elements
    /// </summary>
    internal abstract class SpellCheckHandler
    {
        #region Private data members
        //=====================================================================

        private readonly List<SpellCheckSpan> spans;
        private readonly SpellCheckerConfiguration configuration;

        #endregion

        #region Properties
        //=====================================================================

        public IEnumerable<SpellCheckSpan> Spans => spans;

        /// <summary>
        /// This is used to get the identifier prefix character used by the language when using keywords as
        /// variable names (e.g. C# @foreach, VB [case], F# ``let``).
        /// </summary>
        public abstract char IdentifierKeywordPrefixCharacter { get; }

        /// <summary>
        /// This is used to define the starting delimited comment characters for the language (e.g. "/*" for C#).
        /// </summary>
        /// <remarks>This can be null if the language does not support this type of comment</remarks>
        public abstract string DelimitedCommentCharacters { get; }

        /// <summary>
        /// This is used to define the quad-slash comment characters for the language (e.g. //// for C#,
        /// '''' for VB).
        /// </summary>
        public abstract string QuadSlashCommentCharacters { get; }

        /// <summary>
        /// This read-only property is used to get the word splitter for comments and strings
        /// </summary>
        public CodeAnalyzerWordSplitter WordSplitter { get; }

        /// <summary>
        /// This read-only property is used to get the word splitter for identifiers
        /// </summary>
        public CodeAnalyzerIdentifierSplitter IdentifierSplitter { get; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The spell checker configuration to use</param>
        public SpellCheckHandler(SpellCheckerConfiguration configuration)
        {
            spans = [];

            this.configuration = configuration;
            this.WordSplitter = new CodeAnalyzerWordSplitter { Configuration = configuration };
            this.IdentifierSplitter = new CodeAnalyzerIdentifierSplitter { Configuration = configuration };
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// Recurse the syntax tree to find elements that need to be spell checked
        /// </summary>
        /// <param name="root">The syntax root node</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public void Recurse(SyntaxNode root, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach(var child in root.ChildNodesAndTokens())
            {
                if(child.IsNode)
                    this.Recurse(child.AsNode(), cancellationToken);
                else
                {
                    SpellCheckType subType;

                    var token = child.AsToken();

                    this.ProcessTriviaList(token.LeadingTrivia, cancellationToken);

                    if(token.Span.Length > 0)
                    {
                        switch((SyntaxKind)token.RawKind)
                        {
                            case SyntaxKind.StringLiteralToken:
                                string literal = token.Text;

                                if(literal[0] != '@' && !configuration.CodeAnalyzerOptions.IgnoreNormalStrings)
                                {
                                    spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.StringLiteral,
                                        SpellCheckType.NormalString, literal));
                                }
                                else
                                {
                                    if(literal[0] == '@' && !configuration.CodeAnalyzerOptions.IgnoreVerbatimStrings)
                                    {
                                        spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.StringLiteral,
                                            SpellCheckType.VerbatimString, literal));
                                    }
                                }
                                break;

                            case SyntaxKind.InterpolatedStringText:
                            case SyntaxKind.InterpolatedStringTextToken:
                                subType = SpellCheckType.InterpolatedString;
                                bool ignored = configuration.CodeAnalyzerOptions.IgnoreInterpolatedStrings;

                                if(token.Parent?.Parent is InterpolatedStringExpressionSyntax ise)
                                {
                                    if(ise.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
                                    {
                                        subType |= SpellCheckType.VerbatimString;

                                        if(!ignored)
                                            ignored = configuration.CodeAnalyzerOptions.IgnoreVerbatimStrings;
                                    }
                                    else
                                    {
                                        if(ise.StringStartToken.IsKind(SyntaxKind.InterpolatedSingleLineRawStringStartToken))
                                        {
                                            subType |= SpellCheckType.RawString;

                                            if(!ignored)
                                                ignored = configuration.CodeAnalyzerOptions.IgnoreRawStrings;
                                        }
                                        else
                                        {
                                            if(ise.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
                                            {
                                                subType |= SpellCheckType.RawString;

                                                if(!ignored)
                                                    ignored = configuration.CodeAnalyzerOptions.IgnoreRawStrings;
                                            }
                                        }
                                    }
                                }

                                if(!ignored)
                                {
                                    spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.StringLiteral, subType,
                                        token.Text));
                                }
                                break;

                            case SyntaxKind.SingleLineRawStringLiteralToken:
                            case SyntaxKind.MultiLineRawStringLiteralToken:
                                if(!configuration.CodeAnalyzerOptions.IgnoreRawStrings)
                                {
                                    spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.StringLiteral,
                                        SpellCheckType.RawString, token.Text));
                                }
                                break;

                            case SyntaxKind.IdentifierToken:
                                SpellCheckType spanType = this.DetermineIdentifierSpellCheckType(token);

                                if(spanType != SpellCheckType.None)
                                {
                                    if(spanType != SpellCheckType.TypeParameter)
                                        spans.Add(new SpellCheckSpan(token.Span, spanType, token.Text));
                                    else
                                    {
                                        // Type parameter is a subtype of identifier
                                        spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.Identifier,
                                            spanType, token.Text));
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }

                    this.ProcessTriviaList(token.TrailingTrivia, cancellationToken);
                }
            }
        }

        /// <summary>
        /// This searches for comments spans containing Ignore Spelling directives and tracks the ignored words
        /// </summary>
        public IEnumerable<(string IgnoredWord, bool CaseSensitive)> FindIgnoreSpellingDirectives()
        {
            var ignoreSpellingWords = new List<(string IgnoredWord, bool CaseSensitive)>();

            foreach(var s in spans.Where(s => s.SpanType == SpellCheckType.Comment))
            {
                foreach(Match m in CommonUtilities.IgnoreSpellingDirectiveRegex.Matches(s.Text))
                {
                    string ignored = m.Groups["IgnoredWords"].Value;
                    bool caseSensitive = !String.IsNullOrWhiteSpace(m.Groups["CaseSensitive"].Value);
                    int start = m.Groups["IgnoredWords"].Index;

                    foreach(var ignoreSpan in this.WordSplitter.GetWordsInText(ignored))
                    {
                        string ignoredWord = s.Text.Substring(start + ignoreSpan.Start, ignoreSpan.Length);
                        var match = ignoreSpellingWords.FirstOrDefault(w => w.IgnoredWord.Equals(ignoredWord,
                            w.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

                        if(!String.IsNullOrWhiteSpace(match.IgnoredWord))
                        {
                            // If the span is already there, ignore it
                            if(match.IgnoredWord == ignoredWord && match.CaseSensitive == caseSensitive)
                                continue;

                            // If different, replace it
                            ignoreSpellingWords.Remove(match);
                        }

                        ignoreSpellingWords.Add((ignoredWord, caseSensitive));
                    }
                }
            }

            return ignoreSpellingWords;
        }

        /// <summary>
        /// This is used to determine the identifier spell check type and whether or not it should be spell
        /// checked.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns>A <see cref="SpellCheckType"/> other than <c>None</c> if the identifier should be spell
        /// checked or <c>None</c> if it should not.</returns>
        private SpellCheckType DetermineIdentifierSpellCheckType(SyntaxToken token)
        {
            // Anything less than three characters is ignored as it's less likely to be something worth spell
            // checking and would only create clutter.
            string identifier = token.Text;

            if(identifier.Length < CommonUtilities.MinimumIdentifierLength)
                return SpellCheckType.None;

            if(token.Parent is VariableDeclaratorSyntax vds)
            {
                // Ignore variable names that match language keywords since these are more likely to cause
                // false reports and the user has already made a decision to explicitly escape them
                if(identifier.Length != 0 && identifier[0] == this.IdentifierKeywordPrefixCharacter)
                    return SpellCheckType.None;

                var varDec = vds.Parent as VariableDeclarationSyntax;
                SyntaxTokenList? modifiers = null;

                if(varDec.Parent is BaseFieldDeclarationSyntax baseField)
                    modifiers = baseField.Modifiers;
                else
                {
                    if(varDec.Parent is LocalDeclarationStatementSyntax localDecl)
                        modifiers = localDecl.Modifiers;
                }

                return this.HasWantedVisibility(token.Parent.FirstAncestorOrSelf<BlockSyntax>() != null,
                    modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;
            }

            if(token.Parent is IdentifierNameSyntax ins)
            {
                var parent = ins.Parent;

                while(parent is QualifiedNameSyntax)
                    parent = parent.Parent;

                return (parent is NamespaceDeclarationSyntax) ? SpellCheckType.Identifier : SpellCheckType.None;
            }

            if(token.Parent is BaseTypeDeclarationSyntax td)
                return this.HasWantedVisibility(false, td.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is TypeParameterSyntax tp && !configuration.CodeAnalyzerOptions.IgnoreTypeParameters)
            {
                SyntaxTokenList? modifiers = null;

                if(tp.Parent?.Parent is TypeDeclarationSyntax typeDec)
                    modifiers = typeDec.Modifiers;
                else
                {
                    if(tp.Parent?.Parent is MemberDeclarationSyntax memberDec)
                        modifiers = memberDec.Modifiers;
                }

                return this.HasWantedVisibility(false, modifiers) ? SpellCheckType.TypeParameter : SpellCheckType.None;
            }

            if(token.Parent is DelegateDeclarationSyntax delegateDec)
                return this.HasWantedVisibility(false, delegateDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is MethodDeclarationSyntax methodDec)
                return this.HasWantedVisibility(false, methodDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is ConstructorDeclarationSyntax ctorDec)
                return this.HasWantedVisibility(false, ctorDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is DestructorDeclarationSyntax dtorDec)
                return this.HasWantedVisibility(false, dtorDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is PropertyDeclarationSyntax propDec)
                return this.HasWantedVisibility(false, propDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is EventDeclarationSyntax eventDec)
                return this.HasWantedVisibility(false, eventDec.Modifiers) ? SpellCheckType.Identifier : SpellCheckType.None;

            if(token.Parent is ParameterSyntax parameterSyntax)
            {
                // Only check parameters on methods with wanted visibility
                if(parameterSyntax.Parent is ParameterListSyntax parameterList &&
                  parameterList.Parent is BaseMethodDeclarationSyntax parameterMethod)
                {
                    return this.HasWantedVisibility(false, parameterMethod.Modifiers) ? SpellCheckType.Identifier :
                        SpellCheckType.None;
                }

                // For stuff like lambda expressions and local functions, only include them if spell checking
                // member body identifiers.
                return configuration.CodeAnalyzerOptions.IgnoreIdentifiersWithinMemberBodies ? SpellCheckType.None :
                    SpellCheckType.Identifier;
            }

            if(token.Parent is EnumMemberDeclarationSyntax enumMemberDeclaration)
            {
                var enumDecl = enumMemberDeclaration.Parent as EnumMemberDeclarationSyntax;

                return this.HasWantedVisibility(false, enumDecl?.Modifiers) ? SpellCheckType.Identifier :
                    SpellCheckType.None;
            }

            if(token.Parent is LocalFunctionStatementSyntax || token.Parent is SingleVariableDesignationSyntax ||
              token.Parent is ForEachStatementSyntax || token.Parent is CatchDeclarationSyntax ||
              token.Parent is LabeledStatementSyntax)
            {
                // Only include these if spell checking member body identifiers
                return configuration.CodeAnalyzerOptions.IgnoreIdentifiersWithinMemberBodies ? SpellCheckType.None :
                    SpellCheckType.Identifier;
            }

            if(token.Parent is ExternAliasDirectiveSyntax)
            {
                // Treat these as private and only include them if private members are wanted
                return configuration.CodeAnalyzerOptions.IgnoreIdentifierIfPrivate ? SpellCheckType.None :
                    SpellCheckType.Identifier;
            }

            return SpellCheckType.None;
        }

        /// <summary>
        /// This is used to determine whether or not an identifier has a visibility that is to be included for
        /// spell checking.
        /// </summary>
        /// <param name="isInMemberBody">True if within a member body, false if not</param>
        /// <param name="modifiers">The visibility modifiers to use in determining whether or not to spell check
        /// the identifier.</param>
        /// <returns>True if it should be spell checked, false if not</returns>
        private bool HasWantedVisibility(bool isInMemberBody, SyntaxTokenList? modifiers)
        {
            if(isInMemberBody && configuration.CodeAnalyzerOptions.IgnoreIdentifiersWithinMemberBodies)
                return false;

            // If there are no modifiers and it's not within a member body, it's something like a type's field
            // member and we'll treat it as private.
            if((modifiers == null || modifiers.Value.Count == 0) && !isInMemberBody)
                return !configuration.CodeAnalyzerOptions.IgnoreIdentifierIfPrivate;

            // If it's within a member body without modifiers, its a local variable and the body member check
            // passed above so we'll keep it.  Otherwise, we check the visibility options.
            if(modifiers != null && modifiers.Value.Count != 0)
            {
                // If lacking any visibility keyword, assume private and treat it as such.  This can happen when
                // an identifier is declared with something like const but no visibility keyword.
                if(configuration.CodeAnalyzerOptions.IgnoreIdentifierIfPrivate &&
                  (modifiers.Value.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                  (!modifiers.Value.Any(m => m.IsKind(SyntaxKind.InternalKeyword)) &&
                  !modifiers.Value.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) &&
                  !modifiers.Value.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))))
                {
                    return false;
                }

                if(configuration.CodeAnalyzerOptions.IgnoreIdentifierIfInternal &&
                  modifiers.Value.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
                {
                    // If protected internal, we still want to keep it though since it's still visible to derived
                    // types outside the assembly.
                    return modifiers.Value.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword));
                }
            }

            return true;
        }

        /// <summary>
        /// This is used to process a trivia list to see if there are any comments that need to be spell checked
        /// </summary>
        /// <param name="triviaList">The trivia list to search</param>
        /// <param name="cancellationToken">The cancellation token</param>
        private void ProcessTriviaList(SyntaxTriviaList triviaList, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach(var trivia in triviaList)
            {
                // Roslyn includes SyntaxKind.ShebangDirectiveTrivia (#!) as a comment type but it's only used in
                // scripts to specify the command used to run them.  We're unlikely to see them and they probably
                // don't contain anything that you'd normally want spell checked so we'll ignore them here.
                if((trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                  trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)) && trivia.Span.Length > 0)
                {
                    SpellCheckType subType = SpellCheckType.SingleLineComment;
                    string commentText = trivia.ToFullString();

                    if(commentText.StartsWith(this.DelimitedCommentCharacters, StringComparison.Ordinal))
                        subType = SpellCheckType.DelimitedComment;
                    else
                    {
                        if(commentText.StartsWith(this.QuadSlashCommentCharacters, StringComparison.Ordinal))
                            subType = SpellCheckType.QuadSlashComment;
                    }

                    if((subType == SpellCheckType.DelimitedComment && !configuration.CodeAnalyzerOptions.IgnoreDelimitedComments) ||
                      (subType == SpellCheckType.SingleLineComment && !configuration.CodeAnalyzerOptions.IgnoreStandardSingleLineComments) ||
                      (subType == SpellCheckType.QuadSlashComment && !configuration.CodeAnalyzerOptions.IgnoreQuadrupleSlashComments))
                    {
                        spans.Add(new SpellCheckSpan(trivia.Span, SpellCheckType.Comment, subType, commentText));
                    }
                    else
                    {
                        // If ignored but it contains an Ignore Spelling directive, return it anyway as we need
                        // to know what words to ignore.
                        if(CommonUtilities.IgnoreSpellingDirectiveRegex.IsMatch(commentText))
                            spans.Add(new SpellCheckSpan(trivia.Span, SpellCheckType.Comment, subType, commentText));
                    }
                }
                else
                {
                    if((trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                      trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)) &&
                      !configuration.CodeAnalyzerOptions.IgnoreXmlDocComments)
                    {
                        this.ProcessDocComment(trivia.GetStructure(), cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// This is used to handle a syntax node to see if there are any comments that need to be spell checked
        /// </summary>
        /// <param name="node">The node to search</param>
        /// <param name="cancellationToken">The cancellation token</param>
        private void ProcessDocComment(SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach(var child in node.ChildNodesAndTokens())
            {
                if(child.IsNode)
                    this.ProcessDocComment(child.AsNode(), cancellationToken);
                else
                {
                    var token = child.AsToken();

                    if(token.Span.Length > 0 && token.IsKind(SyntaxKind.XmlTextLiteralToken))
                    {
                        if(token.Parent is XmlTextAttributeSyntax attr)
                        {
                            if(configuration.SpellCheckedXmlAttributes.Contains(attr.Name.LocalName.Text))
                            {
                                spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.AttributeValue,
                                    token.Text));
                            }
                        }
                        else
                        {
                            if(token.Parent?.Parent is not XmlElementSyntax parentElement ||
                              !configuration.IgnoredXmlElements.Contains(parentElement.StartTag.Name.LocalName.Text))
                            {
                                spans.Add(new SpellCheckSpan(token.Span, SpellCheckType.Comment,
                                    SpellCheckType.XmlDocComment, token.Text));
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
