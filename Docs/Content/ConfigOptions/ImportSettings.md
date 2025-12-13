---
uid: b156d5ad-347f-4f63-89dc-4f945953ae41
alt-uid: ImportSettings
title: Import Settings
keywords: "configuration, import settings"
---

This category lets you specify another spell checker configuration file from which settings will be
imported when the containing configuration file is loaded for use.  This allows you to import settings from a
common, non-project-related location such as a company or team-specific configuration file, or from a settings
file in a shared project that isn't part of the current solution or project's folder structure such as a Git
submodule.


To specify the configuration file, enter its name in the text box or use the button to browse for
the file.  If specifying it in the global configuration, the path should be fully qualified.  For non-global
configuration files (solution, project, or folder configurations), relative paths will be considered relative to
the location of the configuration file containing the import file reference.


> [!TIP]
> .editorconfig files in parent folders all the way up to the root of the driver are automatically
> found and their settings used if applicable.  If the file you are importing is in a parent folder of this file,
> you do not need to specify it here unless it has a non-standard name.  By taking advantage of the normal behavior
> of .editorconfig files, it is likely that you will not need to use this option.
> 
>

> [!IMPORTANT]
> When specifying the import file in the global configuration, the settings in the imported file
> will override settings in the global configuration.  This is because the global settings file does not inherit
> from anything else.
> 
> 
> When importing a configuration file into any other non-global configuration file, the settings in
> the containing file will override settings from the imported file.  This allows you to import a common set of
> base configuration settings and then selectively override them in the containing configuration file.
> 
>

``` none{title=" "}
vsspell_import_settings_file_[sectionId] = [pathToImportedSettingsFile]

sectionId = The unique ID for the section.
pathToImportedSettingsFile = The path to the settings file to import.
				
```


## See Also


**Other Resources**  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
