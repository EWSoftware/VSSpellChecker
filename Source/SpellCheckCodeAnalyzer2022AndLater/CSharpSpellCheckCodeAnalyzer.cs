//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CSharpSpellCheckCodeAnalyzer.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/02/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to implement the C# spell check code analyzer
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/26/2023  EFW  Created the code
//===============================================================================================================

// Ignore Spelling: welldone

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#if VS2017AND2019
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#endif

using VisualStudio.SpellChecker.Common;
using VisualStudio.SpellChecker.Common.Configuration;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    // TODO: Separate analyzers for VB and F# are needed since they uses different SyntaxKind values among other
    // things.  May be able to use this as the common analyzer and create the SpellCheckHandler type based on
    // the language in the AnalyzeSyntaxTree method.  May need to rework the code to account for these
    // differences if things like the SyntaxKind values differ between language services.

    /// <summary>
    /// This is used to implement the C# spell check code analyzer
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpSpellCheckCodeAnalyzer : DiagnosticAnalyzer
    {
        #region Private data members and constants
        //=====================================================================

        /// <summary>
        /// This constant represents the diagnostic ID for spelling errors
        /// </summary>
        public const string SpellingDiagnosticId = "VSSpell001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be
        // localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on
        // localization.
        private static readonly LocalizableString SpellingTitle = new LocalizableResourceString(nameof(Resources.SpellingAnalyzerTitle),
            Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString SpellingMessageFormat = new LocalizableResourceString(
            nameof(Resources.SpellingAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString SpellingDescription = new LocalizableResourceString(
            nameof(Resources.SpellingAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string SpellingCategory = "Naming";

        // TODO: Provide help link URL?
        private static readonly DiagnosticDescriptor SpellingRule = new DiagnosticDescriptor(SpellingDiagnosticId,
            SpellingTitle, SpellingMessageFormat, SpellingCategory, DiagnosticSeverity.Warning, true,
            SpellingDescription);

        private static readonly Dictionary<string, Assembly> referenceAssemblies = new Dictionary<string, Assembly>();

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Static constructor
        /// </summary>
        /// <remarks>When running inside a code analyzer, it fails to find the reference assemblies even though
        /// they are in the same folder as this assembly.  This works around the issue by providing an assembly
        /// resolver to load them.</remarks>
        static CSharpSpellCheckCodeAnalyzer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                Assembly refAsm = null;

                // We do see our own assembly name come through here so we'll ignore it
                if(!String.IsNullOrWhiteSpace(e.Name) &&
                  !e.Name.StartsWith(Assembly.GetExecutingAssembly().GetName().Name, StringComparison.OrdinalIgnoreCase) &&
                  !referenceAssemblies.TryGetValue(e.Name, out refAsm))
                {
                    int pos = e.Name.IndexOf(',');

                    if(pos != -1)
                    {
                        string assemblyName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                            e.Name.Substring(0, pos) + ".dll");

                        // If not found it's probably looking for a resources assembly that doesn't exist so
                        // we'll ignore it.
                        if(File.Exists(assemblyName))
                            refAsm = Assembly.LoadFile(assemblyName);
                    }

                    referenceAssemblies[e.Name] = refAsm;
                }

                return refAsm;
            };
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SpellingRule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            if(context != null)
            {
                // Allow this to run and report diagnostics in compiler generated code.  This is useful for spell
                // checking property names etc. generated from things like app.config settings.  There's an option
                // to turn it off in generated code if not wanted.
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                    GeneratedCodeAnalysisFlags.ReportDiagnostics);

                context.EnableConcurrentExecution();

                context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            }
        }

        /// <summary>
        /// This is used to handle the spell checking process
        /// </summary>
        /// <param name="context">The syntax tree analysis context</param>
        public static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            if(!context.Tree.TryGetRoot(out var root))
                return;

            // Generate a configuration from the options in .globalconfig and .editorconfig files
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
            var properties = new List<(string PropertyName, string value)>();

#if VS2017AND2019
            // The older context options instance doesn't have a Keys property so we have to resort to reflection
            // to get the backing dictionary.
            var fi = options.GetType().GetField("_backing", BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public);

            if(fi != null && fi.GetValue(options) is IDictionary<string, string> privateOptions)
            {
                foreach(string propertyName in privateOptions.Keys.Where(
                  k => k.StartsWith(SpellCheckerConfiguration.PropertyPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    if(options.TryGetValue(propertyName, out var value))
                        properties.Add((propertyName, value.UnescapeEditorConfigValue()));
                }
            }
#else
            // The options collection doesn't have an enumerator or an indexer so we have to do it this way
            foreach(string propertyName in options.Keys.Where(
              k => k.StartsWith(SpellCheckerConfiguration.PropertyPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                if(options.TryGetValue(propertyName, out var value))
                    properties.Add((propertyName, value.UnescapeEditorConfigValue()));
            }
#endif
            var configuration = SpellCheckerConfiguration.CreateSpellCheckerConfigurationFor(
                context.Tree.FilePath, properties);

#if VS2017AND2019
            // The older version doesn't have a generated code indicator on the context
            var classDecl = root.DescendantNodes(n => true, false).FirstOrDefault(n => n.IsKind(SyntaxKind.ClassDeclaration));
            bool isCompilerGenerated = false;

            if(classDecl != null)
            {
                foreach(var attr in classDecl.DescendantNodes(n => true, false).Where(n => n.IsKind(SyntaxKind.AttributeList)))
                {
                    if(((AttributeListSyntax)attr).Attributes.Any(a => a.Name.ToString().IndexOf(
                      "CompilerGeneratedAttribute", StringComparison.Ordinal) != -1))
                    {
                        isCompilerGenerated = true;
                        break;
                    }
                }
            }

            if(configuration.EnableCodeAnalyzers && 
              (!isCompilerGenerated || !configuration.CodeAnalyzerOptions.IgnoreIfCompilerGenerated))
#else
            if(configuration.EnableCodeAnalyzers && 
              (!context.IsGeneratedCode || !configuration.CodeAnalyzerOptions.IgnoreIfCompilerGenerated))
#endif
            {
                // Code analysis dictionaries can be passed using the AdditionalFileItemNames project item:
                // https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Using%20Additional%20Files.md
                //
                // <PropertyGroup>
                //   <!-- Update the property to include all code analysis dictionary files -->
                //   <AdditionalFileItemNames>$(AdditionalFileItemNames);CodeAnalysisDictionary</AdditionalFileItemNames>
                // </PropertyGroup>
                //
                foreach(var addFile in context.Options.AdditionalFiles.Where(f => f.Path.EndsWith(".xml",
                    StringComparison.OrdinalIgnoreCase)))
                {
                    configuration.ImportCodeAnalysisDictionary(addFile.Path);
                }

                // Create a dictionary for each configuration dictionary language ignoring any that are
                // invalid and duplicates caused by missing languages which return the en-US dictionary.
                // Dictionaries are initialize synchronously in the code analyzer.
                var globalDictionaries = configuration.DictionaryLanguages.Select(l =>
                    GlobalDictionary.CreateGlobalDictionary(l, configuration.AdditionalDictionaryFolders,
                    configuration.RecognizedWords, false)).Where(d => d != null).Distinct().ToList();

                context.CancellationToken.ThrowIfCancellationRequested();

                if(globalDictionaries.Any())
                {
                    var dictionary = new SpellingDictionary(globalDictionaries, configuration.IgnoredWords);
                    var handler = new CSharpSpellCheckHandler(configuration);

                    handler.WordSplitter.IsCStyleCode = true;

                    handler.Recurse(root, context.CancellationToken);
                        
                    var ignoreSpellingWords = handler.FindIgnoreSpellingDirectives();
                    var rangeExclusions = new List<Match>();
                    var diagnosticProperties = new Dictionary<string, string>
                    {
                        { "Suggestions", null },
                        { "Prefix", null },
                        { "Suffix", null }
                    };

                    // TODO: For the first release, just handle identifiers and leave the rest to the existing
                    // tagger which already handles all the other elements.  This would let me get this out
                    // quicker and figure out how to handle the other stuff later if at all with an analyzer.
                    foreach(var s in handler.Spans.Where(s => s.SpanType == SpellCheckType.Identifier))
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        // Add exclusions from the configuration if any
                        foreach(var exclude in configuration.ExclusionExpressions)
                        {
                            try
                            {
                                rangeExclusions.AddRange(exclude.Matches(s.Text).Cast<Match>());
                            }
                            catch(RegexMatchTimeoutException ex)
                            {
                                // Ignore expression timeouts
                                Debug.WriteLine(ex);
                            }
                        }

                        foreach(var word in handler.IdentifierSplitter.GetWordsInIdentifier(s.Text))
                        {
                            string textToCheck = s.Text.Substring(word.Start, word.Length);

                            if(ignoreSpellingWords.Any(i => i.IgnoredWord.Equals(textToCheck,
                                i.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            if(rangeExclusions.Count == 0 || !rangeExclusions.Any(
                                match => word.Start >= match.Index && word.Start <= match.Index + match.Length - 1))
                            {
                                // We only check for ignored words here.  There is no support for Ignore Once
                                // and we don't bother with doubled word checking.
                                if(configuration.ShouldIgnoreWord(textToCheck) || dictionary.ShouldIgnoreWord(textToCheck))
                                    continue;

                                // Handle code analysis dictionary checks first as they may be not be
                                // recognized as correctly spelled words but have alternate handling.
                                if(configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                                    configuration.DeprecatedTerms.TryGetValue(textToCheck, out string preferredTerm))
                                {
                                    diagnosticProperties["Prefix"] = diagnosticProperties["Suffix"] = null;
                                    diagnosticProperties["Suggestions"] = preferredTerm;

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                if(configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                                    configuration.CompoundTerms.TryGetValue(textToCheck, out preferredTerm))
                                {
                                    diagnosticProperties["Prefix"] = diagnosticProperties["Suffix"] = null;
                                    diagnosticProperties["Suggestions"] = preferredTerm;

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                if(configuration.CadOptions.TreatUnrecognizedWordsAsMisspelled &&
                                    configuration.UnrecognizedWords.TryGetValue(textToCheck, out IList<string> spellingAlternates))
                                {
                                    diagnosticProperties["Prefix"] = diagnosticProperties["Suffix"] = null;
                                    diagnosticProperties["Suggestions"] = String.Join(",", spellingAlternates);

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                if(!dictionary.IsSpelledCorrectly(textToCheck))
                                {
                                    // TODO: Ignore the dictionary the suggestions come from for now.
                                    var (skipMisspelling, suggestions) = CheckSuggestions(textToCheck,
                                        dictionary.SuggestCorrections(textToCheck).Select(ss => ss.Suggestion));

                                    if(!skipMisspelling)
                                    {
                                        if(s.Text.Length != word.Length)
                                        {
                                            diagnosticProperties["Prefix"] = s.Text.Substring(0, word.Start);
                                            diagnosticProperties["Suffix"] = s.Text.Substring(word.Start + word.Length);
                                        }
                                        else
                                            diagnosticProperties["Prefix"] = diagnosticProperties["Suffix"] = null;

                                        diagnosticProperties["Suggestions"] = String.Join(",", suggestions);

                                        context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                            Location.Create(context.Tree, TextSpan.FromBounds(
                                            s.TextSpan.Start + word.Start, s.TextSpan.Start + word.Start + word.Length)),
                                            diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is used to filter and adjust the suggestions used to fix a misspelling in an identifier
        /// </summary>
        /// <param name="misspelling">The misspelling</param>
        /// <param name="suggestions">The suggestions that should replace the misspelling</param>
        /// <returns>A flag indicating whether or not the misspelling should be skipped and an enumerable list of
        /// valid suggestions, if any.</returns>
        /// <remarks>Some suggestions include spaces or punctuation.  Others only differ from the misspelling in
        /// casing.  Misspellings in which there is a suggestion that only differs in case are ignored.  Those
        /// with spaces or punctuation are altered to remove the punctuation and return the suggestion in camel
        /// case.</remarks>
        private static (bool SkipMisspelling, IEnumerable<string> Suggestions) CheckSuggestions(string misspelling,
          IEnumerable<string> suggestions)
        {
            var validSuggestions = new HashSet<string>();
            bool skipMisspelling = false;

            foreach(string s in suggestions)
            {
                var wordChars = s.ToArray();

                if(wordChars.All(c => Char.IsLetter(c)))
                {
                    if(s.Equals(misspelling, StringComparison.OrdinalIgnoreCase))
                    {
                        skipMisspelling = true;
                        break;
                    }

                    validSuggestions.Add(s);
                }
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
                        validSuggestions.Add(new String(wordChars.Where(c => Char.IsLetter(c)).ToArray()));
                }
            }

            return (skipMisspelling, validSuggestions);
        }
        #endregion
    }
}
