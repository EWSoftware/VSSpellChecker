---
uid: 12d44ba0-2bef-4fac-a6f9-7990ecf057c2
alt-uid: CodeAnalyzer
title: Code Analyzer Rules
keywords: "spell check, code analyzer rules"
---
The code analyzer in the spell checker extension is used to spell check identifiers in source code.  Only C# is
supported right now but future support for Visual Basic and, if possible, F# are planned.  Code analyzer spell
checking differs from the string and comments spell checker implemented using the tagger in the editor.
Identifiers are always split up into separate words on capital letters and other separators such as the
underscore.  In addition, there are some extra options related to visibility and placement that can control when
spell checking of identifiers occurs.  See the [](@09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d) configuration category
topic for details.  Due to the way code analyzers work, there are some limitations.  See the
[](@a9ff4ce1-0d6b-4376-8d32-02dae64e2075) topic for more information.

The code analyzer contains one rule, `VSSpell001`, used to flag misspelled words in identifiers, offer suggested
fixes, and offer an option to add the misspelling to an Ignore Spelling directive comment in the source code
file.  The rule defaults to a warning and will show the usual warning underline on the misspellings.

Place the mouse over the misspelled identifier and click the down arrow on the smart tag to see the available
options.  Alternately, you can place the cursor anywhere in the identifier and press **Ctrl+.** or
**Shift+Alt+F10** depending on your version of Visual Studio to show the smart tag options.  Note that if an
identifier consists of multiple words and more than one is misspelled, each misspelled part is flagged and
corrected separately.

Select the "Correct spelling of 'XXX'" option to see the suggested replacements.  If the misspelling represents
only a part of the identifier, you will see the rest of the identifier in each suggestion.  Select a suggestion
to get a preview of the changes that will occur if it is selected.  To add the misspelling to an Ignore spelling
directive comment in the file, select the "Ignore word 'XXX'" option and a preview of the directive will be
displayed.  The directive comments is placed at the top of the file below any header comments and above the first
directive, using statement, or namespace declaration.  If a directive comment already exists in that location,
the new ignored word will be added to it.

## See Also
**Other Resources**  
[](@3094ee74-88ae-4355-b702-23dcd55b4197)  
[](@a7120f4c-5191-4442-b366-c3e792060569)  
