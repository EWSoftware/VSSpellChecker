---
uid: 027d2fbc-7bfb-4dc3-b4f5-85f95fcf7629
alt-uid: Welcome
title: Welcome
keywords: Welcome
---
<!-- Ignore spelling: Golovin, Lehenbauer -->

Welcome to the **Spell Checker My Code** project.

This project is a Visual Studio editor extension that checks the spelling of comments, strings, and plain text as
you type or interactively with a tool window.  It also contains a code analyzer that will spell check identifiers
in C# source code.  It can spell check an entire solution, project, or selected items.  It is based largely on
the spell checker extension originally created by Noah Richards, Roman Golovin, and Michael Lehenbauer.  This
version has been extended as follows:

- It uses Hunspell to perform the spell checking.  As such, custom dictionaries can be added to spell check in
  different languages (OpenOffice versions 2, 3, and 4 dictionaries are supported).
- Added the ability to spell check the inner text of XML elements as well as certain attribute values.
- Added support for replacing all occurrences of a misspelling via the smart tag context menu (hold down the Ctrl
  key when selecting a replacement word).
- Added an Ignore Once option to the smart tag context menu to ignore a specific instance of a misspelled word.
- Fixed up various issues to skip text that should not be spell checked and to break up text into words correctly
  when escape sequences are present in the text.
- Added an interactive spell checking tool window to find and fix spelling errors in the current file.
- Added a solution/project spell checking tool window that is capable of spell checking an entire solution, a
  single project, or any combination of items selected in the Solution Explorer window.
- An "Ignore Spelling" directive can be added in file comments to ignore specific words within it.
- An option is available to spell check any WPF text box within Visual Studio.
- Several new spell checking options have been added and all of the spell checking options have been exposed and
  can be configured.  Configurable options include:
  - Specify one or more dictionary languages to use for spell checking.  The package comes with several
    dictionaries for various languages.
  - Specify additional folders to search for custom dictionaries or user dictionaries.
  - Enable or disable spell checking as you type and whether or not solutions, projects, folders, and/or files
    are included in solution/project spell checking operations.
  - Ignore words with digits, in all uppercase, and/or mixed case.
  - Ignore .NET and C-style format string specifiers.
  - Ignore words that look like filenames and e-mail addresses.
  - Ignore words that look like XML elements in spell checked text.
  - Treat underscores as separators.
  - Various options for excluding specific elements of source code files from being spell checked.  The options
    related to comment types can be applied to all C-style languages as they are implemented through a
    classification tagger.  Options for identifiers are implemented through a code analyzer and currently
    only apply to C# source code (Visual Studio 2019 and later).  The code analyzer can be configured to ignore
    private identifiers, internal identifiers, type parameters, identifiers in all uppercase, compiler generated
    code, and all identifiers within member bodies (local variables in properties, methods, lambdas, etc.).
  - Ignore specific classifications of text based on the Visual Studio content type or file type.
  - Exclude files from spell checking by filename wildcard pattern.
  - Specify a list of XML elements in which the content should be ignored when spell checking XML files.
  - Specify a list of XML attributes for which the value should be spell checked when spell checking XML files.
  - Determine localized resource file language from the filename
  - Configuration options are stored in .editorconfig files and thus can be specified at any level (solution,
    project, folder, or file).  Spell checker options can be inherited or overridden.  A global configuration
    file is used as the base set of configuration options.

## Making a Donation
If you would like to support this project, you can make a donation of any amount you like by clicking on the
PayPal donation button below.  Another option is to click the **Sponsor** button in the page footer to sponsor
the project on GitHub with either a one-time or monthly amount.

[![Make donations with PayPal - It's fast, free and secure!](@PayPal)](https://www.paypal.com/donate/?hosted_button_id=29KUXTJR48CRE "Make a donation")

Thanks to those of you that have made a donation. It is much appreciated!

## See Also
**Other Resources**  
[](@e339cac1-9783-4c2a-919f-88436c78fef8)  
[](@548dc6d7-6d08-4006-82b3-d5830be96f04)  
[License Agreement](https://github.com/EWSoftware/VSSpellChecker/blob/master/LICENSE)  
