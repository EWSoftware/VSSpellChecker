---
uid: 63d5096c-6695-441d-886a-01a120f2894a
alt-uid: ShortcutKeys
title: Binding Spell Checker Options to Shortcut Keys
keywords: shortcut keys
---

The spell checker package does not assign shortcut keys to the options on the **Tools | Spell
Checker** submenu.  This avoids conflicts with built-in Visual Studio commands and other packages.  However,
you can assign the shortcut keys of your choice to them if you want as follows:


1. Select the **Tools** menu, then **Options**.
2. In the dialog box that appears, select the **Environment** category and then the
**Keyboard** subcategory.
3. In the "Show commands containing" textbox, type in "Spell" without the quotation marks.  This
will limit the command list to the spell checker commands.
4. Select one of the commands to which you would like to bind a shortcut key.  The commands
starting with "Tools." correspond to the menu items.  The others are for the Solution Explorer context menus and
you won't typically bind them to shortcut keys.
5. If assigning shortcut keys to the Move to Next/Prior Spelling Issue commands, it is recommended
that you change the "Use new shortcut in" combo box to "Text Editor" so that the shortcuts are limited in scope
to text editor windows.  The others can be assigned in the Global category.
6. To assign a shortcut key combination, select the "Press shortcut keys" text box and press the
shortcut keys to use.  To help avoid conflicts with existing command shortcuts, it is recommended that you use a
combination of the Ctrl, Alt, and/or Shift keys along with the selected shortcut key to find an unused one or one
that doesn't conflict with something else in the selected scope.  If the key combination you select has one or
more conflicts, they will be shown in the "Shortcut currently used by" combo box.  If you've limited the scope of
the shortcut key such as to the text editor, you can typically ignore any conflicts unless they are also in the
text editor.  If you left the scope set to Global, you may want to choose a different key combination.

  As an example, you can assign Ctrl+Alt+Right Arrow and Ctrl+Alt+Left Arrow to the Move to Next
and Move to Prior Spelling Issue commands with the scope limited to the text editor.  Depending on the keyboard
mapping scheme you have selected, you may see that these conflict with the key assignments to the Column to the
Right/Left commands in the HTML Editor Design View.  However, since this is a visual designer and not a text
editor, it won't cause any problems.


To remove a previously assigned shortcut key, follow the above steps and, once the command is
selected, click the **Remove** button to remove the assigned shortcut key.



## See Also


**Other Resources**  
[](@e339cac1-9783-4c2a-919f-88436c78fef8)  
