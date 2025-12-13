---
uid: c592c4d8-7387-47fe-9b79-28bf0168f447
alt-uid: IgnoredWords
title: Ignored Words/Keywords
keywords: "configuration, ignored words", "configuration, ignored keywords"
---

This category lets you define ignored words and keywords that are never presented as misspelled words
nor will they appear as suggestions for other misspelled words.  This is useful for excluding things such as
abbreviations, programming terms, etc.  Ignored keywords differ from ignored words in that they are always
inherited across all configuration files and cannot be cleared by a later configuration file.  Ignored keywords
are typically defined in the global configuration file for the file set to which they apply.


The ignored words list is stored within the configuration file.  The ignored words file is a
standalone text file (one word per line) that allows for an alternate method of managing ignored words.  Words
can be added to this file from the spelling suggestion context menu and from the spell checking tool windows.



## Ignored Words File

If an ignored words file is specified, words in it are added to the ignored word list in the
configuration file.  The global configuration uses a default file called *IgnoredWords.dic*
which will be stored in the same location as the global configuration file.  Click the "**...**" button to
select the location and name of the ignored words file.  This is the preferred way of managing ignored words.
Words can be added to this file while spell checking.


A relative path is considered to be relative to the configuration file's location.  For the global
configuration, the path should always be fully qualified.  For other .editorconfig settings files, the path
should typically be relative so that if the solution or project moves, the ignored words file can still be found.
Environment variable references such as `%USERPROFILE%\OneDrive\Dictionaries`
are supported.  This is useful if you want to store the ignored words file in a location that you can back up
such as your My Documents folder.  Ignored words files in solutions and projects can be added to source control.



## Ignored Words List

To add new words, enter one or more words in the text box and click the **Add** button.
Escaped words are a special class of ignored words.  These are words that start with what looks
like a valid escape sequence (`\a`, `\t`, etc.) but the remainder
of the word should not appear as a misspelled word.  Escaped words can only start with one of the following
sequences: `\a \b \f \n \r \t \v \x \u \U`.  A backslash before any other letter will be
removed and the word will be added without it.  The default global configuration includes Doxygen tags that fit
this category.


To remove a word, select it in the list and click the **Remove** button.  To reset the list to
the default set of ignored words, click the **Default** button.


For non-global settings, an option is available to inherit the ignored words lists from
configurations above the current one.  If enabled, any additional ignored words in the current configuration are
added to those.  If disabled, the settings in the current configuration will replace the inherited list of
ignored words.  If not inherited and the list is left empty, it effectively clears the list of ignored words.


When the inherited option is enabled, the **Default** button clears the list of ignored words.
If not inherited, it will set the list to the same one used in the global configuration.


The **Import** button can be used to import ignored words from a custom dictionary file.  The
**Export** button can be used to export the ignored words to a custom dictionary file for sharing.  Words
can be imported from text files, XML user dictionary files used by code analysis and StyleCop, and from StyleCop
settings files. Words can be exported to text files or XML user dictionary files.  When importing words from an
XML user dictionary file, only words without a `Spelling` attribute or ones on which it is
set to `Ignore` will be imported.  When exported, the `Spelling`
attribute is set to `Ignore` for any words added to or updated in the file.  When
importing or exporting words, you will be asked whether you want to replace the list of words or merge them with
the existing words.  Escaped words (those starting with a backslash) will not be exported to XML user dictionary
files as they do not support escaped words.


``` none{title=" "}
vsspell_ignored_words_[sectionId] = [clear_inherited]|[File:ignoredWordsFilePath]|[word1|word2|...]

sectionId = The unique ID for the section.
clear_inherited = If specified, clear all prior values and use only the settings in this property.  If omitted,
prior values from other property instances are inherited.
File:ignoredWordsFilePath = If a value is prefixed with "File:", the value after the colon is assumed to be a
path to the ignored words file.
word1|word2|... = A pipe-separated list of words to ignore.
				
```


## Ignored Keywords List

The ignored keywords list works the same as the ignored words list above.  It does not have an
ignored keywords file counterpart though.  As noted above, ignored keywords are always inherited across all
configuration files and cannot be cleared by a later configuration file.  They are typically defined in the
global configuration file for the file set to which they apply.  The default configuration contains several sets
of ignored keywords for various languages.


``` none{title=" "}
vsspell_ignored_keywords_[sectionID] = [keyword1|keyword2|...]

sectionId = The unique ID for the section.
keyword1|keyword2|... = A pipe-separated list of keywords to ignore.
				
```


## See Also


**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
[](@3094ee74-88ae-4355-b702-23dcd55b4197)  
