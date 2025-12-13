---
uid: e01bd3d9-c525-4407-8c65-fcdb64539299
alt-uid: CodeAnalysisDictionaries
title: Code Analysis Dictionaries
keywords: "configuration, code analysis dictionaries"
---
This category lets you indicate whether or not to import code analysis dictionaries in the project for use in
spelling checking.  The imported options are only applied to the spell checked elements (comments, strings, XML
content, attribute values, etc.).  To be imported, the code analysis XML file must appear in the project of the
file being spell checked and must have its **Build Action** property set to `CodeAnalysisDictionary`.

> [!IMPORTANT]
> For the code analysis dictionaries to be recognized by the code analyzer for use in spell checking identifiers,
> an additional step is required so that they are passed as additional files to the analyzers.  Add the
> `AdditionalFileItemNames` property to a property group in each project in the solution and update it so that it
> includes all `CodeAnalysisDictionary` items as additional files as shown in the example below.

``` xml{title="Specifying Code Analysis Dictionaries as Additional Files"}
<PropertyGroup>
  <!-- Update the property to include all code analysis dictionary files -->
  <AdditionalFileItemNames>$(AdditionalFileItemNames);CodeAnalysisDictionary</AdditionalFileItemNames>
</PropertyGroup>
```

The following options are available.
- **Import project code analysis dictionaries if present** - This option is enabled by default and controls
  whether or not the code analysis dictionaries are imported.  If disabled, all of the following options are
  ignored.

  ``` none{title=" "}
  vsspell_cad_import_code_analysis_dictionaries = [true|false]
  ```
- **Treat unrecognized words as misspelled words** - This option is enabled by default and will cause
  unrecognized words in the dictionary to be treated as misspelled words.  Adding a `SpellingAlternates`
  attribute to the `Word` element allows you to specify a list of one or more comma-separated words to offer as
  suggested replacements.

  ``` none{title=" "}
  vsspell_cad_treat_unrecognized_words_as_misspelled = [true|false]
  ```
- **Treat deprecated terms as misspelled words** - This option is enabled by default and will cause deprecated
  terms in the dictionary to be treated as misspelled words.  The preferred alternate is offered as the suggested
  replacement.  If the preferred alternate is camel cased, spaces are inserted before each capital letter.

  ``` none{title=" "}
  vsspell_cad_treat_deprecated_terms_as_misspelled = [true|false]
  ```
- **Treat compound terms as misspelled words** - This option is enabled by default and will cause compound terms
  in the dictionary to be treated as misspelled words.  The compound alternate is offered as the suggested
  replacement.  If the compound alternate is camel cased, spaces are inserted before each capital letter.

  ``` none{title=" "}
  vsspell_cad_treat_compound_terms_as_misspelled = [true|false]
  ```
- **Treat casing exceptions as ignored words** - This option is disabled by default.  If enabled, casing
  exceptions in the dictionary will be treated as ignored words.  Typically, casing exceptions are in all
  uppercase or camel case.  Camel cased words are always ignored.  All uppercase words are ignored if the
  **Ignore words in all uppercase** option in the **General Settings** category is enabled.  This option may be
  of use if that option is disabled so that acronyms in all uppercase within this category are not spell checked.

  ``` none{title=" "}
  vsspell_cad_treat_casing_exceptions_as_ignored_words = [true|false]
  ```
- **Recognized Word Handling** - This option controls how recognized words in the dictionary are treated.  The
  available options are:
  - **None** - Recognized words are not imported and are spell checked in the normal manner.
  - **Treat all as ignored words** - Recognized words are treated as ignored words and will not be offered as
    suggested replacements for misspelled words.  This is the default setting.
  - **Add all to dictionary** - Recognized words are added to the dictionary and will be offered as suggested
    replacements for misspelled words.
  - **Spelling attribute determines usage** - An optional `Spelling` attribute on each `Word` element determines
    how each recognized word is handled.  If set to `Add`, the word is added to the dictionary.  If set to
    `Ignore`, the word is treated as an ignored word.  If set to `None`, any other value, or is omitted, the word
    is not imported and will be spell checked in the normal manner.

  ``` none{title=" "}
  vsspell_cad_recognized_word_handling = [None|IgnoreAllWords|AddAllWords|AttributeDeterminesUsage]
  ```

Below is an example of a code analysis dictionary file with the extra attributes used by the spell checker.

> [!TIP]
> Once you have configured and saved the settings, you can open the code analysis dictionary file itself to see
> how the words within it are treated.

``` XML{title=" "}
<Dictionary>
  <!-- This is a code analysis dictionary used for Visual Studio code analysis.
       See http://msdn.microsoft.com/en-us/library/bb514188.aspx -->
  <Words>
    <Unrecognized>
      <!-- SpellingAlternates is a comma-separated list of alternate spellings
           to offer as suggestions -->
      <Word SpellingAlternates="literally, precisely">verbatim</Word>
    </Unrecognized>
    <Recognized>
      <!-- The Spelling attribute tells the spell checker how to treat the word:
           Add = Add to dictionary
           Ignore = Ignore word
           None/other value/attribute omitted = Not handled, pass through as a
           normal word -->
      <Word Spelling="Add">yadda</Word>
      <Word Spelling="Ignore">Epg</Word>
      <Word Spelling="Ignore">Mvp</Word>
      <Word>Gui</Word>
      <Word Spelling="Ignore">Mru</Word>
      <Word Spelling="Ignore">Kpi</Word>
      <Word Spelling="Ignore">Hsl</Word>
      <Word Spelling="Ignore">Rgb</Word>
      <Word Spelling="Ignore">Bim</Word>
      <Word>Appender</Word>
    </Recognized>
    <Deprecated>
      <!-- The preferred alternate is offered as the suggested replacement -->
      <Term PreferredAlternate="yadda">blah</Term>
      <Term PreferredAlternate="Elements">NuoGui</Term>
    </Deprecated>
    <Compound>
      <!-- The compound alternate is offered as the suggested replacement with
           spaces inserted after each capital letter. -->
      <Term CompoundAlternate="BigBox">bigbox</Term>
    </Compound>
  </Words>
  <Acronyms>
    <!-- These are treated as ignored words if the option is enabled -->
    <CasingExceptions>
      <Acronym>LCID</Acronym>
      <Acronym>UI</Acronym>
      <Acronym>SQLite</Acronym>
    </CasingExceptions>
  </Acronyms>
</Dictionary>
```

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
