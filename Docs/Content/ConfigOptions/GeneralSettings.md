---
uid: b4a8726f-5bee-48a4-81a9-00b1be332607
alt-uid: GeneralSettings
title: General Settings
keywords: "configuration, general settings"
---
<autoOutline excludeRelatedTopics="true" lead="This category contains general settings." />

## General
- **Spell check as you type** - This enables and disables the spell checker.  Naturally, it is enabled by
  default. The spell checker tool window relies on this being enabled.  If disabled, spell checking will be
  unavailable until it is turned back on.

  ``` none{title=" "}
  vsspell_spell_check_as_you_type = [true|false]
  ```
- **Include when performing solution/project spell checking** - This option is available in all configurations
  except the global configuration.  If set to Yes or Inherited (from the global configuration), it will include
  the related files in the solution/project spell checking process.  If set to No, the related files will be
  excluded from the process.  For example, this offers a convenient way of excluding an entire folder of files
  from being spell checked as part of the solution/project as it does with the option above for interactive spell
  checking.

  ``` none{title=" "}
  vsspell_include_in_project_spell_check = [true|false]
  ```
- **Enable the identifier spell checking code analyzer in supported languages** - This option enables the spell
  checking code analyzer and is enabled by default.  It will spell check identifiers in all supported languages
  (currently only C# but support for Visual Basic is planned for later).  Unlike comment and string misspellings,
  identifier misspellings will be highlighted using the same colored underline as other code fixes.  Fixing a
  misspelling in an identifier will automatically correct it in all other references to it.

  ``` none{title=" "}
  vsspell_code_analyzers_enabled = [true|false]
  ```
- **Detect doubled words** - This will cause the spell checker to report instances of doubled words.  An option
  to remove the duplicate word or ignore it will be offered as the options to correct any instances that are
  found.  The default is enabled.

  ``` none{title=" "}
  vsspell_detect_doubled_words = [true|false]
  ```
- **Ignore words with digits** - This will cause the spell checker to ignore any words containing digits.  The
  default is enabled.

  ``` none{title=" "}
  vsspell_ignore_words_with_digits = [true|false]
  ```
- **Ignore words in all uppercase** - This will cause the spell checker to ignore any words consisting of all
  uppercase letters.  The default is enabled.

  ``` none{title=" "}
  vsspell_ignore_words_in_all_uppercase = [true|false]
  ```
- **Ignore words in mixed case** - This will cause the spell checker to ignore any words consisting of
  mixed/camel case letters.  The default is enabled.  Note that if disabled, it may result in a large number of
  false reports of misspellings especially on control prefixes such as `btn` and `txt`.  These can be added as
  ignored words to suppress them.  Disabling this option may also result in false reports of doubled words where
  one of the words is part of a mixed case word preceded or followed by another occurrence of the same word.

  Mixed case words that also include an underscore, period, or at sign will not be included for spell checking
  even if this option is disabled based on the option related to the special word break character's setting
  (treat underscores as separators is disabled, ignore words that look like filenames/e-mail addresses is
  enabled)

  ``` none{title=" "}
  vsspell_ignore_words_in_mixed_case = [true|false]
  ```
- **Ignore .NET and C-style format string specifiers** - This will cause the spell checker to skip text in .NET
  and C-style format specifiers (i.e. `{0:MM/dd/yyyy}` and `%ld`).  This prevents the text within them from
  showing up as misspelled words.  The default is enabled.

  ``` none{title=" "}
  vsspell_ignore_format_specifiers = [true|false]
  ```
- **Ignore words that look like filenames and e-mail addresses** - This will cause the spell checker to ignore
  words that contain periods and at signs with no intervening whitespace (i.e. *Userinfo.config* or
  *auser@mydomain.com*).  This option can occasionally cause a misspelled word to be missed such as when a space
  is missing following the period in a sentence.  However, it excludes far more false reports and is enabled by
  default.

  ``` none{title=" "}
  vsspell_ignore_filenames_and_email_addresses = [true|false]
  ```
- **Ignore words that look like XML elements in spell checked text** - This will cause the spell checker to
  ignore words within angle brackets in spell checked text (i.e. "The `<para>` element creates a paragraph").
  This option can occasionally cause a misspelled word to be missed such as when a space is missing following
  the opening angle bracket.  However, it excludes far more false reports and is enabled by default.

  ``` none{title=" "}
  vsspell_ignore_xml_elements_in_text = [true|false]
  ```
- **Treat underscores as separators** - This option is disabled by default and all words containing underscores
  will be ignored.  Enabling this option will treat the underscore as a word separator and each word separated
  by the underscores will be spell checked along with all the other text.

  ``` none{title=" "}
  vsspell_treat_underscore_as_separator = [true|false]
  ```
- **Ignore mnemonics within words** - This option is enabled by default.  It causes the spell checker to ignore
  the mnemonic character (ampersand) within words being spell checked rather than treating it as a word separator
  thus causing the text before and after it to be treated as separate words and possibly reporting them as
  misspellings.

  If a misspelled word contains a mnemonic, it will be reported with the mnemonic and each suggested replacement
  will have a matching mnemonic if it contains a matching letter.  This allows you to see where the mnemonic is
  placed or if it will be lost in the replacement.

  ``` none{title=" "}
  vsspell_ignore_mnemonics = [true|false]
  ```

## Ignored Character Class
This option provides a simplistic way of ignoring non-English words containing specific classes of characters.
It works best when spell checking English text in files containing Cyrillic or Asian text.  The default is
**Include all words** so that all words are included regardless of the characters they contain.  It can be set
to **Ignore words containing non-Latin characters** to ignore words containing characters above 0xFF or it can
be set to **Ignore words containing non-ASCII characters** to ignore words containing characters above 0x7F.

``` none{title=" "}
vsspell_ignored_character_class = [None|NonLatin|NonAscii]
```

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
