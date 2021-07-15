//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : IgnoredWordsSuggestedAction.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 03/18/2020
// Note    : Copyright 2020, Eric Woodruff, All rights reserved
//
// This file contains a class used to provide suggested actions for adding words to an ignored words file
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 03/12/2020  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;
using PackageResources = VisualStudio.SpellChecker.Properties.Resources;

namespace VisualStudio.SpellChecker.SuggestedActions
{
    /// <summary>
    /// Suggested action for adding words to an ignored words file
    /// </summary>
    internal class IgnoredWordsSuggestedAction : SuggestedActionBase
    {
        #region Private data members
        //=====================================================================

        private readonly SpellingDictionary dictionary;
        private readonly string ignoredWordsFile;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="span">The span containing the word to ignore.</param>
        /// <param name="dictionary">The dictionary used to ignore the word or add the word.</param>
        /// <param name="configurationType">The configuration file type associated with the ignored words file.</param>
        /// <param name="ignoredWordsFile">The ignored words file to which the word is added</param>
        /// <param name="displayText">The display text for the suggested action.</param>
        public IgnoredWordsSuggestedAction(ITrackingSpan span, SpellingDictionary dictionary,
          ConfigurationType configurationType, string ignoredWordsFile, string displayText) : base(displayText, span)
        {
            this.dictionary = dictionary;
            this.ignoredWordsFile = ignoredWordsFile;

            if(configurationType == ConfigurationType.Global)
                this.DisplayTextSuffix = "Global";
            else
                this.DisplayTextSuffix = $"{configurationType} ({Path.GetFileName(ignoredWordsFile)})";

            if(String.IsNullOrWhiteSpace(this.DisplayText))
            {
                this.DisplayText = this.DisplayTextSuffix;
                this.DisplayTextSuffix = null;
            }
        }
        #endregion

        #region Abstract method implementations
        //=====================================================================

        /// <inheritdoc />
        public override void Invoke(CancellationToken cancellationToken)
        {
            // Remove mnemonics
            string wordToIgnore = this.Span.GetText(this.Span.TextBuffer.CurrentSnapshot).Replace("&",
                String.Empty).Replace("_", String.Empty);

            try
            {
                var words = new HashSet<string>(Utility.LoadUserDictionary(ignoredWordsFile, false, false),
                    StringComparer.OrdinalIgnoreCase);

                if(!words.Contains(wordToIgnore))
                {
                    words.Add(wordToIgnore);

                    if(!ignoredWordsFile.CanWriteToUserWordsFile(null))
                    {
                        MessageBox.Show("Ignored words file is read-only or could not be checked out",
                            PackageResources.PackageTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    Utility.SaveCustomDictionary(ignoredWordsFile, false, false, words);
                }
            }
            catch(Exception ex)
            {
                // Ignore errors, we just won't save it to the file
                Debug.WriteLine(ex);
            }

            dictionary.IgnoreWord(wordToIgnore);
        }
        #endregion
    }
}
