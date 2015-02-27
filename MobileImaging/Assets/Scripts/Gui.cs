//NOTE: origin of GUI items is upper-left corner of the screen

using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;
using System;

public class Gui : MonoBehaviour {
	
	
	private string editString = "";
	private NumberFormatInfo n;
	
	
	public Texture sceneButtonSelectedTexture;
	public Texture sceneButtonWorldCenterTexture;
	private GUIContent[] sceneButtonContents;
	private MarkerRecorder markerRecorder;
	private ARWrapper arWrapper;
	private FileIO fileIO;
	public GUITexture redDot;
	private string pathToFile;
	private int worldCenterButton = -1;
	[HideInInspector]
	public float editFloat = 0.0f;
	
	
	[HideInInspector]
	public bool worldCenterScene = false;
	
	[HideInInspector]
	public int mergedScenesAmount = 0;
	
	[HideInInspector]
	public float hSliderValue;
	
	public enum MenuMode{
		
		OFF, // disable the menu
		HOME, 
		GAME,
		GEOMETRY,
		SETTINGS,
		ALIGN,
		SCENE
	}
	[HideInInspector]
	public MenuMode menuToShow = MenuMode.HOME;
	
	public enum SceneMode{
		
		LOAD, 
		SAVE,
		MERGE
	}
	[HideInInspector]
	public SceneMode sceneMode;
	
	private string messageRecording = "Recording...";
	private string messageSelect3Points = "Select 3 points first";
	private string messageSelect1Point = "Select 1 point first";
	private string messageKeepTmarkerInView = "Keep at least one tracking marker in view";
	private string messageNotWithOBJ = "This constraint can not be used with OBJ file";
	[HideInInspector]
	public string messageSelectGeometry = "Select geometry first";
	private string messageLoadScene = "Load or save a scene first";
	private string messageSceneSaved = "Scene saved";
	private string messageDoesNotExist = "This scene is not saved yet.";
	private string startRecordingFirst = "Start or save a recording first";
	
	//NOTE: the string and enum values must be in the same order!
	//The enum automatically assigns int values to them, starting from 0. So this is used to reference
	//the string index.
	
	[HideInInspector]
	public string[] homeButtonString;
	
	private string[] homeButtonStringStopRecording2 =   {"Stop Recording",	  "Stop Recording",   "Focus Camera", "Toggle Flash", "Reset All", "Load Scene", "Save Scene", "Merge Scenes", "Game Mode", "Add Geometry", "Settings", "Clear Selections", "Close App"};	
	[HideInInspector]  
	private string[] homeButtonStringStartRecording2 = {"Start New Recording","Active Update",   "Focus Camera", "Toggle Flash", "Reset All", "Load Scene", "Save Scene", "Merge Scenes", "Game Mode", "Add Geometry", "Settings", "Clear Selections","Close App"};
	private enum HomeButton {START_REC, ACTIVE_UPDATE, FOCUS, FLASH, RESET, LOAD, SAVE, MERGE, GAME, GEOMETRY, SETTINGS, CLEAR, FREEZE, CLOSE_APP, NONE}
	HomeButton homeButton = HomeButton.NONE;
	
	private string[] gameButtonString = {"Home", "Focus Camera", "Play Animation", "Close Menu"};
	private enum GameButton {HOME, FOCUS, PLAY_ANIMATION, CLOSE_MENU, NONE}
	private GameButton gameButton = GameButton.NONE;
	
	private string[] geometryButtonString = {"Home", "Focus Camera", "Add Plane", "Add Cube", "Add Cylinder", "Add Dot", "Align Geometry", "Delete Selected", "Load obj", "Clear Selections", "Freeze", "Play Animation", "Toggle Content", "Save"};
	private enum GeometryButton {HOME, FOCUS, PLANE, CUBE, CYLINDER, DOT, ALIGN, DELETE, LOAD, CLEAR, FREEZE, PLAY_ANIMATION, TOGGLE_CONTENT, SAVE, NONE}
	private GeometryButton geometryButton = GeometryButton.NONE;
	
