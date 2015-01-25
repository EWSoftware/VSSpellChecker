Welcome to the **Visual Studio Spell Checker** project.

This project is a Visual Studio editor extension that checks the spelling of comments, strings, and plain text
as you type or interactively with a tool window.  It is based largely on the spell checker extension originally
created by Noah Richards, Roman Golovin, and Michael Lehenbauer.  This version has been extended as follows:

* It uses NHunSpell to perform the spell checking.  As such, custom dictionaries can be added to spell check in
different languages (OpenOffice versions 2, 3, and 4 dictionaries are supported).
* Added the ability to spell check the inner text of XML elements as well as certain attribute values.
* Added support for replacing all occurrences of a misspelling via the smart tag context menu (hold down the
Ctrl key when selecting a replacement word).
* Added an Ignore Once option to the smart tag context menu to ignore a specific instance of a misspelled word.
* Fixed up various issues to skip text that should not be spell checked and to break up text into words
correctly when escape sequences are present in the text.
* Added an interactive spell checking tool window to find and fix spelling errors in the current file.
* Some new spell checking options have been added and all of the spell checking options have been exposed and
can be configured.  Configurable options include:

  * The default language to use for spell checking.
  * Enable or disable spell checking as you type.
  * Ignore words with digits.
  * Ignore words in all uppercase.
  * Ignore .NET and C-style format string specifiers.
  * Ignore words that look like filenames and e-mail addresses.
  * Ignore words that look like XML elements in spell checked text.
  * Treat underscores as separators.
  * Various options for excluding specific elements of C# source code files from being spell checked.
  * Exclude files from spell checking by filename extension.
  * Specify a list of XML elements in which the content should be ignored when spell checking XML files.
  * Specify a list of XML attributes for which the value should be spell checked when spell checking XML files.

See the [Project Wiki](https://github.com/EWSoftware/VSSpellChecker/wiki) for information on requirements for
building the code, contributing to the project, and using and configuring the spell checker extension in Visual
Studio.
