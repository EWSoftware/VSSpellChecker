---
uid: d9dc230f-ae34-464b-a3c2-4a7778907fc9
alt-uid: ConvertingToEditorConfig
title: Converting to .editorconfig Files
keywords: "configuration options, conversion"
---

Starting with version 2023.5.8.0, configuration settings have been moved from the old XML
*.vsspell* configuration files to .editorconfig settings.  This is a significant change from
prior versions but has the following benefits:


- It allows consolidating the settings into fewer configuration files and places them in a
well-known, common location.
- It is easier to define settings for different classes of files by file glob or extension.  This
is useful for such things as ignoring language-specific keywords, common identifiers, prefixes, and suffixes in
source code files but not in other non-source code files so that they are flagged as misspellings.  For example,
it may be preferable to ignore a keyword such as `foreach` in source code comments but
flag it in a help topic where it likely needs a space between the words.
- It allows for the inclusion of settings from .editorconfig files outside of the solution folder
moving up to the root of the file system just like they are handled by the editor for other settings such as for
code formatting and other code analyzers settings.  This allows you to place team settings in a root folder
within the source tree so that they are applied consistently and automatically across all projects managed by the
team rather than having to manually specify that they be imported in the solution or project spell checker
configurations.
- It allows them to be used by the spell check code analyzer that spell checks identifiers also
added in that version.


When you open a file for editing in a solution containing old configuration files or if the old
global configuration has not been converted yet, you will be prompted to convert the settings by an info bar that
will appear just below the Visual Studio main menu bar.  Click the **Convert** link in the info bar to open
the conversion tool window.


> [!NOTE]
> The global spell checker configuration is also stored in the .editorconfig format but remains a
> separate configuration file stored in the same user settings location as the old configuration file
> (*%LOCALAPPDATA%\EWSoftware\Visual Studio Spell Checker*).  If you do not use solution/project
> spell checker configurations, only the global spell checker configuration file will need conversion and the
> currently loaded solution and its projects will remain unchanged.
> 
> 
> If a solution or project with old *.vsspell* configuration files does not
> currently contain an .editorconfig file, one will be added to it to store the spell checker settings.  If one
> does already exist, all other settings in it will remain unchanged.  Only the new spell checker settings will be
> added to it.  The conversion tool window shows a preview of the changes that will be made.
> 
>

<autoOutline lead="none" excludeRelatedTopics="true" />


## The Convert Spelling Configurations Tool Window

The conversion tool window consists of the following parts:


- A list box in the upper left that shows all of the old XML configuration files found in the
solution folder and all of its subfolders.  Note that this may include configuration files outside of the current
solution if multiple solutions exist within the folder structure.
- A list box in the upper right shows all of the .editorconfig files that will be created or
updated if they already exist, to contain the converted spell checker settings.  Note that there may be fewer
.editorconfig files than the old configuration files.  Old folder and file configurations will be merged into a
single folder-level .editorconfig file.  Likewise, if a solution and project configuration file exist, they will
be merged into a common .editorconfig file if they are within the same folder.
- A text box at the bottom of the tool window shows the content of the selected .editorconfig
file with the merged spell checker settings highlighted in bold.  A comment preceding the merged settings is used
to identify from which old configuration file they came.


If you select an old configuration file, the related .editorconfig file that it will be merged into
is selected and the merged settings are shown at the bottom of the tool window.  If the old settings file is
empty or all of its settings match the settings from another merged configuration, there won't be a section
for the selected file.  If converted, the old file will simply be removed from the project.



## Converting Settings

For simple projects consisting of a single solution file and one or more projects, conversion
should fairly straightforward.  Review the changes and, if they are acceptable, click the **Convert All**
button, or the **Convert Selected** button if there is only one file to convert.  The changes will be
applied to the .editorconfig files, new ones will be added to the project, and the old configuration files will
be deleted from the project.


The global configuration file will only appear the very first time that the conversion tool window
is used.  It can be converted separately using the **Convert Selected** button or along with the other
configurations if the **Convert All** button is used.  The old global configuration file will not be deleted
in case you have older projects that use older versions of Visual Studio with a version of the extension that
does not support the .editorconfig settings.


The list box containing the old configuration files allows for multiple selection.  You can select
multiple configuration files and click the **Convert Selected** button to only convert the selected files.
If you'd prefer to create and manually merge the changes into the .editorconfig files, you can copy the bolded
sections and paste them into the new files.  This can be done if you want to further consolidate the settings
into a higher level .editorconfig file other than the one chosen automatically.  You may need to adjust the file
globs to include a subfolder in such cases.


If multiple solution files are found within the folders, the **Convert All** button will be
disabled.  In such cases, it is likely that some of the old configuration files shown belong to the other
solution files that are not loaded or some of the files may be used in the other solutions.  It will be necessary
to manually convert the old configuration files one or a few at a time or copy and paste the settings into the
related .editorconfig files by hand.  You will likely also need to add the new .editorconfig files and remove the
old settings files from the other solutions on a case by case basis.


If you manually merge the settings into the .editorconfig files by copying and pasting them and
removing the old configuration files, you can click the **Refresh** button at the bottom of the tool window
to update the conversion information to see what is left to do.



## Special Considerations

There are a few cases that may require special attention or in which you may want to consider
consolidating settings into a single .editorconfig file.


- If you used the **Import Settings** option to import spell checker settings into a
configuration file from another, you will see a *TO DO:* note in the converted
settings indicating that you should review the use of the imported configuration file.  It is likely that the
settings in the imported file can be placed in a higher level configuration file and the import removed.  It can
be kept if you determine that the import is still needed.
- Review ignored words settings.  You may be able to consolidate them in a higher level
.editorconfig file or, preferably, move them to an ignored words file that is referenced by a higher level
.editorconfig file.
- If an .editorconfig file is created to contain the settings for one or more old file level
configuration files, you may move them to a higher level .editorconfig file.  If you do that, you will need to
manually copy and paste the settings and adjust the file glob to include the folder.


Old configuration files in the Solution Items folder of a project may be removed from the solution
but may not be deleted from disk.  You may have to go to the containing folder and manually delete the old
configuration file.  Likewise, if a new ignored words file is added, it may not appear in the project
automatically and you may have to add it manually.


> [!NOTE]
> It is possible that when the configuration conversion tool window is opened, hot keys will stop
> responding in the Visual Studio IDE for some unknown reason.  If this occurs, click on a main menu option such as
> **File** or **Edit** and then click back in the tool window.  Once done, hot keys will work as
> expected.
> 
>


## See Also


**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
[](@548dc6d7-6d08-4006-82b3-d5830be96f04)  
