//===============================================================================================================
// System  : Spell Check My Code Package
// File    : SectionInfo.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/15/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains a class used to contain information about an .editorconfig section
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/11/2023  EFW  Created the code
//===============================================================================================================

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using VisualStudio.SpellChecker.Common.EditorConfig;

namespace VisualStudio.SpellChecker.Editors
{
    /// <summary>
    /// This class is used to contain information about an .editorconfig section
    /// </summary>
    public class SectionInfo : INotifyPropertyChanged
    {
        #region Private data members
        //=====================================================================

        private string sectionDesc;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property is used to get the .editorconfig section
        /// </summary>
        public EditorConfigSection Section { get; }

        /// <summary>
        /// This is used to get or set the spell checker configuration comments for the section
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// This read-only property returns a section description
        /// </summary>
        public string SectionDescription
        {
            get => sectionDesc;
            private set
            {
                if(sectionDesc != value)
                {
                    sectionDesc = value ?? "(empty)";

                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// This read-only property is used to indicate whether or not the section contains settings other than
        /// those for the spell checker.
        /// </summary>
        public bool ContainsOtherSettings { get; private set; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="section">The .editorconfig section represented by this instance</param>
        public SectionInfo(EditorConfigSection section)
        {
            this.Section = section;
            this.RefreshSectionDescription();
        }
        #endregion

        #region INotifyPropertyChanged Members
        //=====================================================================

        /// <summary>
        /// The property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This raises the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">The property name that changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Refresh the section description based on changes to the section's file glob and comments
        /// </summary>
        public void RefreshSectionDescription()
        {
            StringBuilder sb = new();

            if(this.Section.IsGlobal)
            {
                sb.Append($".globalconfig - Level ");

                if(this.Section.GlobalLevel != null)
                    sb.Append(this.Section.GlobalLevel);
                else
                    sb.Append("not set");
            }
            else
                sb.AppendFormat("[{0}]", this.Section.SectionHeader.FileGlob);

            var spellcheckerProperties = this.Section.SpellCheckerProperties.ToList();
            int commentIdx = -1;

            if(this.Section.SpellCheckerComments.Any())
            {
                sb.Append(" - ");

                commentIdx = sb.Length;

                foreach(var comment in this.Section.SpellCheckerComments)
                {
                    sb.Append(comment.LineText.Substring(10).Trim());
                    sb.Append(' ');
                }
            }
            else
            {
                if(!spellcheckerProperties.Any())
                    sb.Append(" (No spell checker settings)");
            }

            this.SectionDescription = sb.ToString().Trim();
            this.ContainsOtherSettings = this.Section.SectionLines.Except(spellcheckerProperties).Any(
                l => l.LineType == LineType.Property);

            if(commentIdx != -1 && commentIdx < this.SectionDescription.Length)
                this.Comments = this.SectionDescription.Substring(commentIdx);
        }
        #endregion
    }
}
