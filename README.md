This program is designed to help in merging profile and permission set metadata files for SalesForce.com. It works by taking source and target file, running comparison of it's XML nodes and checking if the nodes were added, removed or changed.

**Installation**   
Create new folder and save Wyndnet.SFDC.ProfileMerge.exe and components.xml there. Requires .NET framework 4.6 or higher

**Disclamer**   
It has not been thorougly tested for all situations, so ALWAYS double-check what resulting changes are.

**Standard Mode Instructions**   

Standard mode is launched when the program does not recieve any parameters.   

Source File and Target File buttons allow loading profile files that need to be compared or merged. Source is FROM profile (e.g. 'downstream' sandbox) and Target is TO profile (e.g. 'upstream' sandbox).   

Analyze button will compare two files and will present differences in the table. Clicking on the table entry will show the node differences.   

Merge button will copy selected differences to target file and save it with .merged extension in the same directory as target file.

**MergeTool Mode Instructions**   
In merge tool mode, the program is able to analyze not just the permission files themselves, but also contents of repository. After doing initial comparison of profiles, the program will scan metadata to determine whether components actually exist in current version of repository and make educated guesses if a change can be classifed as valid addition or deletion.   

When doing the merge, note that additions and deletions are in relation to your *local* version of the file. So when you select addition, expect that the element will be added from the remote version to local version. Deletion will remove the element from your local version. Change will bring over the remote version of the node to your local.    

In order for this feature to work best, it's adviced to first merge all other metadata, and work on profiles/permission sets last.    

Set the tool as git merge tool. In your .gitconfig add:

    [mergetool "pmerge"]
	cmd = C:/Users/MyUserName/Documents/some\\ folder\\ with \\spaces/bin/SFPermissionsMerge.exe \"$BASE\" \"$LOCAL\" \"$REMOTE\" \"$MERGED\"
    trustexitcode = false

To run the tool in the mergetool mode profile or permission set that needs merging run the command in ***ROOT*** path of repository, and NOT any level deeper.
     git mergetool -t pmerge src/profiles/KONE%3A\ KC3\ Agent.profile
     

**UI Commands**   
You can multi-select nodes by using selecting start node, holding SHIFT key and selecting end node. To enable merging for multi-selected nodes, press key 'A' on your keyboard. 

**Known Issues**   
Comparison of the Page Layout Assignments with record types is still work in progress because of the assignment logic complexity. Use extreme caurtion when merging LayoutAssignment nodes. You may easily get duplicates that will prevent profile deployment.   

*Debug merge mode in Visual Stuido*   
If you need to test merge mode in VS, use this example in project Properties -> Debug -> Command Line Arguments   
    "\BaseFileSample.profile" "\LocalFileSample.profile" "\RemoteFileSample.profile" "\MergedFileSample.profile"
