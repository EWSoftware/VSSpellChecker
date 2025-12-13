---
uid: 53ffc5b7-b7dc-4f03-9a51-ed4176bff504
alt-uid: SpellCheckCurrentDoc
title: Active Document Spell Check Tool Window
keywords: "spell check, active document"
---

Selecting **Tools | Spell Checker | Spell Check Current Document** opens the active document
spell checker tool window.  This tool window will only be usable when a code or text editor window is selected as
the active document window.  If a non-editor window is selected or if there are no misspellings, the buttons in
the tool window will be disabled.  Open or click on an existing editor window containing editable text to
activate the tool window options.  When there are misspellings in the active editor, it will be repositioned to
the proper location and the misspelling will be highlighted.


> [!NOTE]
> Spell check as you type only applies to non-source code files and only to strings and comments
> within source code files.  If enabled, identifier spell checking is handled by the code analyzer.  Identifier
> misspellings are separate and will be highlighted like other code analyzer warnings.  Such misspellings will not
> appear in the Spell Check Active Document or Solution/Project Spell Check tool windows.
> 
> 
> If the misspelling is within a collapsed section, an attempt is made to expand the section and show
> the word.  If this does not occur, to make it visible, click on the editor window's tab to select it and then
> expand the section containing the cursor.  When you click on the spell checker tool window, it will reposition to
> the word and highlight it as usual.
> 
>

<autoOutline lead="none" excludeRelatedTopics="true" />


## Correcting Issues

^^^
![](@ActiveDocToolWindow)
^^^


The misspelled word is shown at the top of the tool window and any suggested replacements are shown
in the list box on the left side.  The following options are available:


- **Replace** - Replace the current misspelled word with the selected suggestion.  Double
clicking a suggestion in the list will also cause the misspelled word to be replaced with the double clicked
word.
- **Replace All** - Replace all occurrences of the misspelled word with the selected
suggestion.  Holding down the **Ctrl** key when double clicking a suggestion in the list will also cause all
occurrences of the misspelled word to be replaced with the double clicked word.
- **Ignore Once** - This option allows you to ignore the given instance of the misspelled
word in the current file at the current location.  All other misspellings of the same word will still be flagged.
The given instance will be ignored as long as the file remains open.  If closed and reopened, it will be flagged
again.
- **Ignore All** - Ignore all instances of the given misspelled word for the remainder of
the Visual Studio session.  When the solution or Visual Studio is closed and reopened the ignored word will be
flagged again.
- **Add Word** - This will add the word to the user dictionary so that it is no longer
flagged as a misspelled word and will present it as a suggestion for other misspelled words when appropriate.
Use the **Edit Configuration** option to remove words from the user dictionary.  Note that this button will
be disabled for code analysis related misspellings (unrecognized word, deprecated term, and compound term) since
the suggested replacements are what should replace the word and the term should not be added to the dictionary.
- **Add Ignored** - This will add the word to the ignored words file so that it is no longer
flagged as a misspelled word.  Ignored words will not be presented as suggestions for other misspelled words.
Edit the ignored words file to remove words from it.  The ignored words files can be specified in any
configuration file.  The global configuration uses *IgnoredWords.dic* stored in the same
location as the global configuration file.  The configuration file editor has an option to open the file for
editing.  If ignored words files are specified in multiple configurations, the **Add Ignored** button will
show a dropdown menu with an option for each available ignored words file.  Choosing an option will add the word
to the selected file.  Note that this button will be disabled for code analysis related misspellings
(unrecognized word, deprecated term, and compound term) since the suggested replacements are what should replace
the word and the term should not be ignored.


As with the spell check as you type option, if you are spell checking against multiple languages,
the languages in which the suggestion appears follow the word in parentheses.  Choose the suggestion you want to
use based on the language(s) with which it is associated.  The **Add Word** button will show a dropdown menu
when spell checking against multiple languages.  Selecting an option will add the word to that language's user
dictionary.


The misspelled word displayed at the top of the tool window is editable.  You may alter it to
correct the spelling manually.  When changed, the **Undo** button to the right of the text box is enabled
and the list of suggestions is disabled.  Clicking **Undo** will undo any edits made to the word and enable
the list of suggestions.  If you do modify the word, the **Replace** and **Add Word** options will use
the modified word to perform their action.  In addition, the **Add Word** option will replace the original
misspelled word with the edited word.


Once you have taken action on a misspelled word, the tool window will be updated and will move on
to the next issue.  If there are no more, a "(No more issues)" message is displayed in the misspelled word text
box.



## Doubled Word Detection

The spell checker will also detect doubled words and highlight them.  In such cases, the smart tag
will contain an option to delete the doubled word and another to ignore it once.  In the spell checker tool
window, when a doubled word is reached the list box will contain a single "(Delete word)" entry and only the
**Replace** and **Ignore Once** buttons will be available for use.


> [!NOTE]
> Due to the way Visual Studio breaks up spans of text for interactive spell checking, it will only
> be able to detect doubled words if they appear on the same line.  Doubled words that span line breaks cannot be
> detected.  To detect doubled words that span line breaks, use one of the solution/project spell checking
> options.
> 
>


## See Also


**Other Resources**  
[](@e8f67bc4-a8f8-4e50-ab5a-876599f3a645)  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@3094ee74-88ae-4355-b702-23dcd55b4197)  
[](@e339cac1-9783-4c2a-919f-88436c78fef8)  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
