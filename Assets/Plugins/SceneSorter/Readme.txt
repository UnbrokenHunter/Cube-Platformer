SceneSorter 1.3
Copyright Porkchop Company LLC, 2023
All rights reserved

Overview
--------
SceneSorter came about through working on numerous large-scale projects where it was always a pain to coordinate work in multiple scenes. With SceneSorter, you have quick access to all the scenes in your project. Switching scenes is easy, and running different scenes is easy too. 

Open SceneSorter Window
-----------------------
Open Scene Sorter via Tools->Scene Sorter->Open Window on the main menu bar. You can either float or dock this window, whichever you prefer. Upon opening, SceneSorter scans your folder structure for scene files included in your project. Favorite scenes can be pinned to the top of the list by clicking the circle icon. Click again to unpin the scene.

Fast opening is done by clicking on the Folder/Load icon next to the scene name in the Scene Sorter window. Whichever scene is currently in memory is shown with the adorable computer icon.

Run any scene by clicking the Play button next to that scene's name.

You can change how the scene names are displayed by changing the value in the the Unity Preferences window (see below).

Fast Run Hot Key
----------------
The top 3 scenes can be fast-run by using the hotkeys: Alt-1, Alt-2, Alt-3
Upon exiting play mode, SceneSorter automatically reloads whatever scene you were previously editing.

Adjust Scene Order
------------------
Adjust the order of each scene in the list by clicking on the scene name, and little up and down arrows will appear. Click to move that scene up or down in the order.

Hide Unwanted Scenes
--------------------
If you have a load of clutter in your project, tests scenes, demo scenes, then hold the CTRL key and the window switches to hide mode. Here you can toggle the visibility of any scene. Its a great way to get rid of all that clutter so you can focus on the scenes you need.

Unity Preferences
-----------------
Open the Unity Preferences and you will see an entry for Scene Sorter. Here you can quickly show any scenes you have hidden, as well as change how the scene names are displayed in the window.

Multi-scene example
-------------------
Let's say Scene A is your boot scene, but you spend most of your time working in Scene B. Favorite Scene A and B, so they appear at the top of the order. Continue working in Scene B, and when you are ready to test, Alt-1 will fast-run and boot into Scene A. When you hit Stop, your editor will reload SceneB, allowing you to continue where you left off.

It's that simple! I hope you find Scene Sorter as useful as I do. Suggestions and tech support can be sent to chris@porkchopco.com

(The Examples directory can safely be deleted!)

Thanks, and happy SceneSorting!

Chris
Porkchop Company


======================================
Release Notes:
======================================

1.3 - 9/1/23
Clicking on the yellow SceneSorter banner changes how the scene names are display.
	- The same option is in the Preferences panel), but this is way easier.
Clicking on the scene name selects it in the Project view.
 	- Makes it easier to find your scene in the folder structure.


1.2 - 7/19/23
Preferences menu support.
Hide unwanted scenes from the list.
Change order of scenes in the list.
Optiosn for how the scene name is truncated in the list.
Fix for missing scroller.
Refreshed icons.


1.1 ? .... so what happened for 5 years? I thought Unity's new multi-scene editing would eliminate the need for SceneSorter so I was a little sad right after releasing it. I also started an ill-advised GUI system for editor windows. It was uncessesary and a bit over complicated. I had converted SceneSorter to use it, but I never finished it and so left SceneSorter in a bit of a messy state. However I continued to use SceneSorter on many (shipped!) products and eventually realized it was still very useful.


1.0 - 2017?!? - Initial release!
Favorite for easy access to your most used scenes.
