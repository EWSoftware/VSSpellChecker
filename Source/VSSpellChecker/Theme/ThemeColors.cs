//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ThemeColors.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/22/2015
// Note    : Copyright 2015, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to provide Visual Studio theme colors in a version independent manner
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 08/17/2015  EFW  Created the code
//===============================================================================================================

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace VisualStudio.SpellChecker.Theme
{
    /// <summary>
    /// This class is used to provide Visual Studio Theme colors in a version independent manner
    /// </summary>
    public class ThemeColors : INotifyPropertyChanged
    {
        #region Private data members
        //=====================================================================

        private static ThemeColors instance;
        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the theme color instance
        /// </summary>
        public static ThemeColors Instance
        {
            get { return instance ?? (instance = new ThemeColors()); }
        }

        /// <summary>
        /// Button background color
        /// </summary>
        public Color ButtonBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonBackgroundColor); }
        }

        /// <summary>
        /// Button border color
        /// </summary>
        public Color ButtonBorderColor
        {
            get { return this.GetColor(ThemeColorId.ButtonBorderColor); }
        }

        /// <summary>
        /// Button disabled background color
        /// </summary>
        public Color ButtonDisabledBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonDisabledBackgroundColor); }
        }

        /// <summary>
        /// Button disabled border color
        /// </summary>
        public Color ButtonDisabledBorderColor
        {
            get { return this.GetColor(ThemeColorId.ButtonDisabledBorderColor); }
        }

        /// <summary>
        /// Button disabled foreground color
        /// </summary>
        public Color ButtonDisabledForegroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonDisabledForegroundColor); }
        }

        /// <summary>
        /// Button foreground color
        /// </summary>
        public Color ButtonForegroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonForegroundColor); }
        }

        /// <summary>
        /// Button hover background color
        /// </summary>
        public Color ButtonHoverBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonHoverBackgroundColor); }
        }

        /// <summary>
        /// Button hover border color
        /// </summary>
        public Color ButtonHoverBorderColor
        {
            get { return this.GetColor(ThemeColorId.ButtonHoverBorderColor); }
        }

        /// <summary>
        /// Button hover foreground color
        /// </summary>
        public Color ButtonHoverForegroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonHoverForegroundColor); }
        }

        /// <summary>
        /// Button pressed background color
        /// </summary>
        public Color ButtonPressedBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonPressedBackgroundColor); }
        }

        /// <summary>
        /// Button pressed border color
        /// </summary>
        public Color ButtonPressedBorderColor
        {
            get { return this.GetColor(ThemeColorId.ButtonPressedBorderColor); }
        }

        /// <summary>
        /// Button pressed foreground color
        /// </summary>
        public Color ButtonPressedForegroundColor
        {
            get { return this.GetColor(ThemeColorId.ButtonPressedForegroundColor); }
        }

        /// <summary>
        /// Combo box button mouse over color
        /// </summary>
        public Color ComboBoxButtonMouseOverBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ComboBoxButtonMouseOverBackgroundColor); }
        }

        /// <summary>
        /// Combo box disabled glyph color
        /// </summary>
        public Color ComboBoxDisabledGlyphColor
        {
            get { return this.GetColor(ThemeColorId.ComboBoxDisabledGlyphColor); }
        }

        /// <summary>
        /// Combo box glyph color
        /// </summary>
        public Color ComboBoxGlyphColor
        {
            get { return this.GetColor(ThemeColorId.ComboBoxGlyphColor); }
        }

        /// <summary>
        /// Combo box pop-up background color
        /// </summary>
        public Color ComboBoxPopupBackground
        {
            get { return this.GetColor(ThemeColorId.ComboBoxPopupBackground); }
        }

        /// <summary>
        /// Disabled text color
        /// </summary>
        public Color DisabledTextColor
        {
            get { return this.GetColor(ThemeColorId.DisabledTextColor); }
        }

        /// <summary>
        /// Item border color
        /// </summary>
        public Color ItemBorderColor
        {
            get { return this.GetColor(ThemeColorId.ItemBorderColor); }
        }

        /// <summary>
        /// Item color
        /// </summary>
        public Color ItemColor
        {
            get { return this.GetColor(ThemeColorId.ItemColor); }
        }

        /// <summary>
        /// Item hover color
        /// </summary>
        public Color ItemHoverColor
        {
            get { return this.GetColor(ThemeColorId.ItemHoverColor); }
        }

        /// <summary>
        /// Item hover border color
        /// </summary>
        public Color ItemHoverBorderColor
        {
            get { return this.GetColor(ThemeColorId.ItemHoverBorderColor); }
        }

        /// <summary>
        /// Item hover text color
        /// </summary>
        public Color ItemHoverTextColor
        {
            get { return this.GetColor(ThemeColorId.ItemHoverTextColor); }
        }

        /// <summary>
        /// Item selected border color
        /// </summary>
        public Color ItemSelectedBorderColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedBorderColor); }
        }

        /// <summary>
        /// Item selected border not focused color
        /// </summary>
        public Color ItemSelectedBorderNotFocusedColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedBorderNotFocusedColor); }
        }

        /// <summary>
        /// Item selected color
        /// </summary>
        public Color ItemSelectedColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedColor); }
        }

        /// <summary>
        /// Item selected not focused color
        /// </summary>
        public Color ItemSelectedNotFocusedColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedNotFocusedColor); }
        }

        /// <summary>
        /// Item selected text color
        /// </summary>
        public Color ItemSelectedTextColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedTextColor); }
        }

        /// <summary>
        /// Item selected text not focused color
        /// </summary>
        public Color ItemSelectedTextNotFocusedColor
        {
            get { return this.GetColor(ThemeColorId.ItemSelectedTextNotFocusedColor); }
        }

        /// <summary>
        /// Item text color
        /// </summary>
        public Color ItemTextColor
        {
            get { return this.GetColor(ThemeColorId.ItemTextColor); }
        }

        /// <summary>
        /// Light border color
        /// </summary>
        public Color LightBorderColor
        {
            get { return this.GetColor(ThemeColorId.LightBorderColor); }
        }

        /// <summary>
        /// Link text color
        /// </summary>
        public Color LinkTextColor
        {
            get { return this.GetColor(ThemeColorId.LinkTextColor); }
        }

        /// <summary>
        /// Link text hover color
        /// </summary>
        public Color LinkTextHoverColor
        {
            get { return this.GetColor(ThemeColorId.LinkTextHoverColor); }
        }

        /// <summary>
        /// Menu background color
        /// </summary>
        public Color MenuBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.MenuBackgroundColor); }
        }

        /// <summary>
        /// Menu border color
        /// </summary>
        public Color MenuBorderColor
        {
            get { return this.GetColor(ThemeColorId.MenuBorderColor); }
        }

        /// <summary>
        /// Menu hover background color
        /// </summary>
        public Color MenuHoverBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.MenuHoverBackgroundColor); }
        }

        /// <summary>
        /// Menu hover text color
        /// </summary>
        public Color MenuHoverTextColor
        {
            get { return this.GetColor(ThemeColorId.MenuHoverTextColor); }
        }

        /// <summary>
        /// Menu separator color
        /// </summary>
        public Color MenuSeparatorColor
        {
            get { return this.GetColor(ThemeColorId.MenuSeparatorColor); }
        }

        /// <summary>
        /// Menu text color
        /// </summary>
        public Color MenuTextColor
        {
            get { return this.GetColor(ThemeColorId.MenuTextColor); }
        }

        /// <summary>
        /// Notification color
        /// </summary>
        public Color NotificationColor
        {
            get { return this.GetColor(ThemeColorId.NotificationColor); }
        }

        /// <summary>
        /// Notification text color
        /// </summary>
        public Color NotificationTextColor
        {
            get { return this.GetColor(ThemeColorId.NotificationTextColor); }
        }

        /// <summary>
        /// Text box border color
        /// </summary>
        public Color TextBoxBorderColor
        {
            get { return this.GetColor(ThemeColorId.TextBoxBorderColor); }
        }

        /// <summary>
        /// Text box color
        /// </summary>
        public Color TextBoxColor
        {
            get { return this.GetColor(ThemeColorId.TextBoxColor); }
        }

        /// <summary>
        /// Text box text color
        /// </summary>
        public Color TextBoxTextColor
        {
            get { return this.GetColor(ThemeColorId.TextBoxTextColor); }
        }

        /// <summary>
        /// Tool window background color
        /// </summary>
        public Color ToolWindowBackgroundColor
        {
            get { return this.GetColor(ThemeColorId.ToolWindowBackgroundColor); }
        }

        /// <summary>
        /// Tool window border color
        /// </summary>
        public Color ToolWindowBorderColor
        {
            get { return this.GetColor(ThemeColorId.ToolWindowBorderColor); }
        }

        /// <summary>
        /// Tool window text color
        /// </summary>
        public Color ToolWindowTextColor
        {
            get { return this.GetColor(ThemeColorId.ToolWindowTextColor); }
        }

        /// <summary>
        /// Tree view glyph color
        /// </summary>
        public Color TreeViewGlyphColor
        {
            get { return this.GetColor(ThemeColorId.TreeViewGlyphColor); }
        }

        /// <summary>
        /// Tree view mouse over glyph color
        /// </summary>
        public Color TreeViewHoverGlyphColor
        {
            get { return this.GetColor(ThemeColorId.TreeViewHoverGlyphColor); }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Private constructor
        /// </summary>
        private ThemeColors()
        {
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        }
        #endregion

        #region INotifyPropertyChanged implementation
        //=====================================================================

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// This event handler is called when the Visual Studio theme changes and raise the property change
        /// notification so that the colors are updated in any controls that use them.
        /// </summary>
        /// <param name="e">The event arguments</param>
        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            this.OnPropertyChanged(null);
        }
        #endregion

        #region Helper Methods
        //=====================================================================

        /// <summary>
        /// This is used to get a Visual Studio theme color for the given theme color ID
        /// </summary>
        /// <param name="id">The theme color ID for which to get the Visual Studio theme color</param>
        /// <returns>The theme color to use</returns>
        /// <remarks>Theme colors do not appear to be available at design time.  As such, this will return
        /// related default system colors in their place.</remarks>
        private Color GetColor(ThemeColorId id)
        {
            Color? vsColor = this.GetVisualStudioColor(id);

            if(vsColor != null)
                return vsColor.Value;

            // Get colors for design time or if something failed or wasn't defined
            return this.GetDefaultColor(id);
        }

        /// <summary>
        /// This is used to return a Visual Studio theme color for the given theme color ID
        /// </summary>
        /// <param name="id">The theme color ID for which to get the Visual Studio theme color</param>
        /// <returns>The theme color to use or null if it could not be obtained was not recognized</returns>
        private Color? GetVisualStudioColor(ThemeColorId id)
        {
            switch(id)
            {
                case ThemeColorId.ButtonBackgroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonColorKey);

                case ThemeColorId.ButtonBorderColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonBorderColorKey);

                case ThemeColorId.ButtonDisabledBackgroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonDisabledColorKey);

                case ThemeColorId.ButtonDisabledBorderColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonDisabledBorderColorKey);

                case ThemeColorId.ButtonDisabledForegroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonDisabledTextColorKey);

                case ThemeColorId.ButtonForegroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonTextColorKey);

                case ThemeColorId.ButtonHoverBackgroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonMouseOverColorKey);

                case ThemeColorId.ButtonHoverBorderColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonMouseOverBorderColorKey);

                case ThemeColorId.ButtonHoverForegroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonMouseOverTextColorKey);

                case ThemeColorId.ButtonPressedBackgroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonPressedColorKey);

                case ThemeColorId.ButtonPressedBorderColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonPressedBorderColorKey);

                case ThemeColorId.ButtonPressedForegroundColor:
                    return this.GetThemeColor(TeamFoundationColors.ButtonPressedTextColorKey);

                case ThemeColorId.ComboBoxButtonMouseOverBackgroundColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxButtonMouseOverBackgroundColorKey);

                case ThemeColorId.ComboBoxDisabledGlyphColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxDisabledGlyphColorKey);

                case ThemeColorId.ComboBoxGlyphColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxGlyphColorKey);

                case ThemeColorId.ComboBoxPopupBackground:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxPopupBackgroundBeginColorKey);

                case ThemeColorId.DisabledTextColor:
                case ThemeColorId.LightBorderColor:
                    return this.GetThemeColor(EnvironmentColors.SystemGrayTextColorKey);

                case ThemeColorId.ItemBorderColor:
                case ThemeColorId.ItemColor:
                    return this.GetThemeColor(TreeViewColors.BackgroundColorKey);

                case ThemeColorId.ItemHoverColor:
                case ThemeColorId.ItemHoverBorderColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarMouseOverBackgroundMiddle1ColorKey);

                case ThemeColorId.ItemHoverTextColor:
                case ThemeColorId.MenuHoverTextColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarTextHoverColorKey);

                case ThemeColorId.ItemSelectedBorderColor:
                case ThemeColorId.ItemSelectedColor:
                    return this.GetThemeColor(TreeViewColors.SelectedItemActiveColorKey);

                case ThemeColorId.ItemSelectedBorderNotFocusedColor:
                case ThemeColorId.ItemSelectedNotFocusedColor:
                    return this.GetThemeColor(TreeViewColors.SelectedItemInactiveColorKey);

                case ThemeColorId.ItemSelectedTextColor:
                    return this.GetThemeColor(TreeViewColors.SelectedItemActiveTextColorKey);

                case ThemeColorId.ItemSelectedTextNotFocusedColor:
                    return this.GetThemeColor(TreeViewColors.SelectedItemInactiveTextColorKey);

                case ThemeColorId.ItemTextColor:
                    return this.GetThemeColor(TreeViewColors.BackgroundTextColorKey);

                case ThemeColorId.LinkTextColor:
                    return this.GetThemeColor(EnvironmentColors.ControlLinkTextColorKey);

                case ThemeColorId.LinkTextHoverColor:
                    return this.GetThemeColor(EnvironmentColors.ControlLinkTextHoverColorKey);

                case ThemeColorId.MenuBackgroundColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarMenuBackgroundGradientBeginColorKey);

                case ThemeColorId.MenuBorderColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarMenuBorderColorKey);

                case ThemeColorId.MenuHoverBackgroundColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarMouseOverBackgroundBeginColorKey);

                case ThemeColorId.MenuSeparatorColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarMenuSeparatorColorKey);

                case ThemeColorId.MenuTextColor:
                    return this.GetThemeColor(EnvironmentColors.CommandBarTextActiveColorKey);

                case ThemeColorId.NotificationColor:
                    return this.GetThemeColor(EnvironmentColors.SystemInfoBackgroundColorKey);

                case ThemeColorId.NotificationTextColor:
                    return this.GetThemeColor(EnvironmentColors.SystemInfoTextColorKey);

                case ThemeColorId.TextBoxBorderColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxBorderColorKey);

                case ThemeColorId.TextBoxColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxBackgroundColorKey);

                case ThemeColorId.TextBoxTextColor:
                    return this.GetThemeColor(EnvironmentColors.ComboBoxItemTextColorKey);

                case ThemeColorId.ToolWindowBackgroundColor:
                    return this.GetThemeColor(EnvironmentColors.ToolWindowBackgroundColorKey);

                case ThemeColorId.ToolWindowBorderColor:
                    return this.GetThemeColor(EnvironmentColors.ToolWindowBorderColorKey);

                case ThemeColorId.ToolWindowTextColor:
                    return this.GetThemeColor(EnvironmentColors.ToolWindowTextColorKey);

                case ThemeColorId.TreeViewGlyphColor:
                    return this.GetThemeColor(TreeViewColors.GlyphColorKey);

                case ThemeColorId.TreeViewHoverGlyphColor:
                    return this.GetThemeColor(TreeViewColors.GlyphMouseOverColorKey);

                default:
                    return null;
            }
        }

        /// <summary>
        /// This is used to return a default system color for the given theme color ID
        /// </summary>
        /// <param name="id">The theme color ID for which to get the default system color</param>
        /// <returns>The default system color to use</returns>
        private Color GetDefaultColor(ThemeColorId id)
        {
            switch(id)
            {
                case ThemeColorId.ButtonBackgroundColor:
                case ThemeColorId.ButtonBorderColor:
                case ThemeColorId.ButtonDisabledBackgroundColor:
                case ThemeColorId.ButtonDisabledBorderColor:
                case ThemeColorId.ButtonHoverBackgroundColor:
                case ThemeColorId.ButtonHoverBorderColor:
                case ThemeColorId.ButtonPressedBackgroundColor:
                case ThemeColorId.ButtonPressedBorderColor:
                case ThemeColorId.MenuBackgroundColor:
                case ThemeColorId.ToolWindowBackgroundColor:
                case ThemeColorId.ToolWindowBorderColor:
                    return SystemColors.ControlColor;

                case ThemeColorId.ButtonDisabledForegroundColor:
                case ThemeColorId.ButtonForegroundColor:
                case ThemeColorId.ButtonHoverForegroundColor:
                case ThemeColorId.ButtonPressedForegroundColor:
                case ThemeColorId.MenuHoverTextColor:
                case ThemeColorId.MenuTextColor:
                    return SystemColors.ControlTextColor;

                case ThemeColorId.ComboBoxButtonMouseOverBackgroundColor:
                case ThemeColorId.ComboBoxGlyphColor:
                case ThemeColorId.MenuBorderColor:
                case ThemeColorId.MenuSeparatorColor:
                case ThemeColorId.TreeViewGlyphColor:
                case ThemeColorId.TreeViewHoverGlyphColor:
                    return SystemColors.ControlDarkColor;

                case ThemeColorId.ComboBoxDisabledGlyphColor:
                case ThemeColorId.DisabledTextColor:
                case ThemeColorId.LightBorderColor:
                    return SystemColors.GrayTextColor;

                case ThemeColorId.ComboBoxPopupBackground:
                case ThemeColorId.ItemBorderColor:
                case ThemeColorId.ItemColor:
                case ThemeColorId.NotificationColor:
                case ThemeColorId.TextBoxColor:
                    return SystemColors.WindowColor;

                case ThemeColorId.ItemHoverColor:
                case ThemeColorId.ItemHoverBorderColor:
                case ThemeColorId.ItemSelectedBorderColor:
                case ThemeColorId.ItemSelectedBorderNotFocusedColor:
                case ThemeColorId.ItemSelectedColor:
                case ThemeColorId.ItemSelectedNotFocusedColor:
                case ThemeColorId.MenuHoverBackgroundColor:
                    return SystemColors.HighlightColor;

                case ThemeColorId.ItemHoverTextColor:
                case ThemeColorId.ItemSelectedTextColor:
                case ThemeColorId.ItemSelectedTextNotFocusedColor:
                    return SystemColors.HighlightTextColor;

                case ThemeColorId.ItemTextColor:
                case ThemeColorId.NotificationTextColor:
                case ThemeColorId.TextBoxTextColor:
                case ThemeColorId.ToolWindowTextColor:
                    return SystemColors.WindowTextColor;

                case ThemeColorId.LinkTextColor:
                case ThemeColorId.LinkTextHoverColor:
                    return SystemColors.HotTrackColor;

                case ThemeColorId.TextBoxBorderColor:
                    return SystemColors.WindowFrameColor;

                default:
                    return SystemColors.ControlTextColor;
            }
        }

        /// <summary>
        /// This is used to get the theme color for the given them resource key
        /// </summary>
        /// <param name="themeResourceKey">The theme resource key for which to get the color</param>
        /// <returns>The color for the theme resource key or null if it could not be obtained</returns>
        private Color? GetThemeColor(ThemeResourceKey themeResourceKey)
        {
            try
            {
                System.Drawing.Color vsThemeColor = VSColorTheme.GetThemedColor(themeResourceKey);
                return Color.FromArgb(vsThemeColor.A, vsThemeColor.R, vsThemeColor.G, vsThemeColor.B);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get Visual Studio theme color {0}.  Exception:\r\n{1}",
                    themeResourceKey.Name, ex);
            }

            return null;
        }
        #endregion
    }
}
