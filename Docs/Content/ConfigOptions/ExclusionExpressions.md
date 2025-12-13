---
uid: 6216eedb-6434-4cad-be06-576814e0b735
alt-uid: ExclusionExpressions
title: Exclusion Expressions
keywords: "configuration, exclusion expressions"
---
<!-- Ignore spelling: dd nn ss tt btn -->

This category lets you define regular expressions that are used to exclude spans of text from the spell checking
process.  This is useful for excluding things such as common format specifier patterns in designer code, CSS
class name patterns, control name patterns, etc.

> [!IMPORTANT]
> The expressions are applied to classified spans of text, not the file as a whole.  Therefore, the expressions
> cannot match text that spans one or more lines of text unless the editor classifies text in such a way that it
> include multiple lines as well.  Typically, this is not the case and you should not expect to be able to match
> multi-line spans of text.

To add a new expressions, click the **Add** button.  A dialog box will appear in which you can enter the
expression and indicate which options if any should be used with it (Ignore Case, Multi-line, and/or Single
Line).  A comment can also be entered to describe the entry.  To edit an expression, select it in the list
and click the **Edit** button or double click it.  The same dialog box will appear in which you can modify
the expression settings.  To remove an expression, select it in the list and click the **Remove** button.

For non-global settings, an option is available to inherit the exclusion expressions from configurations above
the current one.  If enabled, any additional expressions in the current configuration are added to those.  If
disabled, the settings in the current configuration will replace the inherited list of exclusion expressions.
If not inherited and the list is left empty, it effectively clears the list of exclusion expressions.

The following are some common date/time format specifiers that may appear in designer code.  In designer code,
the format specifiers are typically not contained with braces and are thus included in the spell checking
process.  This can result in a large number of misspelling reports for non-words such as "dd" and "yyyy" in
format specifiers such as "MM/dd/yyyy".  By using one or more exclusion expressions to ignore such spans, they
can be removed and not reported.

``` none{title="Example Exclusion Expressions"}
Various Date/Time Formats

(d{1,2}, )?yyyy
d{2,4},?\s{0,3}M{2,3}(/dd)?
(d{1,2}[/\- ])?yy(yy)?
d{3,4}( dd)?
h{1,2}:(mm|nn)(:ss)?(.f{1,4})?( tt| AM/?PM)?  Options: Ignore Case
M{2,4}[/\- ]d{1,2}
M{2,4} yyyy
yyyy-MM-dd

GUIDs

[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?  Options: Ignore Case
```
Another example is CSS class name or control ID patterns.  For example, adding the expression `btn[a-z\-]*?`
would prevent the ID and class names from being reported in the following line of Razor code in an HTML file.

``` html{title=" "}
@Html.MyButton("Save", id: "btn-save", classes: "btn btn-default my-btn-class")
```

> [!TIP]
> It is possible that a large number of exclusion expressions may slow down the overall spell checking process,
> especially for spell checking as you type.  It may be better to define expressions common to a particular type
> of project within that project's solution or project spell checking configuration file rather than putting all
> of them in the global configuration.

``` none{title=" "}
vsspell_exclusion_expressions_[sectionId] = [clear_inherited(?@@PND@@/Options/)][regEx1(?@@PND@@/Options/)]...

Note: Because expressions may contain any given character, the regular expressions are separated by their
options information which is included in the expression as a comment.  Due to a Visual Studio bug, # and ; are
encoded as @@PND@@ and @@SEMI@@.

sectionId = The unique ID for the section.
clear_inherited = If specified, clear all prior values and use only the settings in this property.  If omitted,
prior values from other property instances are inherited.
regEx1(?@@PND@@/Options/)... = One or more regular expressions to use and their options.
```

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
