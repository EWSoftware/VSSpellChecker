---
uid: 09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d
alt-uid: CodeAnalyzerOptions
title: Code Analyzer Options
keywords: "configuration, code analyzer options"
---
This category contains several options that let you fine tune how source code files are spell checked.  The
following options are available.

The following options only apply to the code analyzer used to spell check identifiers in C# source code (Visual
Studio 2019 and later).  Note that camel cased identifiers and those with underscores will be split into
individual words and each word in the identifier will be spell checked.  The default settings are configured
so that only public and protected member identifiers are spell checked.  The options below can be adjusted to
include private members, internal members, local variables, and compiler generated code if so desired.

- **Ignore identifiers if private** - If enabled, the default, all identifiers with a visibility of private are
  ignored.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_identifier_if_private = [true|false]
  ```
- **Ignore identifier if internal** - If enabled, the default, all identifiers with a visibility of internal are
  ignored.  Note that protected internal identifiers will still be spell checked as the protected visibility will
  take precedence.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_identifier_if_internal = [true|false]
  ```
- **Ignore identifier if all uppercase** - If enabled, any identifier that consists only of uppercase letters
  will be ignored.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_identifier_if_all_uppercase = [true|false]
  ```
- **Ignore identifiers within member bodies** - If enabled, the default, all identifiers within the body of a
  member will be ignored (local variables in properties, methods, lambda expressions, etc.).  This can be useful
  in reducing the number of spelling errors reported since method bodies tend to contain more abbreviations and
  other shortened identifiers.  Since these will never be publicly visible, it may be preferable to skip spell
  checking them.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_identifiers_within_member_bodies = [true|false]
  ```
- **Ignore type parameters** - If enabled, type parameters on generic types such as `TKey` and `TValue` will be
  ignored.  If spell checked, the leading uppercase "T" in the examples would be skipped so it would only flag a
  spelling error if the remainder of the type parameter was misspelled.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_type_parameters = [true|false]
  ```
- **Ignore compiler generated code** - If enabled, the default, all compiler generated code is skipped and will
  not be spell checked.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_if_compiler_generated = [true|false]
  ```

The following options apply to C# source code and are implemented via the classifier in the Visual Studio editor.
By enabling the last option, it is also possible to have these options applied to all C-style code as applicable.

- **Ignore XML documentation comments (`/// ...` and `/** ... */`)** - If enabled, XML documentation comments will
  be excluded from spell checking.  Unless disabled with the option below, standard delimited comments will still
  be included for spell checking.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_xml_doc_comments = [true|false]
  ```
- **Ignore delimited comments `(/* ... */)`** -  This is useful for excluding private comments that nobody will see
  except for developers and commented out code.  Unless disabled with the option above, delimited XML comments
  will still be included for spell checking.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_delimited_comments = [true|false]
  ```
- **Ignore standard single line comments (`// ...`)** - This is useful for excluding private comments that nobody
  will see except for developers and commented out code.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_standard_single_line_comments = [true|false]
  ```
- **Ignore quadruple slash single line comments (`//// ...`)** - This option is useful for ignoring commented out
  code using the method recommended by StyleCop so that it does not produce style warnings.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_quadruple_slash_comments = [true|false]
  ```
- **Ignore normal string literals (`"..."`)** - This is useful for ignoring string literals in code if you use
  resource files for localized text.  In such cases, the string literals are usually not valid words and enabling
  this option prevents the resource keys from showing up as spelling errors.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_normal_strings = [true|false]
  ```
- **Ignore verbatim string literals (`@"..."`)** - Verbatim string literals typically contain such things as
  filenames or multi-line text that is not usually considered text that needs to be spell checked.  Enabling this
  option prevents their content from being checked for spelling errors.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_verbatim_strings = [true|false]
  ```
- **Ignore interpolated string literals (`$"{PropertyName}..."`)** - This is useful for skipping interpolated
  strings which may only contain property format specifiers.  Enabling this option prevents their content from
  being checked for spelling errors.  If left disabled, all string content except text within braces which denote
  format specifiers will be spell checked.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_interpolated_strings = [true|false]
  ```
- **Ignore raw string literals (`"""..."""`)** - Verbatim string literals typically contain such things as
  filenames or multi-line text that is not usually considered text that needs to be spell checked.  Enabling this
  option prevents their content from being checked for spelling errors.

  ``` none{title=" "}
  vsspell_code_analyzer_ignore_raw_strings = [true|false]
  ```
- **Apply the above options to all C-style languages as applicable** - If this option is enabled, the above
  options are applied to all C-style languages as applicable.  For example, all of the comments options and the
  normal string literal option will be applied to C, C++, JavaScript, etc.

  ``` none{title=" "}
  vsspell_code_analyzer_apply_to_all_c_style_languages = [true|false]
  ```

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
