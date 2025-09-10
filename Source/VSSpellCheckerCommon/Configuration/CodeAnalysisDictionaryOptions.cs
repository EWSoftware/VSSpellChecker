//===============================================================================================================
// System  : Spell Check My Code Package
// File    : CodeAnalysisDictionaryOptions.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/28/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to contain the code analysis dictionary configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/26/2015  EFW  Created the code
//===============================================================================================================

using System.ComponentModel;

namespace VisualStudio.SpellChecker.Common.Configuration
{
    /// <summary>
    /// This class contains the spell checker configuration options for code analysis dictionary files
    /// </summary>
    public class CodeAnalysisDictionaryOptions
    {
        /// <summary>
        /// This is used to get or set whether or not to import code analysis dictionary files
        /// </summary>
        /// <value>The default is true to import them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_cad_import_code_analysis_dictionaries")]
        public bool ImportCodeAnalysisDictionaries { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to import code analysis dictionary files
        /// </summary>
        /// <value>The default is to ignore all words</value>
        [DefaultValue(RecognizedWordHandling.IgnoreAllWords), EditorConfigProperty("vsspell_cad_recognized_word_handling")]
        public RecognizedWordHandling RecognizedWordHandling { get; set; } = RecognizedWordHandling.IgnoreAllWords;

        /// <summary>
        /// This is used to get or set whether or not to treat unrecognized words as misspelled words
        /// </summary>
        /// <value>The default is true to treat them as misspelled</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_cad_treat_unrecognized_words_as_misspelled")]
        public bool TreatUnrecognizedWordsAsMisspelled { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to treat deprecated words as misspelled words
        /// </summary>
        /// <value>The default is true to treat them as misspelled</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_cad_treat_deprecated_terms_as_misspelled")]
        public bool TreatDeprecatedTermsAsMisspelled { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to treat compound terms as misspelled words
        /// </summary>
        /// <value>The default is true to treat them as misspelled</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_cad_treat_compound_terms_as_misspelled")]
        public bool TreatCompoundTermsAsMisspelled { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to treat casing exceptions as ignored words
        /// </summary>
        /// <value>The default is false to treat them as misspelled</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_cad_treat_casing_exceptions_as_ignored_words")]
        public bool TreatCasingExceptionsAsIgnoredWords { get; set; }
    }
}
