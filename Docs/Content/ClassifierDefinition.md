---
uid: 0ff35371-69b5-48dd-a062-037abe2469de
alt-uid: ClassifierDefinition
title: Defining Solution/Project Spell Check Classifiers
keywords: classifier definition
---

Unlike spell check as you type and the active document spell checking tool window, the
solution/project spell checking tool window must classify the content of each and every file for itself to
determine what parts need to be spell checked.  To facilitate this process, a configuration file is used to
define a well-known set of filename extensions and map them to an appropriate classifier definition.


In the event that an unrecognized filename extension is encountered, the spell checking process looks
at the content and tries to determine whether or not it is well-formed XML.  If it is, it parses the content as
XML.  If not, it treats the file's content as plain text.

<autoOutline lead="none" excludeRelatedTopics="true">2</autoOutline>


## Classifier Types

There are seven different types of classifier:


- `PlainTextClassifier` - This is the simplest of the classifiers.  It
just returns all of the file's content as a single block to be broken up into words for spell checking.
- `XmlClassifier` - This classifier is used to parse XML files and spell
check them based on the active configuration settings.  For example, it skips the inner text of unwanted XML
elements and spell checks the values of attributes that have been specified as needing spell checking.  This
classifier can only be used on well-formed XML content.
- `ResourceFileClassifier` - This is a variant of the XML classifier used
for resource files (*.resx*).  This classifier will automatically ignore certain non-words
common to the default comments in resource files which describe the elements within it.  It also automatically
ignores `data` and `metadata` elements with a
`type` attribute with any value or a `mimetype` attribute with
"base64" in its value.  These typically do not contain a value that should be spell checked.  A common example is
base 64 encoded image data.  This prevents a number of unwanted issues being reported.
- `ReportingServicesClassifier` - This is a variant of the XML classifier
used for Reporting Services report files (*.rdl* and *.rdlc*).  This
classifier will automatically ignore various elements that should not be spell checked, will limit the content
that is spell check in `Code` elements to comments and string literals, and will limit the
content that is spell checked in expressions to literal strings.  This prevents a number of unwanted issues being
reported.
- `HtmlClassifier` - This classifier is used to parse HTML files and works
in a manner similar to the XML classifier.  Unlike the XML classifier, this one can handle ill-formed content
common to many HTML file types.
- `MarkdownClassifier` - This classifier is identical to the HTML
classifier but it is used on markdown files and excludes spans that look like inline or fenced code blocks.  This
helps reduce the number of false misspelling reports that can result in such spans.
- `CodeClassifier` - This classifier is geared toward parsing code.  It
uses a series of regular expressions to identify file content that looks like code comments and literal strings.
Once done, it takes additional steps to further classify different string literal types (i.e. verbatim and
interpolated) as well as parsing XML documentation comment elements to remove sections that should not be
spell checked and include attribute values that should.  Expressions can be added to classify text as undefined.
Such undefined spans are removed from other classified spans to reduce the number of false misspelling reports
from the removed spans.
- `ScriptWithHtmlClassifier` - This classifier is similar to the
`CodeClassifier` described above but is specific to JavaScript and TypeScript files that
contain embedded HTML elements.  It will parse the code and also the HTML elements to include the wanted HTML
element parts such as inner text and exclude the unwanted HTML elements parts such as ignored attribute values.
- `RegexClassifier` - This is a general classifier that uses a series of
regular expressions to classify file content as code comments or string literals.  This is useful for non-code
files such as style sheets or SQL scripts that do not require the extra classification steps performed by the
`CodeClassifier` described above.



## Classification Configuration Files

The package comes with a standard configuration file that contains the current set of classifier
definitions and recognized file types.  By creating your own *Classifications.config* file in
the spell checker's application data configuration folder (see below), you can add additional classifier
definitions, override existing ones, and/or add additional recognized file types and map them to a classifier.
The following sections describe the various elements found in the configuration file.  For an example, see the
package's [standard configuration file](https://github.com/EWSoftware/VSSpellChecker/blob/master/Source/VSSpellChecker/Classifications.config "Classifications.config").  This is also where you can see which file extensions are currently recognized and to which
classifier they are being mapped.



#### Classifications

This is the root element.  All other elements are nested within this one.



#### Classifiers

This element contains the classifier definitions.  It consists of a set of
`Classifier` elements that define the ID, type, and configuration for each one.



#### Classifier

This element defines a classifier.  The `Id` element uniquely identifies
the classifier.  Typically, it will be set to a language name or content type.  The `Type`
attribute is used to indicate which type of classifier it will use.  The value should be set to one of the
classifier types described in the first section.  If the classifier has configurable elements, they will be
nested within this element.


The `XmlDocCommentDelimiter` attribute can be used to define the XML
documentation comment delimiter which can vary by language.  For example, the C-style classifier defines it as
`///` while the VB-style classifier defines it as `'''`.  In a
similar manner, the `QuadSlashDelimiter` attribute can be used to define the quad-slash
delimiter and the `OldStyleDocCommentDelimiter` attribute can be used to define the
delimiter for the older style of XML comments.  Currently, these two options are only used by the C# classifier
to exclude quad-slash commented code and to determine old-style XML commented code if so indicated in the C#
configuration options.


