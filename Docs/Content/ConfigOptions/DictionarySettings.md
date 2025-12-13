---
uid: af34b863-6a1c-41ed-bcf2-48a714686519
alt-uid: DictionarySettings
title: Dictionary Settings
keywords: "configuration, dictionary settings"
---

<autoOutline excludeRelatedTopics="true" lead="This category lets you configure the dictionary settings." />

## Additional Dictionary Folders
This option lets you define a set of additional folders to search for dictionary files, both
complete dictionaries (dictionary and affix file pairs) or just user dictionaries.  Enter the name of a folder or
click the "**...**" button to select a folder.  Relative paths are considered to be relative to the
configuration file's location.  For the global configuration, the paths should always be fully qualified.  For
solution, project, or folder settings files, the folders should typically be relative so that if the solution or
project moves, the dictionary files can still be found.  You are prompted when adding a folder if you would like
to make it relative to the current configuration file.  Environment variable references such as
`%USERPROFILE%\OneDrive\Dictionaries` are supported.

Once a folder has been added, the **Language(s)** combo box will be updated to contain any
additional languages for dictionaries found in the new folder.  Any user dictionaries found will replace the
default user dictionaries associated with the related languages.  This allows you to use the standard package
dictionary but store the user dictionary within a solution or project.  To provide consistent access to custom
dictionaries and user dictionaries, non-global configuration files will automatically include any additional
folders inherited from parent configuration files within the editor.

You can determine the type and location of the language dictionary and user dictionary by looking
at the labels below the user dictionary list box.  The left-hand label displays "Package dictionary" for a
standard dictionary supplied with the package or "Custom dictionary" for a custom dictionary.  The right-hand
label displays "Standard user dictionary" for a user dictionary located in the local application data folder or
one stored with the custom dictionary.  It displays "Alternate user dictionary" for a user dictionary stored in
some other location such as a solution or project folder.  Hover over either label for a tool tip that shows the
folder containing the language or user dictionary.

To remove a folder, select it in the list and click the **Remove** button.  To clear the list
of all additional folders, click the **Clear** button.

For non-global settings, an option is available to inherit the additional dictionary folders from
configurations above the current one.  If enabled, any additional folders in the current configuration are added
to those.  If disabled, the settings in the current configuration will replace the inherited list of folders.  If
not inherited and the list is left empty, it effectively clears the list of additional dictionary folders.  In
such a case, only the default dictionaries supplied with the package will be available for use.

> [!IMPORTANT]
> Dictionaries are global resources and are cached for reuse when subsequent files are opened
> within the same solution.  If you make changes to the dictionary locations used in a configuration, you must
> close and reopen the solution to ensure that the dictionary location changes take effect.
> 
> Since they are a global resource, user dictionaries are best specified at the solution file level
> if used.  They can be placed at the project level if there is only a single project in the solution.  If you have
> multiple project-specific dictionaries, only the first project that creates the dictionary will get it's
> project-specific dictionary added to it and it will be used across all projects since they are all using the same
> dictionary.  Since subsequent projects will not create the dictionary any user dictionaries in their
> configurations are ignored.  Likewise, if a solution configuration contains a user dictionary and a project
> contains a user dictionary, the project user dictionary would override the solution-level one rather than being
> additive to it.

``` none{title=" "}
vsspell_additional_dictionary_folders_[sectionId] = [clear_inherited]|[path1|path2|...]

sectionId = The unique ID for the section.
clear_inherited = If specified, clear all prior values and use only the settings in this property.  If omitted,
prior values from other property instances are inherited.
path1|path2|... = A pipe-separated list of additional paths to use in finding dictionaries.
```

## Language(s)
This option defines one or more languages to use for spell checking.  The package comes with
several dictionaries for various languages.  [Custom dictionaries](@508e7e8f-e00f-43f1-ad4c-5439ddec84b8)
can also be added and will be selectable here once installed.

In order to use a specific language, select it in the combo box and click the **Add** button
to add it to the list.  In the absence of a specific selection, the default English US language will be used for
the global configuration.  For non-global solutions, the inherited language selections will be used.

