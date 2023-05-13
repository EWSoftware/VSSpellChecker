//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CodeAnalyzerOptions.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/13/2023
// Note    : Copyright 2015-2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to contain the code analyzer configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 02/01/2015  EFW  Refactored configuration settings
//===============================================================================================================

using System.ComponentModel;

namespace VisualStudio.SpellChecker.Common.Configuration
{
    /// <summary>
    /// This class contains the spell checker configuration options for code analyzers
    /// </summary>
    public class CodeAnalyzerOptions
    {
        /// <summary>
        /// This is used to get or set whether or not to ignore identifiers for private types and members
        /// </summary>
        /// <value>The default is true to exclude them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_code_analyzer_ignore_identifier_if_private")]
        public bool IgnoreIdentifierIfPrivate { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore identifiers for internal types and members
        /// </summary>
        /// <value>The default is true to exclude them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_code_analyzer_ignore_identifier_if_internal")]
        public bool IgnoreIdentifierIfInternal { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore identifiers in all uppercase
        /// </summary>
        /// <value>The default is false to include them</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_identifier_if_all_uppercase")]
        public bool IgnoreIdentifierIfAllUppercase { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore identifiers within member bodies such as
        /// variable declarations local to a method.
        /// </summary>
        /// <value>The default is true to exclude them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_code_analyzer_ignore_identifiers_within_member_bodies")]
        public bool IgnoreIdentifiersWithinMemberBodies { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore type parameters (e.g. TKey, TValue)
        /// </summary>
        /// <value>The default is false to include them.  If the first two letters are capitalized, the
        /// first letter is skipped an only the remaining part of the name is spell checked.</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_type_parameters")]
        public bool IgnoreTypeParameters { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore compiler generated types and members
        /// </summary>
        /// <value>The default is true to exclude them</value>
        [DefaultValue(true), EditorConfigProperty("vsspell_code_analyzer_ignore_if_compiler_generated")]
        public bool IgnoreIfCompilerGenerated { get; set; } = true;

        /// <summary>
        /// This is used to get or set whether or not to ignore XML documentation comments
        /// (<c>/** ... */</c> or <c>/// ...</c>)
        /// </summary>
        /// <value>The default is false to include XML documentation comments</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_xml_doc_comments")]
        public bool IgnoreXmlDocComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore delimited comments (<c>/* ... */</c>)
        /// </summary>
        /// <value>The default is false to include delimited comments</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_delimited_comments")]
        public bool IgnoreDelimitedComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore standard single line comments (<c>// ...</c>)
        /// </summary>
        /// <value>The default is false to include standard single line comments</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_standard_single_line_comments")]
        public bool IgnoreStandardSingleLineComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore quadruple slash comments (<c>//// ...</c>)
        /// </summary>
        /// <value>The default is false to include quadruple slash comments</value>
        /// <remarks>This is useful for ignoring commented out blocks of code while still spell checking the
        /// other comment styles.</remarks>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_quadruple_slash_comments")]
        public bool IgnoreQuadrupleSlashComments { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore normal strings (<c>"..."</c>)
        /// </summary>
        /// <value>The default is false to include normal strings</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_normal_strings")]
        public bool IgnoreNormalStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore verbatim strings (<c>@"..."</c>)
        /// </summary>
        /// <value>The default is false to include verbatim strings</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_verbatim_strings")]
        public bool IgnoreVerbatimStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore interpolated strings (<c>$"{PropertyName} ..."</c>)
        /// </summary>
        /// <value>The default is false to include interpolated strings</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_interpolated_strings")]
        public bool IgnoreInterpolatedStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to ignore raw strings (<c>"""..."""</c>)
        /// </summary>
        /// <value>The default is false to include raw strings</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_ignore_raw_strings")]
        public bool IgnoreRawStrings { get; set; }

        /// <summary>
        /// This is used to get or set whether or not to apply these options to all C-style languages
        /// </summary>
        /// <value>The default is false to only apply the settings to Roslyn-based code analyzers or C# source
        /// code if using the older style tagger or solution/project spell checking.  If enabled, only the
        /// options relevant to the language are used based on how the code is parsed by the tagger.</value>
        [DefaultValue(false), EditorConfigProperty("vsspell_code_analyzer_apply_to_all_c_style_languages")]
        public bool ApplyToAllCStyleLanguages { get; set; }
    }
}