The `Mnemonic` attribute can be added to define the mnemonic character
for user interface label text that indicates a hot key.  Only the ampersand and underscore are recognized as
valid mnemonic characters.  If not specified, it defaults to the ampersand character.  This is used when the
Ignore Mnemonics option is enabled in the General Settings configuration category.



#### Match

This is a configuration element used by several of the classifiers to define a regular
expression that will be used to find content and classify it accordingly.  The `Expression`
attribute is used to define the regular expression.  The `Options` attribute is used to
define one or more regular expression options that should be used with the expression.  Specify a comma-separated
list of [](@T:System.Text.RegularExpressions.RegexOptions)
values as needed.  If omitted, no options will be defined for the expression.  The `Classification`
attribute defines how the matched text is to be classified.  Below are the most common classifications:

- `DelimitedComments` - Delimited comments in code such as `/* Comments */`.
- `SingleLineComment` - A single line comment such as `// Comment`.
- `NormalStringLiteral` - A normal string literal such as `"A text string"`.
- `RegionDirective` - A region directive such as `#region Private data members`.
- `Undefined` - Some classifiers use this to mark spans of text that should be removed from other classifications
   to reduce false misspelling reports from unwanted text.

There are several other classifications not typically used within the configuration files.
These are used mostly by the classifiers themselves after parsing the file to further classify a range of text.
For example, the code classifier will change the `NormalStringLiteral` classification to
`VerbatimStringLiteral` or `InterpolatedStringLiteral` based on
whether or not it has the appropriate prefix character before the opening quote.  The code classifier will also
reclassify `SingleLineComment` and `DelimitedComments` if necessary
to handle XML documentation and quad-slash comment types when necessary.  Typically, these subtypes will not
appear within the classifier configurations.  The `RangeClassification` enumerated type
in the source code contains all of the currently defined range classifications used by the spell checker.



#### Extensions

This element contains the file type definitions that map file extensions to the classifiers.
It consists of a set of `Extension` elements that define the extension and classifier for
each one.  As noted above, any extension not represented is tried first as XML and, if not XML, then as plain
text.



#### Extension

This maps a specific file extension to a classifier definition.  The `Value`
attribute defines the extension without the leading period.  The `Classifier` attribute
defines the ID of the classifier to use for this file type.  It should match the `Id`
attribute value of a `Classifier` element defined earlier in the configuration file or one
from the standard configuration file.


An exception is the `None` classifier ID.  This is technically not a
classifier.  It is a simple means of excluding certain file types from being spell checked.  For example, the
*aff* and *dic* file extensions are mapped to this classifier to prevent
dictionary files from being spell checked.  This is a convenient way of excluding a class of files from being
spell checked in the solution/project spell checking process without having to exclude them explicitly in the
global configuration or within each solution or project's configuration.


> [!NOTE]
> Extensions for binary file types do not need to be specifically excluded using the
> `None` classifier.  Binary files are automatically excluded from being spell checked prior
> to reaching the point of determining the classifier type.
> 
>


## Defining Your Own Classifier Configurations

To define additional file extension mappings or to define your own classifiers for new file types,
create a file in the *%LOCALAPPDATA%\EWSoftware\Visual Studio Spell Checker* folder
called *Classifications.config*.  In it, add the necessary sections as described above.  If
all you need to do is map file extensions to classifiers, just add an `Extensions` section
and define the mappings.  You can reuse the existing classifier IDs if you find one that will suit your needs.


Redefining a classifier or an extension with an ID that appears in the standard configuration file
will effectively replace its definition with yours.  This is a convenient way to try out changes or fixes to the
existing classifier expressions should you find an issue with them.


> [!IMPORTANT]
> Any changes made to the classifications configuration file will not take effect until Visual
> Studio is restarted.
> 
>

``` XML{title="Example Custom Configuration Files"}
<Classifications>
	<!-- A simple example that maps new file types to existing classifier definitions -->
	<Extensions>
		<Extension Value="myext" Classifier="XML" />
		<Extension Value="xyz" Classifier="HTML" />
		
		<!-- Never spell check "xxx" files as part of the project -->
		<Extension Value="xxx" Classifier="None" />
	</Extensions>
</Classifications>
```

``` XML{title=" "}
<Classifications>
	<!-- An example of defining a new classifier type -->
	<Classifiers>
		<Classifier Id="SomeLanguage" Type="RegexClassifier">
			<Match Expression="\s*#.*?[\r\n]{1,2}" Classification="SingleLineComment" />
			<Match Expression="(&quot;&quot;)|(@&quot;(.|[\r\n])*?&quot;|&quot;(.|\\&quot;|\\\r\n)*?((\\\\)+&quot;|[^\\]{1}&quot;))"
				Classification="NormalStringLiteral" />
		</Classifier>
	</Classifiers>

	<Extensions>
		<Extension Value="abc" Classifier="SomeLanguage" />
	</Extensions>
</Classifications>
```


## See Also


**Other Resources**  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@df2a45c1-1996-46f6-9d33-e73f0fa1d88a)  
