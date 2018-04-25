Aseprite to Unity allows you to save time importing those awesome pixel art animations your artists have spent hours creating!

It's actually pretty simple to use. First, make sure your artists have followed standard practices for creating their animations.
This includes: 

	- saving all animations loops for the same object in .ase file
	- animation loops have names that accurately reflect their purpose in-game
	- all .ase files can be found under a single folder
	- hiding layers that should not be visible in the final sprite
	- (optional) setting loop types in Aseprite for animations that do not loop
	
Once that has been verified, now you must set the extraction settings in Unity.

	1) Aseprite.exe - In order to run this tool, you must have Aseprite installed on the local machine. This field is set to the default
	installation location acording to your machine, but if you've installed it in a custom folder you must browse to it.
	
	2) Art Source Folder - This is where all the .ase files are stored. ASE to Unity will also create .JSON representations that will be used
	to fully read all animation data, as it is unable to directly parse the .ase file itself.
	
	3) Sprites Folder - This is where you want store all your exported sprites. Because the tool also uses Unity's Resource manager, this folder
	must be located within a folder called "Resources". This folder can be anywhere in your projcet, and the Sprites folder need not be a direct
	child of it.
	
	4) Aseprite file - This is the file you want to import! If you have set the Art Source Folder to one that contains .ase files, this will list
	ALL of the .ase files found, INCLUDING FILES THAT MIGHT SHARE THE SAME NAME. Please make sure you have selected the correct file!
	
Next, it is time to import the animations. This can only be done if the proper extraction settings have been set. Now, you might notice that you have 3
options for how to import your file.

	Debugging Output - This doesn't import any animation data, but it will output information it was able to read from the JSON
	
	Applying Directly to Object - This allows you to update an object that currently exists within the scene with the newly created animation data
	
	Creating New GameObject - This will create a new GameObject with the necessary components into the scene. It will have the same name as the selected
	.ase file. You can also copy components from a reference GameObject into the newly created object.
	
Importing animations for the first time will create animation clips AND animation controllers specific to the .ase file. All animations will
be saved under "Resources/Animations/[ase file name]/", for improved organization and Resource Management. If a .controller file already
exists for the .ase you want to import but is not in the previously mentioned folder, a new one will be created and attached to the GameObject.

Organization

