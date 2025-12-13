---
uid: 9581715a-a5bc-4de5-9474-eaefa38cf211
alt-uid: SpellCheckOptions
title: Spell Checking Options
keywords: "spell check, solution/project options"
---

The left side of the tool window contains the following items that control the process and display the results.

- **Spell Check** - This combo box lets you choose what is spell checked.  The first option is always
  **Entire Solution**.  There will be one entry for each project in the solution following it that, if selected,
  spell checks only the selected project.  The last option is always **Selected Solution Explorer items** which
  will only spell check the items currently selected in the Solution Explorer tool window.  This can be any
  combination of projects, folders, and/or files.  If the solution node is selected, it effectively checks the
  entire solution.
- **Max Issues** - This option lets you specify the maximum number of spell checking issues that will be
  reported.  The default is 5,000 which should be sufficient for most projects.  If you have a very large number
  of projects in a solution, you can increase this value up to 99,999 as you see fit.
- **Start** - Click this button to start the spell checking process.  Once started, the button is changed to a
  **Cancel** button.  Clicking it will stop the spell checking process as soon as possible.  A spinner will
  appear to the right of the button when spell checking is in progress and the current file being spell checked
  will be reported in the message area just below the option controls.
- **Issues** - The grid in the lower half of the tool window displays all issues found once the spell checking
  process completes or has been canceled.  A counter above the first column displays the currently selected issue
  number and the total number of issues found.  The grid contains the following columns:
  - **Project** - The project containing the file in which the issues was found.
  - **File** - The name of the file in which the issue was found
  - **Line #** - The line number on which the issue was found.  A column number is not reported as configuration
    options can vary the size of tab characters thus making it difficult to accurately report a true column
    number as displayed in the editor.
  - **Issue** - The issue that was found: Misspelled Word, Double Word, Compound Term, Deprecated Term, or
    Unrecognized Word.  The first two are the most common.  The last three will only be seen if code analysis
    dictionaries are enabled for use as part of the spell checking process.
  - **Word** - The word that caused the issue
  - **Text** - The text of the line containing the word at issue.  This allows you to determine the location and
    actual context to determine whether or not the issue should be corrected or ignored.

Clicking on the project, file, issue, or word column header will sort the grid by the values in that column.
Click on the column header again to change the sort between ascending and descending order.  The line number and
text columns are not sortable as sorting by them serves no useful purpose.

Right click on an issue to bring up a context menu with the following options:

- **Ignore Once** - Ignore the selected issue once.  This is equivalent to clicking the **Ignore Once** button
  in the correction options on the right side of the tool window.  The issue will be removed from the list.  If
  the spell check process is ran again, the issue will be reported again.
- **Ignore All** - Ignore all similar issues with the same word.  This is equivalent to clicking the
  **Ignore All** button in the correction options on the right side of the tool window.  The current issue and
  all similar issues containing the same word will be removed from the list.  Once ignored using this option,
  the issues will not be reported again until the solution or Visual Studio is closed and reopened.
- **Ignore Issues in This File** - Ignore all issues in the current issue's file.  The current issue and all
  other issues in the same file will be removed from the list.  If the spell check process is ran again, the
  issues will be reported again.
- **Ignore Issues in This Project** - Ignore all issues in the current issue's project. The current issue and all
  other issues in the same project will be removed from the list.  If the spell check process is ran again, the
  issues will be reported again.
- **Go To Issue** - Open the file in a text editor and go to the issue's location.  An issue in the grid can be
  double clicked to perform the same action.  Once positioned, the issue's word is highlighted.
- **Copy Word to Clipboard** - This will copy the current issue's word to the clipboard.  Pressing **Ctrl+C** in
  the grid will also copy the word to the clipboard.  This is useful if you are adding words to the ignored list
  in a configuration file.  You can copy and paste them into the configuration editor and add them to the list.
- **Copy as Ignore Spelling directive** - This will copy the current issue's word to the clipboard as an Ignore
  Spelling directive.  This is useful if you are adding infrequently used words to a file that you do not want to
  be flagged as misspellings.  You can copy and paste them into a comment in the file to have them ignored within
  it.
- **Export** - This contains a submenu with three options: Export All Issues, Export Project Issues (only those
  related to the project of the current issue), and Export File Issues (only those related to the file of the
  current issue).  The selected issues will be exported to a comma-separated values (CSV) file suitable for
  opening in Microsoft Excel or another such application.  Once exported, you will be asked whether or not you
  want to open the file.
- **Edit Configuration File** - This contains a submenu with an option for each of the configuration files
  associated with the selected issue.  This allows you to open them for editing so that you can adjust the
  options, add ignored files, add ignored words, etc.  The global configuration file is always listed first
  followed by any solution, project, folder, and file related configuration files that were used to create the
  options used to spell check the file for the selected issue.

## See Also
**Other Resources**  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@ddf22015-1680-4421-aaa7-dda867660522)  
[](@bda126a1-e534-4172-81dc-35a32d91e4cc)  
