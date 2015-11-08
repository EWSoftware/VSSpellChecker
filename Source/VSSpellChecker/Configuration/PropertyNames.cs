//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : PropertyNames.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 10/29/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains the class containing the configuration property name constants
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/01/2015  EFW  Refactored configuration settings
// 07/22/2015  EFW  Added support for selecting multiple languages
//===============================================================================================================

namespace VisualStudio.SpellChecker.Configuration
{
    /// <summary>
    /// This class contains the configuration property name constants
    /// </summary>
    internal static class PropertyNames
    {
        #region Property name constants
        //=====================================================================

        /// <summary>
        /// Selected languages list
        /// </summary>
        public const string SelectedLanguages = "SelectedLanguages";

        /// <summary>
        /// Selected languages list item
        /// </summary>
        public const string SelectedLanguagesItem = "LanguageName";

        /// <summary>
        /// Spell check as you type
        /// </summary>
        public const string SpellCheckAsYouType = "SpellCheckAsYouType";

        /// <summary>
        /// Include in project spell check
        /// </summary>
        public const string IncludeInProjectSpellCheck = "IncludeInProjectSpellCheck";

        /// <summary>
        /// Detect doubled words
        /// </summary>
        public const string DetectDoubledWords = "DetectDoubledWords";

        /// <summary>
        /// Ignore words with digits
        /// </summary>
        public const string IgnoreWordsWithDigits = "IgnoreWordsWithDigits";

        /// <summary>
        /// Ignore words in all uppercase
        /// </summary>
        public const string IgnoreWordsInAllUppercase = "IgnoreWordsInAllUppercase";

        /// <summary>
        /// Ignore format specifiers
        /// </summary>
        public const string IgnoreFormatSpecifiers = "IgnoreFormatSpecifiers";

        /// <summary>
        /// Ignore filenames and e-mail addresses
        /// </summary>
        public const string IgnoreFilenamesAndEMailAddresses = "IgnoreFilenamesAndEMailAddresses";

        /// <summary>
        /// Ignore XML elements in text
        /// </summary>
        public const string IgnoreXmlElementsInText = "IgnoreXmlElementsInText";

        /// <summary>
        /// Treat underscore as separator
        /// </summary>
        public const string TreatUnderscoreAsSeparator = "TreatUnderscoreAsSeparator";

        /// <summary>
        /// Ignore mnemonics
        /// </summary>
        public const string IgnoreMnemonics = "IgnoreMnemonics";

        /// <summary>
        /// Ignore character class
        /// </summary>
        public const string IgnoreCharacterClass = "IgnoreCharacterClass";

        /// <summary>
        /// Determine resource file language from name
        /// </summary>
        public const string DetermineResourceFileLanguageFromName = "DetermineResourceFileLanguageFromName";

        /// <summary>
        /// C# - Ignore XML doc comments
        /// </summary>
        public const string CSharpOptionsIgnoreXmlDocComments = "CSharpOptions.IgnoreXmlDocComments";

        /// <summary>
        /// C# - Ignore delimited comments
        /// </summary>
        public const string CSharpOptionsIgnoreDelimitedComments = "CSharpOptions.IgnoreDelimitedComments";

        /// <summary>
        /// C# - Ignore standard single line comments
        /// </summary>
        public const string CSharpOptionsIgnoreStandardSingleLineComments = "CSharpOptions.IgnoreStandardSingleLineComments";

        /// <summary>
        /// C# - Ignore quadruple slash comments
        /// </summary>
        public const string CSharpOptionsIgnoreQuadrupleSlashComments = "CSharpOptions.IgnoreQuadrupleSlashComments";

        /// <summary>
        /// C# - Ignore normal strings
        /// </summary>
        public const string CSharpOptionsIgnoreNormalStrings = "CSharpOptions.IgnoreNormalStrings";

        /// <summary>
        /// C# - Ignore verbatim strings
        /// </summary>
        public const string CSharpOptionsIgnoreVerbatimStrings = "CSharpOptions.IgnoreVerbatimStrings";

        /// <summary>
        /// C# - Ignore interpolated strings
        /// </summary>
        public const string CSharpOptionsIgnoreInterpolatedStrings = "CSharpOptions.IgnoreInterpolatedStrings";

