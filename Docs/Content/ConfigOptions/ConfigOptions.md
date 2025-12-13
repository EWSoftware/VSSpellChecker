---
uid: fb81c214-0fe0-4d62-a172-d7928d5b91d5
alt-uid: ConfigOptions
title: Configuration Options
keywords: "configuration options, categories"
---
<!-- Ignore Spelling: cpp cuh cxx hh hpp hxx ipp inl rc xpp alignas alignof asm -->

Configuration options for the spell checker are stored in .editorconfig files.  There is one global configuration
that contains the default settings.  Spell checker options can also be stored in .editorconfig files within a
solution, project, and/or folder.  This allows you to define different settings at each level while inheriting
the options that are not changed from higher level configurations.

When a project file that can be spell checked is opened, the global settings are merged with any defined
solution, project, folder .editorconfig settings.  In all settings other than global, properties default to being
inherited rather than being set to a specific default value.  If set to a specific value in a settings file, they
override any settings loaded earlier in the chain.  Additional dictionary folders, ignored filename patterns,
ignored words, ignored XML elements, and spell checked XML attributes get special treatment.  Each can be
inherited and added to or, if marked as not to be inherited, can replace the lists from previous settings files
earlier in the chain.  This provides for very flexible settings that should cover a wide variety of situations.

The global settings file is stored in the local application data folder (*%LOCALAPPDATA%\EWSoftware\Visual Studio
Spell Checker*).  These are the base settings used in the absence of, and inherited by, solution, project, and
folder configuration files.  The settings in the general section (**[*]**) will be applied to all files.
Additional sections with specific file globs are present to disable the spell checker in certain files or to
adjust the ignored words and keywords based on the file type.

Options that are set in other .editorconfig files will override the global settings and any other settings
inherited from .editorconfig files higher up in the folder structure.  Settings will be inherited by all files
within the folder and its subfolders.  Subfolders may themselves contain .editorconfig files that make further
changes to the spell checker configuration settings.

A file editor in Visual Studio allows you to modify the settings at any level by opening the settings file in the
project.  Global settings are modified by selecting the **Tools | Spell Checker | Edit Global Configuration**
option.

To add folder or file-specific settings, use one of the following methods when a solution file is loaded:

- Select the solution, a project, a folder, or a file in the **Solution Explorer** window and use the
  **File** | **New** | **Spell Checker Configuration for Selected Item** option to add a configuration for the
  selected item.
- Right click on the solution, a project, or a folder in the **Solution Explorer** window and use the
  **Add** | **Spell Checker Configuration** context menu option to add a configuration file for the selected
  item.
- Right click on a file that can be spell checked (source code, text, HTML, XML, etc.) or an .editorconfig file
  in the **Solution Explorer** window and use the **Spell Checker Configuration** context menu option to add or
  edit a configuration for the selected item.

In all cases, a new .editorconfig file will be added in the folder and project if one does not already exist.
If one does exist, a new section will be added to it for the selected folder or file if necessary.  The top
section of the editor allows you to manage the sections (file globs) within the .editorconfig file.  Selecting a
section will allow you to edit its settings in the bottom part of the editor.  Options are available to move
sections up or down in the order, edit the file glob for the section and add an optional spell checker specific
comment for the section, and if a section only contains spell checker settings, an option is available to delete
it.

To set the spelling tag underline color, select **Tools | Options | Environment | Fonts and Colors** and select
the **Spelling Error** display item.  The default color is magenta.  All other spell checker options are found
in the configuration editor that appears when you select **Tools | Spell Checker | Edit Global Configuration**
or open an .editorconfig file within a solution or project.  They are divided into several categories described
below.

- [](@b4a8726f-5bee-48a4-81a9-00b1be332607)
- [](@09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d)
- [](@af34b863-6a1c-41ed-bcf2-48a714686519)
- [](@c592c4d8-7387-47fe-9b79-28bf0168f447)
- [](@6216eedb-6434-4cad-be06-576814e0b735)
- [](@db9ee77f-6932-4df7-bd06-e94f20fc7450)
- [](@e01bd3d9-c525-4407-8c65-fcdb64539299)
- [](@6a987caf-5ad9-4dab-a17c-c887881fec7a)
- [](@b156d5ad-347f-4f63-89dc-4f945953ae41)
- [](@e23551ac-52f5-4505-b2d2-0728c7607fd3)

