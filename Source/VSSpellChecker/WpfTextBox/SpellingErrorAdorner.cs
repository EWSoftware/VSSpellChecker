//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellingErrorAdorner.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/29/2018
// Note    : Copyright 2016-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is used to adorn spelling errors in a WPF text box with an underline
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/22/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;

using TextSpan = Microsoft.VisualStudio.Text.Span;

using VisualStudio.SpellChecker.ProjectSpellCheck;
using Microsoft.VisualStudio.Threading;

namespace VisualStudio.SpellChecker.WpfTextBox
{
    /// <summary>
    /// This is used to adorn spelling errors in a WPF text box with an underline
    /// </summary>
    /// <remarks>A solid underline is used rather than a squiggle.  I tried to do a squiggle but it shifted all
    /// over the place depending on where it was drawn and looked crappy.  My WPF skills are rather meager so if
    /// someone wants to come up with something better that keeps the squiggles looking consistent, feel free.</remarks>
    internal class SpellingErrorAdorner : Adorner
    {
        #region Private data members
        //=====================================================================

        private List<FileMisspelling> misspelledWords;
        private TextBox textBox;
        private Pen pen;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="textBox">The textbox to adorn</param>
        public SpellingErrorAdorner(TextBox textBox) : base(textBox)
        {
            this.textBox = textBox;

            misspelledWords = new List<FileMisspelling>();

            textBox.IsVisibleChanged += this.textBox_IsVisibleChanged;
            textBox.SizeChanged += this.textBox_SizeChanged;
            textBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(this.textBox_ScrollChanged));

            pen = new Pen(new SolidColorBrush(Colors.Magenta), 1);
            pen.Freeze();
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// Disconnect the event handlers from the text box to dispose of this instance
        /// </summary>
        /// <remarks>Adorners don't implement <c>IDisposable</c> so we'll manage it ourselves</remarks>
        public void Disconnect()
        {
            textBox.IsVisibleChanged -= this.textBox_IsVisibleChanged;
            textBox.SizeChanged -= this.textBox_SizeChanged;
            textBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(this.textBox_ScrollChanged));
        }

        /// <summary>
        /// This is used to update the list of misspelled words
        /// </summary>
        /// <param name="misspellings">An enumerable list of misspelled words</param>
        public void UpdateMisspellings(IEnumerable<FileMisspelling> misspellings)
        {
            misspelledWords.Clear();
            misspelledWords.AddRange(misspellings);

            this.InvalidateInternal(false);
        }

        /// <summary>
        /// This is used to update the misspelling offsets when the text in the related textbox changes
        /// </summary>
        /// <param name="changes">An enumerable list of changes made</param>
        public void UpdateOffsets(IEnumerable<TextChange> changes)
        {
            foreach(TextChange c in changes)
                foreach(var w in misspelledWords.ToArray())
                {
                    // If the change occurred within the word, remove it.  Otherwise adjust its position if it's
                    // after the change location.
                    if(c.Offset >= w.Span.Start && c.Offset < w.Span.Start + w.Span.Length)
                        misspelledWords.Remove(w);
                    else
                        if(w.Span.Start >= c.Offset)
                        {
                            int start = w.Span.Start + c.AddedLength - c.RemovedLength;

                            if(start < 0)
                                misspelledWords.Remove(w);
                            else
                            {
                                w.Span = new TextSpan(start, w.Span.Length);
                                w.ActualBounds = Rect.Empty;
                            }
                        }
                }

            this.InvalidateInternal(false);
        }

        /// <summary>
        /// This gets the current adorner layer for the instance
        /// </summary>
        /// <returns>This returns the current adornment layer for the instance by getting the <c>_parent</c>
        /// field value from the inherited <c>Visual</c> base type using reflection.  This is the only parent
        /// field that contains the actual layer.</returns>
        private AdornerLayer GetAdornerLayer()
        {
            FieldInfo fi = typeof(Visual).GetField("_parent", BindingFlags.NonPublic | BindingFlags.Instance);

            return fi.GetValue(this) as AdornerLayer;
        }