        /// <summary>
        /// C# - Apply to all C-style languages
        /// </summary>
        public const string CSharpOptionsApplyToAllCStyleLanguages = "CSharpOptions.ApplyToAllCStyleLanguages";

        /// <summary>
        /// Inherit excluded extensions
        /// </summary>
        public const string InheritExcludedExtensions = "InheritExcludedExtensions";

        /// <summary>
        /// Excluded filename extensions list
        /// </summary>
        public const string ExcludedExtensions = "ExcludedExtension";

        /// <summary>
        /// Excluded filename extensions list item
        /// </summary>
        public const string ExcludedExtensionsItem = "Exclude";

        /// <summary>
        /// Inherit additional dictionary folders
        /// </summary>
        public const string InheritAdditionalDictionaryFolders = "InheritAdditionalDictionaryFolders";

        /// <summary>
        /// Additional dictionary folders list
        /// </summary>
        public const string AdditionalDictionaryFolders = "AdditionalDictionaryFolders";

        /// <summary>
        /// Additional dictionary folders list item
        /// </summary>
        public const string AdditionalDictionaryFoldersItem = "Folder";

        /// <summary>
        /// Inherit ignored words
        /// </summary>
        public const string InheritIgnoredWords = "InheritIgnoredWords";

        /// <summary>
        /// Ignored words
        /// </summary>
        public const string IgnoredWords = "IgnoredWords";

        /// <summary>
        /// Ignored words item
        /// </summary>
        public const string IgnoredWordsItem = "Ignore";

        /// <summary>
        /// Inherit exclusion expressions
        /// </summary>
        public const string InheritExclusionExpressions = "InheritExclusionExpressions";

        /// <summary>
        /// Exclusion expressions
        /// </summary>
        public const string ExclusionExpressions = "ExclusionExpressions";

        /// <summary>
        /// Exclusion expression item
        /// </summary>
        public const string ExclusionExpressionItem = "Expression";

        /// <summary>
        /// Inherit XML settings
        /// </summary>
        public const string InheritXmlSettings = "InheritXmlSettings";

        /// <summary>
        /// Ignored XML elements
        /// </summary>
        public const string IgnoredXmlElements = "IgnoredXmlElements";

        /// <summary>
        /// Ignored XML elements item
        /// </summary>
        public const string IgnoredXmlElementsItem = "Ignore";

        /// <summary>
        /// Spell checked XML attributes
        /// </summary>
        public const string SpellCheckedXmlAttributes = "SpellCheckedXmlAttributes";

        /// <summary>
        /// Spell checked XML attributes item
        /// </summary>
        public const string SpellCheckedXmlAttributesItem = "SpellCheck";

        /// <summary>
        /// Code analysis dictionary - Import code analysis dictionaries
        /// </summary>
        public const string CadOptionsImportCodeAnalysisDictionaries = "CadOptions.ImportCodeAnalysisDictionaries";

        /// <summary>
        /// Code analysis dictionary - Recognized word handling
        /// </summary>
        public const string CadOptionsRecognizedWordHandling = "CadOptions.RecognizedWordHandling";

        /// <summary>
        /// Code analysis dictionary - Treat unrecognized words as misspelled
        /// </summary>
        public const string CadOptionsTreatUnrecognizedWordsAsMisspelled = "CadOptions.TreatUnrecognizedWordsAsMisspelled";

        /// <summary>
        /// Code analysis dictionary - Treat deprecated terms as misspelled
        /// </summary>
        public const string CadOptionsTreatDeprecatedTermsAsMisspelled = "CadOptions.TreatDeprecatedTermsAsMisspelled";

        /// <summary>
        /// Code analysis dictionary - Treat compound terms as misspelled
        /// </summary>
        public const string CadOptionsTreatCompoundTermsAsMisspelled = "CadOptions.TreatCompoundTermsAsMisspelled";

        /// <summary>
        /// Code analysis dictionary - Treat casing exceptions as ignored words
        /// </summary>
        public const string CadOptionsTreatCasingExceptionsAsIgnoredWords = "CadOptions.TreatCasingExceptionsAsIgnoredWords";

        #endregion
    }
}
