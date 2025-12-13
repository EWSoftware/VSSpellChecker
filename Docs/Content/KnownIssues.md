---
uid: a9ff4ce1-0d6b-4376-8d32-02dae64e2075
alt-uid: KnownIssues
title: Known Issues and Limitations
keywords: known issues
---

The following are some known issues and workarounds and some limitations with the spell checker.



## Code Analyzers

The code analyzers only apply to identifiers in supported languages.  Identifier misspellings
will not appear in the Spell Check Active Document or Solution/Project Spell Check tool windows.


- The code analyzer runs in a separate process.  As such, it may not always pick up new words
added to the dictionaries or ignored words files by the spell check as you type option and solution/project
spell checking until a change is made to the source code of an open editor.  Likewise, changes made to the global
and/or local project configurations may not be reflected until a change is made to the source code of an open
editor.
- The code analyzer is only able to correct the spelling on single part namespaces (e.g.
`namespace SinglePartNamespace`) or the last part of multi-part namespaces (e.g.
`namespace FirstPart.MiddlePart.LastPart`).  For parts prior to the last part,
misspellings will be flagged and suggestions offered but you will need to manually correct the spelling and apply
the refactoring code fix to change the name throughout the code base if necessary.  This appears to be a
limitation with symbol renaming and I was unable to find a workaround.  If anyone with more knowledge wants to
fix it, feel free to do so and submit a pull request with the changes.
- Correcting a misspelling on an identifier will correct it throughout the code base wherever the
containing identifier is used.  However, there are no provisions for correcting a common misspelling in multiple,
different identifiers (replace all).  Each must be corrected individually.
- Code analyzers are, naturally, designed to work with source code.  Unfortunately, they do not
work well for updating non-code files or files outside of the project.  There are some provisions for updating
non-code files as long as they are added to the additional file set used by code analyzers but that requires some
extra steps to set up properly in every project.  Files in the solution items or outside of the solution entirely
such as those specified in the global spell checker configuration are not updateable.  As such, there are no
fixes offered to allow adding a word in a misspelled identifier to ignored words files in the solution or project
or the global configuration nor to the dictionary itself.  If you want a word flagged by the code analyzer added
to either of those files, you must do so manually or through the spell checker configuration editor.  Another
workaround is to copy and paste the misspelling into a comment or string temporarily and use the action from
there to add the word.



## Spell Check as You Type and Solution/Project Spell Checking

Spell check as you type and solution/project spell checking applies to comments and strings within
source code and for the text in all non-source code files.


- The Spell Check Active Document tool window relies on the **Spell check as you type**
configuration option being enabled.  If disabled, spell checking will be unavailable until it is turned back on.
Any open editors will need to be closed and reopened for the new setting to take effect.
- The spell checking code attempts to ignore escape sequences within C-style code files so that
it does not flag text as misspelled when preceded by an escape sequence such as `\r`,
`\n`, or `\t`.  This can occasionally result in a false report in
certain situations such as words in a file path that start with what looks like an escape sequence character
(i.e. `C:\transform\file.txt`).  In such cases, just select the option to ignore the
misspelled instance once or always.  If a certain escaped word is a common enough occurrence, you can add it as
an ignored word in the configuration options.  For C# source code, you can also enable the option to ignore
verbatim strings which typically contain such occurrences.

  Also note that the tagger used in text editor lacks enough context to limit the check to
comments and normal string literals so you may see false reports in other text.  The solution/project spell
checking process does have the necessary context to limit the check to where it is needed.
- Due to the way Visual Studio breaks up spans of text for interactive spell checking, it will
only be able to detect doubled words if they appear on the same line.  Doubled words that span line breaks cannot
be detected.  Use the solution/project spell checking tool window to find doubled words that span line breaks.
- In C# code, the content of certain elements such as `code` that are
ignored in conceptual topics and by the solution/project spell checking process will most likely be spell checked
when using spell check as you type in the editor.  Only XML comments elements that are contained within the same
line can be ignored due to the way the tagger is implemented.
- On a similar note to the item above, in other languages such as Visual Basic, the content of
`code` elements will typically not be spell checked unless you make a change on a
particular line within it.  Moving the cursor away from the edited line will usually clear any issues on it.
Again, this is due to the way the tagger is implemented.  As noted, such elements are always ignored as expected
when doing a full solution/project spell check.
- For a list of common issues related to solution/project spell checking, see the related
[](@bda126a1-e534-4172-81dc-35a32d91e4cc) topic.
- For a list of known issues and limitations with the WPF text box spell checking feature, see
the [](@e23551ac-52f5-4505-b2d2-0728c7607fd3) topic.



## See Also


**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
[](@027d2fbc-7bfb-4dc3-b4f5-85f95fcf7629)  
