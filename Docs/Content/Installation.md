---
uid: e339cac1-9783-4c2a-919f-88436c78fef8
alt-uid: Installation
title: Installation Instructions and Usage Notes
keywords: installation, usage notes
---

To install this package:


- If you currently have a spell checker extension installed, uninstall or disable it before
installing this package.  Examples are the ones in Resharper and GhostDoc.  If using Visual Studio 2022, disable
the built-in spell checker by going to **Edit** | **Advanced** and disabling the **Toggle Text Spell
Checker** option.
- Download the latest release of the VSIX installer from the [Releases](https://GitHub.com/EWSoftware/VSSpellChecker/releases) page of this project site.  Separate packages are available for Visual Studio 2013/2015,
Visual Studio 2017/2019, and Visual Studio 2022 and later.
- Close all open instances of Visual Studio before installing the package.
- Run the downloaded package and follow the prompts to install it.  It can be installed in any
edition of Visual Studio except Express editions.  Express editions are not supported as they do not allow
installation of third-party extensions.  Consider switching to the newer Community Edition which does support
them.


This package can also be installed from within Visual Studio from the Visual Studio Marketplace
([Visual Studio 2013/2015](https://marketplace.visualstudio.com/items?itemName=EWoodruff.VisualStudioSpellChecker),
[Visual Studio 2017/2019](https://marketplace.visualstudio.com/items?itemName=EWoodruff.VisualStudioSpellCheckerVS2017andLater), and [Visual Studio 2022 and Later](https://marketplace.visualstudio.com/items?itemName=EWoodruff.VisualStudioSpellCheckerVS2022andLater)) using the **Tools | Extension and Updates** option (Visual Studio 2017) or
**Extensions | Manage Extensions** option (Visual Studio 2019 or later).  Select the online marketplace and
search for "spell check my code".  Include the quote marks for an exact match and find the one created by
*EWSoftware*.  Once found, you can click the **Download** button to download and
install it.


> [!TIP]
> The spell checker contains many configuration options that can help you fine tune how and when
> spell checking occurs.  See the [](@fb81c214-0fe0-4d62-a172-d7928d5b91d5) topic for complete
> details and information on what each of the options does.  The global configuration can be edited using the menu
> option noted below.
> 
>

Once installed, you will find a new **Spell Checker** option on the **Tools** menu.
Unless other packages have been installed that alter its position, it will be the fourth or fifth option from the
bottom between the **External Tools** and the **Import and Export Settings** options.  Its submenu
contains following options:


- **Spell Check Current Document** - This opens the
[active document spell check tool window](@53ffc5b7-b7dc-4f03-9a51-ed4176bff504).
- **Move to Next Spelling Issue** and **Move to Prior Spelling Issue** - These can be
used in text editors to move to the next/prior spelling issue in the file.  If these are bound to shortcut keys
and used in conjunction with the Quick Actions shortcut key (**Ctrl+.**), it can be a quick and convenient
way to find and resolve spelling issues without using the interactive spell checking tool window.  See the
[](@63d5096c-6695-441d-886a-01a120f2894a) topic for more information.
- **Disable in Current Session**/**Enable in Current Session** - This option acts as a
toggle to temporarily disable and subsequently re-enable interactive spell checking in editors during the current
Visual Studio session.  This is separate from the **Spell check as you type** option in the configuration
files.  If that option is disabled, this menu option will have no effect.

  Use this option to temporarily suspend spell checking in the current session.  To turn it off
on a more permanent basis either globally or for a solution, project, folder, or file, use the configuration file
option instead.
- **Spell Check Entire Solution** - This opens the
[solution/project spell check tool window](@fa790577-88c0-4141-b8f4-d8b70f625cfd) and
immediately spell checks the entire solution.
- **Spell Check Current Project** - This opens the solution/project spell check tool window
and immediately spell checks just the currently selected project.
- **Spell Check Selected Items** - This opens the solution/project spell check tool window
and immediately spell checks just the items selected in the Solution Explorer window (projects, folders, files,
or any combination thereof).  If the solution node is selected, it effectively checks the entire solution.
- **Spell Check All Open Documents** - This opens the solution/project spell check tool
window and immediately spell checks just the files currently open for editing in Visual Studio.
- **Open the Solution/Project Spell Checker Window** - This opens the solution/project spell
check tool window but takes no action.
- **Edit Global Configuration** - This opens the
[configuration editor](@fb81c214-0fe0-4d62-a172-d7928d5b91d5) which lets you adjust the
global spell checker settings.


Correcting spelling errors within the editor is handled using the Quick Actions and Refactorings
context menu option or the smart tags.  See the [](@e8f67bc4-a8f8-4e50-ab5a-876599f3a645)
topic for more information.  A spell checker toolbar is also available.  Right click anywhere in the Visual
Studio toolbar area and select the Spell Checker option from the context menu to display it.



## See Also


**Other Resources**  
[](@e8f67bc4-a8f8-4e50-ab5a-876599f3a645)  
[](@53ffc5b7-b7dc-4f03-9a51-ed4176bff504)  
[](@fa790577-88c0-4141-b8f4-d8b70f625cfd)  
[](@fb81c214-0fe0-4d62-a172-d7928d5b91d5)  
[](@027d2fbc-7bfb-4dc3-b4f5-85f95fcf7629)  