	private string[] alignButtonString = {"Home", "Focus Camera", "Stretch to Point", "Align to Plane", "3 Points", "Align obj", "Clear Selections", "Add Geometry", "Delete Geometry", "Save" };
	private enum AlignButton {HOME, FOCUS, STRETCH_TO_POINT, ALIGN_TO_NORMAL, THREE_POINTS, ALIGN_OBJ, CLEAR, GEOMETRY, DELETE, SAVE, NONE}
	private AlignButton alignButton = AlignButton.NONE;
	
	private int menuButton = -1;
	
	private bool[] sceneButtonsSelected;
	
	
	//menu size
	[HideInInspector]
	public Rect menuRect2;
	
	[HideInInspector]
	public string message; 
	
	void Start () {
		
		
		n = CultureInfo.InvariantCulture.NumberFormat;
		
		
		menuRect2 = new Rect(25, 35, 380, 380); 
		
		//Set home menu
		menuToShow = MenuMode.HOME;
		
		markerRecorder = gameObject.GetComponent<MarkerRecorder>();
		fileIO = gameObject.GetComponent<FileIO>();
		arWrapper = gameObject.GetComponent<ARWrapper>();
		
		homeButtonString = homeButtonStringStartRecording2;
		
		//for recording animation
		this.StartCoroutine(BlinkDot());
		
		int sceneButtonAmount = markerRecorder.sceneObjects.Length + 1;
		sceneButtonContents = new GUIContent[sceneButtonAmount];
		sceneButtonsSelected = new bool[sceneButtonAmount];
		
		//set the first scene button to home
		sceneButtonContents[0] = new GUIContent("Home");
		
		//Now populate the scene button based on the name of the scene game objects 
		int index = 0;
		foreach(GameObject sceneObject in markerRecorder.sceneObjects){
			
			index++;
			sceneButtonContents[index] = new GUIContent(sceneObject.name);
		}		
	}
	
	void Update(){
		
		if(markerRecorder.mode == MarkerRecorder.Mode.GAME){
			
			//show hidden menu?
			if(markerRecorder.customTouchCount >= 3){
				
				menuToShow = MenuMode.GAME;
			}
		}
	}
	
	//For blinking dot.
	//This is not CPU friendly but it will do for now.
	private IEnumerator BlinkDot(){
		
		while(true){
			
			yield return new WaitForSeconds(0.5f);	
			
			if(markerRecorder.recording){
				
				redDot.enabled = !redDot.enabled;
			}
			
			else{
				
				redDot.enabled = false;
			}
		}
	}
	
	
	
