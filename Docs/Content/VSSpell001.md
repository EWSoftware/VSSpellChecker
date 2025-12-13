---
uid: a7120f4c-5191-4442-b366-c3e792060569
alt-uid: VSSpell001
title: VSSpell001: Correct spelling of 'XXX'
keywords: "code analyzer, VSSpell001"
---
<table>
  <thead>
    <tr>
      <th>Item</th>
      <th>Value</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Rule ID</td>
      <td>VSSpell001</td>
    </tr>
    <tr>
      <td>Category</td>
      <td>Naming</td>
    </tr>
    <tr>
      <td>Fix is breaking or non-breaking</td>
      <td>Correcting the spelling in the identifier of a namespace or a public or protected type or
member is considered a breaking change.  For internal and private types and members, the change is non-breaking.</td>
    </tr>
  </tbody>
</table>

## Cause
One or more parts of an identifier are possibly misspelled.

## Rule Description
Code element spell check.

## How to Fix Violations
Select a suggested replacement to correct the spelling.

## When to Suppress Warnings
The warning can be suppressed if the word is correctly spelled but not in the dictionary by adding the word to
the dictionary.  If misspelled but you do not want it flagged, it can be added to an ignored words file defined
in the spell checker configuration settings or to an Ignore Spelling directive comment in the source code file.
An option is available to add it to an Ignore Spelling directive in the file.

> [!NOTE]
> There are currently no options available to add the word to a dictionary or ignored words file due to
> limitations of the code fix implementation that do not allow for modifying non-code files.  It must be added
> to them manually or by copying and pasting the word into a comment or string and adding it from there instead.
> See [](@a9ff4ce1-0d6b-4376-8d32-02dae64e2075) for more information.

## Configure Code to Analyze
See the [](@09cc5bfa-9eba-47e5-ba5f-a36e04f09b0d) configuration category topic for information on the available
options.

## Related Rules
None.

## See Also
**Other Resources**  
[](@12d44ba0-2bef-4fac-a6f9-7990ecf057c2)  
[](@3094ee74-88ae-4355-b702-23dcd55b4197)  
