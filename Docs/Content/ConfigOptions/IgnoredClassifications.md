---
uid: 6a987caf-5ad9-4dab-a17c-c887881fec7a
alt-uid: IgnoredClassifications
title: Ignored Classifications
keywords: "configuration, ignored classifications"
---
This category lets you exclude specific classifications of text from being spell checked by content/file type.
This is more generic than the C#/C-Style language category options and can be applied to any language or file
type for which Visual Studio or the solution/project spell checker can classify text.

The options for this category are divided into two sections: one for Visual Studio text editor classifications
and one for solution/project spell checking classifications.  Classifications can be ignored in either category
or both as you see fit.  See the sections below for more information.

For non-global settings, an option is available to inherit the ignored classifications from configurations above
the current one.  If enabled, any additional ignored classifications in the current configuration are added to
those.  If disabled, the settings in the current configuration will replace the inherited list of ignored
classifications.  If not inherited and the list is left empty, it effectively clears the list of ignored
classifications.

#### Visual Studio Classifications
Visual Studio text editor classifications are determined as needed.  When first opened, this page will only
display content types and classifications that it knows about from the current configuration file.  Select this
configuration category page to enable classification tracking, open some files containing the content that you
want to exclude, wait a few seconds to allow the spell checker to finish processing the files, and return to the
configuration editor.  Click the **Refresh Content Types** button to make the new content types and related
classifications available in the content type combo box and classification list box.

> [!NOTE]
> You will not see any classifications for C# files nor will you see any classifications for other C-style
> languages if you have enabled applying the C# options to all C-Style languages.  Use the C# options to control
> the spell checked classifications for those languages when it is enabled.  If the option is disabled, you will
> see classifications for non-C# C-style languages.

Select a content type from the combo box and the related spell checked classifications that have been seen in the
files that you opened will be displayed in the list box to the right of it.  Check the box next to each
classification that you want to exclude or uncheck them to include them again.  The classifications shown are the
internal ones from the language classifiers and vary from language to language.  Many are self-explanatory but
others may not be.  You may need to experiment with the classifications to determine which ones to use to exclude
the elements you are interested in ignoring.

``` none{title=" "}
vsspell_ignored_classifications_[sectionId] = [clear_inherited],
[classificationType|classification1|classification2|...],
[File Type: fileType|classification1|classification2|...],
[Extension: extension|classification1|classification2|...]

sectionId = The unique ID for the section
clear_inherited = If specified, clear all prior values and use only the settings in this property.  If omitted,
prior values from other property instances are inherited.
classificationType|classification1|classification2|... = A classifier type and the classifications to ignore.
The values are separated by pipes.
File Type: fileType|classification1|classification2|... = A set of classifications for a specific file type to
ignore.  The values are separated by pipes.
Extension: extension|classification1|classification2|... = A set of classifications for a specific file extension
to ignore.  The values are separated by pipes.
```

#### Solution/Project Spell Check Classifications
Solution/project spell checking uses a separate process with a fixed set of possible classifications.  You can
exclude classifications by file type or by individual file extension when performing solution/project spell
checking.  Extension settings will override their corresponding file type settings if both are present in the
list of classifications to ignore.  Note that not all classifiers use the full set of classifications.

To add exclusions, select a file type from the combo box.  When selected, the related set of file extensions can
be found in the extension combo box.  The "All" option for file extension covers all extensions for the selected
file type.  Click the **Add** button to add the selected file type or extension to the ignored set.  Note that
many file types are quite similar but use different rules to classify their elements (i.e. XML and XAML).  If
you want to exclude an element common to both such as XML comments, you will need to add each file type and
exclude the related classification from both types.  Once added, the classifications can be found in the list
box on the right.  Check the box next to each classification that you want to exclude or uncheck them to include
them again.

Unlike the Visual Studio classifications, the solution/project spell checker uses a fixed set of well-known
classification types.  The full set is always displayed for each file type or extension but not all are used by
each.  The classification types are as follows:

<table>
  <thead>
    <tr>
      <th>Classification Type</th>
      <th>Description</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>AttributeValue</td>
      <td>

An XML/HTML element attribute value

</td>
    </tr>
    <tr>
      <td>DelimitedComments</td>
      <td>

Delimited comments in code, format varies by language (`/* Comments */`)

</td>
    </tr>
    <tr>
      <td>InnerText</td>
      <td>The inner text of an XML/HTML element</td>
    </tr>
    <tr>
      <td>InterpolatedStringLiteral</td>
      <td>

An interpolated string literal in code (`$"Age: {age}"`)

</td>
    </tr>
    <tr>
      <td>NormalStringLiteral</td>
      <td>

A normal string literal in code, format varies by language (`"Some text"` or 'SQL string literal')

</td>
    </tr>
    <tr>
      <td>PlainText</td>
      <td>

Run of the mill plain text.  This is typically only seen for actual plain text files and files not recognized as
any other known file type.

</td>
    </tr>
    <tr>
      <td>QuadSlashComment</td>
      <td>

A quadruple slash comment in code, typically used to comment out code so that it is not included when spell
checking, format varies by language (`//// Comment`)

</td>
    </tr>
    <tr>
      <td>RegionDirective</td>
      <td>

A region directive in code (`#region Private data members`)

</td>
    </tr>
    <tr>
      <td>SingleLineComment</td>
      <td>

A single-line comment in code, format varies by language (`// Single-line comment`)

</td>
    </tr>
    <tr>
      <td>VerbatimStringLiteral</td>
      <td>

A verbatim/raw string literal in code (`@"C:\Path\File.txt" or R"(C:\Path\File.txt)"`)

</td>
    </tr>
    <tr>
      <td>XmlCommentsInnerText</td>
      <td>

The inner text of an XML comments element in code (`<summary>Member summary</summary>`)

</td>
    </tr>
    <tr>
      <td>XmlDocComments</td>
      <td>

XML documentation comments in code.  This is a general designation to classify an entire
section as XML documentation comments in code.  If excluded, the entire set of XML documentation will be omitted
from spell checking.  If not excluded, it will typically be classified further into attribute values if the
attribute is spell checked and XML comments inner text.

</td>
    </tr>
    <tr>
      <td>XmlFileCData</td>
      <td>

A `<![CDATA[....]]>` section in an XML file

</td>
    </tr>
    <tr>
      <td>XmlFileComment</td>
      <td>

An XML/HTML file comment (`<!-- Comments -->`)

</td>
    </tr>
  </tbody>
</table>

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