Once you have finished making changes to the configuration options, save the file.  Global configuration options
are saved to a file in the local application data folder where the custom dictionaries are kept and will be used
by all versions of Visual Studio in which the spell checker package is installed.  Solution, project, folder, and
file-specific settings files are stored within the solution or project to which they apply.

> [!NOTE]
> In general, you must close and reopen a spell checked file for any modified settings to take effect.
> Dictionaries are global resources and are cached for reuse when subsequent files are opened within the same
> solution.  If you make changes to the dictionary locations used in a configuration, you must close and reopen
> the solution to ensure that the dictionary changes take effect.  The exception is removing words from a user
> dictionary.  Changes to user dictionaries are immediate.

Click the **Reset** button to reset the configuration to its default state.  All options with the exception of
the user dictionary are reset to their default value.  Use the **Dictionary Settings** category to remove words
from the user dictionary.

## Section IDs
Normally, the values from configuration options with the same property name are replaced if encountered in a
section more relevant to a particular file or an .editorconig file closer to it in the folder structure.  For
example, settings from properties in a section for *[\*.cs]* files will override identical properties from the
global section (*[\*]*).  Several spell checker properties can be inherited across multiple sections and/or
.editorconfig files with their settings added to those from other sections and files (dictionaries, ignored
words, exclusion expressions, etc.).  To allow those settings to be inherited rather than replaced, their
property names are given a unique suffix within each section.  The `section_id` property is used to define the
unique ID for each .editorconfig section when such properties appear in it.  A GUID is used to guarantee unique
values and one will be generated by the configuration editor when needed.  Below are some examples for some of
the spell checker settings affected by this.

``` none{title="Section ID Example"}
[*]
# VSSPELL: Spell checker settings for all files
vsspell_section_id = 9f07c577adcd4fd7a93a42a503828225
vsspell_ignored_words_9f07c577adcd4fd7a93a42a503828225 = File:IgnoredWords.dic
vsspell_exclusion_expressions_9f07c577adcd4fd7a93a42a503828225 = [a-z]{2}-([A-Z]{2}|Cyrl|Latn)(?@@PND@@/Options/None)\\\\\w+(?@@PND@@/Options/None)

[*.resx]
# VSSPELL: Ignored resource file specific keywords
vsspell_section_id = 1C663502B9244D4DB52510C55DF2AB99
vsspell_ignored_keywords_1C663502B9244D4DB52510C55DF2AB99 = microsoft|mimetype|mscorlib|resheader|resx|utf

[*.{c,cc,cpp,cu,cuh,cxx,h,hh,hpp,hxx,ii,ipp,inl,rc,xpp}]
# VSSPELL: Ignored C/C++ language-specific keywords
vsspell_section_id = 2ED8EEE5E7BB4650A5DF3D504B15A4E5
vsspell_ignored_keywords_2ED8EEE5E7BB4650A5DF3D504B15A4E5 = alignas|alignof|asm|assert|atomic|auto|...
```

A comment starting with `# VSSPELL:` can be added to the section to provide comments on the spell checker
settings.  This comment is used by and can be updated using the configuration editor.

> [!NOTE]
> A bug in Visual Studio 2022 and earlier and the code analyzers within them truncates property values containing
> a pound sign (#) or semi-colon (;) as it incorrectly interprets them as a comment at the end of the line which
> is no longer allowed per the .editorconfig specification.  To work around this, the configuration editor
> encodes those characters as `@@PND@@` and `@@SEMI@@` if they occur within the value of an affected spell
> checker configuration property.  An example can be seen above in the exclusion expressions property.

## See Also
**Other Resources**  
[](@d9dc230f-ae34-464b-a3c2-4a7778907fc9)  
[](@e339cac1-9783-4c2a-919f-88436c78fef8)  
