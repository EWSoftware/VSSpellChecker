//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CSharpSpellCheckCodeAnalyzer.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/01/2025
// Note    : Copyright 2023-2025, Eric Woodruff, All rights reserved
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

        private static readonly Dictionary<string, Assembly> referenceAssemblies = [];

        /// <summary>
        /// This constant represents the diagnostic ID for spelling errors
        /// </summary>
        public const string SpellingDiagnosticId = "VSSpell001";

        private const string SpellingCategory = "Naming";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be
        // localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on
        // localization.
        private static readonly LocalizableString SpellingTitle = new LocalizableResourceString(
            nameof(Resources.SpellingAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString SpellingMessageFormat = new LocalizableResourceString(
            nameof(Resources.SpellingAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString SpellingDescription = new LocalizableResourceString(
            nameof(Resources.SpellingAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor SpellingRule = new(
            SpellingDiagnosticId, SpellingTitle, SpellingMessageFormat, SpellingCategory,
            DiagnosticSeverity.Warning, true, SpellingDescription,
            "https://ewsoftware.github.io/VSSpellChecker/html/a7120f4c-5191-4442-b366-c3e792060569.htm");

        // The path to our dependencies
        private static readonly string codeAnalyzerPath;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the global configuration file path
        /// </summary>
        /// <value>We need this to get the analyzer code path so that we can find our dependencies because
        /// Visual Studio makes a copy of this assembly in some other cache location without them.</value>
        public static string GlobalConfigurationFilePath
        {
            get
            {
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"EWSoftware\Visual Studio Spell Checker");

                if(!Directory.Exists(configPath))
                    Directory.CreateDirectory(configPath);

                return configPath;
            }
        }
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
            // This is a pain but for some reason starting in v17.12, Visual Studio started running the actual
            // code analysis in a separate copy of the assembly in a cache folder and we can no longer find our
            // dependencies.  The odd thing is that it loads a copy to initialize it from the expected location.
            // As such, I'm forced to store a copy of the real path in a location the other copy can get to.
            // I haven't been able to find a better way around this.  If you know of one, let me know.
            string analyzerCodePath = Path.Combine(GlobalConfigurationFilePath, "AnalyzerCodePath.txt");

            var asm = Assembly.GetExecutingAssembly();
            string dependencyPath = Path.GetDirectoryName(asm.Location),
                nameAndLocation = $"{asm.FullName}|{dependencyPath}", analyzerPath = null;

            if(File.Exists(analyzerCodePath))
            {
                // If running from the installed location, make sure the path is up to date.  If not, use it as is.
                if(File.Exists(Path.Combine(dependencyPath, "VisualStudio.SpellChecker.Common.dll")))
                {
                    analyzerPath = File.ReadAllLines(analyzerCodePath).FirstOrDefault(
                        l => l.Equals(nameAndLocation, StringComparison.Ordinal));
                }
                else
                    analyzerPath = File.ReadAllLines(analyzerCodePath).FirstOrDefault();
            }

            if(String.IsNullOrWhiteSpace(analyzerPath))
            {
                if(File.Exists(Path.Combine(dependencyPath, "VisualStudio.SpellChecker.Common.dll")))
                {
                    File.WriteAllText(analyzerCodePath, nameAndLocation);
                    codeAnalyzerPath = dependencyPath;
                }
            }
            else
            {
                int pos = analyzerPath.IndexOf('|');

                if(pos != -1)
                    codeAnalyzerPath = analyzerPath.Substring(pos + 1);
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                Assembly refAsm = null;

                // We do see our own assembly name come through here so we'll ignore it
                if(!String.IsNullOrWhiteSpace(e.Name) && !String.IsNullOrWhiteSpace(codeAnalyzerPath) &&
                  !e.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) &&
                  !e.Name.StartsWith(asm.GetName().Name, StringComparison.OrdinalIgnoreCase) &&
                  !referenceAssemblies.TryGetValue(e.Name, out refAsm))
                {
                    int pos = e.Name.IndexOf(',');

                    if(pos != -1)
                    {
                        string assemblyName = Path.Combine(codeAnalyzerPath, e.Name.Substring(0, pos) + ".dll");

                        // If not found it's probably looking for a resources assembly that doesn't exist so
                        // we'll ignore it.
                        if(File.Exists(assemblyName))
                        {
                            refAsm = Assembly.LoadFile(assemblyName);

                            if(refAsm.FullName != e.Name)
                                refAsm = null;
                        }
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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [SpellingRule];

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

            // The options collection doesn't have an enumerator or an indexer so we have to do it this way
            foreach(string propertyName in options.Keys.Where(
              k => k.StartsWith(SpellCheckerConfiguration.PropertyPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                if(options.TryGetValue(propertyName, out var value))
                    properties.Add((propertyName, value.UnescapeEditorConfigValue()));
            }

            var configuration = SpellCheckerConfiguration.CreateSpellCheckerConfigurationFor(
                context.Tree.FilePath, properties);

            if(configuration.EnableCodeAnalyzers &&
              (!context.IsGeneratedCode || !configuration.CodeAnalyzerOptions.IgnoreIfCompilerGenerated))
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

                if(globalDictionaries.Count != 0)
                {
                    var dictionary = new SpellingDictionary(globalDictionaries, configuration.IgnoredWords);
                    var handler = new CSharpSpellCheckHandler(configuration);

                    handler.WordSplitter.IsCStyleCode = true;

                    handler.Recurse(root, context.CancellationToken);

                    var ignoreSpellingWords = handler.FindIgnoreSpellingDirectives();
                    var rangeExclusions = new List<Match>();
                    var diagnosticProperties = new Dictionary<string, string>();

                    // Just handle identifiers and leave the rest to the existing tagger which already handles
                    // all the other elements.  The inability to add words to the ignored words files and the
                    // dictionary from the code analyzer is a limitation the tagger doesn't have so, for now,
                    // they'll continue to handle strings and comments.  The code's already written so I'll
                    // leave it in here to parse the other elements.  If the above issue is ever resolved, I
                    // could move that functionality in here later on.
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

                                if(s.Text.Length != word.Length)
                                {
                                    diagnosticProperties["Prefix"] = s.Text.Substring(0, word.Start);
                                    diagnosticProperties["Suffix"] = s.Text.Substring(word.Start + word.Length);
                                }

                                // Handle code analysis dictionary checks first as they may be not be
                                // recognized as correctly spelled words but have alternate handling.
                                if(configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                                    configuration.DeprecatedTerms.TryGetValue(textToCheck, out string preferredTerm))
                                {
                                    diagnosticProperties["Suggestions"] = preferredTerm.CapitalizeIfNecessary(
                                        Char.IsUpper(textToCheck[0]));

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                if(configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                                    configuration.CompoundTerms.TryGetValue(textToCheck, out preferredTerm))
                                {
                                    diagnosticProperties["Suggestions"] = preferredTerm.CapitalizeIfNecessary(
                                        Char.IsUpper(textToCheck[0]));

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                if(configuration.CadOptions.TreatUnrecognizedWordsAsMisspelled &&
                                    configuration.UnrecognizedWords.TryGetValue(textToCheck, out IList<string> spellingAlternates))
                                {
                                    diagnosticProperties["Suggestions"] = String.Join(",", spellingAlternates.Select(
                                        sa => sa.CapitalizeIfNecessary(Char.IsUpper(textToCheck[0]))));

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, s.TextSpan),
                                        diagnosticProperties.ToImmutableDictionary(), s.Text));
                                    continue;
                                }

                                // Ignore case as variable names may not match the case and that's okay.  This
                                // prevents a lot of false reports for things we don't care about.
                                if(!dictionary.IsSpelledCorrectlyIgnoreCase(textToCheck))
                                {
                                    // Getting suggestions is expensive so it is deferred until it's really
                                    // needed when the code fix is invoked.
                                    diagnosticProperties["TextToCheck"] = textToCheck;
                                    diagnosticProperties["Languages"] = String.Join(",",
                                        dictionary.Dictionaries.Select(d => d.Culture.Name));

                                    //// if(configuration.IgnoredWordsFiles.Any())
                                    //// {
                                    ////     diagnosticProperties["IgnoredWordsFiles"] = String.Join("|",
                                    ////         configuration.IgnoredWordsFiles);
                                    //// }

                                    context.ReportDiagnostic(Diagnostic.Create(SpellingRule,
                                        Location.Create(context.Tree, TextSpan.FromBounds(
                                            s.TextSpan.Start + word.Start,
                                            s.TextSpan.Start + word.Start + word.Length)),
                                        diagnosticProperties.ToImmutableDictionary(), textToCheck));
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
