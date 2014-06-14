//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : GeneralSettingsUserControl.xaml.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 06/10/2014
// Note    : Copyright 2014, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a user control used to edit the general spell checker configuration settings
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 06/09/2014  EFW  Moved the general settings to a user control
//===============================================================================================================

using System.Windows.Controls;

namespace VisualStudio.SpellChecker.UI
{
    /// <summary>
    /// This user control is used to edit the general spell checker configuration settings
    /// </summary>
    public partial class GeneralSettingsUserControl : UserControl, ISpellCheckerConfiguration
    {
        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public GeneralSettingsUserControl()
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
            get { return "General Settings"; }
        }

        /// <inheritdoc />
        public bool IsValid
        {
            get { return true; }
        }

        /// <inheritdoc />
        public void LoadConfiguration()
        {
            chkSpellCheckAsYouType.IsChecked = SpellCheckerConfiguration.SpellCheckAsYouType;
            chkIgnoreWordsWithDigits.IsChecked = SpellCheckerConfiguration.IgnoreWordsWithDigits;
            chkIgnoreAllUppercase.IsChecked = SpellCheckerConfiguration.IgnoreWordsInAllUppercase;
            chkIgnoreFormatSpecifiers.IsChecked = SpellCheckerConfiguration.IgnoreFormatSpecifiers;
            chkIgnoreFilenamesAndEMail.IsChecked = SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses;
            chkIgnoreXmlInText.IsChecked = SpellCheckerConfiguration.IgnoreXmlElementsInText;
            chkTreatUnderscoresAsSeparators.IsChecked = SpellCheckerConfiguration.TreatUnderscoreAsSeparator;

            txtExcludeByExtension.Text = SpellCheckerConfiguration.ExcludeByFilenameExtension;
        }

        /// <inheritdoc />
        public bool SaveConfiguration()
        {
            SpellCheckerConfiguration.SpellCheckAsYouType = chkSpellCheckAsYouType.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreWordsWithDigits = chkIgnoreWordsWithDigits.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreWordsInAllUppercase = chkIgnoreAllUppercase.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreFormatSpecifiers = chkIgnoreFormatSpecifiers.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreFilenamesAndEMailAddresses = chkIgnoreFilenamesAndEMail.IsChecked.Value;
            SpellCheckerConfiguration.IgnoreXmlElementsInText = chkIgnoreXmlInText.IsChecked.Value;
            SpellCheckerConfiguration.TreatUnderscoreAsSeparator = chkTreatUnderscoresAsSeparators.IsChecked.Value;

            SpellCheckerConfiguration.ExcludeByFilenameExtension = txtExcludeByExtension.Text;

            return true;
        }
        #endregion
    }
}
