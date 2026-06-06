// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// RS1035 is suppressed as we do need to use the banned Assembly.Load(byte[]) API to load embedded dependencies
[assembly: SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "<Pending>", Scope = "member", Target = "~M:VisualStudio.SpellChecker.CodeAnalyzer.CSharpSpellCheckCodeAnalyzer.#cctor")]