To add additional languages, select each in the combo box and click the **Add** button.  When
spell checking is performed, the dictionaries will be checked in the order given.  If your files are written
predominantly in a particular language, you can use the **Move Up** and **Move Down** buttons to alter
the language order so that the most common language is checked first followed by any others.  Click the
**Remove** button to remove an unwanted language.

For non-global configurations, an additional *Inherited* option will
appear in the list of selections.  This can be used to specify that the inherited languages should be included.
The inherited languages will be checked in the order they appear in the list.  For example, if you make French
the primary language and then add the Inherited option, the French dictionary will be checked first followed by
any inherited dictionary languages such as English US.  If the inherited option is not added to the list, only
the selected languages will be used (French alone in the example above).

> [!IMPORTANT]
> Adding multiple dictionaries will add extra overhead to the spell checking process and will
> cause additional memory usage.  Only add extra languages when necessary.  Consider using solution or project
> configurations rather than specifying them in the global configuration.

``` none{title=" "}
vsspell_dictionary_languages_[sectionId] = [langId1],[langId2]...,inherited,[langIdN]...

sectionId = The unique ID for the section.
langId1,langId2...,langIdN = A comma-separated list of languages to use for spell checking (en-US, fr-FR, etc.)
inherited = If present, this indicates the point at which inherited languages are inserted into the list.
If omitted, languages from prior configurations are ignored.
```

## Determine Localized Resource File Language from the Filename
This option allows you to specify whether or not to determine the dictionary language for localized
resource files based on their filename.  For example, if the file *LocalizedForm.de-DE.resx*
is opened in the XML file editor, the German language dictionary will be selected and used automatically if
available regardless of any other languages that have been selected.

> [!NOTE]
> If this option is enabled, it will override all other dictionary languages.  The determined
> language will be the only one used for spell checking the resource file.

``` none{title=" "}
vsspell_determine_resource_file_language_from_name = [true|false]
```

## User Dictionary
This option lets you see the content of the current user dictionary and remove unwanted items.
User dictionaries are language-specific and are associated with the currently selected language.  As such, the
content of the list box will be updated when a different language is chosen in the **Language(s)** combo box
or the list of selected languages to use for spell checking.  To remove an entry, select it in the list and click
the **Remove** button.

The **Import** button can be used to import a custom dictionary file.  The **Export**
button can be used to export the custom dictionary to a file for sharing.  Words can be imported from text files,
XML user dictionary files used by code analysis and StyleCop, and from StyleCop settings files. Words can be
exported to text files or XML user dictionary files.  When importing words from an XML user dictionary file, only
words without a `Spelling` attribute or ones on which it is set to `Add`
will be imported.  When exported, the `Spelling` attribute is set to `Add`
for any words added to or updated in the file.  When importing or exporting words, you will be asked whether you
want to replace the list of words or merge them with the existing words.

As noted above, the labels below the user dictionary list box can be used to determine the type
and location of the language dictionary and the user dictionary.  Hover over either label to show a tool tip
containing the location of the dictionary.  By default, user dictionary files are stored in the same folder as
the dictionaries with which they are associated.  Typically this will be the local application data folder for
custom dictionaries and those supplied with the package.  They are named after the associated language with an
*_User* suffix and a *.dic* extension (i.e. *en-US_User.dic*).

If you would prefer to store the user dictionaries in an alternate location, create an empty
file using the naming convention above or copy an existing user dictionary to your preferred location.  Add
the folder to the **Additional Dictionary Folders** list.  You should then see the user dictionary label
change to "Alternate user dictionary" for the affected languages.  The folder shown when hovering over the label
should be the location in which you stored the user dictionary file.

This is useful if you want to store the user dictionaries in a location that you can back up such
as your *My Documents* folder rather than the default local application data folder.  To do
this, copy the user dictionaries to your *My Documents* folder and add it to the global configuration.

By adding the user dictionary to a solution or project and adding a reference to the root folder
(*.\\*) in the solution or project configuration file, you can store user dictionaries with the
solution or project so that they can be checked in to source control.  This allows you to share the user
dictionary for the project amongst your team members while continuing to use the language dictionary supplied
with the package.  It also allows for project-specific user dictionaries without having to copy the same
language dictionary into each project especially if using one supplied with the package.

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
