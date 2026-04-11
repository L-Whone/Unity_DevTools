PG29VI New Scene Setup Tool - DD

Problem
Setting up new Unity Levels Requires a minimum of 5 minutes of setup time, You must create a new scene, wait for it to load, individually add the additive scenes
and manager, and add necessary prefabs to each scene one by one.

Solution
Unity Tool that automates new scene setup process editor window. User sets prefix name and lists the additive scenes they would want, click a button to generate
new scene setup and the full scene structure is created and saved.

Goal
Remove some repetitive manual scene creation steps
Enforce consistent naming conventions across scenes
Reduce scene setup time
Allow scene creation with necessary prefabs created per additive scene

Scope
Saves scenes to `Assets/Scenes/`
Automatically adds all scenes in Build Settings
Spawns prefabs the proper additive scene