	//NOTE: this GUI menu system is messy. I need to re-do it at some point. Waiting for the new Unity 4 GUI system...
	void OnGUI() {
		
		bool touchAValid = false;
		
		//is a button pressed by clicking on top of the file window?
		//BUGFIX: for some reason touchInfo sometimes suddenly is null. So check for this.
		if((markerRecorder.touchInfo != null) && (markerRecorder.customTouchCount > 0)){
			
			touchAValid = markerRecorder.IsTouchValid(markerRecorder.touchInfo.positionA, menuRect2, markerRecorder.fileRect, markerRecorder.fileWindowOpen, false, false);
		}
		
		//un-stick the buttons
		if(!markerRecorder.recording){
			
			menuButton = -1;
		}
		
		//reset
		homeButton = HomeButton.NONE;
		gameButton = GameButton.NONE;
		geometryButton = GeometryButton.NONE;
		alignButton = AlignButton.NONE;
		
		if(message != ""){
			
			GUI.Box(new Rect(0, 0, Screen.width, 30), message); 
		}
		
		switch (menuToShow)
		{
		case MenuMode.HOME:
			
			menuButton = GUI.SelectionGrid(menuRect2, menuButton, homeButtonString, 2);
			
			//draw a number edit box so the user can enter the scene scale.
			if(markerRecorder.recording){
				
				char chr = Event.current.character;
				
				//only allow numbers
				if((chr < '0' || chr > '9') && (chr != '.') && (chr != ',')){
					
					Event.current.character = '\0';
				}
				
				editString = GUI.TextField(new Rect(310, 35, 100, 20), editString);
				float temp = 0.0f;
				
				if(float.TryParse(editString, NumberStyles.Number, n, out temp)){
					
					editFloat = temp;
				}				
				else if(editString == ""){
					
					editFloat = 0;
				}
			}
			
			
			if(GUI.changed && touchAValid){
				
				ProcessHomeMenu(menuButton);
			}
			break;
			
		case MenuMode.GAME:
			menuButton = GUI.SelectionGrid(menuRect2, menuButton, gameButtonString, 2);	
			
			if(GUI.changed && touchAValid){				
				processGameMenu(menuButton);
			}
			break;
			
		case MenuMode.GEOMETRY:
			menuButton = GUI.SelectionGrid(menuRect2, menuButton, geometryButtonString, 2);
			
			markerRecorder.mode = (int)MarkerRecorder.Mode.GEOMETRY;
			
			if(GUI.changed && touchAValid){
				ProcessGeometryMenu(menuButton);
			}
			break;		
			
		case MenuMode.ALIGN:
			menuButton = GUI.SelectionGrid(menuRect2, menuButton, alignButtonString, 2);
			
			markerRecorder.mode = (int)MarkerRecorder.Mode.GEOMETRY;
			
			if(GUI.changed && touchAValid){
				ProcessAlignMenu(menuButton);
			}
			break;	
			
		case MenuMode.SCENE:
			
			menuButton = GUI.SelectionGrid(menuRect2, menuButton, sceneButtonContents, 2);
			
			if(GUI.changed && touchAValid){
				
				ProcessSceneMenu(menuButton);
			}
			break;
			
		case MenuMode.SETTINGS:
			
			markerRecorder.forceOutlineSetting = GUI.Toggle(new Rect(25, 25, 250, 20), markerRecorder.forceOutlineSetting, "Force Marker Outline or SLAM dots");
			
			markerRecorder.usePoseFilter = GUI.Toggle(new Rect(25, 50, 150, 20), markerRecorder.usePoseFilter, "Use Pose Filter");
			
			if((GUI.Button(new Rect (25, 150, 100, 70), "Home"))  && touchAValid){
				
				message = "";
				menuToShow = MenuMode.HOME;
				markerRecorder.HomeButtonPressed();
			}
			
			break;
			
		default:
			//MENU_OFF is nothing to do, so the menu will disappear
			break;
		}
	}
	
	
	void ProcessHomeMenu(int menuButton){				
		
		//this conversion is just done to get rid of the "not used" warning the compiler throws, 
		//otherwise the menuButton int can be used in the switch.
		homeButton = (HomeButton)menuButton;
		
		if((arWrapper.ready) && !markerRecorder.recording){
			
			message = "";
		}
		
		switch (homeButton){
			
		case HomeButton.FOCUS:
			markerRecorder.FocusButtonPressed();
			break;
			
		case HomeButton.RESET:
			
			homeButtonString = homeButtonStringStartRecording2;
			markerRecorder.ResetButtonPressed();
			break;	
			
		case HomeButton.FLASH:
			
			markerRecorder.FlashButtonPressed();
			break;
			
		case HomeButton.FREEZE:
			
			markerRecorder.FreezeButtonPressed();
			break;
			
		case HomeButton.CLEAR:
			
			markerRecorder.ClearSelectionsButtonPressed();
			break;			

			//start game mode
		case HomeButton.GAME:				
			
			if(markerRecorder.pathfindingFinished){
				
				markerRecorder.mode = (int)MarkerRecorder.Mode.GAME;
				
				menuToShow = MenuMode.GAME;
				
				markerRecorder.GameButtonPressed();
			}
			
			else{
				
				message = messageLoadScene;
			}			
			
			break;
			
		case HomeButton.GEOMETRY:
			
			if(markerRecorder.pathfindingFinished){
				
				//Are some scenes currently merged? If so, do not enter geometry mode.
				bool merged = markerRecorder.AreScenesMerged(markerRecorder.mergedScenes);
				
				if(!merged){
					
					markerRecorder.mode = (int)MarkerRecorder.Mode.GEOMETRY;					
					menuToShow = MenuMode.GEOMETRY;					
					markerRecorder.GeometryButtonPressed();
				}
				
				//Some scenes are merged, so display a warning message
				else{
					
					message = "Geometry mode can only be used if no scenes are merged";
				}
			}
			
			else{
				message = messageLoadScene;
			}
			
			break;
			
			//Start to record a new scene from scratch
		case HomeButton.START_REC:
			
			if(arWrapper.ready){
				
				//start marker recording?
				if(!markerRecorder.recording){
					
					markerRecorder.StartRecordingPressed(false);
					homeButtonString = homeButtonStringStopRecording2;
					
					message = messageRecording;
				}
				
				//stop marker recording
				else{
					
					markerRecorder.StopRecordingPressed();
				}
			}
			break;
			
			//Update the relative marker position and rotation transform from the currently loaded scene.
		case HomeButton.ACTIVE_UPDATE:			
			
			if(arWrapper.ready){			
				
				//start marker recording?
				if(markerRecorder.recording == false){
					
					if(markerRecorder.pathfindingFinished == true){
						
						markerRecorder.StartRecordingPressed(true);
						
						homeButtonString = homeButtonStringStopRecording2;
						
						message = messageRecording;
					}
					
					
					else{
						message = startRecordingFirst;
					}
				}				
				
				//stop marker recording
				else{
					markerRecorder.StopRecordingPressed();
				}				
			}
			
			break;
			
		case HomeButton.LOAD:
			
			ResetSelections();
			
			if(markerRecorder.recording){	
				
				markerRecorder.CancelRecordingPressed();
				homeButtonString = homeButtonStringStartRecording2;
			}
			
			message = "Select which scene you want to load";
			
			menuToShow = MenuMode.SCENE;
			sceneMode = SceneMode.LOAD;
			break;
			
		case HomeButton.SAVE:
			
			SaveButtonPressed();
			break;
			
		case HomeButton.MERGE:
			
			
			ResetSelections();
			
			message = "Select which scenes you want to merge";
			
			menuToShow = MenuMode.SCENE;
			sceneMode = SceneMode.MERGE;
			
			
			//
			//	message = "Merge scenes not supported for PointCloud";
			
			break;
			
		case HomeButton.SETTINGS:
			
			if(markerRecorder.recording){	
				
				markerRecorder.CancelRecordingPressed();
				homeButtonString = homeButtonStringStartRecording2;
			}
			
			menuToShow = MenuMode.SETTINGS;
			break;
			
			
			//close app
		case HomeButton.CLOSE_APP:
			Application.Quit();
			break;
			
		default:
			break;
		}
	}
	
	
	void ProcessSceneMenu(int menuButton){	
		
		//home button pressed
		if(menuButton == 0){
			
			if(sceneMode != SceneMode.MERGE){
				
				markerRecorder.HomeButtonPressed();
			}
			
			//We are in merge mode
			else{
				
				//Check how many scene buttons have been selected
				mergedScenesAmount = 0;
				for(int i = 0; i < sceneButtonsSelected.Length; i++){
					
					if(sceneButtonsSelected[i]){
						
						mergedScenesAmount++;
					}
				}
				
				//A world center button has been selected
				if(worldCenterButton != -1){
					
					//There are two or more scenes selected
					if(mergedScenesAmount >= 2){
						
						Array.Clear(markerRecorder.mergedScenes, 0, markerRecorder.mergedScenes.Length);
						
						//Loop through the selected scene buttons.
						for(int i = 0; i < sceneButtonsSelected.Length; i++){
							
							if(sceneButtonsSelected[i]){
								
								//get the path
								string path = Application.persistentDataPath + "/" + sceneButtonContents[i].text + ".ucs";
								
								string sceneName = fileIO.GetFileNameFromPath(path, false);
								int sceneIndex = markerRecorder.GetSceneIndex(sceneName, markerRecorder.sceneObjects);
								markerRecorder.mergedScenes[sceneIndex] = true;
							}
						}
						
						markerRecorder.HomeButtonPressed();
						
						//First set all the outlines to non-tracking marker
						markerRecorder.outlineType = MarkerRecorder.OutlineType.FORCE_NONTRACKING;
						
						//first delete all existing geometry
						markerRecorder.DeleteAllGeometryPressed();
						
						//Load the world center scene. The path is for the world center scene is
						//the last path set by the last button pressed.
						worldCenterScene = true;
						markerRecorder.OpenFile(pathToFile);
						
						//load the remaining scenes to be merged
						for(int i = 1; i < sceneButtonsSelected.Length; i++){
							
							//is this not the world center scene already loaded?
							if(i != worldCenterButton){
								
								if(sceneButtonsSelected[i]){
									
									//load the scene
									string path = Application.persistentDataPath + "/" + sceneButtonContents[i].text + ".ucs";
									
									worldCenterScene = false;								
									markerRecorder.OpenFile(path);
								}
							}
						}
					}
					
					//There are less then two scenes selected.
					if(mergedScenesAmount == 1){
						
						message = "Select two or more scenes";
					}
				}
				
				//A world center button has not been selected
				else{
					
					//Is any of the non-world center buttons selected?
					bool nonWorldSelected = false;
					for(int i = 1; i < sceneButtonsSelected.Length; i++){
						
						if(sceneButtonsSelected[i]){
							
							nonWorldSelected = true;
							break;
						}
					}
					
					if(!nonWorldSelected){
						
						markerRecorder.HomeButtonPressed();
					}
					
					else{
						
						message = "Select a World Center Scene";
					}
				}
			}
		}
		
		//Any other button then the home button is pressed.
		else{
			
			//get the ucs file name path depending on which scene button is pressed
			pathToFile = Application.persistentDataPath + "/" + sceneButtonContents[menuButton].text + ".ucs";
			
			if(sceneMode == SceneMode.LOAD){
				
				if(File.Exists(pathToFile)){
					
					markerRecorder.DeleteAllGeometryPressed();
					
					worldCenterScene = true;					
					
					//Load the world center scene.
					markerRecorder.OpenFile(pathToFile);
					
					//we have to get the amount of merged scenes after the OpenFile() call.
					mergedScenesAmount = markerRecorder.mergedScenesAmountFile;
					
					//If there are merged scenes in the saved file, we need to load them separately.
					if(mergedScenesAmount > 1){					
						
						//load the remaining scenes to be merged
						for(int i = 0; i < markerRecorder.mergedScenes.Length; i++){
							
							//is this not the world center scene already loaded?
							if((i != markerRecorder.worldCenterSceneIndex) && (markerRecorder.mergedScenes[i] == true)){
								
								//load the scene
								string path = Application.persistentDataPath + "/" + markerRecorder.sceneObjects[i].name + ".ucs";
								worldCenterScene = false;
								markerRecorder.OpenFile(path);
							}
						}
					}
				}
				
				//file does not exist
				else{
					
					message = messageDoesNotExist;
				}
			}
			
			if(sceneMode == SceneMode.SAVE){
				
				markerRecorder.SaveFile(pathToFile);
			}
			
			if(sceneMode == SceneMode.MERGE){
				
				//Does the scene exist?
				if(File.Exists(pathToFile)){
					
					//keep track of which buttons are selected 
					if(sceneButtonsSelected[menuButton]){
						
						sceneButtonsSelected[menuButton] = false;
						
						//change the button texture
						sceneButtonContents[menuButton].image = null;
						
						if(menuButton == worldCenterButton){
							
							worldCenterButton = -1;
						}
					}
					
					//not yet selected
					else{
						
						//first change all buttons to not-world center
						for(int i = 1; i < sceneButtonContents.Length; i++){
							
							//is the button already selected?
							if(sceneButtonsSelected[i]){
								
								sceneButtonContents[i].image = sceneButtonSelectedTexture;
							}
						}
						
						sceneButtonsSelected[menuButton] = true;
						
						//change the button texture
						sceneButtonContents[menuButton].image = sceneButtonWorldCenterTexture;
						worldCenterButton = menuButton;
					}
				}
				
				//file does not exist
				else{
					
					message = messageDoesNotExist;
				}
			}
		}
	}
	
	
	void processGameMenu(int menuButton){	
		
		gameButton = (GameButton)menuButton;
		
		switch (gameButton){
			
		case GameButton.HOME:
			message = "";
			menuToShow = MenuMode.HOME;
			markerRecorder.HomeButtonPressed();
			break;
			
			//focus camera (Vuforia only for now)
		case GameButton.FOCUS:
			markerRecorder.FocusButtonPressed();
			break;
			
			//close menu
		case GameButton.CLOSE_MENU:
			menuToShow = MenuMode.OFF;
			break;
			
		case GameButton.PLAY_ANIMATION:
			markerRecorder.AnimationButtonPressed();
			break;

		default:
			break;
		}
	}
	
