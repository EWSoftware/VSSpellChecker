System Requirements
-------------------
In order to use the Visual Studio Spell Checker you will need either Visual Studio 2010 or Visual Studio 2012
(Professional, Premium, or Ultimate).

In order to build the source code you will need the following tools:

    Required:
        - Visual Studio 2010 SP1 (Required to build the C# projects for the tools)
        - Visual Studio 2010 SDK SP1 (Required for VSPackage development)
        - VSPackage Builder Extension (Required by the VSPackage project)
        - NuGet Package Manager Extension (Required to download the NuGet packages used by the main project)


Folder Layout
-------------
Deployment - This folder contains the deployment resources (the installer and all related files).  These are
used when creating the distribution package for the project.

Source - This folder contains the source code for all of the projects.


Building the Project
--------------------
To build the project, open the .\Source\VSSpellChecker.sln solution file and build it.  You can also run the
MasterBuild.bat script from a command prompt to build the projects.

TODO: Add info on setting up the VSPackage project for debugging (.user file settings).
