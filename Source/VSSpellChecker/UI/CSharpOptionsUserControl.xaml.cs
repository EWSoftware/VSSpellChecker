//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : CSharpOptionsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/14/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the C# spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/12/2014  EFW  Created the code
//===============================================================================================================

using System.Windows.Controls;

namespace VisualStudio.SpellChecker.UI
{
    /// <summary>
    /// This user control is used to edit the C# spell checker configuration settings
    /// </summary>
    public partial class CSharpOptionsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public CSharpOptionsUserControl()
        {
            InitializeComponent();
        }
        #endregion

        #region ISpellCheckerConfiguration Members
        //=====================================================================

        /// <inheritdoc />
        public UserControl Control
        {
            get { return this; }
        }

        /// <inheritdoc />
        public string Title
        {
            get { return "C# Options"; }
        }

        /// <inheritdoc />
        public string HelpUrl
        {
            get { return "CSharpOptions"; }
        }

        /// <inheritdoc />
        public bool IsValid
        {
            get { return true; }
        }

        /// <inheritdoc />
        public void LoadConfiguration()
        {
            chkIgnoreXmlDocComments.IsChecked = SpellCheckerConfiguration.IgnoreXmlDocComments;
            chkIgnoreDelimitedComments.IsChecked = SpellCheckerConfiguration.IgnoreDelimitedComments;
            chkIgnoreStandardSingleLineComments.IsChecked = SpellCheckerConfiguration.IgnoreStandardSingleLineComments;
            chkIgnoreQuadrupleSlashComments.IsChecked = SpellCheckerConfiguration.IgnoreQuadrupleSlashComments;
            chkIgnoreNormalStrings.IsChecked = SpellCheckerConfiguration.IgnoreNormalStrings;
            chkIgnoreVerbatimStrings.IsChecked = SpellCheckerConfiguration.IgnoreVerbatimStrings;
        }

        /// <inheritdoc />
        public bool SaveConfiguration()
        {
            SpellCheckerConfiguration.IgnoreXmlDocComments = chkIgnoreXmlDocComments.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreDelimitedComments = chkIgnoreDelimitedComments.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreStandardSingleLineComments = chkIgnoreStandardSingleLineComments.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreQuadrupleSlashComments = chkIgnoreQuadrupleSlashComments.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreNormalStrings = chkIgnoreNormalStrings.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreVerbatimStrings = chkIgnoreVerbatimStrings.IsChecked.Value;

            return true;
        }
        #endregion
    }
}
