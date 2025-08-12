//this is the Match-3 type project for my intern unity developer test
//Here are what I've done to create:
Step 1: Remake the logic of the game flow: 
- Create 2 different scenes: StartScene and GameScene
- Create start button in StartScene to load the GameScene
- Create fish prefabs, cell prefab using UI Image
Step 2: Create new game logic:
- Create empty GameScene
- Create a Canvas, set up the Canvas Inspector
- Create an Image as BG (background) as a child of the Canvas
- Create Empty GameObject, name it CollectionBar, then create 5 UI Image as child
- Create 3 Board, name Board_1, _2 and _3, each board has different width and height, add Grid Layout Group as component
- Create empty game object GameManager to hold scripts:
  + MultiBoardGenerator (handle the automatically spawn cell and fish to board, can adjust number of fish types that appear in each board), auto spawn bigger board when all fishes in smaller one are collected
  + GameResultManager: Handle the logic of Win Lose panel
  + AutoPlayManager: Handle the logic to auto play
- Create empty game object FishCollectionManager to handle Board order (smallest one comes first, bigger one shows later) and the animation of fish collection
- Create WinLose panel that contains Replay and Quit button
Step 3: Test game logic so that it fit the requirements.
