﻿//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : WordSplitter.cs
// Authors : Noah Richards, Roman Golovin, Michael Lehenbauer, Eric Woodruff
// Updated : 08/09/2018
// Note    : Copyright 2010-2018, Microsoft Corporation, All rights reserved
//           Portions Copyright 2013-2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that handles splitting spans of text up into individual words for spell checking
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 09/02/2015  EFW  Moved the word splitting code into its own class for use in project spell checking as well
// 10/27/2015  EFW  Added support for ignoring mnemonics
//===============================================================================================================

// Ignore spelling: Za tp sp nis ta ttest ransform xh uhhhh Uhhhhhhhh xh hh

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.Text;

using VisualStudio.SpellChecker.Configuration;
using VisualStudio.SpellChecker.ProjectSpellCheck;

namespace VisualStudio.SpellChecker
{
    /// <summary>
    /// This class is used to split spans of text up into individual words for spell checking
    /// </summary>
    internal sealed class WordSplitter
    {
        #region Private data members
        //=====================================================================

        // Regular expressions used to find things that look like XML elements and URLs
        internal static Regex XmlElement = new Regex(@"<[A-Za-z/]+?.*?>");

        internal static Regex Url = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-z]([-.\w]*[0-9a-z])*(:(0-9)*)*(\/?)" +
            @"([a-z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?", RegexOptions.IgnoreCase);

        private char mnemonic;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// The spell checker configuration to use for splitting text into words
        /// </summary>
        public SpellCheckerConfiguration Configuration { get; set; }

        /// <summary>
        /// The range classification
        /// </summary>
        /// <remarks>This is used during solution/project spell checking to change how certain parts of the
        /// word splitting process are handled.</remarks>
        public RangeClassification Classification { get; set; }

        /// <summary>
        /// This is used to get or set whether the file contains C-style code
        /// </summary>
        /// <remarks>This affects whether or not escape sequence checking will be performed</remarks>
        public bool IsCStyleCode { get; set; }

        /// <summary>
        /// The mnemonic character
        /// </summary>
        public char Mnemonic
        {
            get => mnemonic;
            set
            {
                if(value != '&' && value != '_')
                    value = '&';

                mnemonic = value;
            }
        }

        /// <summary>
        /// This is used to determine whether or not the word splitter detects words that appear to span
        /// string literals.
        /// </summary>
        /// <value>False by default.  If set to true, the word splitter will try to detect words that appear
        /// to span string literals (i.e. "A string with a sp" + "lit word").  The span returned will include
        /// the concatenating text.  The caller is responsible for removing it before spell checking the word.
        /// This can be done with the <see cref="ActualWord"/> method.</value>
        public bool DetectWordsSpanningStringLiterals { get; set; }

        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        public WordSplitter()
        {
            mnemonic = '&';
        }
        #endregion

        #region Helper methods
        //=====================================================================

        /// <summary>
        /// Get all words in the specified text string
        /// </summary>
        /// <param name="text">The text to split into words</param>
        /// <returns>An enumerable list of word spans</returns>
        internal IEnumerable<Span> GetWordsInText(string text)
        {
            if(String.IsNullOrWhiteSpace(text))
                yield break;

            for(int i = 0, end = 0; i < text.Length; i++)
            {
                // Skip escape sequences.  If not, they can end up as part of the word or cause words to be
                // missed.  For example, "This\r\nis\ta\ttest \x22missing\x22" would incorrectly yield "nis",
                // "ta", and "ttest" and incorrectly exclude "missing".  This can cause the occasional false
                // positive in file paths (i.e. \Folder\transform\File.txt flags "ransform" as a misspelled word
                // because of the lowercase "t" following the backslash) but I can live with that.  If they are
                // common enough, they can be added to the configuration's ignored word list as an escaped word.
                if(text[i] == '\\')
                {
                    end = i + 1;

                    if(end < text.Length)
                    {
                        // Skip escaped words.  Only need to check the escape sequence letters.
                        switch(text[end])
                        {
                            case 'a':   // BEL
                            case 'b':   // BS
                            case 'f':   // FF
                            case 'n':   // LF
                            case 'r':   // CR
                            case 't':   // TAB
                            case 'v':   // VT
                            case 'x':   // Hex value
                            case 'u':   // Unicode value
                            case 'U':
                                {
                                    // Find the end of the word
                                    int wordEnd = end;

                                    while(++wordEnd < text.Length && !this.IsWordBreakCharacter(text[wordEnd], true))
                                        ;

                                    if(this.Configuration.ShouldIgnoreWord(text.Substring(end - 1, --wordEnd - i + 1)))
                                    {
                                        i = wordEnd;
                                        continue;
                                    }

                                    break;
                                }
                        }

                        switch(this.Classification)
                        {
                            // For these classifications, skip further checking.  These aren't likely to contain
                            // escape sequences that need skipping.  Note that we only get a classification when
                            // doing solution/project spell checking.
                            case RangeClassification.PlainText:
                            case RangeClassification.XmlFileComment:
                            case RangeClassification.XmlFileCData:
                            case RangeClassification.AttributeValue:
                            case RangeClassification.InnerText:
                            case RangeClassification.VerbatimStringLiteral:
                            case RangeClassification.RegionDirective:
                                continue;

                            default:
                                // For all others, base it on whether or not this is C-style code
                                if(!this.IsCStyleCode)
                                    continue;

                                // If it looks like an verbatim string, skip it too
                                if((text.Length > 2 && (text[0] == '@' || text[0] == 'R') && text[1] == '"') ||
                                  (text.Length > 3 && ((text[0] == '$' && text[1] == '@' && text[2] == '"') ||
                                  (text[0] == '@' && text[1] == '$' && text[2] == '"'))))
                                    continue;
                                break;
                        }

                        // Escape sequences
                        switch(text[end])
                        {
                            case '\'':
                            case '\"':
                            case '\\':
                            case '?':   // Anti-Trigraph
                            case '0':   // NUL or Octal
                            case 'a':   // BEL
                            case 'b':   // BS
                            case 'f':   // FF
                            case 'n':   // LF
                            case 'r':   // CR
                            case 't':   // TAB
                            case 'v':   // VT
                                i++;
                                break;

                            case 'x':   // xh[h[h[h]]]
                                while(++end < text.Length && (end - i) < 6 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                if((--end - i) > 1)
                                    i = end;
                                break;

                            case 'u':   // uhhhh
                                while(++end < text.Length && (end - i) < 6 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                if((--end - i) == 5)
                                    i = end;
                                break;

                            case 'U':   // Uhhhhhhhh
                                while(++end < text.Length && (end - i) < 10 && (Char.IsDigit(text[end]) ||
                                  (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                    ;

                                if((--end - i) == 9)
                                    i = end;
                                break;

                            default:
                                break;
                        }
                    }

                    continue;
                }

                // Skip XML entities
                if(text[i] == '&')
                {
                    end = i + 1;

                    if(end < text.Length && text[end] == '#')
                    {
                        // Numeric Reference &#n[n][n][n];
                        while(++end < text.Length && (end - i) < 7 && Char.IsDigit(text[end]))
                            ;

                        // Hexadecimal Reference &#xh[h][h][h];
                        if(end < text.Length && text[end] == 'x')
                        {
                            while(++end < text.Length && (end - i) < 8 && (Char.IsDigit(text[end]) ||
                              (Char.ToLower(text[end]) >= 'a' && Char.ToLower(text[end]) <= 'f')))
                                ;
                        }

                        // Check for entity closer
                        if(end < text.Length && text[end] == ';')
                            i = end;
                    }

                    continue;
                }

                // Skip .NET format string specifiers if so indicated.  This ignores stuff like date formats
                // such as "{0:MM/dd/yyyy hh:nn tt}".
                if(text[i] == '{' && this.Configuration.IgnoreFormatSpecifiers)
                {
                    end = i + 1;

                    if(i > 0 && ((text.Length > 2 && text[0] == '$' && text[1] == '"') ||
                      (text.Length > 3 && ((text[0] == '$' && text[1] == '@' && text[2] == '"') ||
                      (text[0] == '@' && text[1] == '$' && text[2] == '"')))))
                    {
                        // Interpolated string: $"{Property}" or $@"\{Property}\".  Find the end accounting for
                        // escaped braces.
                        while(++end < text.Length)
                            if(text[end] == '}')
                            {
                                if(end + 1 == text.Length || text[end + 1] != '}')
                                    break;

                                end++;
                            }
                    }
                    else
                        while(end < text.Length && Char.IsDigit(text[end]))
                            end++;

                    if(end < text.Length && text[end] == ':')
                    {
                        // Find the end accounting for escaped braces
                        while(++end < text.Length)
                            if(text[end] == '}')
                            {
                                if(end + 1 == text.Length || text[end + 1] != '}')
                                    break;

                                end++;
                            }
                    }

                    if(end < text.Length && text[end] == '}')
                        i = end;

                    continue;
                }

                // Skip C-style format string specifiers if so indicated.  These can cause spelling errors in
                // cases where there are multiple characters such as "%ld".  My C/C++ skills are very rusty but
                // this should cover it.
                if(text[i] == '%' && this.Configuration.IgnoreFormatSpecifiers)
                {
                    end = i + 1;

                    if(end < text.Length)
                    {
                        // Flags
                        switch(text[end])
                        {
                            // NOTE: A space is also a valid flag character but we can't tell if it's part of
                            // the format or just a percentage followed by a word without some lookahead which
                            // probably isn't worth the effort (i.e. "% i" vs "100% stuff").  As such, the space
                            // flag character is not included here.
                            case '-':
                            case '+':
                            case '#':
                            case '0':
                                end++;
                                break;

                            default:
                                break;
                        }

                        // Width and precision not accounting for validity to keep it simple
                        while(end < text.Length && (Char.IsDigit(text[end]) || text[end] == '.' || text[end] == '*'))
                            end++;

                        if(end < text.Length)
                        {
                            // Length
                            switch(text[end])
                            {
                                case 'h':
                                case 'l':
                                    end++;

                                    // Check for "hh" and "ll"
                                    if(end < text.Length && text[end] == text[end - 1])
                                        end++;
                                    break;

                                case 'j':
                                case 'z':
                                case 't':
                                case 'L':
                                    end++;
                                    break;

                                default:
                                    break;
                            }

                            if(end < text.Length)
                            {
                                // And finally, the specifier
                                switch(text[end])
                                {
                                    case 'd':
                                    case 'i':
                                    case 'u':
                                    case 'o':
                                    case 'x':
                                    case 'X':
                                    case 'f':
                                    case 'F':
                                    case 'e':
                                    case 'E':
                                    case 'g':
                                    case 'G':
                                    case 'a':
                                    case 'A':
                                    case 'c':
                                    case 's':
                                    case 'p':
                                    case 'n':
                                        i = end;
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    continue;
                }

                // Skip word separator
                if(this.IsWordBreakCharacter(text[i], true))
                    continue;

                // Find the end of the word
                end = i;

                while(++end < text.Length && !this.IsWordBreakCharacter(text[end], false))
                    ;

                // Special case if ignoring mnemonics.  If the word ends in what looks like an XML entity, break
                // it before the entity (i.e. "Caption&gt;" will only return "Caption").
                if(mnemonic == '&' && end < text.Length && text[end] == ';' && this.Configuration.IgnoreMnemonics)
                {
                    int tempEnd = end;

                    while(--tempEnd > i)
                        if(text[tempEnd] == '&')
                        {
                            end = tempEnd;
                            break;
                        }
                }

                // Skip XML entity reference &[name];
                if(end < text.Length && i > 0 && text[i - 1] == '&' && text[end] == ';')
                {
                    i = end;
                    continue;
                }

                // Skip leading apostrophes
                while(i < end && (text[i] == '\'' || text[i] == '\u2019'))
                    i++;

                // Skip trailing apostrophes, periods, at-signs, and mnemonics
                while(--end > i && (text[end] == '\'' || text[end] == '\u2019' || text[end] == '.' ||
                  text[end] == '@' || text[end] == mnemonic))
                    ;

                end++;    // Move back to last match

                if(this.DetectWordsSpanningStringLiterals && end < text.Length && text[end] == '\"' &&
                  this.Classification.IsStringLiteral())
                {
                    int spanEnd = end + 1;
                    bool concatSeen = false;

                    while(spanEnd < text.Length && (text[spanEnd] == '+' || text[spanEnd] == '&' ||
                      text[spanEnd] == '@' || text[spanEnd] == '$' || text[spanEnd] == 'R' ||
                      text[spanEnd] == '_' || Char.IsWhiteSpace(text[spanEnd])))
                    {
                        if((text[spanEnd] == '@' || text[spanEnd] == '$' || text[spanEnd] == 'R') &&
                          (spanEnd + 1 >= text.Length || (text[spanEnd + 1] != '\"' && text[spanEnd + 1] != '@' &&
                          text[spanEnd + 1] != '$')))
                        {
                            break;
                        }

                        if(text[spanEnd] == '+' || text[spanEnd] == '&')
                            concatSeen = true;

                        spanEnd++;
                    }

                    if(concatSeen && spanEnd + 1 < text.Length && text[spanEnd] == '\"' &&
                      !this.IsWordBreakCharacter(text[spanEnd + 1], false))
                    {
                        spanEnd++;

                        while(spanEnd < text.Length && !this.IsWordBreakCharacter(text[spanEnd], false))
                            spanEnd++;

                        if(spanEnd < text.Length)
                        {
                            while(--spanEnd > i && (text[spanEnd] == '\'' || text[spanEnd] == '\u2019' ||
                              text[spanEnd] == '.' || text[spanEnd] == '@' || text[spanEnd] == mnemonic))
                                ;

                            end = spanEnd + 1;
                        }
                    }
                }

                // Ignore empty strings and single characters
                if(end - i > 1)
                {
                    if(this.Configuration.IgnoreWordsInMixedCase)
                    {
                        yield return Span.FromBounds(i, end);
                    }
                    else
                    {
                        // If spell checking mixed/camel case words, see if this is one
                        string word = text.Substring(i, end - i);

                        if(Char.IsLetter(word[0]) && word.Skip(1).Any(c => Char.IsUpper(c)))
                        {
                            // An exception is if it appears in the code analysis dictionary options.  These may
                            // be camel cased but the user wants them replaced with something else.  Another
                            // exception is if the word contains a special word break character which will
                            // cause the word as a whole to be ignored when the related option is enabled/disabled.
                            if((this.Configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                              this.Configuration.DeprecatedTerms.ContainsKey(word)) ||
                              (this.Configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                              this.Configuration.CompoundTerms.ContainsKey(word)) ||
                              word.IndexOfAny(new[] { '_', '.', '@' }) != -1)
                            {
                                yield return Span.FromBounds(i, end);
                            }
                            else
                            {
                                int split = i;

                                while(split < end)
                                {
                                    // Skip consecutive uppercase letters (i.e NHunSpell).  This may not always
                                    // be accurate but it's the best we can do.
                                    while(split + 1 < end && Char.IsUpper(text[split + 1]))
                                        split++;

                                    i = split;
                                    split++;

                                    while(split < end && !Char.IsUpper(text[split]))
                                        split++;

                                    // A common occurrence is a final uppercase letter followed by 's' such as
                                    // IDs or GUIDs.  Ignore those.
                                    if(split - i == 2 && Char.IsUpper(text[i]) && text[i + 1] == 's')
                                        i = split;

                                    if(split - i > 1)
                                        yield return Span.FromBounds(i, split);
                                }
                            }
                        }
                        else
                            yield return Span.FromBounds(i, end);
                    }
                }

                i = --end;
            }
        }

        /// <summary>
        /// See if the specified character is a word break character
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <param name="includingMnemonic">True to also include the mnemonic character as a word break or false
        /// to ignore it.</param>
        /// <returns>True if the character is a word break, false if not</returns>
        private bool IsWordBreakCharacter(char c, bool includingMnemonic)
        {
            if(c == mnemonic)
                return (!this.Configuration.IgnoreMnemonics || includingMnemonic);

            switch(c)
            {
                case '\'':      // These could be apostrophes
                case '\u2019':
                    return false;

                case '.':
                case '@':
                    return !this.Configuration.IgnoreFilenamesAndEMailAddresses;

                case '_':
                    return this.Configuration.TreatUnderscoreAsSeparator;

                default:
                    return (Char.IsWhiteSpace(c) || Char.IsPunctuation(c) || Char.IsSymbol(c) ||
                        Char.IsControl(c));
            }
        }

        /// <summary>
        /// Determine if a word is probably a real word
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if it appears to be a real word or false if any of the following conditions are met:
        ///
        /// <list type="bullet">
        ///     <description>The word contains a period or an at-sign (it looks like a filename or an e-mail
        /// address) and those words are being ignored.  We may miss a few real misspellings in this case due
        /// to a missed space after a period, but that's acceptable.</description>
        ///     <description>The word contains an underscore and underscores are not being treated as
        /// separators.</description>
        ///     <description>The word contains a digit and words with digits are being ignored.</description>
        ///     <description>The word is composed entirely of digits when words with digits are not being
        /// ignored.</description>
        ///     <description>The word is in all uppercase and words in all uppercase are being ignored.</description>
        ///     <description>The word is camel cased.</description>
        /// </list>
        /// </returns>
        internal bool IsProbablyARealWord(string word)
        {
            if(String.IsNullOrWhiteSpace(word))
                return false;

            word = word.Trim();

            // Check for a period or an at-sign in the word (things that look like filenames and e-mail addresses)
            if(word.IndexOfAny(new[] { '.', '@' }) >= 0)
                return false;

            // Check for underscores and digits
            if(word.Any(c => (c == '_' && mnemonic != '_') || (Char.IsDigit(c) && this.Configuration.IgnoreWordsWithDigits)))
                return false;

            // Ignore if all digits (this only happens if the Ignore Words With Digits option is false)
            if(!word.Any(c => Char.IsLetter(c) && c != mnemonic))
                return false;

            // Ignore if all uppercase, accounting for apostrophes, digits, and mnemonics
            if(word.All(c => Char.IsUpper(c) || !Char.IsLetter(c)))
                return !this.Configuration.IgnoreWordsInAllUppercase;

            // Ignore if mixed/camel cased
            if(Char.IsLetter(word[0]) && word.Skip(1).Any(c => Char.IsUpper(c)))
            {
                // An exception is if it appears in the code analysis dictionary options.  These may be camel
                // cased but the user wants them replaced with something else.
                if((this.Configuration.CadOptions.TreatDeprecatedTermsAsMisspelled &&
                  this.Configuration.DeprecatedTerms.ContainsKey(word)) ||
                  (this.Configuration.CadOptions.TreatCompoundTermsAsMisspelled &&
                  this.Configuration.CompoundTerms.ContainsKey(word)))
                    return true;

                return false;
            }

            // Ignore by character class.  A rather simplistic way to ignore some foreign language words in files
            // with mixed English/non-English text.
            if(this.Configuration.IgnoreCharacterClass != IgnoredCharacterClass.None)
            {
                if(this.Configuration.IgnoreCharacterClass == IgnoredCharacterClass.NonAscii && word.Any(c => c > '\x07F'))
                    return false;

                if(this.Configuration.IgnoreCharacterClass == IgnoredCharacterClass.NonLatin && word.Any(c => c > '\x0FF'))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// This is used to get the actual word from a span excluding any concatenating text from words that
        /// span string literals.
        /// </summary>
        /// <param name="containingText">The text containing the word span</param>
        /// <param name="wordSpan">The word span location</param>
        /// <returns>The word at the given span location without any concatenation text that may be within it</returns>
        internal static string ActualWord(string containingText, Span wordSpan)
        {
            string word = containingText.Substring(wordSpan.Start, wordSpan.Length);

            int concatPos = word.IndexOf('\"');

            if(concatPos != -1)
            {
                int end = concatPos + 1;

                while(end < word.Length && word[end] != '\"')
                    end++;

                if(end < word.Length - 1)
                    word = word.Substring(0, concatPos) + word.Substring(end + 1);
                else
                    word = word.Substring(0, concatPos);
            }

            return word;
        }
        #endregion
    }
}