	void ProcessGeometryMenu(int menuButton){
		
		bool allSelected = markerRecorder.Are3DotsSelected();
		
		geometryButton = (GeometryButton)menuButton;
		
		message = "";
		
		switch (geometryButton){
			
		case GeometryButton.HOME:
			message = "";
			menuToShow = MenuMode.HOME;
			markerRecorder.HomeButtonPressed();
			break;
			
			//focus camera (Vuforia only for now)
		case GeometryButton.FOCUS:
			markerRecorder.FocusButtonPressed();
			break;
			
		case GeometryButton.CLEAR:
			
			markerRecorder.ClearSelectionsButtonPressed();
			break;
			
		case GeometryButton.TOGGLE_CONTENT:
			
			markerRecorder.ToggleContentButtonPressed();
			break;
			
		case GeometryButton.DOT:
			
			if(!allSelected){
				
				message = messageSelect3Points;
			}
			
			else{
				if(markerRecorder.trackingFrozen){
					
					markerRecorder.AddDotButtonPressed();
				}
				
				else{
					message = "Freeze screen first";
				}
			}
			break;
			
		case GeometryButton.FREEZE:
			
			markerRecorder.FreezeButtonPressed();
			break;
			
		case GeometryButton.PLAY_ANIMATION:
			markerRecorder.AnimationButtonPressed();
			break;

			//add plane
		case GeometryButton.PLANE:	
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(allSelected){
					
					markerRecorder.PrimitiveGeometryButtonPressed(MarkerRecorder.GeometryType.PLANE);
				}
				
				else{
					
					message = messageSelect3Points;
				}
			}
			
			//Not for Slam
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
		case GeometryButton.CUBE:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(allSelected){				
					
					markerRecorder.PrimitiveGeometryButtonPressed(MarkerRecorder.GeometryType.CUBE);
				}
				
				else{
					
					message = messageSelect3Points;
				}
				
			}
			
