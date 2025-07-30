// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// RS1035 is suppressed in several places as we do need to use the banned APIs for things other than modifying the code
[assembly: SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "<Pending>", Scope = "member", Target = "~M:VisualStudio.SpellChecker.CodeAnalyzer.CSharpSpellCheckCodeAnalyzer.#cctor")]
[assembly: SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "<Pending>", Scope = "member", Target = "~P:VisualStudio.SpellChecker.CodeAnalyzer.CSharpSpellCheckCodeAnalyzer.GlobalConfigurationFilePath")]