        /// <summary>
        /// Invalidate the control
        /// </summary>
        /// <param name="clearCachedBounds">True to clear cached bounds, false to keep them</param>
        private void InvalidateInternal(bool clearCachedBounds)
        {
            if(clearCachedBounds)
                misspelledWords.ForEach(m => m.ActualBounds = Rect.Empty);

            // Fire and forget
            Task.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.InvalidateVisual();
            }).Forget();
        }

        /// <summary>
        /// Get the top level control for the given control
        /// </summary>
        /// <param name="control">The control for which to get the top level control</param>
        /// <returns>The top level control or null if there isn't one</returns>
        private static DependencyObject GetTopLevelControl(DependencyObject control)
        {
            DependencyObject current = VisualTreeHelper.GetParent(control);
            DependencyObject parent = null;

            while(current != null)
            {
                parent = current;
                current = VisualTreeHelper.GetParent(current);
            }

            return parent;
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect controlBounds, wordBounds, startRect, endRect;

            if(misspelledWords.Count != 0 && textBox.LineCount != 0 && GetTopLevelControl(textBox) is Visual topLevelControl)
                try
                {
                    controlBounds = textBox.TransformToVisual(topLevelControl).TransformBounds(
                        LayoutInformation.GetLayoutSlot(textBox));

                    // If the scroll bars are visible, shrink the control bounds to avoid drawing over them
                    var scrollViewer = GetDescendants(textBox).OfType<ScrollViewer>().First();

                    if(scrollViewer != null)
                    {
                        if(scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                            controlBounds.Width -= SystemParameters.VerticalScrollBarWidth + 1;

                        if(scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                            controlBounds.Height -= SystemParameters.HorizontalScrollBarHeight + 1;
                    }

                    // Limit what's drawn to what's visible to improve performance in text boxes with lots of
                    // content.  We also cache the computed bounds below.
                    int nearestIndex = textBox.GetCharacterIndexFromLineIndex(textBox.GetFirstVisibleLineIndex());

                    foreach(var word in misspelledWords)
                    {
                        if(word.Span.Start < nearestIndex)
                            continue;

                        if(word.ActualBounds == Rect.Empty)
                        {
                            if(word.Span.Start + word.Span.Length > textBox.Text.Length)
                                continue;

                            startRect = textBox.GetRectFromCharacterIndex(word.Span.Start);
                            endRect = textBox.GetRectFromCharacterIndex(word.Span.Start + word.Span.Length);

                            word.ActualBounds = new Rect(startRect.X + textBox.HorizontalOffset,
                                startRect.Y + textBox.VerticalOffset, Math.Max(endRect.X - startRect.X, 0),
                                endRect.Height);
                        }

                        wordBounds = word.ActualBounds;
                        wordBounds.Offset(controlBounds.X - textBox.HorizontalOffset,
                            controlBounds.Y - textBox.VerticalOffset);

                        if(wordBounds.Y > controlBounds.Y + controlBounds.Height)
                            break;

                        if(word.ActualBounds.Width > 0 && controlBounds.Contains(wordBounds.BottomLeft))
                            drawingContext.DrawLine(pen,
                                new Point(word.ActualBounds.Left - textBox.HorizontalOffset,
                                    Math.Min((word.ActualBounds.Y + word.ActualBounds.Height - textBox.VerticalOffset),
                                    controlBounds.Height)),
                                new Point(Math.Min(word.ActualBounds.Right - textBox.HorizontalOffset, controlBounds.Width),
                                    Math.Min((word.ActualBounds.Y + word.ActualBounds.Height - textBox.VerticalOffset),
                                    controlBounds.Height)));
                    }
                }
                catch(Exception ex)
                {
                    // If we get any out of bounds errors, just ignore them
                    System.Diagnostics.Debug.WriteLine(ex);
                }
        }

        /// <summary>
        /// This is used to return the descendants of the given parent object
        /// </summary>
        /// <param name="parent">The parent for which to get descendants</param>
        /// <returns>An enumerable list of the parent objects descendants and their descendants recursively</returns>
        private static IEnumerable<DependencyObject> GetDescendants(DependencyObject parent)
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                yield return child;

                foreach(var descendant in GetDescendants(child))
                    yield return descendant;
            }
        }
        #endregion

        #region Event handlers
        //=====================================================================

        /// <summary>
        /// This is used to add or remove the adorner from the adornment layer when the text box visibility
        /// changes.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        /// <remarks>This is necessary as we're dependent upon an adornment layer in Visual Studio.  In tool
        /// windows, it appears to change as you dock and undock the tool window.  The only reliable way to keep
        /// the adornments visible is to remove this from the current layer when the text box is made invisible
        /// and add it back when it becomes visible again.</remarks>
        private void textBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if(textBox.IsVisible)
                {
                    this.Visibility = Visibility.Visible;

                    var al = this.GetAdornerLayer();

                    if(al != null)
                        al.Remove(this);

                    al = AdornerLayer.GetAdornerLayer(textBox);

                    if(al != null)
                    {
                        al.Add(this);

                        this.InvalidateInternal(true);
                    }
                }
                else
                {
                    this.Visibility = Visibility.Hidden;

                    var al = this.GetAdornerLayer();

                    if(al != null)
                        al.Remove(this);
                }
            }
            catch(Exception ex)
            {
                // Ignore errors, it'll just not show the adornments
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Invalidate the visual but keep the cached locations when the text box content is scrolled
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void textBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.InvalidateInternal(false);
        }

        /// <summary>
        /// Invalidate the visual and clear the cached locations when the textbox size changes
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void textBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.InvalidateInternal(true);
        }
        #endregion
    }
}
