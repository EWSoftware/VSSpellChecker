﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : RegexClassifier.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/02/2018
// Note    : Copyright 2015-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to classify text file content using a set of regular expressions
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/26/2015  EFW  Created the code
//===============================================================================================================

// Ignore spelling: separ ated

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;

namespace VisualStudio.SpellChecker.ProjectSpellCheck
{
    /// <summary>
    /// This class is used to classify text using a set of regular expressions
    /// </summary>
    internal class RegexClassifier : TextClassifier
    {
        #region Private data members
        //=====================================================================

        private List<RegexClassification> expressions;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property returns the classification expressions
        /// </summary>
        protected IEnumerable<RegexClassification> Expressions => expressions;

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// 
        /// <param name="filename">The filename to load</param>
        /// <param name="spellCheckConfiguration">The spell checker configuration for the file</param>
        /// <param name="classifierConfiguration">The configuration element containing the classification
        /// expressions and their range types.</param>
        public RegexClassifier(string filename, SpellCheckerConfiguration spellCheckConfiguration,
          XElement classifierConfiguration) : base(filename, spellCheckConfiguration)
        {
            string expression, options;

            expressions = new List<RegexClassification>();

            if(classifierConfiguration != null)
                foreach(XElement match in classifierConfiguration.Elements("Match"))
                {
                    expression = (string)match.Attribute("Expression");
                
                    if(!String.IsNullOrWhiteSpace(expression))
                    {
                        options = (string)match.Attribute("Options");

                        if(String.IsNullOrWhiteSpace(options) || !Enum.TryParse<RegexOptions>(options, true,
                          out RegexOptions regexOptions))
                        {
                            regexOptions = RegexOptions.None;
                        }

                        if(!Enum.TryParse<RangeClassification>((string)match.Attribute("Classification"),
                          out RangeClassification classification))
                        {
                            classification = RangeClassification.PlainText;
                        }

                        try
                        {
                            // Enforce a 1 second timeout on all expressions.  If we can't get a match within
                            // that amount of time, ignore it.  This can happen on some files with odd formatting.
                            expressions.Add(new RegexClassification(new Regex(expression, regexOptions,
                                TimeSpan.FromSeconds(1)), classification));
                        }
                        catch(ArgumentException ex)
                        {
                            // Ignore invalid regular expression entries
                            System.Diagnostics.Debug.WriteLine(ex);
                        }
                    }
                }
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <inheritdoc />
        public override IEnumerable<SpellCheckSpan> Parse()
        {
            List<SpellCheckSpan> spans = new List<SpellCheckSpan>();
            SpellCheckSpan current, next;

            foreach(var rc in expressions)
            {
                try
                {
                    var matches = rc.Expression.Matches(this.Text);

                    foreach(Match m in matches)
                        spans.Add(new SpellCheckSpan
                        {
                            Span = new Span(m.Index, m.Length),
                            Text = m.Value,
                            Classification = rc.Classification
                        });
                }
                catch(RegexMatchTimeoutException ex)
                {
                    // Ignore timeouts, we just won't get anymore more matches for the failing expression
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            this.AdjustClassifications(spans);

            spans = spans.OrderBy(s => s.Span.Start).ToList();

            // Merge intersecting spans and either remove or split entirely overlapped spans
            for(int idx = 0; idx < spans.Count - 1; idx++)
            {
                current = spans[idx];
                next = spans[idx + 1];

                if(current.Span.IntersectsWith(next.Span))
                {
                    // The spans overlap.  If overlapped entirely, remove one or the other or split the
                    // containing span accordingly.  If partially overlapping, combine them and account for the
                    // overlap if they are of the same type.  If overlapped but of a different type, leave them
                    // alone except the noted special case.
                    if(current.Span.OverlapsWith(next.Span))
                    {
                        if(current.Span.Contains(next.Span))
                        {
                            if(current.Classification == RangeClassification.Undefined ||
                              next.Classification == RangeClassification.Undefined)
                            {
                                SplitSpan(spans, idx, idx + 1);
                            }
                            else
                                spans.Remove(next);

                            idx--;
                        }
                        else
                            if(next.Span.Contains(current.Span))
                            {
                                if(current.Classification == RangeClassification.Undefined ||
                                  next.Classification == RangeClassification.Undefined)
                                {
                                    SplitSpan(spans, idx + 1, idx);
                                }
                                else
                                    spans.Remove(current);

                                idx--;
                            }
                            else
                                if(current.Classification == next.Classification)
                                {
                                    current.Span = new Span(current.Span.Start, next.Span.Start + next.Span.Length -
                                        current.Span.Start - (current.Span.Start + current.Span.Length -
                                        next.Span.Start) + 2);
                                    current.Text = this.Text.Substring(current.Span.Start, current.Span.Length);

                                    spans.Remove(next);
                                    idx--;
                                }
                                else
                                    if((current.Classification == RangeClassification.NormalStringLiteral ||
                                      current.Classification == RangeClassification.VerbatimStringLiteral ||
                                      current.Classification == RangeClassification.InterpolatedStringLiteral) &&
                                      (next.Classification == RangeClassification.SingleLineComment ||
                                      next.Classification == RangeClassification.XmlDocComments ||
                                      next.Classification == RangeClassification.QuadSlashComment ||
                                      next.Classification == RangeClassification.DelimitedComments))
                                    {
                                        // Special case.  Parts of a URL, XPath query, or what looks like block
                                        // comment delimiters within a literal string may get picked up as a
                                        // comment span.  In this case, we'll ignore the incorrect comment span.
                                        spans.Remove(next);
                                        idx--;
                                    }
                    }
                    else
                        if(current.Classification == next.Classification)
                        {
                            // The spans are of the same type and are adjacent so combine them
                            current.Span = new Span(current.Span.Start, next.Span.Start + next.Span.Length -
                                current.Span.Start);
                            current.Text = this.Text.Substring(current.Span.Start, current.Span.Length);

                            spans.Remove(next);
                            idx--;
                        }
                }
                else
                    if(current.Classification.ConsecutiveStringLiterals(next.Classification))
                    {
                        // See if two string literals of the same type are being concatenated that contain what
                        // looks like a word spanning both.  This happens a lot in Windows Forms designer code.
                        // For example: "A string of text that gets separ" + "ated by the designer"
                        // This can result in a lot of false reports.  The word splitter will attempt to
                        // join such words for the purposes of spell checking them.
                        //
                        // The classifications must match.  You can't for example combine a normal string literal
                        // with a verbatim string literal or it may generate false reports on things like
                        // escape sequences in the verbatim string literal.
                        int pos = current.Span.Start + current.Span.Length, end = next.Span.Start;
                        bool concatSeen = false;

                        while(pos < end && (this.Text[pos] == '+' || this.Text[pos] == '&' ||
                          this.Text[pos] == '@' || this.Text[pos] == '$' || this.Text[pos] == 'R' ||
                          this.Text[pos] == '_' || Char.IsWhiteSpace(this.Text[pos])))
                        {
                            if((this.Text[pos] == '@' || this.Text[pos] == '$' || this.Text[pos] == 'R') &&
                              (pos + 1 >= end || (this.Text[pos + 1] != '\"' && this.Text[pos + 1] != '@' &&
                              this.Text[pos + 1] != '$')))
                            {
                                break;
                            }

                            if(this.Text[pos] == '+' || this.Text[pos] == '&')
                                concatSeen = true;

                            pos++;
                        }

                        if(concatSeen && pos == end)
                        {
                            current.Span = new Span(current.Span.Start, next.Span.Start + next.Span.Length -
                                current.Span.Start);
                            current.Text = this.Text.Substring(current.Span.Start, current.Span.Length);

                            spans.Remove(next);
                            idx--;
                        }
                    }
            }

            return spans.Where(s => !this.IgnoredClassifications.Contains(s.Classification));
        }

        /// <summary>
        /// This is called from the <see cref="Parse"/> method and can be overridden in derived classes to adjust
        /// the spans classifications as needed once they have been parsed.
        /// </summary>
        /// <param name="spans">The spans on which to adjust the classifications</param>
        /// <remarks>The default implementation does nothing.</remarks>
        protected virtual void AdjustClassifications(IEnumerable<SpellCheckSpan> spans)
        {
        }
        #endregion
    }
}
