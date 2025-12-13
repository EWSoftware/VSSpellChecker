---
uid: 3094ee74-88ae-4355-b702-23dcd55b4197
alt-uid: IgnoreSpellingDirective
title: Using the Ignore Spelling Directive
keywords: "spell check, ignore spelling directive"
---

Often, there are a some words in each project that are used in one or more files that are flagged as
misspellings such as names, abbreviations, or programming terms.  While these can be added to a configuration
file as ignored words, it may not be convenient or worthwhile to do so.  Another alternative is to indicate that
they should be skipped while spell checking using the Ignore Spelling directive.  This is similar to the Ignore
Once or Ignore All option but will be applied automatically every time the file is spell checked.


To use the Ignore Spelling directive, add a comment line containing it to the file and specify one
or more words that you do not want spell checked after it.  For example: 


``` cs{title=" "}
					// Ignore Spelling: http, resx, proj
					// Ignore Spelling: endregion pragma /matchCase
				
```

The directives can appear anywhere within the file and any words specified will be ignore
throughout the file before and after the directive.  The directive can appear any number of times in the file
and any file type that can be spell checked is supported.  One or more words can appear after the directive
separated by commas, tabs, spaces, etc.  By default, the words will be matched case-insensitively.  If you
would prefer that they be matched case-sensitively, add the `/matchCase` option at the
end of the line.


Words in certain strings may be flagged as misspelled if separated by the ampersand as it is the
default mnemonic character (in XAML files it is the underscore).  If ignoring such cases using the directive,
include the mnemonic character in the word.  The same applies to escaped apostrophes in SQL script.  For
example:

<!-- Ignore Spelling: xhelp -->

``` cs{title=" "}
					// Ignore Spelling: page&id
					string msHelpViewer = "ms-xhelp:///?method=page&id=-1"
				
```

``` sql{title=" "}
					-- Ignore Spelling: Bobbytable''s
					Select Test = 'Bobbytable''s tables'
				
```


## See Also


**Other Resources**  
[](@e8f67bc4-a8f8-4e50-ab5a-876599f3a645)  
[](@53ffc5b7-b7dc-4f03-9a51-ed4176bff504)  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@c592c4d8-7387-47fe-9b79-28bf0168f447)  
