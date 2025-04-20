//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : SpellCheckSpan.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 04/23/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains the class used to define a spell checked span of text for the code analyzer
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/25/2023  EFW  Created the code
//===============================================================================================================

using System;

using Microsoft.CodeAnalysis.Text;

namespace VisualStudio.SpellChecker.CodeAnalyzer
{
    /// <summary>
    /// This class is used to define a spell checked span of text for the code analyzer
    /// </summary>
    public sealed class SpellCheckSpan
    {
        /// <summary>
        /// This is used to get or set the spell checked text span that defines its location
        /// </summary>
        public TextSpan TextSpan { get; set; }

        /// <summary>
        /// This is used to get or set the span type
        /// </summary>
        public SpellCheckType SpanType { get; set; }

        /// <summary>
        /// This is used to get or set the span subtype if applicable
        /// </summary>
        public SpellCheckType SpanSubtype { get; set; }

        /// <summary>
        /// This is used to get the span text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="textSpan">The text span</param>
        /// <param name="spanType">The span type</param>
        /// <param name="text">The text to spell check</param>
        /// <exception cref="InvalidOperationException">This is thrown if a span subtype is specified for
        /// <paramref name="spanType"/>.</exception>
        /// <overloads>There are two overloads for the constructor</overloads>
        public SpellCheckSpan(TextSpan textSpan, SpellCheckType spanType, string text)
        {
            if(spanType > SpellCheckType.AttributeValue)
                throw new InvalidOperationException("Span type must be less than or equal to AttributeValue");

            this.TextSpan = textSpan;
            this.SpanType = spanType;
            this.Text = text;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="textSpan">The text span</param>
        /// <param name="spanType">The span type</param>
        /// <param name="spanSubtype">The span subtype</param>
        /// <param name="text">The text to spell check</param>
        /// <exception cref="InvalidOperationException">This is thrown if a span type is specified for
        /// <paramref name="spanSubtype"/>.</exception>
        public SpellCheckSpan(TextSpan textSpan, SpellCheckType spanType, SpellCheckType spanSubtype,
          string text) : this(textSpan, spanType, text)
        {
            if(spanSubtype <= SpellCheckType.AttributeValue)
                throw new InvalidOperationException("Span subtype must be greater than AttributeValue");

            this.SpanSubtype = spanSubtype;
        }
    }
}