			//not for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
		case GeometryButton.CYLINDER:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(allSelected){				
					
					markerRecorder.PrimitiveGeometryButtonPressed(MarkerRecorder.GeometryType.CYLINDER);
				}
				
				else{
					message = messageSelect3Points;
				}
			}
			
			//not for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
		case GeometryButton.ALIGN:
			menuToShow = MenuMode.ALIGN;
			break;
			
		case GeometryButton.DELETE:	
			
			if(markerRecorder.selectedGeometry == null){
				
				message = messageSelectGeometry;
			}
			else{
				
				markerRecorder.DeleteButtonPressed();
			}
			break;
			
		case GeometryButton.SAVE:
			
			markerRecorder.SaveWorldCenterScene();
			message = messageSceneSaved;
			break;
			
			//Load OBJ
		case GeometryButton.LOAD:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(allSelected){				
					markerRecorder.LoadButtonPressed();
				}
				
				else{
					message = messageSelect3Points;
				}
			}
			
			//not for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
		default:
			break;
		}
	}
	
	void ProcessAlignMenu(int menuButton){	
		
		int amount = markerRecorder.GetSelectionAmount();
		
		alignButton = (AlignButton)menuButton;
		
		ObjectLineManager selectedGeometryLineManager;
		
		message = "";
		
		if(markerRecorder.selectedGeometry != null){
			
			selectedGeometryLineManager = markerRecorder.selectedGeometry.GetComponent<ObjectLineManager>();
		}
		else{
			selectedGeometryLineManager = null;
		}
		
		switch (alignButton){
			
		case AlignButton.HOME:
			message = "";
			menuToShow = MenuMode.HOME;
			markerRecorder.HomeButtonPressed();
			break;
			
			//focus camera (Vuforia only for now)
		case AlignButton.FOCUS:
			
			markerRecorder.FocusButtonPressed();
			break;

		case AlignButton.STRETCH_TO_POINT:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(amount == 1){
					
					if(markerRecorder.selectedGeometry == null){
						message = messageSelectGeometry;
					}
					
					else{
						
						if(selectedGeometryLineManager.objectType == ObjectLineManager.ObjectType.OBJ){
							message = messageNotWithOBJ;
						}
						
						else{
							
							markerRecorder.AlignTypeButtonPressed(MarkerRecorder.AlignType.STRETCH_TO_POINT);
						}
					}
				}
				
				else{
					message = messageSelect1Point;
				}
			}
			
			//not for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
			
		case AlignButton.ALIGN_TO_NORMAL:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(amount == 3){
					
					if(markerRecorder.selectedGeometry == null){
						message = messageSelectGeometry;
					}
					
					else{
						
						if(selectedGeometryLineManager.objectType == ObjectLineManager.ObjectType.OBJ){
							message = messageNotWithOBJ;
						}
						
						else{
							
							markerRecorder.AlignTypeButtonPressed(MarkerRecorder.AlignType.ALIGN_TO_NORMAL);
						}
					}
				}
				
				else{
					message = messageSelect3Points;
				}
			}
			
			//nor for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
			
		case AlignButton.THREE_POINTS:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(amount == 3){
					
					if(markerRecorder.selectedGeometry == null){
						
						message = messageSelectGeometry;
					}
					
					else{
						
						if(selectedGeometryLineManager.objectType == ObjectLineManager.ObjectType.OBJ){
							
							message = messageNotWithOBJ;
						}
						
						else{
							markerRecorder.AlignTypeButtonPressed(MarkerRecorder.AlignType.THREE_POINTS);
						}
					}
				}
				
				else{
					message = messageSelect3Points;
				}
			}
			
			//not for SLAM
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
		case AlignButton.ALIGN_OBJ:
			
			if(markerRecorder.trackingMarkerInViewScenes[markerRecorder.worldCenterSceneIndex]){
				
				if(amount == 3){
					
					if(markerRecorder.selectedGeometry == null){
						
						message = messageSelectGeometry;
					}
					
					else{
						
						if(selectedGeometryLineManager.objectType != ObjectLineManager.ObjectType.OBJ){
							
							message = "This constraint can only be used with OBJ file";
						}
						
						else{
							
							markerRecorder.AlignTypeButtonPressed(MarkerRecorder.AlignType.ALIGN_OBJ);
						}
					}
				}
				
				else{
					message = messageSelect3Points;
				}
			}
			
			//not for slam
			else{
				message = messageKeepTmarkerInView;
			}
			break;
			
			
		case AlignButton.CLEAR:
			
			markerRecorder.ClearSelectionsButtonPressed();
			break;
			
		case AlignButton.SAVE:
			
			markerRecorder.SaveWorldCenterScene();
			message = messageSceneSaved;
			break;
			
		case AlignButton.GEOMETRY:
			menuToShow = MenuMode.GEOMETRY;
			break;
			
		case AlignButton.DELETE:
			
			if(markerRecorder.selectedGeometry == null){
				
				message = messageSelectGeometry;
			}
			else{
				
				markerRecorder.DeleteButtonPressed();
			}
			break;
			
		default:
			break;
		}
	}
	
	
	void ResetSelections(){
		
		//reset selections
		for(int i = 1; i < sceneButtonsSelected.Length; i++){
			
			sceneButtonsSelected[i] = false;
			sceneButtonContents[i].image = null;
		}
		
		mergedScenesAmount = 0;
		worldCenterButton = -1;
	}
	
	
	public void SaveButtonPressed(){
		
		ResetSelections();
		
		if(markerRecorder.pathfindingFinished){
			
			message = "Select which 3d content you want to link to this scene";	
			
			markerRecorder.mode = MarkerRecorder.Mode.HOME;	
			
			homeButtonString = homeButtonStringStartRecording2;
			
			menuToShow = MenuMode.SCENE;
			sceneMode = SceneMode.SAVE;
		}
		
		else{
			
			if(markerRecorder.recording){
				
				message = "Stop recording first";
			}
			
			bool merged = markerRecorder.AreScenesMerged(markerRecorder.mergedScenes);
			
			if(!markerRecorder.recording && merged){
				
				message = "Save the merged scenes";
			}
			
			if(!markerRecorder.recording && !merged){
				
				message = startRecordingFirst;
			}
		}	
	}
}
