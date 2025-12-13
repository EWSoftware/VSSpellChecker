---
uid: e8f67bc4-a8f8-4e50-ab5a-876599f3a645
alt-uid: SpellCheckAsYouType
title: Spell Check as You Type
keywords: "spell check, as you type"
---

In text editor windows, misspellings are highlighted by underlining them with a squiggly line.  Place
the mouse over the word and click the down arrow on the smart tag to see the available options.  Alternately, you
can place the cursor anywhere in the word and press **Ctrl+.** or **Shift+Alt+F10** depending on your
version of Visual Studio to show the smart tag options.


> [!NOTE]
> Spell check as you type only applies to non-source code files and only to strings and comments
> within source code files.  If enabled, identifier spell checking is handled by the code analyzer.  Identifier
> misspellings are separate and will be highlighted like other code analyzer warnings.  Such misspellings will not
> appear in the Spell Check Active Document or Solution/Project Spell Check tool windows.
> 
>

The smart tag menu will display the following options:


- A list of suggestions is shown at the top.  Selecting a word will replace the misspelling with
that word.  If you hold down the **Ctrl** key while clicking a suggestion or select one with the keyboard
and press **Ctrl+Enter**, all occurrences of the misspelled word will be replaced with the selected
suggestion.  If you are spell checking against multiple languages, the languages in which the suggestion appears
follow the word in parentheses.  Choose the suggestion you want to use based on the language(s) with which it is
associated.
- **Ignore Once** - This option allows you to ignore the given instance of the misspelled
word in the current file at the current location.  All other misspellings of the same word will still be flagged.
The given instance will be ignored as long as the file remains open.  If closed and reopened, it will be flagged
again.
- **Ignore All** - Ignore all instances of the given misspelled word for the remainder of
the Visual Studio session.  When the solution or Visual Studio is closed and reopened the ignored word will be
flagged again.
- **Add To Dictionary** - This will add the word to the user dictionary so that it is no
longer flagged as a misspelled word.  In addition, the word will be presented as a suggestion for other
misspelled words when appropriate.  Use the **Edit Configuration** option to remove words from the user
dictionary.  If spell checking against multiple dictionaries, you will see one **Add to Dictionary** option
for each available language.  Choosing an option will add the word to that language's user dictionary.
- **Add To Ignored Words File** - This will add the word to the ignored words file so that
it is no longer flagged as a misspelled word.  Ignored words will not be presented as suggestions for other
misspelled words.  Edit the ignored words file to remove words from it.  The ignored words files can be specified
in any configuration file.  The global configuration uses *IgnoredWords.dic* stored in the
same location as the global configuration file.  The configuration file editor has an option to open the file for
editing.  If ignored words files are specified in multiple configurations, you will see one **Add to Ignored
Words file** option for each available ignored words file.  Choosing an option will add the word to the
selected file.


You can add Ignore Spelling directives to a comment within a file to inform the spell checker about
words that you do not want spell checked in it.  See the [](@3094ee74-88ae-4355-b702-23dcd55b4197)
help topic form more information.



## See Also


**Other Resources**  
[](@53ffc5b7-b7dc-4f03-9a51-ed4176bff504)  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@e339cac1-9783-4c2a-919f-88436c78fef8)  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
