---
uid: 508e7e8f-e00f-43f1-ad4c-5439ddec84b8
alt-uid: CustomDictionaries
title: Adding Custom Dictionaries
keywords: custom dictionaries
---

The package uses Hunspell to perform spell checking and comes with several dictionaries for various languages.
Since Hunspell makes use of Open Office dictionaries, you can download additional dictionaries for other
languages.  Dictionaries for OpenOffice versions 2, 3, and 4 are supported.  To make them available to the spell
checker, do the following:

- Go to the Open Office Extensions
  [dictionary page](http://extensions.services.openoffice.org/dictionary "Search for OpenOffice dictionaries")
  and download the dictionaries for the languages you need.  If the downloaded file has a *.oxt* extension,
  rename it to have a *.zip* extension.  Extract the content of the file and locate the *.aff* and *.dic*
  dictionary files.
- Another source for Open Office dictionaries is the
  [LibreOffice dictionary repository](https://cgit.freedesktop.org/libreoffice/dictionaries/tree "LibreOffice dictionary repository").
  Locate the language you want to use and download the *.aff* and *.dic* dictionary files for it.
- To make the dictionaries available to all solutions and projects, the *.aff* and *.dic* file pairs will need to
  be copied into the local application data folder which equates to one of the following folders based on your
  operating system:
  - Windows Vista or later: *%LOCALAPPDATA%\EWSoftware\Visual Studio Spell Checker*
  - Windows XP: *%USERPROFILE%\Local Settings\Application Data\EWSoftware\Visual Studio Spell Checker*
- Dictionaries can be stored in another location of your choosing.  If you do this, you will need to edit the
  global configuration and add the folder to the **Additional Dictionary Folders** list in the **Dictionary
  Settings** category.
- Dictionaries can be added to solutions and projects and checked into source control so that they are local to
  each project.  To do this:
  - Add the *.aff* and *.dic* files to the solution or project to which they will apply.
  - Add a [spell checker configuration file](@fb81c214-0fe0-4d62-a172-d7928d5b91d5) based on how you want the
    dictionaries to be made available and used.
  - In the configuration file, add the folder location to the **Additional Dictionary Folders** list in the
    **Dictionary Settings** category.  When prompted, make the path relative to the configuration file so that
    the dictionary files can be found if the project is moved.
- Note that the *.aff* and *.dic* files must be named after the language they represent with no other text in the
  filename and the language parts must be separated with an underscore or a dash.  If necessary, rename the files
  to match the required format.  For example:
  - *de_DE.aff* and *de_DE.dic* or *de-DE.aff* and *de-DE.dic* for German.
  - *sr_Latn.aff* and *sr_Latn.dic* or *sr-Latn.aff* and *sr-Latn.dic* for Serbian (Latin).
- Adding dictionary files to the local application data folder or a solution, project, folder, or file
  configuration for a language that matches one of the default languages supplied with the package will
  effectively replace the default dictionary files supplied with the package for that language.
- Once the files are in the chosen location and are named correctly, you will be able to select the related
  language in the **Dictionary Settings** category of the [configuration editor](@fb81c214-0fe0-4d62-a172-d7928d5b91d5).
  Custom dictionaries are noted with a value of "Custom dictionary" in the left-hand label below the user
  dictionary list box on the **Dictionary Settings** category page.  Hovering over the label will show a tool tip
  containing the location of the custom dictionary.

> [!NOTE]
> After installing the custom dictionary files and adding their location to the additional folders list when
> necessary, if the language does not appear in the configuration editor, the files may not be named correctly.
> If the language is selected but spelling is still occurring in English, there may be a problem with one or both
> of the dictionary files.  Search the
> [issues page](https://GitHub.com/EWSoftware/VSSpellChecker/issues "VSSpellChecker Issues") to see if the
> problem has been reported and solved already.  If you are not able to resolve the problem, open a new issue
> asking for help.

## See Also
**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
[](@027d2fbc-7bfb-4dc3-b4f5-85f95fcf7629)  
