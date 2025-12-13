---
uid: ddf22015-1680-4421-aaa7-dda867660522
alt-uid: CorrectingIssues
title: Correcting Issues
keywords: "spell check, correcting solution/project issues"
---

The right side of the tool window contains the following items that allow you to correct issues.  The
options will be updated based on the issue currently selected in the grid on the left side of the tool window.


> [!NOTE]
> When correcting an issue, the file containing the issue will be opened in a text editor to perform
> the update.  This allows you to see what was changed and, in the event that you made a mistake, undo the change
> or simply close the file without saving any changes.  Any changes that are undone will be reported as new issues
> when the spell checking process is ran again.  This approach was deemed safer than making changes to the files on
> disk without actually showing what was changed.
> 
>

The misspelled word is shown at the top of the tool window and any suggested replacements are shown
in the list box below it.  The following options are available:


- **Replace** - Replace the current misspelled word with the selected suggestion.  Double
clicking a suggestion in the list will also cause the misspelled word to be replaced with the double clicked
word.
- **Replace All** - Replace all occurrences of the misspelled word with the selected
suggestion.  Holding down the **Ctrl** key when double clicking a suggestion in the list will also cause all
occurrences of the misspelled word to be replaced with the double clicked word.

  Since the solution/project spell checking update may affect several files across one or more
projects, a confirmation dialog box will appear telling you how many total replacements will be made, the word
being replaced, its replacement, and the total number of files that will be updated.  If you want to proceed with
the update, click the Yes button.  To cancel the update, click No.  If you proceed with the change, each affected
file is opened in an editor and the necessary replacements are performed.
- **Ignore Once** - This option allows you to ignore the given instance of the misspelled
word in the given file location.  All other misspellings of the same word will still be left in the list for
handling.  If the spell checking process is ran again, the issue will be reported again.
- **Ignore All** - Ignore all instances of the given misspelled word for the remainder of
the Visual Studio session.  The current issue and all similar issues in the list containing the same word will be
removed.  When the solution or Visual Studio is closed and reopened the ignored word will be reported again.
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

The spell checking process will also detect doubled words and report them.  When a doubled word
issue is selected, the list box will contain a single "(Delete word)" entry and only the **Replace** and
**Ignore Once** buttons will be available for use.


> [!NOTE]
> Unlike the active document spell checking tool window, the solution/project spell checking window
> can detect doubled words that span line breaks.  If the text for the issue does not show a doubled word and it
> appears at the start of the line, the first word is probably at the end of the prior line.  You can use the
> **Go To Issue** option to view the issue in the file to confirm that this is the case before correcting it.
> 
>


## See Also


**Other Resources**  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@9581715a-a5bc-4de5-9474-eaefa38cf211)  
[](@bda126a1-e534-4172-81dc-35a32d91e4cc)  
