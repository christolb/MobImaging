/*
USAGE:
Please read the manual on how to use this script. However, this source code contains extensive information which is not in the manual
in the form of comments.

LICENSE:
This source code is provide free of charge. You can use it in its entirety or partly 
for both commercial or non commercial purposes. However, it is appreciated that any enhancements are communicated in 
detail to the author, so they can be added in a future release.

No Copyright applies. 
Bit Barrel Media

CURRENT VERSION:
v1.1

TODO:
-Make new GUI system (wait for update of Unity 4) which displays step by step relevant information. 
-PointCloud: fix visual glitch when switching between home and geometry mode.
-Add SLAM map save/load file error handling.
-disable merged scenes button for point cloud.
-Remove added dots when not used anymore and reset userDotAmount
-Flash something (button?) when error message pops up.
-Add material property to user created geometry and added special effect which uses this. (bullet, splinters)
-Place lights in the scene (user content creation) so that the scene can mimic shadows on the 3d content
apparently created by real world lights.
-Add camera shake special effect andreco make sure it is compatible with video texture. Make sure there is a bool which the
user can set to decide whether or not to use this feature because it will eat up some of the camera field of view.
Use 3rd party camera shaker if possible.
-Fix recording dot animation (not round and position incorrect). Use upcoming new GUI system for this.
-Pose Filter: Disable pose if the marker normal is at a too great angle with the camera normal. Check if really needed for 
use with Image Targets and String markers. Frame Markers already checked and is ok. Must be added to String as well.
-Replace static plane effect with dynamic effect.
-Add ability to rotate geometry manually.
-Add window property to geometry. This will make the geometry interact with physics but will not occlude AR content behind it.
-Add triangle geometry type. Do not scale it but set the vertex position instead. Merge selected triangles to 
a single object and make add it to the save file.
-Ability to place geometry at a known distance, such as from the room floor to the street.
-Add ability to snap each corner of a geometry cube to a dot. This way you can make skewed shapes.
-Make new frame markers. Area in middle should be more dark to allow for tracking in high light intensity areas.
-Create new image targets (same for String and Vuforia) which have a better tracking score.
-Publish in asset store (remove 3rd party data).
-Make sure that not more geometry then specified in the Editor max variable can be added.
-Multiplying is faster then dividing. Investigate if performance improvements can be made in the code.
-Any game objects with a collider which are moved in the scene should have a rigidbody attached for performance reasons.
-Use a kalman filter instead of low pass filter for smoothing out pose estimate rotational jitters. Remove the low pass 
filter from the marker itself and instead smooth out the movement of the camera after it has been placed with reference to
the markers. It is very unlikely that the physical camera will violently move around the scene, so this can be dampened. 
As an additional input for the kalman filter, the accelerometers can be used. Perhaps it is not even neccesary to use
the correct orientation of the various accelerometers, but any input will do, to detect whether the user did violently 
move the physical camera around. In that case the bias to the dampening can be reduced which allows for a more rapid
3d camera movement.
-When a new tracking marker enters the view or a marker is lost (when there are two or more tracking markers in view),
make the scene not jump to the new average transform but accelerate and decelerate (kalman filter?)
to that transform. This is to prevent 3d content from jumping position and orientation, however slight, and allow for
a more smooth transition.
-Check compatibility with playMaker "touch to walk". Add feature to automatically add an assigned script to each instantiated
UCS geometry? Or perhaps let the user design a playmaker enabled object, and then copy all that data to the UCS geometry...
Add this to manual as well.
-Investigate this smoothing logic: https://ar.qualcomm.at/content/filter-camera-position-and-rotation-avoid-jitter
-Make UCS an API.
-PointCloud: call pointcloud_destroy_point_cloud() after pointcloud_get_points() is called
and the visual points are not needed anymore. This is to free up memory. The original slam map
will still work.
-Reduce the amount of global variables.
-Implement vuforia init callback function for checkVidInit (Vuforia needs to implement this).
-Also make a normal arrow for any other 3 dots selected.
-Add visual arrow to align obj feature
-Fix freeze flicker bug Vuforia when using USE_FREEZE_A
-Separate code into other files.
-Make video tutorial on how to setup and use UCS.
-Enable this line when Vuforia bug fixed: UniFileBrowser.use.SendWindowCloseMessage(FileWindowClosed);
-Find out if GetMVPmatrix requires Unity pro. Find alternative if so.
-Try out obvious ar engine
-Fix video texture showing a thin line around the border.
-add error messages to the marker size file handling.
-add marker prefabs to ar wrapper similar to Vuforia.
-Change ARwrapper PointCloud installation in manual when PointCloud supports saving of scenes out of the box.
-add pointcloud to arwrapper example scene.
-Add SLAM implementation for PC.
-improve user friendliness of recording and saving merged scenes.
-Use stored line managers for the outline objects instead of getting one at runtime each time.
-check if 3d content is shown in geometry mode when frozen for all AR engines.
-Check special effects for all AR engines.
-Remove the scale factor edit box in non-slam configurations.
-Check different sized markers with String and Vuforia.
-Investigate the use of Editor Scripts to ease the installation procedure.
-Fix box texture mapping for special effect.
-Points lag with other content if camera is moved.
-add comments to save file so it is clear which variable is for what
-add documentation to manual on how to load a saved scene.
-Add support for StudierStube + source
-Fix bug with ShouldFitWidth not returning true on some devices
-Fix PointCloud compatibility
-Try global defines again and modify in manual.
-Fix compatibility (script execution order) with Vuforia 2.8

Exporting unityPackage:
-See arWrapper

NOTE:
A handy debugging output library called DebugStreamer can be used in this way to display debug text on screen:
DebugStreamer.message = ("Debug: " + variable);
*/

//Use Starscene Software UniFileBrowser?
//#define UNIFILEBROWSER

//Use Starscene Software Vectrosity?
//#define VECTROSITY

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if VECTROSITY
using Vectrosity;
#endif

public class MarkerRecorder : MonoBehaviour {
	
	//debug
	[HideInInspector]
	public bool debug1 = false;
	private bool debug2 = false;
	private bool debug3 = false;

	private GameObject disableGameObject;
	[HideInInspector]
	public GameObject geometryContainer;

	private bool firstRun = true;

	//to store the marker ID the dot4 object belongs to
	//private MarkerData[] markerDotData;

	[HideInInspector]
	public string[] mergedScenesNames;
	private string worldCenterSceneName = "";

	[HideInInspector]
	public int outlineType = -1;
	private int outlineTypeBuf = -1;

	private float timePassed = 0;

	[HideInInspector]
	public bool recording = false;
	private bool recordingUpdate = false;

	private Gui gui;
	private FileIO fileIO;
	private SpecialEffect specialEffect;

	//Maximum amount of points to be drawn on screen
	[HideInInspector]
	public static int maxDotAmount = 500;
	
	private float scaleFactor = 1.0f;
	private Vector3 cameraStartPosition;
	private Quaternion cameraStartRotation;

	private int currentDotAmount = 0;
	
	private ARWrapper arWrapper;
	
	[HideInInspector]
	public int mergedScenesAmountFile = 0;

	[HideInInspector]
	public Rect fileRect = new Rect(0, 0, 0, 0);

	[HideInInspector]
	public bool markerRecorderReady = false;
	
	[HideInInspector]
	public bool fileWindowOpen = false;
	private bool fileWindowOpenBuf = false;
	
	private int customTouchCountBuf = 0;

	[HideInInspector]
	public bool touchAValid = false;
	
	[HideInInspector]
	public int customTouchCount = 0; 
	
	[HideInInspector]
	public bool trackingFrozen = false;


	private int flip = 1;
	private bool showContent = true;
	
	private Vector2 positionAInitial;
	private Vector3 initialConstrainedPos;
	private Vector3 initialConstrainedScale;
	private bool modeGameOneshot = false;
	private bool modeHomeOneshot = false;
	private bool modeGeometryOneshot = false;

	[HideInInspector]
	public GameObject selectedGeometry;

	private Vector2 mouseAposGlobal = Vector2.zero;
	
	//maximum distance in pixels between mouse down and mouse up which will create a valid selection event
	private float maxSelectionDistance = 10.0f;
	
	//Distance in pixels which will be regarded a hit for selecting dots in screen space.
	private float distanceThreshold2d = 20.0f; //12
	private float distanceThreshold3d = 0.01f; 
	
	//the maximum amount of markers which can be selected at the same time in align mode.
	private static int maxSimultaniousDotSelections = 3;

	//maximum amount of user added dots
//	private static int maxuserDotAmount = 3;
	
	//position of object when it has to be disabled. This is many times faster then disabling using SetActiveRecursively.
	private float disableDistance = 50000.0f;

	public Material geometryModeMaterial;
	public Material gameModeMaterial;
	//public Material videoScreenMaterial;

	public LayerMask rayCastLayerMask;
	public GameObject[] sceneObjects;
	
	private Vector3[,] distanceVecBetweenMarkers;
	private Vector3[,] distanceVecBetweenMarkersAdd;
	private Quaternion[,] rotationDiffBetweenMarkers;
	private Quaternion[,] rotationDiffBetweenMarkersAdd;
	private int[,] addAmount;
	
	///////////////////////////////////////
	//These variables need to be saved to file when recording is done.
	//Do not change the order in which these variables appear here as it is used 
	//as a reference for the sorting when writing to and reading from a file.
	private float versionFile = 0.8f; //Current file version. Only change this number if the file logic changes.
	[HideInInspector]
	public bool[] mergedScenes;
	private bool[] mergedScenesBuf;
	[HideInInspector]
	public int worldCenterSceneIndex = -1;
	private bool[] markersForTracking;	
	private	Vector3[] offsetVectorToRefMarker;
	private	Quaternion[] offsetRotationToRefMarker;
	private int referenceMarkerNumber = -1;	
	private int geometryAmountFile = 0;
	private int geometryOBJAmountFile = 0;
	private int[] geometryTypeArray;
	private string[] geometryOBJNames;	
	///////////////////////////////////////	
	
	///////////////////////////////////////
	//These variables are similar to the ones above but are used 
	//during gameplay and are compatible with merged scenes
	private bool[,] markersForTrackingScenes;	
	private	Vector3[,] offsetVectorToRefMarkerScenes;
	private	Quaternion[,] offsetRotationToRefMarkerScenes;
	private int[] referenceMarkerNumberScenes;
	private GameObject[,] geometryObjectsScenes;
	private GameObject[,] geometryOBJObjectsScenes;
	///////////////////////////////////////	

	[HideInInspector]
	public bool[] trackingMarkerInViewScenes;

	[HideInInspector]
	public  bool forceOutlineSetting = false;
	
	public GameObject arrowPrefab;
	public GameObject trianglePrefab;
	public GameObject planePrefab;	
	public GameObject outlinePrefab;	
	public GameObject cubePrefab;
	public GameObject cylinderPrefab;
	public GameObject linePrefab;
	public GameObject dotPrefab1;
	
	private GameObject arrowObject;
	private GameObject triangleObject;
	private ObjectLineManager triangleLineManager;
	
	public GameObject axisAllPrefab;
	public GameObject objPrefab;
	
	public GameObject zoomScreenPrefab;	
	public GameObject backgroundCameraZoomScreenPrefab;	
	
	public GameObject dotsPrefab;
	public GameObject dotsSelectionPrefab;
	
	public int geometryAmountMax;	//the max amount of geometry objects that can be used. 

	private GameObject dotsObject;
	private SingleLineManager lineManagerDotsObject;
	[HideInInspector]
	public Quaternion dotsObjectRotation = Quaternion.identity;
	[HideInInspector]
	public Vector3 dotsObjectPosition = Vector3.zero;
	private Vector3[] dotsPositions = new Vector3[maxDotAmount];
	
	
	private GameObject selectedDotsObject;
	private SingleLineManager lineManagerSelectedDots;
	private int[] selectedDots = new int[maxSimultaniousDotSelections];
	private Vector3[] selectedDotsPositions = new Vector3[maxSimultaniousDotSelections];
	
	private GameObject userDotsObject;
	private SingleLineManager lineManagerUserDots;
	private Vector3[] userDotsPositions = new Vector3[maxDotAmount];
	private int userDotAmount = 0;
	
	private bool[] markerSeen; //no need to save this to file

	private GameObject markerLineOBJObject;	
	private GameObject markerDotOBJObject;
	private GameObject markerLineObject;
	private int OBJcycle = 0;
	
	[HideInInspector]
	public static Color colorNotForTracking = Color.red;
	[HideInInspector]
	public static Color colorForTracking = Color.green;
	[HideInInspector]
	public static Color colorForConstraint = Color.cyan;
	[HideInInspector]
	public static Color colorForSelected = Color.magenta;
	[HideInInspector]
	public static Color colorForObject = Color.blue;
	
	private bool mouseUpEvent = false;
	private GameObject axisAll;

	[HideInInspector]
	public int mode = (int)Mode.HOME;
	private int modeBuf = (int)Mode.HOME;
	private bool modeChanged = false;
	
	[HideInInspector]
	public GameObject[] outlineObjects;

	[HideInInspector]
	public bool pathfindingFinished = false;	

	private GameObject worldCenterObject;

	private bool arInitOneshotFired = false; 

	private SingleLineManager[] singleAxisLlinemanagers;	
	
	//for multitouch
	private bool scaleBeginPosSet = false;
	private float beginDistance = 0.0f;
	private float pinch = 0.0f;
	
	private bool moveBeginPosSet = false;
	private Vector3 dragOffset;
	
	private Vector3 planeNormalGlobal = Vector3.zero;
	private Vector3 linePosGlobal = Vector3.zero;
	private Vector3 lineVecGlobal = Vector3.zero;
	private bool scalingGlobal = false;

	private int globalHitIndex;
	private Vector3 OBJLinePoint; //object space
	private Vector3 OBJLineVector; //object space
	private Vector3 OBJHitNormal; //object space

	private SingleLineManager lineManagerOBJLine;
	private SingleLineManager lineManagerOBJDot;
	private SingleLineManager lineManagerMarkerLine;
	
	[HideInInspector]
	public bool usePoseFilter = true; // For Vuforia only

	//This game object is only used to do transform calculations and is not visible in the scene.
	//sceneObject is placed at 0,0,0, and the camera is transformed.
	//sceneObjectTransformed is transformed and the camera is at 0,0,0.
	private GameObject sceneObjectTransformed;
	
 
	//These game objects are only used to do transform calculations and are not visible in the scene.
	private GameObject tempGameObject;
	
	void OnApplicationQuit(){

		//global settings (not specific to ucs scene)
		PlayerPrefs.SetInt("forceOutlineSetting", Convert.ToInt32(forceOutlineSetting));
		PlayerPrefs.SetInt("usePoseFilter", Convert.ToInt32(usePoseFilter));
	}


	//Awake is called first, Start is called last
	void Start()
	{
		arWrapper = gameObject.GetComponent<ARWrapper>();		
		gui = gameObject.GetComponent<Gui>();
		fileIO = gameObject.GetComponent<FileIO>();
		specialEffect = gameObject.GetComponent<SpecialEffect>();

		cameraStartPosition = Camera.main.transform.position;
		cameraStartRotation = Camera.main.transform.rotation;
		
		sceneObjectTransformed = new GameObject("sceneObjectTransformed");
		
	//These game objects are only used to do transform calculations and are not visible in the scene.
		tempGameObject = new GameObject("tempGameObject");

		mode = Mode.HOME;
		
#if UNIFILEBROWSER
		UniFileBrowser.use.SendWindowCloseMessage(FileWindowClosed);
#endif

		//Instantiate the dots objects which will be rendered at screen;
		dotsObject = Instantiate(dotsPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		dotsObject.transform.parent = null;
		dotsObject.name = "dotsObject";		
		lineManagerDotsObject = dotsObject.GetComponent<SingleLineManager>();	
		
		selectedDotsObject = Instantiate(dotsSelectionPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		selectedDotsObject.transform.parent = null;
		selectedDotsObject.name = "selectedDotsObject";		
		lineManagerSelectedDots = selectedDotsObject.GetComponent<SingleLineManager>();

		userDotsObject = Instantiate(dotsPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		userDotsObject.transform.parent = null;
		userDotsObject.name = "userDotsObject";		
		lineManagerUserDots = userDotsObject.GetComponent<SingleLineManager>();

		lineManagerSelectedDots.ResetDotsSelections();

		//global settings (not specific to ucs scene)
		forceOutlineSetting = Convert.ToBoolean(PlayerPrefs.GetInt("forceOutlineSetting"));
		
		//set default value to true if it doesn't exist
		if(PlayerPrefs.HasKey("usePoseFilter")){
		
			usePoseFilter = Convert.ToBoolean(PlayerPrefs.GetInt("usePoseFilter"));
		}
		else{
			usePoseFilter = true;
		}
		
		//for mouse selection
		if(!Application.isEditor){
		
			Input.multiTouchEnabled = true;	
		}	
		
		touchInfo = new TouchInfoClass();
		constraintClass = new ConstraintClass();
		xPinchClass = new PinchClass();
		yPinchClass = new PinchClass();
		zPinchClass = new PinchClass();

		//These are global variables because they need to be updated from the previous frame. They cannot
		//loose their previous values at each frame.
		/////////////////////////////
		distanceVecBetweenMarkers = new Vector3[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];
		distanceVecBetweenMarkersAdd = new Vector3[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];
		addAmount = new int[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];
		rotationDiffBetweenMarkers = new Quaternion[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];
		rotationDiffBetweenMarkersAdd = new Quaternion[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];		
		markersForTracking = new bool[arWrapper.totalMarkerAmount]; //is this marker being used for tracking i.e, was it's position resolved after recording?
		offsetVectorToRefMarker = new Vector3[arWrapper.totalMarkerAmount];
		offsetRotationToRefMarker = new Quaternion[arWrapper.totalMarkerAmount];
		/////////////////////////////
		
		markersForTrackingScenes = new bool[sceneObjects.Length, arWrapper.totalMarkerAmount];	
		offsetVectorToRefMarkerScenes =	new Vector3[sceneObjects.Length, arWrapper.totalMarkerAmount];
		offsetRotationToRefMarkerScenes = new Quaternion[sceneObjects.Length, arWrapper.totalMarkerAmount];
		referenceMarkerNumberScenes = new int[sceneObjects.Length];
		trackingMarkerInViewScenes = new bool[sceneObjects.Length];
		geometryObjectsScenes = new GameObject[sceneObjects.Length, geometryAmountMax];
		geometryOBJObjectsScenes = new GameObject[sceneObjects.Length, geometryAmountMax];

		markerSeen = new bool[arWrapper.totalMarkerAmount];	//was this marker seen at least once during the recording stage?
		
		worldCenterObject = new GameObject("worldCenterObject");
				
		singleAxisLlinemanagers = new SingleLineManager[3];
		geometryTypeArray = new int[geometryAmountMax];

		mergedScenes = new bool[sceneObjects.Length];
		mergedScenesBuf = new bool[sceneObjects.Length];
		mergedScenesNames = new string[sceneObjects.Length];

		//Create a game object and place it behind the camera. This will be used to parent objects to
		//which have to be invisible.
		disableGameObject = new GameObject("disableGameObject");

		geometryContainer = new GameObject("geometryContainer");
		
		worldCenterObject.transform.position = Vector3.zero;
		worldCenterObject.transform.rotation = Quaternion.identity;	

		//Calculate the position of the disable game object. This is a location far away behind the camera.
		Vector3 translationVector = Math3d.SetVectorLength(-Camera.main.transform.forward, disableDistance);
		disableGameObject.transform.parent = Camera.main.transform;
		disableGameObject.transform.localPosition = Vector3.zero;
		disableGameObject.transform.Translate(translationVector, Space.Self);
		disableGameObject.transform.rotation = Quaternion.identity;
		
		geometryOBJNames = new string[geometryAmountMax];
		
		//These game objects are used to place all the AR content 
		for(int i = 0; i < sceneObjects.Length; i++){
			
			DisableFast(sceneObjects[i]);
			
			sceneObjects[i].transform.position = Vector3.zero;
			sceneObjects[i].transform.rotation = Quaternion.identity;			
		}

		DisableFast(geometryContainer);
		
		//create the axis system
		axisAll = Instantiate(axisAllPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		singleAxisLlinemanagers = axisAll.GetComponentsInChildren<SingleLineManager>();
		DisableFast(axisAll);
		
		//create the arrow
		arrowObject = Instantiate(arrowPrefab, Vector3.zero, Quaternion.identity) as GameObject;
		
		//create the triangle
		triangleObject = Instantiate(trianglePrefab, Vector3.zero, Quaternion.identity) as GameObject;
		triangleLineManager = triangleObject.GetComponent<ObjectLineManager>();

		DisableFast(triangleObject);
		DisableFast(arrowObject);

		outlineObjects = new GameObject[arWrapper.maxMarkerAmountInView];
		
		arWrapper.Init();			
		
		//instantiate one markerLine for the custom geometry object
		markerLineOBJObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity) as GameObject;
		markerLineOBJObject.transform.parent = null;
		markerLineOBJObject.name = "markerLineOBJObject";
		
		//dotPrefab1
		//instantiate one markerDot for the custom geometry object
		markerDotOBJObject = Instantiate(dotPrefab1, Vector3.zero, Quaternion.identity) as GameObject;
		markerDotOBJObject.transform.parent = null;
		markerDotOBJObject.name = "markerDotOBJObject";
		
		lineManagerOBJLine = markerLineOBJObject.GetComponent<SingleLineManager>();
		lineManagerOBJDot = markerDotOBJObject.GetComponent<SingleLineManager>();
		
		//change the color of the dot
		lineManagerOBJDot.SetDotsSelectionColor(0, colorForSelected);	

		DisableFast(markerLineOBJObject);
		DisableFast(markerDotOBJObject);
		
		//Create marker line
		markerLineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity) as GameObject;
		lineManagerMarkerLine = markerLineObject.GetComponent<SingleLineManager>();

		DisableFast(markerLineObject);

		DisableFast(dotsObject);
		lineManagerDotsObject.EnableVectorLine(false);

		DisableFast(selectedDotsObject);		
		lineManagerSelectedDots.EnableVectorLine(false);

		DisableFast(userDotsObject);		
		lineManagerUserDots.EnableVectorLine(false);

		for(int i = 0; i < selectedDots.Length; i++){

			selectedDots[i] = -1;
			lineManagerSelectedDots.SetDotsSelectionColor(i, colorForSelected);
		}

		markerRecorderReady = true;
	}
		
	
	void Update(){
		
		bool sceneChanged = false;
		bool somethingChanged = false;		

		if(arWrapper.ready){	

			if(!arInitOneshotFired){

				gui.message = arWrapper.message;
				CreateMarkerAccessories();
				arInitOneshotFired = true;
			}
			
			float scaleFactorMod;

			if(modeBuf != mode){

				modeChanged = true;
			}

			else{

				modeChanged = false;
			}

			if(!Math3d.CompareBoolArray(mergedScenes, mergedScenesBuf)){

				sceneChanged = true;
			}

			else{

				sceneChanged = false;
			}

			if((mode != MarkerRecorder.Mode.GEOMETRY) && (mode != MarkerRecorder.Mode.GAME)){
					
				scaleFactorMod = 1.0f;
			}

			else{

				scaleFactorMod = scaleFactor;
			}
			
			//Get all the tracking data such as marker/SLAM pose, id, etc.
			arWrapper.GetTrackingData(usePoseFilter, recording, scaleFactorMod);
			

			if(((arWrapper.markerCompositionInViewChanged || modeChanged) || sceneChanged || (outlineTypeBuf != outlineType) || firstRun) ){

				somethingChanged = true;
			}

			else{

				somethingChanged = false;
			}
			
			//make a list (array) of all the markers tracked in the current frame.
			//loop through all the markers in the project
			//NOTE: this code must be the last one in the function.
			Array.Clear(trackingMarkerInViewScenes, 0, trackingMarkerInViewScenes.Length);
		
			//merged scenes are not supported for SLAM
			if(arWrapper.markerType != ARWrapper.MarkerType.SLAM){

				for(int sceneIndex = 0; sceneIndex < sceneObjects.Length; sceneIndex++){

					//is this scene part of a merged scene?
					if(mergedScenes[sceneIndex] == true){
			
						for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

							if(arWrapper.markerId[i] != -1){

								//Is this marker used for tracking (recorded and resolved during recording process)?
								if(markersForTrackingScenes[sceneIndex, arWrapper.markerId[i]]){
							
									trackingMarkerInViewScenes[sceneIndex] = true;
								}		
							}
						}
					}					
				}
			}

			//Place the merged scenes and the content
			//First place the world center scene so the camera transform is known
			PlaceCameraAndSceneObjects(worldCenterSceneIndex);

			for(int i = 0; i < sceneObjects.Length; i++){
				
				//Now place the rest of the scenes
				if((mergedScenes[i] == true) && (i != worldCenterSceneIndex)){
				
					PlaceCameraAndSceneObjects(i);					
				}
	
				PlaceContent(i, somethingChanged);
			}

			//Has the marker composition possibly changed?
			if(somethingChanged && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){

				//set the correct scale of the accessories
				if((mode != Mode.GAME) || ((mode == Mode.GAME) && forceOutlineSetting)){

					for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

						if(arWrapper.markerId[i] != -1){

							outlineObjects[i].transform.localScale = new Vector3(arWrapper.markerSize[arWrapper.markerId[i]].x * 2.0f, 0.001f,  arWrapper.markerSize[arWrapper.markerId[i]].y * 2.0f);
						}
					}
				}

				//set the marker outline colors
				SetOutlineObjectColors(outlineObjects, markersForTrackingScenes, colorForObject, colorNotForTracking, outlineType);
			}
			

			Vector3 hidePosition;

			//Set the SLAM dot positions
			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				hidePosition = dotsObject.transform.InverseTransformPoint(Camera.main.transform.position);

				//set the position of all the SLAM points
				if(arWrapper.SLAMpoints != null){
					
					currentDotAmount = PopulateDotsSLAM(ref dotsPositions, arWrapper.SLAMpoints, hidePosition);
				}
				else{
					
					currentDotAmount = 0;
				}
			}

			//set the other dot positions such as the marker corner dots
			else{

				hidePosition = Camera.main.transform.position;

				//set all the dot positions
				currentDotAmount = PopulateDotsMarkers(ref dotsPositions, hidePosition);	
			}

			PopulateDotsSelections(ref selectedDotsPositions, selectedDots, hidePosition);
			PopulateDotsUser(ref userDotsPositions, userDotAmount, hidePosition);

			lineManagerSelectedDots.SetDotsPositions(selectedDotsPositions);
			lineManagerUserDots.SetDotsPositions(userDotsPositions);
			lineManagerDotsObject.SetDotsPositions(dotsPositions);

			//The disable gameObject has to have an identity rotation, otherwise physics object might
			//fall off the screen while no markers are in view.
			disableGameObject.transform.rotation = Quaternion.identity;

			

			if(recording){	

				ProcessRecording();	
			}

			//Execute ProcessRecordingUpdate every so many seconds. We could use
			//coroutines but the IEnumerator logic is really crap.
			if(recordingUpdate){

				timePassed += Time.deltaTime;

				if(timePassed >= 5.0f){

					ProcessRecordingUpdate();
					timePassed = 0;
				}
			}

			modeBuf = mode;
			outlineTypeBuf = outlineType;
			Array.Copy(mergedScenes, mergedScenesBuf, mergedScenes.Length);
			firstRun = false;
		}

		//arWrapper not ready
		else{

			gui.message = arWrapper.message;
		}
		
		//this must be done after the camera has been set
		ProcessTouch();
	}

	
	//Dijkstra path finding algorithm.
	//The input of this function is an array called "dist" which is a 2 dimensional array which
	//indicates which nodes are connected (with no distance information). Another input is the
	//marker (reference marker) for which to calculate the path to (s). The output is an array
	//called "prev". This array contains the path (as node numbers, not the actual distances) for
	//each marker to the reference marker. But that format is a bit cryptic and has to be reformatted 
	//using the function getPath();
	int[] Dijkstra(int s, int[,] dist) {
		
		int i;
		int k;
		int mini;
		int[] d = new int[arWrapper.totalMarkerAmount];
		int[] prev = new int[arWrapper.totalMarkerAmount];		
		
		if(s != -1){				
			int[] visited = new int[arWrapper.totalMarkerAmount];
		
			for (i = 0; i < arWrapper.totalMarkerAmount; ++i) {
				d[i] = arWrapper.totalMarkerAmount*arWrapper.totalMarkerAmount; 
				prev[i] = -1; // no path has yet been found to i 
				visited[i] = 0; // the i-th element has not yet been visited 
			}
		
			d[s] = 0;
		
			for (k = 0; k < arWrapper.totalMarkerAmount; ++k) {
				mini = -1;
		
				for (i = 0; i < arWrapper.totalMarkerAmount; ++i){
					if ((visited[i] == 0) && ((mini == -1) || (d[i] < d[mini])))
						mini = i;
				}
		
				visited[mini] = 1;
		
				for (i = 0; i < arWrapper.totalMarkerAmount; ++i){
		
					if (dist[mini, i] != 0){
		
						if (d[mini] + dist[mini, i] < d[i]) {
							d[i] = d[mini] + dist[mini, i];
							prev[i] = mini;
						}
					}
				}
			}
		}
		
		return prev;
	}
	
	
	//Extract the path from the input marker to the reference marker from the Dijkstra output.
	//The input is the marker to find the path to the reference marker for (dest). Another input is
	//an array called "prev", which is the output of the Dijkstra algorithm. The output is
	//an array called "path". The format of this array is:
	//3,5,0,-1,-1,-1,
	//With 0 as the reference marker, and 3 as the marker for which to find the path to the
	//reference marker for.
	int[] GetPath(int dest, int[] prev){
		
		int[] path = new int[arWrapper.totalMarkerAmount];
		int index = 0;
	
		//set the array
		for(int i = 0; i < arWrapper.totalMarkerAmount; i++){
			path[i] = -1;
		}	
		
		//bail out if the path does not exist
		if(prev[dest] == -1){
			return path;
		}
	
		path[0] = dest;
		index++;
	
		while(prev[dest] != -1){
	
			path[index] = prev[dest];			
			dest = prev[dest];
			index++;
		}
		
		return path;
	}
	
	
	//Get the amount of markers in the path from the current marker to the reference marker. 
	//The input is an array called "path" which contains the path from the current marker to 
	//the reference marker. The output is the amount of markers (nodes) in the path found.
	//This function outputs the total amount markers in one found path.
	int GetNodeCount(int[] path){

		int count = 0;
		
		for(int i = 0; i < arWrapper.totalMarkerAmount; i++){
			if(path[i] != -1){
				count++;
			}
			
			else{
				return count;
			}
		}
		
		return count;
	}

	
	
	//*Pathfinding algorithm Done as post processing.
	//*The pathfinding is used to find the location of each marker in relation to the reference marker. For some
	//markers, the location with reference to the reference marker can be easily calculated if the two markers
	//are visible in the same frame. However, for most markers, this is not the case. The location of those markers
	//must be calculated by finding a path between the marker in question and the reference marker. Using all
	//markers which lie in between the two markers, the relative position of the marker can be calculated.
	//*There are a few different methods to find the relative position of each marker to the reference marker.
	//One method is to find all possible paths, and then average the relative position. However, as each marker
	//contains a certain error in it's position and rotation, all the errors will be added up. So most likely it
	//is better to find the path with the least amount of markers in between, which is currently done.
	//*This function outputs the relative location and orientation of each marker relative to the reference marker.
	//The input is: distanceVecBetweenMarkers and rotationDiffBetweenMarkers. These contains the actual distances
	//and rotational information between markers.
	//The output is: markersForTracking (array which indicates which markers are used for tracking), and
	//offsetVectorToRefMarker, and offsetRotationToRefMarker
	void PathFindingDijkstra(ref bool[] markersForTracking, ref Vector3[] offsetVectorToRefMarker, ref Quaternion[] offsetRotationToRefMk, Vector3[,] distanceVecBetweenMarkers, Quaternion[,] rotationDiffBetweenMarkers, int referenceMarkerNumber){
		
		if(referenceMarkerNumber != -1){
		
			int nodeAmountInPath = 0;
			Vector3 offsetVectorToRefMarkerTmp;
			Quaternion offsetRotationToRefMarkerTmp;
			pathfindingFinished = false;

			//for Dijkstra
			int[] prev = new int[arWrapper.totalMarkerAmount];
			int[] path = new int[arWrapper.totalMarkerAmount];
			int[,] dist = new int[arWrapper.totalMarkerAmount, arWrapper.totalMarkerAmount];
	
			//Reset
			for(int i = 0; i < arWrapper.totalMarkerAmount; i++){ //go down in the array 
				
				markersForTracking[i] = false;
				offsetVectorToRefMarker[i] = Vector3.zero;
				offsetRotationToRefMarker[i] = Quaternion.identity;
				
				for(int e = 0; e < arWrapper.totalMarkerAmount; e++){//go sideways in the array
					dist[i, e] = 0;
				}
			}	

			//Populate an array used as an input for the Dijkstra algorithm.
			//The algorithm used is for a directional path, meaning it might be possible to go from A to B but
			//not from B to A. However, our implementation is un-directional. So fill the array with a mirror 
			//image. Note that this method involves redundant computation and it is to be optimized in the future.
			//Also, in our implementation, the actual distance between the nodes (markers) is irrelevant. So the
			//weighting variable for the Dijkstra algorithm is to be changed to 1, which means the effective 
			//distance between all neighboring markers is 1. It would be optimal to modify the Dijkstra algorithm
			//but this will be done at a later stage. At the moment a new array is created, compatible with Dijkstra.
			//This will increase the memory usage though.		
			for(int i = 0; i < arWrapper.totalMarkerAmount; i++){ //go down in the array
				
				for(int e = 0; e < arWrapper.totalMarkerAmount; e++){//go sideways in the array
					
					//is there a valid distance between these markers?
					if(distanceVecBetweenMarkers[i, e] != Vector3.zero){
						dist[i,e] = 1;
						
						//mirror the result
						dist[e,i] = 1;
					}
				}
			}
			
			//Find the path to all markers from the reference marker. The reference marker is considered
			//the start node for the Dijkstra algorithm.
			//The input of this function is an array called "dist" which is a 2 dimensional array which
			//indicates which nodes are connected (with no distance information). Another input is the
			//marker (reference marker) for which to calculate the path to. The output is an array
			//called "prev". This array contains the path (as node numbers, not the actual distances) for
			//each marker to the reference marker. But that format is a bit cryptic and has to be extracted
			//using the function getPath();
			prev = Dijkstra(referenceMarkerNumber, dist);
			
			//Loop through all the markers and calculate it's offset to the reference marker.
			for (int i = 0; i < arWrapper.totalMarkerAmount; i++) {			
				
				//A reference marker has to be selected
				if(referenceMarkerNumber != -1 )
				{			
					//Extract the path from the current marker to the reference marker from the Dijkstra output.
					//One input is the marker to find the path to the reference marker for. Another input is
					//an array called "prev", which is the output of the Dijkstra algorithm. The output is
					//an array called "path". The format of this array is:
					//3,5,0,-1,-1,-1,
					//With 0 as the reference marker, and 3 as the marker for which to find the path to the
					//reference marker for.
					path = GetPath(i, prev);					
					
					//Get the amount of markers in the path from the current marker to the reference marker. 
					//The input is an array called "path" which contains the path from the current marker to 
					//the reference marker. The output is the amount of markers (nodes) in the path found.
					nodeAmountInPath = GetNodeCount(path);
					
					//Store the markers used for tracking in an array. This is to differentiate between which markers
					//are used for tracking purposes only, and which ones are used for other purposes such as geometry
					//creation. This array isn't used in this function but it will be used at another location.
					if(nodeAmountInPath > 0){
					
						markersForTracking[i] = true;
					}
					
					//If if the current marker is the reference marker,
					//mark it as a marker for tracking as well.
					if(referenceMarkerNumber == i){
					
						markersForTracking[i] = true;
					}					
					
					//The path from the reference marker to the current marker is now in an array called "path"
					//use this path to calculate this marker's offset in location and rotation to the reference marker.
					//Calculate the position offset. The distance vector is calculated as e-i, which means
					//the vector is from i to e. "i" are the the columns in the array going down, and "e" are the 
					//columns going right (sideways). For example, the path to be calculated is 3,5,0, with 
					//0 as the reference marker and 3 the marker for which the offset has to be calculated. Then
					//the distance vectors to be used are 3 to 5 and 5 to 0. Now, if 3 to 5 is not available, but
					//5 to 3 is, then 3 to 5 has to be calculated. This is done by reversing the vector.				
					offsetVectorToRefMarkerTmp = Vector3.zero; //reset
					offsetRotationToRefMarkerTmp = Quaternion.identity; //reset
					for(int z = 0; z < (nodeAmountInPath-1); z++){	
					
						//Add the vectors together to get the final single vector.
						offsetVectorToRefMarkerTmp += distanceVecBetweenMarkers[path[z], path[z+1]];
						
						//Add the rotations together to get the final rotation. Note that the math is different.
						offsetRotationToRefMarkerTmp *= rotationDiffBetweenMarkers[path[z], path[z+1]];
					}
					
					//store the offset for the current marker in an array.
					offsetVectorToRefMarker[i] = offsetVectorToRefMarkerTmp;
					
					//do the same for the rotation
					offsetRotationToRefMarker[i] = offsetRotationToRefMarkerTmp;					
				}
			}			
			pathfindingFinished = true;			
		}
	}
	
	void ResetDotSelections(){

		//selectedSingleDotBuf = -1;

		//reset the selection array
		for(int i = 0; i< 3; i++){				
			
			selectedDots[i] = -1;
			selectedDotsPositions[i] = Vector3.zero;
		}

		DisableFast(arrowObject);
		DisableFast(triangleObject);
	}
	
	void ResetGeometrySelections(){
	
		SetSelections(selectedGeometry, outlineObjects, false);
		
		constraintClass.Reset();
		
		SetLineSegmentColor(selectedGeometry, colorForObject, colorForConstraint, constraintClass);
		
		DetachAxis();	
		
		selectedGeometry = null;
		
		OBJcycle = 0;

		DisableFast(markerLineOBJObject);
		DisableFast(markerDotOBJObject);
	}
	
	public void ResetSelections(int reset){
		
		if(reset == Reset.ALL){
		
			ResetDotSelections();
			ResetGeometrySelections();
		}
	
		if(reset == Reset.DOTS){
		
			ResetDotSelections();
		}
		
	  if(reset == Reset.GEOMETRY){

			ResetGeometrySelections();
	  }
	}

	
	//Reset all the colors of ObjectsInView, and then set the color of selectedObject.
	//If selectedObject is null then all the ObjectsInView will be set to "selectThese".
	//If ObjectsInView is null, then only selectedObject will set to "selectThese".
	public void SetSelections(GameObject selectedObject, GameObject[] allObjects, bool selectThese){
		
		ObjectLineManager lineManager;
		
		//First reset all the selections
		if(allObjects != null){
			
			foreach(GameObject lineObject in allObjects) { 
				
				if(lineObject != null){
				
					lineManager = lineObject.GetComponent<ObjectLineManager>();
					lineManager.ChangeSelection(false, colorForObject);
				}
			} 
		}
		
		//now select
		if(selectedObject != null){
			
			lineManager = selectedObject.GetComponent<ObjectLineManager>();
			lineManager.ChangeSelection(selectThese, colorForObject);		
		}
	}
	
	
	//Set the color of one line of the object. For debugging only.
	void SetColorOneLine(GameObject selectedObject, Color color, int lineIndex){
		
		ObjectLineManager lineManager = selectedObject.GetComponent<ObjectLineManager>();		
		lineManager.SetColorOneLine(color, lineIndex);
	}
	
	//Set the color of the entire line object.
	public void ChangeColorAllLines(GameObject inputObject, Color color){
		
		if(inputObject != null){
		
			ObjectLineManager lineManager = inputObject.GetComponent<ObjectLineManager>();		
			lineManager.ChangeColorAllLines(color);
		}
	}
	
	//Sets the color of multiple lines at the same time. Which lines change depends on preset values in the
	//ObjectLineManager.cs file.
	void SetLineSegmentColor(GameObject selectedObject, Color oldColor, Color newColor, ConstraintClass constraintClass){
		
		if(selectedObject != null){
			
			ObjectLineManager lineManager = selectedObject.GetComponent<ObjectLineManager>();			
			lineManager.SetLineSegmentColor(oldColor, newColor, constraintClass);
		}
	}

	bool IsMarkerPartOfMergedScenes(bool[,] markersForTrackingScenes, int markerId){

		for(int i = 0; i < sceneObjects.Length; i++){

			for(int e = 0; e < arWrapper.totalMarkerAmount; e++){
				
				if(markersForTrackingScenes[i, e] == true){

					if(e == markerId){

						return true;
					}
				}
			}
		}

		return false;
	}
	
	public void SetOutlineObjectColors(GameObject[] outlineObjects, bool[,] markersForTrackingScenes, Color colorForObject, Color colorNotForTracking, int outlineType){
		
		bool merged = AreScenesMerged(mergedScenes);

		for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

			if(arWrapper.markerId[i] != -1){


				if(outlineType == OutlineType.MARKER_FOR_TRACKING){

					if(merged){					
					
						if(IsMarkerPartOfMergedScenes(markersForTrackingScenes, arWrapper.markerId[i])){	
				
							ChangeColorAllLines(outlineObjects[i], colorForObject);
						}

						else{
				
							ChangeColorAllLines(outlineObjects[i], colorNotForTracking);
						}
					}

					else{

						if(markersForTracking[arWrapper.markerId[i]]){	
				
							ChangeColorAllLines(outlineObjects[i], colorForObject);
						}

						else{
				
							ChangeColorAllLines(outlineObjects[i], colorNotForTracking);
						}
					}		
				}

				if(outlineType == OutlineType.FORCE_TRACKING){

					ChangeColorAllLines(outlineObjects[i], colorForObject);
				}

				if(outlineType == OutlineType.FORCE_NONTRACKING){

					ChangeColorAllLines(outlineObjects[i], colorNotForTracking);
				}							
			}
		}
	}
	
	//select an axis according to mouse selection
	void SetAxisSelectionMouse(GameObject selectedObject, bool selectAxis){		
		
		SingleLineManager lineManager;
		
		if(selectedObject != null){
			lineManager = selectedObject.GetComponent<SingleLineManager>();
			lineManager.ChangeLineSelection(selectAxis);
		}
	}
	

	
	void ResetAxisSelection(SingleLineManager[] singleAxisLlinemanagers){
		
		foreach (SingleLineManager lineManager  in singleAxisLlinemanagers)  { 		
			lineManager.ChangeLineSelection(false);	
		} 
	}

	
	bool IsObjectSelected(GameObject testedObject){
		
		bool result;
		
		if(testedObject != null){
			
			ObjectLineManager lineManager;
			
			lineManager = testedObject.GetComponent<ObjectLineManager>();
			result = lineManager.isSelected();
		}

		else{

			result = false;
		}
		
		return result;
	}
	
	//Is the mouse selected axis already selected?
	bool IsAxisObjectSelected(GameObject testedObject){
		
		bool result;
		
		SingleLineManager lineManager;
		
		lineManager = testedObject.GetComponent<SingleLineManager>();
		result = lineManager.IsSelected();
		
		return result;
	}
	
	//Is the specified axis selected?
	bool IsAxisTypeSelected(int axisType, SingleLineManager[] singleAxisLlinemanagers){
		
		foreach (SingleLineManager lineManager  in singleAxisLlinemanagers){ 
			
			//are we looking at the same axis type?
			if(axisType == (int)lineManager.objectType){
				
				//is the axis selected?
				if(lineManager.IsSelected()){
					return true;
				}
			}
		}
		
		return false;
	}
	
	//select a specified single axis
	void SelectSingleAxisType(int axisType, SingleLineManager[] singleAxisLlinemanagers){	
		
		foreach (SingleLineManager lineManager  in singleAxisLlinemanagers){ 
			
			//are we looking at the same axis type?
			if(axisType == (int)lineManager.objectType){
				
				//is the axis not yet selected?
				if(!lineManager.IsSelected()){
					lineManager.ChangeLineSelection(true);
				}
			}
			
			else{

				lineManager.ChangeLineSelection(false);
			}
		}

	}
	
	
	//Find out how many axis are selected
	int AxisAmountSelected(SingleLineManager[] singleAxisLlinemanagers){
		
		int amount = 0;
		
		foreach (SingleLineManager lineManager  in singleAxisLlinemanagers){ 	
			
			if(lineManager.IsSelected()){

				amount++;
			}
		}
			
		return amount;
	}
	

	//Calculates whether the touch on the screen is not in the menu area. This is to prevent a button 
	//click being interpreted as an object selection.
	public bool IsTouchValid(Vector2 screenPos, Rect menuRect2, Rect fileRect, bool fileWindowOpen, bool fileWindowOpenBuf, bool includeButtons){
		
		bool horizontalValid = false;
		bool verticalValid = false;
		int bottomRect = 0;
		int topRect = 0;
		bool result = false;
		
		//check where the mouse click is in relation to the buttons
		if(includeButtons){
		
			//there is a bug in Unity causing yMin and yMax to be incorrect (based on the top of the screen
			//instead of the origin) This is a workaround for that.
			bottomRect = Screen.height - (int)menuRect2.y - (int)menuRect2.height;
			topRect = Screen.height - (int)menuRect2.y;
			
			if((screenPos.x < menuRect2.xMin) || (screenPos.x > menuRect2.xMax)){
				horizontalValid = true;
			}
			
			if( ((screenPos.x > menuRect2.xMin) && (screenPos.x < menuRect2.xMax)) && ((screenPos.y < bottomRect) || (screenPos.y > topRect))  ){
				horizontalValid = true;
			}
			
			if((screenPos.y < bottomRect) || (screenPos.y > topRect)){
				verticalValid = true;
			}
			
			if( ((screenPos.y > bottomRect) && (screenPos.y < topRect)) && ((screenPos.x < menuRect2.xMin) || (screenPos.x > menuRect2.xMax))  ){
				verticalValid = true;
			}
		}
		
		//do not include buttons
		else{

			horizontalValid = true;
			verticalValid = true;
		}
		
		if(horizontalValid && verticalValid){
			
			if(fileWindowOpen || fileWindowOpenBuf){
				
				//reset
				horizontalValid = false;
				verticalValid = false;
				
				//there is a bug in Unity causing yMin and yMax to be incorrect (based on the top of the screen
				//instead of the origin) This is a workaround for that.
				bottomRect = Screen.height - (int)fileRect.y - (int)fileRect.height;
				topRect = Screen.height - (int)fileRect.y;
				
				if((screenPos.x < fileRect.xMin) || (screenPos.x > fileRect.xMax)){
					horizontalValid = true;
				}
				
				if( ((screenPos.x > fileRect.xMin) && (screenPos.x < fileRect.xMax)) && ((screenPos.y < bottomRect) || (screenPos.y > topRect))  ){
					horizontalValid = true;
				}
		
				
				if((screenPos.y < bottomRect) || (screenPos.y > topRect)){
					verticalValid = true;
				}
				
				if( ((screenPos.y > bottomRect) && (screenPos.y < topRect)) && ((screenPos.x <fileRect.xMin) || (screenPos.x > fileRect.xMax))  ){
					verticalValid = true;
				}
				
				if(horizontalValid && verticalValid){
					result = true;
				}
				
				else{
					result = false;
				}			
			}
			
			//file window closed
			else{
				result = true;
			}
		}
		
		else{
			result = false;
		}

		return result;
	}
	

	void ProcessRecording(){
		
		Vector3 distanceVectorGlobal;	
		Vector3 distanceVectorLocal;
		Quaternion markerRotationDiff = Quaternion.identity;	
		Quaternion firstRotation = Quaternion.identity;
		bool firstRotationSet = false;
		Vector4 cumulativeRotation = Vector4.zero;

		//Find the offset vector and rotational difference between all tracked markers and 
		//store the average value.		
		for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){	//go down in the array	

			if(arWrapper.markerId[i] != -1){
			
				//Log the fact that this marker is seen at least once.
				markerSeen[arWrapper.markerId[i]] = true;

				//Find another marker in the same frame
				for(int e = i+1; e < arWrapper.maxMarkerAmountInView; e++){ //go right in the array

					if(arWrapper.markerId[e] != -1){
					
						//Get a vector between each two markers. 
						//The vector is calculated as e-i, which means the vector is from i to e.
						distanceVectorGlobal = arWrapper.markerTransform[e].transform.position - arWrapper.markerTransform[i].transform.position;
						
						//We want the vector to be relative to marker i (the marker we are projecting
						//from), not relative to the world reference frame.
						distanceVectorLocal = arWrapper.markerTransform[i].transform.InverseTransformDirection(distanceVectorGlobal);
						
						//Do the same for the rotation. Note that the math for this is different.
						//This calculates the rotation from i to e.
						markerRotationDiff = Math3d.SubtractRotation(arWrapper.markerTransform[e].transform.rotation, arWrapper.markerTransform[i].transform.rotation);					
						
						//Calculate the new average value and store it in the 2D array		
						//Note that only half of the array is filled in this loop. In theory we could use
						//an array which is only half the size, but this will make the code more complex. 
						//Besides, we want to use this array to compute it with the Dijkstra algorithm, which
						//requires a 2d array anyway.
						distanceVecBetweenMarkersAdd[arWrapper.markerId[i], arWrapper.markerId[e]] += distanceVectorLocal;
						addAmount[arWrapper.markerId[i], arWrapper.markerId[e]] = addAmount[arWrapper.markerId[i], arWrapper.markerId[e]] + 1;
						distanceVecBetweenMarkers[arWrapper.markerId[i], arWrapper.markerId[e]] = distanceVecBetweenMarkersAdd[arWrapper.markerId[i], arWrapper.markerId[e]] / (float)addAmount[arWrapper.markerId[i], arWrapper.markerId[e]];
						
						//Fill the array with a vector from e to i as well. Because the vector from one marker to
						//the other is based on local coordinates, the vector cannot just be reversed to get the 
						//vector from e to i.
						//First transform the vector from local coordinates back to global coordinates. The vector
						//straight from the markers cannot be used (that would be faster) because at this point the
						//vector is an average value based on multiple samples. We just take this average value and
						//reverse that. Also reverse the vector. This will become the vector from e to i with 
						//reference to the global coordinate system.
						distanceVectorGlobal = -arWrapper.markerTransform[i].transform.TransformDirection(distanceVecBetweenMarkers[arWrapper.markerId[i], arWrapper.markerId[e]]);
						
						//Transform the vector so it is with reference to the local coordinate system of marker e.
						distanceVecBetweenMarkers[arWrapper.markerId[e], arWrapper.markerId[i]] = arWrapper.markerTransform[e].transform.InverseTransformDirection(distanceVectorGlobal);

						//Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
						//q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
						if(!firstRotationSet){

							firstRotation = markerRotationDiff;
							firstRotationSet = true;
						}						
						
						//do a type cast
						//TODO: get rid of type casting and make another version of  Math3d.AverageQuaternion
						cumulativeRotation = Math3d.QuaternionToVector4(rotationDiffBetweenMarkersAdd[arWrapper.markerId[i], arWrapper.markerId[e]]);

						rotationDiffBetweenMarkers[arWrapper.markerId[i], arWrapper.markerId[e]] = Math3d.AverageQuaternion(ref cumulativeRotation, markerRotationDiff, firstRotation, addAmount[arWrapper.markerId[i], arWrapper.markerId[e]]);
						
						//type cast back again
						rotationDiffBetweenMarkersAdd[arWrapper.markerId[i], arWrapper.markerId[e]] = Math3d.Vector4ToQuaternion(cumulativeRotation);
						
						//Store the inverse rotation. This step could be skipped and calculated offline to save cpu time
						//but the direction has to be stored in that case, which takes up more memory.
						rotationDiffBetweenMarkers[arWrapper.markerId[e], arWrapper.markerId[i]] = Quaternion.Inverse(rotationDiffBetweenMarkers[arWrapper.markerId[i], arWrapper.markerId[e]]);
					}	
				}
			}
		}
	}

	
	//Note that mouse up and down events are handled manually instead of using the buildin API, because that one is buggy.
	void ProcessTouch(){
		
		GameObject selectedObject;					
		GameObject[] outoutlineObjectsInView;	
		bool touchBValid = false;
		bool selectionValid = false;
		ObjectLineManager selectedGeometryLineManager;

		//Get the touch info (multi touch compatible).
		//Note: this method of getting multi touch info is not very clean. I need to re-do it.
		customTouchCount = GetTouchCount();	
		
		if((mode == Mode.HOME) || (mode == Mode.GEOMETRY))
		{

			//The user removed one or both fingers. Also generate mouse up event.
			if(customTouchCount < customTouchCountBuf){	
				
				mouseUpEvent = true;
			
				ResetAllPinch();
				
				//If this is not done, the mouse click will create continuous events if the button is held down.	
				if(customTouchCount == 0){
		
					moveBeginPosSet = false;
					scalingGlobal = false;
				}	
			}
			else{
				mouseUpEvent = false;
			}
			
			//mouse down event (one finger only)
			if((customTouchCount > customTouchCountBuf) && (customTouchCount == 1)){	
				
				//store the initial mouse position
				touchInfo.GetPositionA(customTouchCount);
				positionAInitial = touchInfo.positionA;
			}
			
			customTouchCountBuf = customTouchCount;

			if((customTouchCount > 0) || mouseUpEvent){
				
				touchInfo.GetPositionA(customTouchCount);	
				touchInfo.GetPositionB(customTouchCount);

				//is the selection valid (finger not moved too far since mouse down event)
				if(Vector2.Distance(positionAInitial, touchInfo.positionA) <= maxSelectionDistance){
					
					if(customTouchCount < 2){
						selectionValid = true;
					}
				}

				touchAValid = IsTouchValid(touchInfo.positionA, gui.menuRect2, fileRect, fileWindowOpen, fileWindowOpenBuf, true);
				touchBValid = IsTouchValid(touchInfo.positionB, gui.menuRect2, fileRect, fileWindowOpen, fileWindowOpenBuf, true);
				fileWindowOpenBuf = fileWindowOpen;
				
				//Update zoom screen uv mapping
				if((mode == Mode.GEOMETRY) && trackingFrozen && (customTouchCount == 1) && touchAValid ){

					mouseAposGlobal = touchInfo.positionA;
					
					//In the editor the mouse values can be outside a valid range, so clamp it
					if(Application.isEditor){
					
						if(mouseAposGlobal.x < 0.0f){
							mouseAposGlobal.x = 0.0f;
						}
						
						if(mouseAposGlobal.y < 0.0f){
							mouseAposGlobal.y = 0.0f;
						}
						
						if(mouseAposGlobal.x > Screen.width){
							mouseAposGlobal.x = Screen.width;
						}
						
						if(mouseAposGlobal.y > Screen.height){
							mouseAposGlobal.y = Screen.height;
						}
					}

					specialEffect.UpdateZoomScreenUVMapping(mouseAposGlobal);
				}	
				
				//pinch to scale
				if(touchAValid && touchBValid && (customTouchCount == 2) && (AxisAmountSelected(singleAxisLlinemanagers) >= 1) && (selectedGeometry != null)){
				
					selectedGeometryLineManager = selectedGeometry.GetComponent<ObjectLineManager>();
					
					bool xSelected = IsAxisTypeSelected(AxisType.X, singleAxisLlinemanagers);
					bool ySelected = IsAxisTypeSelected(AxisType.Y, singleAxisLlinemanagers);
					bool zSelected = IsAxisTypeSelected(AxisType.Z, singleAxisLlinemanagers);
					
					if(!scaleBeginPosSet){	
						
						//set the object scale						

						xPinchClass.SetBeginScale(selectedGeometry.transform.localScale.x);
						yPinchClass.SetBeginScale(selectedGeometry.transform.localScale.y);
						zPinchClass.SetBeginScale(selectedGeometry.transform.localScale.z);
		
						beginDistance = Vector2.Distance(touchInfo.positionA, touchInfo.positionB); 
						scaleBeginPosSet = true;
						scalingGlobal = true;
					}
					
					//Get the distance between the fingers on the screen. Note that we are not using 
					//finger movement speed for the object scaling and moving. That is a really bad way
					//of calculating the change as it is time and resolution dependant. It creates all 
					//sorts of problems and is the reason why you can't scroll or zoom very slowly with
					//most software. Let's use distance instead of speed to avoid this.
					float currentDistance = Vector2.Distance(touchInfo.positionA, touchInfo.positionB);  
					pinch = currentDistance - beginDistance;
		
					//calculate the pinch for the selected axis
					if(xSelected ){
						xPinchClass.CalculatePinch(pinch);
					}	
					else{
						xPinchClass.scale = selectedGeometry.transform.localScale.x;
					}
					
					if(ySelected ){
					
						//If the object is a plane, do not scale in Y.
						if(selectedGeometryLineManager.objectType != ObjectLineManager.ObjectType.PLANE){
						
							yPinchClass.CalculatePinch(pinch);
						}
					}		
					else{
						yPinchClass.scale = selectedGeometry.transform.localScale.y;
					}
					
					if(zSelected ){
						zPinchClass.CalculatePinch(pinch);
					}
					else{
						zPinchClass.scale = selectedGeometry.transform.localScale.z;
					}
					
					//Now do the actual scaling.
					selectedGeometry.transform.localScale = new Vector3(xPinchClass.scale, yPinchClass.scale, zPinchClass.scale);
					
					//set the position if a constraint is enabled.
					if(constraintClass.AnySet() && (selectedGeometryLineManager.objectType != ObjectLineManager.ObjectType.OBJ)){
						
						//get translation vector with reference to the object itself
						Vector3 translationVectorLocal = GetConstraintCorrectedTranslation(selectedGeometryLineManager.objectType, selectedGeometryLineManager.size, selectedGeometry.transform.localScale, initialConstrainedScale);
					
						//convert the translation vector to world space
						Vector3 translationVector = selectedGeometry.transform.TransformDirection(translationVectorLocal);
							
						//set the geometry position
						selectedGeometry.transform.position = initialConstrainedPos + translationVector;
					}
				}

		
				//Object selection, one finger on touchscreen.
				//Note: this code block has to be in front of drag to move code.	
				//TODO: no need to check for isTouchValid if no game menu is shown
				if(mouseUpEvent && touchAValid){

					Ray ray = Camera.main.ScreenPointToRay(touchInfo.positionA);

					MouseDotSelection(touchInfo.positionA, dotsPositions, currentDotAmount, selectedDots, selectedDotsPositions);
					
					RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, rayCastLayerMask);
					
					foreach(RaycastHit hit in hits){

						//get the selected gameObject
						selectedObject = hit.transform.gameObject;
						
						//only store the line manager for a selected geometry object
						if(selectedObject.tag == "Geometry"){
							
							selectedGeometryLineManager = selectedObject.GetComponent<ObjectLineManager>();
						}
		
						//is the selected object a line object?
						if((selectedObject.tag == "Outline") && selectionValid && recording){	

							//Get the marker ID
							referenceMarkerNumber = selectedObject.transform.parent.gameObject.GetComponent<MarkerData>().id;
		
							//is the marker not selected?
							if(!IsObjectSelected(selectedObject)){
								
								//The outlineObjects created previously cannot be used here because they need to be active
								//for the logic here to work. Thus they need to be in view, hence looking for it here again.
								//There might be a more efficient way of doing this but this is to be implemented at a later time.
								outoutlineObjectsInView = GameObject.FindGameObjectsWithTag("Outline"); 
								
								//select only one marker						
								SetSelections(selectedObject, outoutlineObjectsInView, true);
							}
							
							//marker already selected
							else{

								SetSelections(selectedObject, null, false);
							}
							
						}
						
						if((selectedObject.tag == "Geometry") && selectionValid){
							
							int constraintSide = -1;
							
							//geometry is not an OBJ file
							selectedGeometryLineManager = selectedObject.GetComponent<ObjectLineManager>();
							if(selectedGeometryLineManager.objectType != ObjectLineManager.ObjectType.OBJ){
								
								if(selectedGeometry != null){
								
									//is the same object already selected?
									if(selectedGeometry == selectedObject){
									
										//is the triangle selected part of a valid object side?
										if(IsTriangleIndexSide(selectedGeometryLineManager.objectType, hit.triangleIndex)){
											
											//convert the triangle index to a constraint side
											constraintSide = TriangleIndexToConstraint(selectedGeometryLineManager.objectType, hit.triangleIndex);
											
											if(constraintSide != ConstraintSide.none){
											
												//the constraint is not already set
												if(!constraintClass.IsSet(constraintSide)){
													
													constraintClass.Reset();
													
													//set the constraint
													constraintClass.Set(constraintSide);
													
													//auto select an axis
													if((constraintSide == ConstraintSide.left) || (constraintSide == ConstraintSide.right)){
														SelectSingleAxisType(AxisType.X, singleAxisLlinemanagers);
													}
													
													if((constraintSide == ConstraintSide.forward) || (constraintSide == ConstraintSide.backward)){
														SelectSingleAxisType(AxisType.Z, singleAxisLlinemanagers);
													}
													
													if((constraintSide == ConstraintSide.up) || (constraintSide == ConstraintSide.down)){
														SelectSingleAxisType(AxisType.Y, singleAxisLlinemanagers);
													}
													
													//store the initial position and scale
													initialConstrainedPos = selectedGeometry.transform.position;
													initialConstrainedScale = selectedGeometry.transform.localScale;
												}
												
												//the constraint is already set
												else{
													constraintClass.Remove(constraintSide);	
													ResetAxisSelection(singleAxisLlinemanagers);
												}
												
												//visualize the constraint side set
												SetLineSegmentColor(selectedObject, colorForObject, colorForConstraint, constraintClass);
											}
										}
									}
										
									//same geometry not already selected, so select it
									else{									
										SelectObject(selectedObject);
										selectedGeometry = selectedObject;
									}
								}
								
								//selected geometry is null
								else{
									SelectObject(selectedObject);
									selectedGeometry = selectedObject;
								}
							}
							
							//is OBJ
							else{
								SelectObject(selectedObject);
								selectedGeometry = selectedObject;
							}
							
							//Is the object an OBJ file? 
							if((selectedGeometryLineManager.objectType == ObjectLineManager.ObjectType.OBJ) && selectionValid){
								
								//attach line to OBJ
								markerLineOBJObject.transform.parent = selectedGeometry.transform;
								markerLineOBJObject.transform.localPosition = Vector3.zero; //reset 
								markerLineOBJObject.transform.localRotation = Quaternion.identity; //reset
								markerLineOBJObject.transform.localScale = new Vector3(1,1,1); //reset 

								//attach dot to OBJ
								markerDotOBJObject.transform.parent = selectedGeometry.transform;
								markerDotOBJObject.transform.localPosition = Vector3.zero; //reset 
								markerDotOBJObject.transform.localRotation = Quaternion.identity; //reset
								markerDotOBJObject.transform.localScale = new Vector3(1,1,1); //reset 

								MeshCollider meshCollider = hit.collider as MeshCollider;
								Mesh mesh = meshCollider.sharedMesh;
								Vector3[] vertices = mesh.vertices;
								int[] triangles = mesh.triangles;
								
								//get the triangle index of the polygon which is hit by the mouse click
								Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
								Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];	
								Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]]; 
								
								//Since we only get the triangle normal once when the user clicks on the object, we need to
								//store it in object space. If it is stored in world space, the value will be invalid once
								//the object is rotated after the user selection.
								OBJHitNormal = selectedGeometry.transform.InverseTransformDirection(hit.normal);	
								
								if(globalHitIndex >= 6){
									globalHitIndex = 0;
								}
								
								if(globalHitIndex == 0){
									OBJLineVector = p1 - p0; //object space
									OBJLinePoint = p0; //object space
									
									lineManagerOBJLine.SetLinePoints(p0 - OBJLineVector, p1 + OBJLineVector);
								}
								
								if(globalHitIndex == 1){
									OBJLineVector = p0 - p1;
									OBJLinePoint = p1;
									
									lineManagerOBJLine.SetLinePoints(p0 + OBJLineVector, p1 - OBJLineVector);
								}
								
								if(globalHitIndex == 2){
									OBJLineVector = p2 - p1;
									OBJLinePoint = p1;

									lineManagerOBJLine.SetLinePoints(p1 - OBJLineVector, p2 + OBJLineVector);
								}
								
								if(globalHitIndex == 3){
									OBJLineVector = p1 - p2;
									OBJLinePoint = p2;

									lineManagerOBJLine.SetLinePoints(p1 + OBJLineVector, p2 - OBJLineVector);
								}
								
								if(globalHitIndex == 4){
									OBJLineVector = p0 - p2;
									OBJLinePoint = p2;

									lineManagerOBJLine.SetLinePoints(p2 - OBJLineVector, p0 + OBJLineVector);
								}
								
								if(globalHitIndex == 5){
									OBJLineVector = p2 - p0;
									OBJLinePoint = p0;

									lineManagerOBJLine.SetLinePoints(p2 + OBJLineVector, p0 - OBJLineVector);
								}
								
								lineManagerOBJDot.SetDot1PointLocation(OBJLinePoint);
								
								globalHitIndex++;
							}
							
							//not OBJ
							else{
								//detach dot and line from OBJ
								DisableFast(markerLineOBJObject);
								DisableFast(markerDotOBJObject);
							}
						}
						
						
						if((selectedObject.tag == "Axis") && selectionValid){
							
							//is not selected
							if(!IsAxisObjectSelected(selectedObject)){						
								SetAxisSelectionMouse(selectedObject, true);
							}
							
							//is already selected
							else{
								SetAxisSelectionMouse(selectedObject, false);
							}
						}
					}
				}
				

				//Drag to move code. 
				//NOTE: this should be after the "RaycastHit hit" code.
				int amount = AxisAmountSelected(singleAxisLlinemanagers);
				if(touchAValid && (customTouchCount == 1) && ((amount == 1) || amount == 2) && (selectedGeometry != null) && !scalingGlobal && trackingMarkerInViewScenes[worldCenterSceneIndex]){
					
					if(!moveBeginPosSet){			
						
						bool xSelected = IsAxisTypeSelected(AxisType.X, singleAxisLlinemanagers);
						bool ySelected = IsAxisTypeSelected(AxisType.Y, singleAxisLlinemanagers);
						bool zSelected = IsAxisTypeSelected(AxisType.Z, singleAxisLlinemanagers);
						
						//Calculate the offset distance between the mouse position and object origin position
						//so the geometry does not jump to the current mouse position but stays where it is until the
						//mouse is moved.
						Vector3 objectScreenPos = Camera.main.WorldToScreenPoint(selectedGeometry.transform.position);
						Vector3 mousePos = new Vector3(touchInfo.positionA.x, touchInfo.positionA.y, Camera.main.nearClipPlane);
						
						//Get the offset between the selectedGeometry and the mouse position in screen coordinates. The z
						//coordinate is irrelevant
						dragOffset = objectScreenPos - mousePos;
						
						if(amount == 2){
							if(xSelected && zSelected){ //Move along XZ
			
								planeNormalGlobal = selectedGeometry.transform.up;
							}
							
							if(xSelected && ySelected) { //Move along XY
			
								planeNormalGlobal = selectedGeometry.transform.forward; 
							}
							
							if(ySelected && zSelected){ //Move along YZ
			
								planeNormalGlobal = selectedGeometry.transform.right; 
							}
						}
						
						
						if(amount == 1){
							
							linePosGlobal = selectedGeometry.transform.position;
							
							if(xSelected){ //Move along X
			
								lineVecGlobal = selectedGeometry.transform.right; 
							}
			
							if(ySelected){ //Move along Y
			
								lineVecGlobal = selectedGeometry.transform.up; 
							}
							
							if(zSelected){ //Move along Z
			
								lineVecGlobal = selectedGeometry.transform.forward; 
							}
						}
					   
						moveBeginPosSet = true;
					}
					
					if(moveBeginPosSet){
						
						Ray ray;
						Vector3 position = Vector3.zero;
		
						//Get a line which starts at the mouse position and has the direction of the camera.
						//Add an offset to this, which is a vector from the mouse position to the selected geometry
						//in screen coordinates. This prevents the selected geometry from jumping to the mouse position.
						Vector2 modifiedMouse = new Vector2(touchInfo.positionA.x + dragOffset.x, touchInfo.positionA.y + dragOffset.y);	
						ray = Camera.main.ScreenPointToRay(modifiedMouse);
						
						//move the geometry in a plane
						if(amount == 2){
							
							//Now we have a line originating from the mouse position (linePos) with direction "lineVec". This line will cross the 
							//geometry plane somewhere. We need to find this point and place the selected geometry there.
							Math3d.LinePlaneIntersection(out position, ray.origin, ray.direction, planeNormalGlobal, selectedGeometry.transform.position);
						}
						
						//move the geometry on a line
						if(amount == 1){
							
							Vector3 dummyVec = Vector3.zero;
							
							Math3d.ClosestPointsOnTwoLines(out position, out dummyVec, linePosGlobal, lineVecGlobal, ray.origin, ray.direction);

						}
						
						if((amount == 1) || (amount == 2)){
							
							if(constraintClass.AnySet()){
					
								if(constraintClass.IsSet(ConstraintSide.left) || constraintClass.IsSet(ConstraintSide.right)){
									
									selectedGeometry.transform.position = new Vector3(selectedGeometry.transform.position.x, position.y, position.z);
								}
								
								if(constraintClass.IsSet(ConstraintSide.forward) || constraintClass.IsSet(ConstraintSide.backward)){
									
									selectedGeometry.transform.position = new Vector3(position.x, position.y, selectedGeometry.transform.position.z);
								}
								
								if(constraintClass.IsSet(ConstraintSide.up) || constraintClass.IsSet(ConstraintSide.down)){
									
									selectedGeometry.transform.position = new Vector3(position.x, selectedGeometry.transform.position.y, position.z);
								}
							}
							
							//no constraint is set
							else{
								selectedGeometry.transform.position = position;
							}
						}
						
						//store the initial position and scale
						initialConstrainedPos = selectedGeometry.transform.position;
						initialConstrainedScale = selectedGeometry.transform.localScale;
					}
				}
			}
		}
	}
	
	void ResetAllPinch(){
		
		//BUGFIX: for some reason sometimes suddenly the pinchclass is zero, so check for this
		if(xPinchClass == null){
			xPinchClass = new PinchClass();
		}
		if(yPinchClass == null){
			yPinchClass = new PinchClass();
		}
		if(zPinchClass == null){
			zPinchClass = new PinchClass();
		}

		scaleBeginPosSet = false;
	}
	
	
	
	//Flip the input geometry around the specified side
	void FlipAroundSide(GameObject objectToModify, Vector3 objectSize, int side){
		
		//get a vector from the object center to the constrained side
		Vector3 translationVector = GetConstraintVector(objectToModify, objectSize, side);
   
		objectToModify.transform.RotateAround(objectToModify.transform.position + translationVector, objectToModify.transform.right, 180.0f);
	}
	
	//Get the default size (width if scale is 1) of the specified side of the object
	float GetDefaultSideObjectSize(int side, Vector3 objectSize){
	
		float defaultSize = 0.0f;
		
		if((side == ConstraintSide.left) || (side == ConstraintSide.right)){
			defaultSize = objectSize.x;
		}
		if((side == ConstraintSide.forward) || (side == ConstraintSide.backward)){
			defaultSize = objectSize.z;
		}
		if((side == ConstraintSide.up) || (side == ConstraintSide.down)){
			defaultSize = objectSize.y;
		}  
		
		return defaultSize;
	}
	
	//Make the object edges fit between two points. Calculate the new object center and scale.
	//Inputs are: 
	//-which object side to modify
	//-the game object to modify
	//-the new distance between the object edges
	//Output is:
	//-modified gameObject scale and local position
	void CalculateObjectStretch(int side, GameObject objectToModify, float distance, Vector3 objectSize){

		float scale;
		Vector3 translationVec = Vector3.zero;
		float translationDistance;
		float defaultSize = 0.0f; 
		
		//globalSize is half the total width.
		float globalSize = GetGlobalSize(objectSize, objectToModify, side);
		
		defaultSize = GetDefaultSideObjectSize(side, objectSize);	  
	
		scale = CalculateScaleFromDistance(distance, defaultSize);
		
		//Calculate the distance between the old object center and the new object center 
		//after it has been scaled.
		translationDistance = (distance / 2.0f) - globalSize;
		
		if((side == ConstraintSide.left) || (side == ConstraintSide.right)){

			//calculate the new position
			if(side == ConstraintSide.left){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(objectToModify.transform.right, translationDistance);
			}
			
			if(side == ConstraintSide.right){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(-objectToModify.transform.right, translationDistance);
			}
			
			//translate the geometry
			objectToModify.transform.Translate(translationVec, Space.World);

			//set the new scale of the geometry
			objectToModify.transform.localScale = new Vector3(scale, objectToModify.transform.localScale.y, objectToModify.transform.localScale.z);
		}
		
		
		if((side == ConstraintSide.forward) || (side == ConstraintSide.backward)){

			//calculate the new position
			if(side == ConstraintSide.forward){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(-objectToModify.transform.forward, translationDistance);
			}
			
			if(side == ConstraintSide.backward){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(objectToModify.transform.forward, translationDistance);
			}
			
			//translate the geometry
			objectToModify.transform.Translate(translationVec, Space.World);

			//set the new scale of the geometry
			objectToModify.transform.localScale = new Vector3(objectToModify.transform.localScale.x, objectToModify.transform.localScale.y, scale);
		}
		
		
		if((side == ConstraintSide.up) || (side == ConstraintSide.down)){

			//calculate the new position
			if(side == ConstraintSide.up){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(-objectToModify.transform.up, translationDistance);
			}
			
			if(side == ConstraintSide.down){

				//get a vector from the original object position to the new scaled position
				translationVec = Math3d.SetVectorLength(objectToModify.transform.up, translationDistance);
			}
			
			//translate the geometry
			objectToModify.transform.Translate(translationVec, Space.World);

			//set the new scale of the geometry
			objectToModify.transform.localScale = new Vector3(objectToModify.transform.localScale.x, scale, objectToModify.transform.localScale.z);
		}
	}
	
	
	//This function gets the object size after the scale is applied
	float GetGlobalSize(Vector3 objectSize, GameObject selectedGeometry, int side){
		
		float localScale = GetLocalScale(side, selectedGeometry);
		
		if((side == ConstraintSide.left) || (side == ConstraintSide.right)){
		
			return objectSize.x * localScale;
		}
		
		if((side == ConstraintSide.up) || (side == ConstraintSide.down)){
		
			return objectSize.y * localScale;
		}
		
		if((side == ConstraintSide.forward) || (side == ConstraintSide.backward)){
		
			return objectSize.z * localScale;
		}
		
		return 0.0f;
	}
	
	
	//Get the side of the geometry of which the tranlation vector is pointing at.
	int GetSideOfGeometry(Vector3 translationVector, GameObject geometryObject){
		
		int side = ConstraintSide.none;
		float angle;
		
		Vector3 translationVectorNormalized = Vector3.Normalize(translationVector);

		angle = Vector3.Dot(geometryObject.transform.up, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.up;
		}
		
		angle = Vector3.Dot(-geometryObject.transform.up, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.down;
		}
		
		angle = Vector3.Dot(geometryObject.transform.right, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.right;
		}
		
		angle = Vector3.Dot(-geometryObject.transform.right, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.left;
		}
		
		angle = Vector3.Dot(geometryObject.transform.forward, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.forward;
		}
		
		angle = Vector3.Dot(-geometryObject.transform.forward, translationVectorNormalized);			
		if(angle >= 0.666666666666667f){	//<= 30
			side = ConstraintSide.backward;
		}
		
		return side;		
	}
	
	
	//Get the normal of the constraint side plane
	Vector3 GetConstraintNormal(GameObject geometryObject, int side){

		Vector3 normal = Vector3.zero;
		
		switch(side){
			case ConstraintSide.right:
			normal = geometryObject.transform.right;
			break;
			
			case ConstraintSide.left:
			normal = -geometryObject.transform.right;
			break;
			
			case ConstraintSide.forward:
			normal = geometryObject.transform.forward;
			break;
			
			case ConstraintSide.backward:
			normal = -geometryObject.transform.forward;
			break;
			
			case ConstraintSide.up:
			normal = geometryObject.transform.up;
			break;
			
			case ConstraintSide.down:
			normal = -geometryObject.transform.up;
			break;
			
			default:
			break;
		}

		return normal;
	}
	
	//Get a vector from the object center to the constraint side
	Vector3 GetConstraintVector(GameObject geometryObject, Vector3 size, int side){

		Vector3 vector = Vector3.zero;
		
		//width output is half the total width.
		float width = GetGlobalSize(size, geometryObject, side);
		
		switch(side){
			case ConstraintSide.right:
			vector = geometryObject.transform.right;
			break;
			
			case ConstraintSide.left:
			vector = -geometryObject.transform.right;
			break;
			
			case ConstraintSide.forward:
			vector = geometryObject.transform.forward;
			break;
			
			case ConstraintSide.backward:
			vector = -geometryObject.transform.forward;
			break;
			
			case ConstraintSide.up:
			vector = geometryObject.transform.up;
			break;
			
			case ConstraintSide.down:
			vector = -geometryObject.transform.up;
			break;
			
			default:
			break;
		}
		
		vector = Math3d.SetVectorLength(vector, width);
		
		return vector;
	}
	
	//Sets a constraint of one of the sides (left, right, forward, etc) based on the translationVector. For example,
	//if the translationVector makes the geometry object move to it's left side, then the left constraint will be set.
	void SetSideConstraint(GameObject geometryObject, Vector3 translationVector, ref ConstraintClass constraintClass){

		int side = GetSideOfGeometry(translationVector, geometryObject);
		
		switch(side){
			case ConstraintSide.right:
			constraintClass.right = true;
			break;
			
			case ConstraintSide.left:
			constraintClass.left = true;
			break;
			
			case ConstraintSide.forward:
			constraintClass.forward = true;
			break;
			
			case ConstraintSide.backward:
			constraintClass.backward = true;
			break;
			
			case ConstraintSide.up:
			constraintClass.up = true;
			break;
			
			case ConstraintSide.down:
			constraintClass.down = true;
			break;
			
			default:
			break;
		}
	}
	
	
	//Gets the localScale value of the transform from the specified side (x, y, or z).
	float GetLocalScale(int side, GameObject selectedGeometry){
		
		float localScaleSide = 0.0f;
		
		if((side == ConstraintSide.left) || (side == ConstraintSide.right)){
			localScaleSide = selectedGeometry.transform.localScale.x;
		}
		
		if((side == ConstraintSide.forward) || (side == ConstraintSide.backward)){
			localScaleSide = selectedGeometry.transform.localScale.z;
		}
	
		if((side == ConstraintSide.up) || (side == ConstraintSide.down)){
			localScaleSide = selectedGeometry.transform.localScale.y;
		}
		
		return localScaleSide;	
	}
	
	
	//This is to keep track of the constraints. Each edge or surface of a geometry object can be constrained
	//in space. For example, if the left edge of a plane is set to be fixed (constrained), and another
	//constraint at the opposite side (right) is placed, the plane will be stretched instead of moved.
	//Only one geometry object can be edited (transformed to fit constrains) at a time, so only one
	//class for that is used.
	public class ConstraintClass{
	
		public bool right;
		public bool left;
		public bool forward;
		public bool backward;
		public bool up;
		public bool down;
		public bool side;
	
		public ConstraintClass(){
			right = false;
			left = false;
			forward = false;
			backward = false;
			up = false;
			down = false;
			side = false;
		}
		
		//reset function
		public void Reset(){
			right = false;
			left = false;
			forward = false;
			backward = false;
			up = false;
			down = false; 
			side = false;			
		}
		
		public void Set(int sideOfObject){
			
			switch(sideOfObject){
				case ConstraintSide.right:
				right = true;
				break;
				
				case ConstraintSide.left:
				left = true;
				break;
				
				case ConstraintSide.forward:
				forward = true;
				break;
				
				case ConstraintSide.backward:
				backward = true;
				break;
				
				case ConstraintSide.up:
				up = true;
				break;
				
				case ConstraintSide.down:
				down = true;
				break;
				
				case ConstraintSide.side:
				side = true;
				break;
				
				default:
				break;
			}
		}
		
		//This function assumes only one constraint can be set
		public int Get(){
			
			if(right){
				return ConstraintSide.right;
			}
			
			if(left){
				return ConstraintSide.left;
			}
			
			if(forward){
				return ConstraintSide.forward;
			}
			
			if(backward){
				return ConstraintSide.backward;
			}
			
			if(up){
				return ConstraintSide.up;
			}
			
			if(down){
				return ConstraintSide.down;
			}
			
			if(side){
				return ConstraintSide.side;
			}
			
			return ConstraintSide.none;

		}
		
		//Remove one of the constraints
		public void Remove(int sideOfObject){
			
			switch(sideOfObject){
				case ConstraintSide.right:
				right = false;
				break;
				
				case ConstraintSide.left:
				left = false;
				break;
				
				case ConstraintSide.forward:
				forward = false;
				break;
				
				case ConstraintSide.backward:
				backward = false;
				break;
				
				case ConstraintSide.up:
				up = false;
				break;
				
				case ConstraintSide.down:
				down = false;
				break;
				
				case ConstraintSide.side:
				side = false;
				break;
				
				default:
				break;
			}
		}
					
		//is any constraint set?
		public bool AnySet(){
			
			if(right || left || forward || backward || up || down){
				return true;
			}
			else{
				return false;
			}
		}
					
			
		//find out if the input side is set or not
		public bool IsSet(int sideOfObject){
			
			switch(sideOfObject){
				case ConstraintSide.right:
				if(right){
					return true;
				}
				break;
				
				case ConstraintSide.left:
				if(left){
					return true;
				}
				break;
				
				case ConstraintSide.forward:
				if(forward){
					return true;
				}
				break;
				
				case ConstraintSide.backward:
				if(backward){
					return true;
				}
				break;
				
				case ConstraintSide.up:
				if(up){
					return true;
				}
				break;
				
				case ConstraintSide.down:
				if(down){
					return true;
				}
				break;
				
				case ConstraintSide.side:
				if(side){
					return true;
				}
				break;
				
				default:
				break;
			}
			
			return false;
		}
		
	}
	ConstraintClass constraintClass;
	

	
	static class AxisType{
	
		public const int X = 0;
		public const int Y = 1;
		public const int Z = 2;
	}

	public static class OutlineType{
	
		public const int FORCE_NONTRACKING = 0;
		public const int FORCE_TRACKING = 1;
		public const int MARKER_FOR_TRACKING = 2;
	}
	
	public static class AlignType{
	
		public const int NONE = -1;
		public const int STRETCH_TO_POINT = 0;
		public const int ALIGN_TO_NORMAL = 1;
		public const int THREE_POINTS = 2;
		public const int ALIGN_OBJ = 3;
		public const int FLIP = 4;
	}
	
	
	public static class ConstraintSide{
	
		public const int none = -1;
		public const int right = 0;
		public const int left = 1;
		public const int forward = 2;
		public const int backward = 3;
		public const int up = 4;
		public const int down = 5;
		public const int side = 6;
	}
	
	static class ModType{
	
		public const int NONE = -1;
		public const int MOVE = 0;
		public const int STRETCH = 1;

	}
	
	public static class GeometryType{
	
		public const int NONE = 0;
		public const int PLANE = 1;
		public const int CUBE = 2;
		public const int CYLINDER = 3;
	}
	
	public static class Mode{
	
		public const int HOME = 0;
		public const int GAME = 1;
		public const int GEOMETRY = 2;
	}
	
	public static class Reset{
	
		public const int ALL = 0;
		public const int DOTS = 1;
		public const int GEOMETRY = 2;
	}

	public void ToggleContentButtonPressed(){

		if(showContent){

			showContent = false;
			DisableFast(sceneObjects[worldCenterSceneIndex]);
		}

		else{

			showContent = true;
			EnableFast(sceneObjects[worldCenterSceneIndex], worldCenterObject, true);
		}
	}


	public void AnimationButtonPressed(){

		specialEffect.TriggerAnimation();
	}
	

	public void AlignTypeButtonPressed(int alignType){

		Align(alignType, selectedGeometry, selectedDotsPositions, ref constraintClass);
		
		if(alignType != AlignType.ALIGN_OBJ){
		
			ResetSelections(Reset.DOTS);
		}
	}
	
	void AddDot(){
	
		if(userDotAmount < maxDotAmount){

			Vector3 intersectPosition = Vector3.zero;
			Vector3 dotsNormal = Vector3.zero;
			Vector3 dotsPosition = Vector3.zero;
	
			//get a ray from the mouse position into the world
			Ray ray = Camera.main.ScreenPointToRay(mouseAposGlobal);
		
			//Create a plane from the 3 selected dots
			Math3d.PlaneFrom3Points(out dotsNormal, out dotsPosition, selectedDotsPositions[0], selectedDotsPositions[1], selectedDotsPositions[2]);
		
			//Find out where the ray intersects the plane created by the 3 points.
			Math3d.LinePlaneIntersection(out intersectPosition, ray.origin, ray.direction, dotsNormal, dotsPosition);

			userDotsPositions[userDotAmount] = intersectPosition;
		
			userDotAmount++;
		}
	}
	

	//Calculates the scale of one side of an object (float, not Vector3) based on the width it should get.
	//"originalSize" is half the width of the object when the scale is 1.
	float CalculateScaleFromDistance(float targetWidth, float originalSize){
	
		return targetWidth / (originalSize * 2.0f);
	}
	
	
	//Align an object with the selected dots.
	//Selected marker is the marker which has the red color. Attached marker is the marker which was used to create
	//the geometry object.
	void Align(int alignType, GameObject objectToBeModified, Vector3[] selectedDotsPositions, ref ConstraintClass constraintClass){
		
		Vector3 translationVector;
		float distance;
		int side = 0;
		Vector3 dotsPosition = Vector3.zero;
		Vector3 dotsNormal = Vector3.zero;
		
		ObjectLineManager objectToBeModifiedLineManager = objectToBeModified.GetComponent<ObjectLineManager>();
		Math3d.PlaneFrom3Points(out dotsNormal, out dotsPosition, selectedDotsPositions[0], selectedDotsPositions[1], selectedDotsPositions[2]);
		
		//Make sure the normal points the correct way.
		dotsNormal = SetNormaltoCameraDirection(dotsNormal);
		
		//get the current constrained side (can be only one)
		side = constraintClass.Get();

		if((alignType == AlignType.STRETCH_TO_POINT) && (objectToBeModifiedLineManager.objectType != ObjectLineManager.ObjectType.OBJ) && (side != ConstraintSide.side)){

			//get the normal of the plane of the constrained side.
			Vector3 constraintNormal = GetConstraintNormal(objectToBeModified, side);
			
			//get a vector from the object center to the constrained side
			Vector3 constraintVector = GetConstraintVector(objectToBeModified, objectToBeModifiedLineManager.size, side);
			
			//get a point on the constrained plane
			Vector3 constraintPoint = objectToBeModifiedLineManager.transform.position + constraintVector;
			
			//Is the point on the correct side of the constrained edge?
			float edgeDotDistance = Math3d.SignedDistancePlanePoint(constraintNormal, constraintPoint, selectedDotsPositions[0]);
			
			//Flip the geometry if the dot is on the wrong side.
			if(edgeDotDistance > 0){
			
				FlipAroundSide(objectToBeModified, objectToBeModifiedLineManager.size, side);

				//Recalculate this
				constraintNormal = GetConstraintNormal(objectToBeModified, side);
				constraintVector = GetConstraintVector(objectToBeModified, objectToBeModifiedLineManager.size, side);
				constraintPoint = objectToBeModifiedLineManager.transform.position + constraintVector;
			}
			
			//Project the selected dot onto the constrained plane. 
			Vector3 projected = Math3d.ProjectPointOnPlane(constraintNormal, constraintPoint, selectedDotsPositions[0]);
			
			//Calculate the distance between the projected point and the selected dot.
			distance = Vector3.Magnitude(projected - selectedDotsPositions[0]);

			//stretch and move the geometry
			CalculateObjectStretch(side, objectToBeModified, distance, objectToBeModifiedLineManager.size);
		}
		
		if((alignType == AlignType.ALIGN_TO_NORMAL) && (objectToBeModifiedLineManager.objectType != ObjectLineManager.ObjectType.OBJ)){
			
			//calculate the forward vector by projecting the forward vector of the geometry onto the marker plane			
			Vector3 forward = Math3d.ProjectVectorOnPlane(dotsNormal, objectToBeModified.transform.forward);
			
			forward = Vector3.Normalize(forward);
			
			//align the normal of the geometry with the normal of the marker
			objectToBeModified.transform.rotation = Math3d.VectorsToQuaternion(forward, dotsNormal);
		}
		
		if(alignType == AlignType.FLIP){
		
			FlipAroundSide(objectToBeModified, objectToBeModifiedLineManager.size, side);
		}
		
		
		if((alignType == AlignType.THREE_POINTS) && (objectToBeModifiedLineManager.objectType != ObjectLineManager.ObjectType.OBJ)){

			Vector3 point1 = selectedDotsPositions[0];
			Vector3 point2 = selectedDotsPositions[1];
			Vector3 point3 = selectedDotsPositions[2];
			
			//Is this not a cylinder object?
			if(objectToBeModifiedLineManager.objectType != ObjectLineManager.ObjectType.CYLINDER){
			
				Vector3 scale = Vector3.zero;
				Vector3 rearrangedPoint1 = Vector3.zero;
				Vector3 rearrangedPoint2 = Vector3.zero;
				Vector3 rearrangedPoint3 = Vector3.zero;
				Vector3 vec1;
				Vector3 vec2;
				float angle;
				float div1;
				float div2;
				float div3;
				Vector3 projectedPoint3 = Vector3.zero;
				Vector3 normal = Vector3.zero;
				
				
				//Re-arrange the points so that number 2 is in the corner (middle) of the three.
				//To do that, calculate the angle between the two vectors from a point to the two other points.
				//The angle which is closest to 90 degrees is the middle point.
				vec1 = Vector3.Normalize(point2 - point1);
				vec2 = Vector3.Normalize(point3 - point1);
				angle = Vector3.Dot(vec1, vec2);
				div1 = Math.Abs(angle); //(90.0f - angle)
				
				vec1 = Vector3.Normalize(point1 - point2);
				vec2 = Vector3.Normalize(point3 - point2);
				angle = Vector3.Dot(vec1, vec2);
				div2 = Math.Abs(angle); //(90.0f - angle)
				
				vec1 = Vector3.Normalize(point2 - point3);
				vec2 = Vector3.Normalize(point1 - point3);
				angle = Vector3.Dot(vec1, vec2);
				div3 = Math.Abs(angle); //(90.0f - angle)
				
				//marker 1 is closer to 90 degrees
				if(div1 < div2){
					
					if(div1 < div3){
						//Marker 1 is the middle
						rearrangedPoint1 = point2;
						rearrangedPoint2 = point1;
						rearrangedPoint3 = point3;
					}
					
					else{
						//Marker 3 is the middle
						rearrangedPoint1 = point1;
						rearrangedPoint2 = point3;
						rearrangedPoint3 = point2;
					}
				}
				
				//marker 2 is closer to 90 degrees
				else{
					if(div2 < div3){
						//Marker 2 is the middle
						rearrangedPoint1 = point1;
						rearrangedPoint2 = point2;
						rearrangedPoint3 = point3;
					}
					
					else{
						//Marker 3 is the middle
						rearrangedPoint1 = point1;
						rearrangedPoint2 = point3;
						rearrangedPoint3 = point2;
					}
				}
	
				//Create a plane where the 3rd point should be projected on.
				normal = Vector3.Normalize(rearrangedPoint1 - rearrangedPoint2);
			
				//Project the third point on the plane we created
				projectedPoint3 = Math3d.ProjectPointOnPlane(normal, rearrangedPoint2, rearrangedPoint3);
				
				//Calculate the object width (x) scale, which is the distance between point 2 and 3
				distance = Vector3.Distance(rearrangedPoint2, projectedPoint3);
				scale.x = CalculateScaleFromDistance(distance, objectToBeModifiedLineManager.size.x);
				
				//Calculate the object length (z) scale, which is the distance between point 1 and 2
				distance = Vector3.Distance(rearrangedPoint1, rearrangedPoint2);
				scale.z = CalculateScaleFromDistance(distance, objectToBeModifiedLineManager.size.z);
				
				//store the original y scale
				scale.y = objectToBeModified.transform.localScale.y;
				
				//now set the scale
				objectToBeModified.transform.localScale = scale;
				
				//Caclulate the new position of the geometry. This is the position half way between points 1 and 3.
				translationVector = projectedPoint3 - rearrangedPoint1;
				translationVector /= 2.0f;
				objectToBeModified.transform.position = rearrangedPoint1 + translationVector;
				
				//Put the object on top of the plane instead of in the middle of it.
				//The up vector we get is in world space so the translation should be in world space too.
				translationVector = Math3d.SetVectorLength(dotsNormal, objectToBeModifiedLineManager.size.y * objectToBeModifiedLineManager.scale.y);
				objectToBeModified.transform.Translate(translationVector, Space.World);
				
				//Calculate surface normal of the 3 points
				Vector3 vec2to1 = rearrangedPoint1 - rearrangedPoint2;
				Vector3 vec2toP3 = projectedPoint3 - rearrangedPoint2;
				normal = Vector3.Cross(vec2to1, vec2toP3);
				normal.Normalize();
				
				//Make sure the normal points the correct way.
				normal = SetNormaltoCameraDirection(normal);
				
				//Now set the rotation of the geometry
				objectToBeModified.transform.rotation = Math3d.VectorsToQuaternion(vec2to1.normalized, normal);

				//Get a vector from the object to the plane created by the 3 points
				Vector3 projected = Math3d.ProjectPointOnPlane(dotsNormal, dotsPosition, objectToBeModified.transform.position);
				translationVector = projected - objectToBeModified.transform.position;
				
				//Figure out which side of the geometry the translationVector is pointing at.
				side = GetSideOfGeometry(translationVector, objectToBeModified);	

				//set the constraints
				constraintClass.Reset();
				constraintClass.Set(side);
				SetLineSegmentColor(selectedGeometry, colorForObject, colorForConstraint, constraintClass);
			}
			
			if(objectToBeModifiedLineManager.objectType == ObjectLineManager.ObjectType.CYLINDER){
				
				//After this constraint is applied, constrain (and color accordingly) the entire side (circular) of
				//the cylinder. This will need additions to the constrain class. This will also need additions to 
				//the Marker Plane constraint logic to detect we are dealing with a cylinder and stretch and move
				//the cylinder accordingly.
				
				//Get two lines which are perpendicular to the surface normal (created by the 3 points) and also
				//perpendicular to a line between two points, located in the middle of those two points.

				//Calculate coordinate system vectors which are used to calculate the line vectors and the surface normal.
				Vector3 coordVec1 = Vector3.Normalize(point1 - point2);
				Vector3 coordVec2 = Vector3.Normalize(point3 - point2);
				
				//Calculate the surface normal, created by the three points.
				Vector3 normal = Vector3.Cross(coordVec1, coordVec2);

				//Calculate the line points
				Vector3 linePoint1 = Vector3.Lerp(point1, point2, 0.5f);
				Vector3 linePoint2 = Vector3.Lerp(point2, point3, 0.5f);

				//Calculate the line vectors
				Vector3 lineVec1 = Vector3.Cross(coordVec1, normal);
				Vector3 lineVec2 = Vector3.Cross(coordVec2, normal);
				
				//Get the intersection of the two lines:
				Vector3 centerPoint1 =  Vector3.zero;
				Vector3 centerPoint2 = Vector3.zero;
				Math3d.ClosestPointsOnTwoLines(out centerPoint1, out centerPoint2, linePoint1, lineVec1, linePoint2, lineVec2);
				
				//Place the cylinder at the center point. Note that the two center points are at the same position,
				objectToBeModified.transform.position = centerPoint1;
				
				//Put the object on top of the plane instead of in the middle of it.
				//The up vector we get is in world space so the translation should be in world space too.
				translationVector = Math3d.SetVectorLength(dotsNormal, objectToBeModifiedLineManager.size.y * objectToBeModifiedLineManager.scale.y);
				objectToBeModified.transform.Translate(translationVector, Space.World);
				
				//Set the orientation of the cylinder.
				//Note that the direction of "destinationNormal (lineVec1 here) is not important, as long as it is
				//perpendicular to the normal vector.
				Math3d.LookRotationExtended(ref objectToBeModified, normal, lineVec1, objectToBeModified.transform.up, objectToBeModified.transform.forward);
				
				//Set the radius of the cylinder
				float diameter = 2.0f * Vector3.Magnitude(centerPoint1 - point1);
				objectToBeModified.transform.localScale = new Vector3(diameter, objectToBeModified.transform.localScale.y, diameter);

				//Get a vector from the object to the plane created by the 3 points
				Vector3 projected = Math3d.ProjectPointOnPlane(dotsNormal, dotsPosition, objectToBeModified.transform.position);
				translationVector = projected - objectToBeModified.transform.position;
				
				//Figure out which side of the geometry the translationVector is pointing at.
				side = GetSideOfGeometry(translationVector, objectToBeModified);	

				//set the constraints
				constraintClass.Reset();
				constraintClass.Set(side);
				SetLineSegmentColor(selectedGeometry, colorForObject, colorForConstraint, constraintClass);
			}
		}

		//For OBJ only. Align the OBJ magenta line and dot with the marker line and dot.
		if((alignType == AlignType.ALIGN_OBJ) && (objectToBeModifiedLineManager.objectType == ObjectLineManager.ObjectType.OBJ)){
			
			Vector3 markerLineVecWorld = Vector3.zero;
			Vector3 dotsNormalMod = dotsNormal;
			
			//Cycle the direction of the normal
			if(OBJcycle >= 6){
			
				dotsNormalMod *= -1;
			}

			//Cycle the direction of the vector
			if((OBJcycle == 0) || (OBJcycle == 6)){
			
				markerLineVecWorld = selectedDotsPositions[1] - selectedDotsPositions[0];
			}
			if((OBJcycle == 1) || (OBJcycle == 7)){
			
				markerLineVecWorld = selectedDotsPositions[0] - selectedDotsPositions[1];
			}
			if((OBJcycle == 2) || (OBJcycle == 8)){
			
				markerLineVecWorld = selectedDotsPositions[2] - selectedDotsPositions[1];
			}
			if((OBJcycle == 3) || (OBJcycle == 9)){
			
				markerLineVecWorld = selectedDotsPositions[1] - selectedDotsPositions[2];
			}
			if((OBJcycle == 4) || (OBJcycle == 10)){
			
				markerLineVecWorld = selectedDotsPositions[0] - selectedDotsPositions[2];
			}
			if((OBJcycle == 5) || (OBJcycle == 11)){
			
				markerLineVecWorld = selectedDotsPositions[2] - selectedDotsPositions[0];
			}

			markerLineVecWorld.Normalize();
			
			
			Vector3 trianglePosition = Vector3.zero;
			
			//Now set the selected dot position			
			if((OBJcycle == 0) || (OBJcycle == 5) || (OBJcycle == 6) || (OBJcycle == 11)){
			
				trianglePosition = selectedDotsPositions[0];
			}
			if((OBJcycle == 1) || (OBJcycle == 2) || (OBJcycle == 7) || (OBJcycle == 8)){
			
				trianglePosition = selectedDotsPositions[1];
			}
			if((OBJcycle == 3) || (OBJcycle == 4) || (OBJcycle == 9) || (OBJcycle == 10)){
			
				trianglePosition = selectedDotsPositions[2];
			}
			
			Math3d.PreciseAlign(ref objectToBeModified, markerLineVecWorld, dotsNormalMod, trianglePosition, OBJLineVector.normalized, OBJHitNormal, OBJLinePoint);
			
			//Cycle the state of the OBJ alignment
			OBJcycle++;
			
			if(OBJcycle > 11){
			
				OBJcycle = 0;
			}
		}
	}



	
	//this class supports multi touch 
	public class TouchInfoClass{
	
		public Vector2 positionA;
		public Vector2 positionB;
		
		public void GetPositionA(int customTouchCount){
			
			if(Application.isEditor){

				positionA = Input.mousePosition;  
			}
			
			if(!Application.isEditor){		
					
				if(customTouchCount >= 1){ 
					
					//Input.GetTouch(0) will cause an error on the pc, so hack around this problem. Of course it doesn't matter so much here 
					//since we are in MOBILE code mode but if we forget to set the mode to PC if we are debugging, at least 
					//single touch will still work. So we don't have to change the pre-processor all the time if we are debugging
					//between the IOS device and on the MAC or PC. This is especially nice since MonoDevelop doesn't update the
					//syntax highlighting if you change the preprocessor. You will have to reload the doc in order for that to work.
					if((Input.touchCount == 0) && (Input.anyKey == true)){

						positionA = Input.mousePosition;  
					}
					
					else{
						Touch touch1 = Input.GetTouch(0);

						positionA = touch1.position;
					}
				}
			}
		}

		public void  GetPositionB(int customTouchCount){	
			
			//We can't have multi touch on the pc, so simulate it by creating a zero coordinate.
			if(Application.isEditor){

				positionB = Vector2.zero; 
			}
			
			if(!Application.isEditor){

				if(customTouchCount == 2) {

					Touch touch2  = Input.GetTouch(1); 
					positionB = touch2.position;
				}	
			}
		}
	}
	public TouchInfoClass touchInfo;


	//this supports multi touch
	int GetTouchCount(){	
		
		if(Application.isEditor){
			
			if((Input.GetKey (KeyCode.DownArrow)) && (Input.GetMouseButton(0))){
				return 2;
			}
			
			if((!Input.GetKey (KeyCode.DownArrow)) && (Input.GetMouseButton(0))){
				return 1;
			}
			
			if((Input.GetKey (KeyCode.DownArrow)) && (!Input.GetMouseButton(0))){
				return 1;
			}
			
			if((!Input.GetKey (KeyCode.DownArrow)) && (!Input.GetMouseButton(0))){
				return 0;
			}
	
			return -1;
		}
		
		if(!Application.isEditor){
		
			//touchCount is always 0 on the PC so hack around this problem. Of course it doesn't matter so much here 
			//since we are in MOBILE code mode but if we forget to set the mode to PC if we are debugging, at least 
			//single touch will still work. So we don't have to change the pre-processor all the time if we are debugging
			//between the IOS device and on the MAC or PC. This is especially nice since MonoDevelop doesn't update the
			//syntax highlighting if you change the preprocessor. You will have to reload the doc in order for that to work.
			if((Input.touchCount == 0) && (Input.anyKey == true)){
				return 1;
			}
			
			else{
				return Input.touchCount;
			}
		}
		
		return -1;
	}
	

	public class PinchClass{

		public float scale = 0.0f;
		private float beginScale = 0.0f;

		public void SetBeginScale(float scale){
		
			beginScale = scale;
		}
				
		public void CalculatePinch(float pinch){

			float pinchFactor = pinch / 500.0f;
		
			scale = beginScale + pinchFactor;
			
			if(scale < 0.08f){
			
				scale = 0.08f;
			}		
		}
	}
	PinchClass xPinchClass;
	PinchClass yPinchClass;
	PinchClass zPinchClass;

	


	public void StartRecordingPressed(bool update){
	
		if(!update){

			referenceMarkerNumber = -1;
			pathfindingFinished = false;
			scaleFactor = 1.0f;
			recordingUpdate = false;

			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				lineManagerDotsObject.EnableVectorLine(true);
				lineManagerSelectedDots.EnableVectorLine(true);
			}

			//This is important. Reset these global variables before we start to record
			for(int i = 0; i < arWrapper.totalMarkerAmount; i++){ //go down in the array 		
		
				markerSeen[i] = false;
				   
				for(int e = 0; e < arWrapper.totalMarkerAmount; e++){//go sideways in the array
			
					distanceVecBetweenMarkers[i, e] = Vector3.zero;
					rotationDiffBetweenMarkers[i, e] = Quaternion.identity;	

					distanceVecBetweenMarkersAdd[i, e] = Vector3.zero;
					rotationDiffBetweenMarkersAdd[i, e] = Quaternion.identity;	
					addAmount[i, e] = 0;
				}
			}	
	
			outlineType = OutlineType.FORCE_TRACKING;
						
			ResetSelections(Reset.ALL);						
	
			DeleteAllGeometry(ref geometryAmountFile, ref geometryOBJAmountFile, worldCenterSceneIndex);
		
			//Delete all user created dots
			DeleteDots(true);
		}

		else{

			recordingUpdate = true;			
		}


		recording = true;
		arWrapper.StartRecording(update);
	}

	
	public void GameButtonPressed(){
		
		modeGameOneshot = true;
			
		ResetSelections(Reset.ALL);
		
		//detach the geometry object from the marker
		if(selectedGeometry != null){
			
			DetachAxis();
		}	
	}
	
	public void GeometryButtonPressed(){
					
		modeGeometryOneshot = true;
	}
	
	//Enable or disable the geometry lines and change the material of the object between
	//the depth mask and the blue shader.
	public void EnableAllGeometryLines(bool enable){
		
		//disable or enable the vector lines of the geometry objects
		for(int sceneIndex = 0; sceneIndex < sceneObjects.Length; sceneIndex++){
			
			if(mergedScenes[sceneIndex] == true){
			
				for(int i = 0; i < geometryAmountMax; i++){
				
					if(geometryObjectsScenes[sceneIndex, i] != null){
						
						//change the visibility of the lines
						ObjectLineManager lineManager = geometryObjectsScenes[sceneIndex, i].GetComponent<ObjectLineManager>();
						lineManager.EnableVectorLine(enable);
						
						//change the material
						if(enable){
							geometryObjectsScenes[sceneIndex, i].renderer.material = geometryModeMaterial;
						}
						else{
							geometryObjectsScenes[sceneIndex, i].renderer.material = gameModeMaterial;
						}
					}
				}
			}
		}
		
		//disable or enable the vector lines of the OBJ objects
		for(int sceneIndex = 0; sceneIndex < sceneObjects.Length; sceneIndex++){
			
			if(mergedScenes[sceneIndex] == true){
			
				for(int i = 0; i < geometryAmountMax; i++){
				
					if(geometryOBJObjectsScenes[sceneIndex, i] != null){
				
						//change the visibility of the lines
						ObjectLineManager lineManager = geometryOBJObjectsScenes[sceneIndex, i].GetComponent<ObjectLineManager>();
						lineManager.EnableVectorLine(enable);
						
						//change the material
						if(enable){
							geometryOBJObjectsScenes[sceneIndex, i].renderer.material = geometryModeMaterial;
						}
						else{
							geometryOBJObjectsScenes[sceneIndex, i].renderer.material = gameModeMaterial;
						}
					}
				}
			}
		}
		
		foreach(SingleLineManager lineManager in singleAxisLlinemanagers){	

			//change the visibility of the dots
			lineManager.EnableVectorLine(enable);			
		}
		
		if(!enable){

			DisableFast(axisAll);

			DisableFast(markerLineOBJObject);
			DisableFast(markerDotOBJObject);
			DisableFast(markerLineObject);
		}
		
		//enable
		else{
		
			//These objects have to be enabled after a scene is saved in geometry mode.
			axisAll.SetActive(true);
			markerLineOBJObject.SetActive(true); 
			markerDotOBJObject.SetActive(true);
			markerLineObject.SetActive(true);
		}
		

		lineManagerOBJLine.EnableVectorLine(enable);
		lineManagerOBJDot.EnableVectorLine(enable);
		lineManagerMarkerLine.EnableVectorLine(enable);
		
		singleAxisLlinemanagers[0].EnableVectorLine(enable);
		singleAxisLlinemanagers[1].EnableVectorLine(enable);
		singleAxisLlinemanagers[2].EnableVectorLine(enable);
	}
	
	
	public void FlashButtonPressed(){

		arWrapper.ToggleFlash();
	}


	public void LoadButtonPressed(){
	
#if UNIFILEBROWSER
		UniFileBrowser.use.OpenFileWindow(OpenFile);
		fileWindowOpen = true;
#endif
	}
	
	
	public void HomeButtonPressed(){
	
		gui.message = "";
		gui.menuToShow = Gui.MenuMode.HOME;
	
		if(trackingFrozen){
		
			ToggleFreezeCameraAndTracking();
		}	
		
		modeHomeOneshot = true;
		mode = Mode.HOME;
	
		//detach the geometry object from the marker
		DetachAxis();

		ResetSelections(Reset.ALL);
		
		bool merged = AreScenesMerged(mergedScenes);
		
		//are none of the scenes merged?
		if((!merged) && (worldCenterSceneIndex != -1)){
			
			gui.message = "Scene loaded: " + sceneObjects[worldCenterSceneIndex].name;
		}
		
		if(merged){
		
			gui.message = fileIO.mergedScenesString;
		}
		
		//Delete all user created dots.
		//Note: not deleting will be better, but this requires extra visibility logic
		//which I might add later, but for now, this will do:
		DeleteDots(true);
		
		EnableAllGeometry(false);
	}
	

	//NOTE: Vuforia uses automatic focus so there is no need to focus the camera manually.
	public void FocusButtonPressed(){
	
		arWrapper.Focus();
	}
	
	public void FlipButtonPressed(){
	
		if(flip == 1){
			
			flip = -1;
		}
		
		else{
			
			flip = 1;
		}

		if(Are3DotsSelected() && (arWrapper.markerType == ARWrapper.MarkerType.SLAM)){
		
			EnableFast(arrowObject, dotsObject, true);
			EnableFast(triangleObject, dotsObject, true);
		
			float dotsDistance;
			Vector3 dotsNormal;
			Vector3 dotsPosition;
			Vector3 dotsDirection;

			Vector3[] pointsWorldSpace = new Vector3[selectedDotsPositions.Length];
			pointsWorldSpace = Math3d.PointsToSpace(selectedDotsPositions, selectedDotsObject, Space.World);

			GetTriangleTransform(out dotsDistance, out dotsNormal, out dotsPosition, out dotsDirection, pointsWorldSpace, flip);
			SetArrow(ref arrowObject, dotsPosition, dotsDirection, dotsNormal, dotsDistance);
			SetTriangle(pointsWorldSpace, dotsNormal, dotsPosition, dotsDirection);
		}
	}
	
	
	//stop the recording and process the result
	public void StopRecordingPressed(){
		
		arWrapper.StopRecording();		

		if(((referenceMarkerNumber != -1) || Are3DotsSelected()) || recordingUpdate){

			mode = Mode.HOME;
			pathfindingFinished = true;
			recording = false;

			ProcessRecordingFinished(recordingUpdate);

			if((arWrapper.markerType != ARWrapper.MarkerType.SLAM) && (!recordingUpdate)){

				string unresolvedMarkers;
				bool unresolved = GetUnresolvedMarkers(out unresolvedMarkers, markerSeen, markersForTracking);
			
				if(unresolved){

					gui.message = "These marker(s) are not resolved: " + unresolvedMarkers;
				}
			}

			recordingUpdate = false;

			ResetSelections(Reset.ALL);	
		}

		else{

			gui.message = "Select a reference frame first";
		}

	}

	//stop the recording and reset the whole scene
	public void ResetButtonPressed(){
	
		ResetSelections(Reset.ALL);
		gui.message = "";
		referenceMarkerNumber = -1;
		mode = Mode.HOME;		
		pathfindingFinished = false;
		worldCenterSceneIndex = -1;
		worldCenterSceneName = "";
		geometryAmountFile = 0;
		geometryOBJAmountFile = 0;
		Array.Clear(mergedScenes, 0, mergedScenes.Length);
		Array.Clear(mergedScenesNames, 0, mergedScenesNames.Length);
		recording = false;

		DeleteAllGeometry(ref geometryAmountFile, ref geometryOBJAmountFile, worldCenterSceneIndex);
		
		//Make sure all scene objects are detached
		int index = 0;
		foreach(GameObject sceneObject in sceneObjects){
			
			sceneObject.transform.parent = null;
			sceneObject.SetActive(false);
			referenceMarkerNumberScenes[index] = -1;
			
			index++;
		}
		
		for(int i = 0; i < arWrapper.totalMarkerAmount; i++){ //go down in the array 
		
			offsetVectorToRefMarker[i] = Vector3.zero;
			offsetRotationToRefMarker[i] = Quaternion.identity;
		}	
		
		Array.Clear(markersForTracking, 0, markersForTracking.Length);
		
		//TODO: not sure if Array.Clear() works on a multidimensional array
		SetMultiDimensionalBoolArray(ref markersForTrackingScenes, sceneObjects.Length, arWrapper.totalMarkerAmount, false);

		outlineType = OutlineType.FORCE_TRACKING;
		
		//Delete all user created dots
		DeleteDots(true);
		
		arWrapper.Reset();
	}
	



	//stop the recording
	public void CancelRecordingPressed(){
		
		if(recording){	
			
			referenceMarkerNumber = -1;
			mode = Mode.HOME;
			recording = false;
			recordingUpdate = false;
			pathfindingFinished = false;
			
			arWrapper.Reset();
		}
	}
	
	public void ClearSelectionsButtonPressed(){
	
		ResetSelections(Reset.ALL);
	}
	
	public void AddDotButtonPressed(){
	
		AddDot();
	}
	
	public void FreezeButtonPressed(){
	
		ToggleFreezeCameraAndTracking();
	}
	
	public void PrimitiveGeometryButtonPressed(int geometryType){
				
		if(geometryAmountFile < geometryAmountMax){
		
			InstantiateGeometry(ref geometryAmountFile, ref geometryTypeArray, selectedDotsPositions, geometryType);
		}
		
		else{		
		
			gui.message = "Maximum amount of geometry objects reached";
		}
	}
	
	public void DeleteButtonPressed(){
	
		DeleteGeometry(selectedGeometry, ref geometryAmountFile);
		
		//Delete all selected user created dots
		DeleteDots(false);
	}
	
	
	void InstantiateGeometry(ref int geometryAmountFile, ref int[] geometryTypeArray, Vector3[] selectedDotsPositions, int geometryType){

		//Detach the axis system and reset the transform. This is to prevent transform
		//position and rotation wandering with each parent change.
		DetachAxis();

		if(geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile] == null){
			
			//instantiate the geometry
			if(geometryType == GeometryType.CUBE){
				
				geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile] = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity) as GameObject;
				geometryTypeArray[geometryAmountFile] = GeometryType.CUBE;
			}
			
			if(geometryType == GeometryType.PLANE){
				
				geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile] = Instantiate(planePrefab, Vector3.zero, Quaternion.identity) as GameObject;
				geometryTypeArray[geometryAmountFile] = GeometryType.PLANE;
			}
			
			if(geometryType == GeometryType.CYLINDER){
				
				geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile] = Instantiate(cylinderPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				geometryTypeArray[geometryAmountFile] = GeometryType.CYLINDER;
			}
		}
	 
		//set the scale
		ObjectLineManager lineManager = geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile].GetComponent<ObjectLineManager>();	
		geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile].transform.localScale = lineManager.scale;

		//Only select one geometry object.
		//De-select the marker attached to the geometry object.
		ResetSelections(Reset.GEOMETRY);
		
		selectedGeometry = geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile];		

		//Stretch geometry so it fits between the 3 points.
		Align(AlignType.THREE_POINTS, geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile], selectedDotsPositions, ref constraintClass);
		
		ResetSelections(Reset.DOTS);

		SetSelections(selectedGeometry, null, true);
		AttachAxis(selectedGeometry);

		//Parent to world center scene scene
		geometryObjectsScenes[worldCenterSceneIndex, geometryAmountFile].transform.parent = geometryContainer.transform;
		
		geometryAmountFile++;
	}
	
	private void ProcessRecordingUpdate(){

		PathFindingDijkstra(ref markersForTracking, ref offsetVectorToRefMarker, ref offsetRotationToRefMarker, distanceVecBetweenMarkers, rotationDiffBetweenMarkers, referenceMarkerNumber);		
	
		string path = Application.persistentDataPath + "/" + sceneObjects[worldCenterSceneIndex].name + ".ucs";
		SaveFile(path);
	}
	
	private void ProcessRecordingFinished(bool update){
		
		if(!update){

			if(arWrapper.markerType != ARWrapper.MarkerType.SLAM){

				PathFindingDijkstra(ref markersForTracking, ref offsetVectorToRefMarker, ref offsetRotationToRefMarker, distanceVecBetweenMarkers, rotationDiffBetweenMarkers, referenceMarkerNumber);		
			}

			//for SLAM
			else{

				//reset the offset transform
				offsetVectorToRefMarker[0] = Vector3.zero;
				offsetRotationToRefMarker[0] = Quaternion.identity;

				//set the offset transform if 3 points are selected
				if(Are3DotsSelected()){
		
					float dotsDistance;
					Vector3 dotsNormal;
					Vector3 dotsPosition;
					Vector3 dotsDirection;
		
					//no need for the arrow and triangle anymore, so disable it
					DisableFast(arrowObject);
					DisableFast(triangleObject);
			
					//Get the triangle transform. This is to be used for the rotational and positional offset of the camera
					//Here we need the selected points in object space
					GetTriangleTransform(out dotsDistance, out dotsNormal, out dotsPosition, out dotsDirection, selectedDotsPositions, flip);

					//Get the scale from the edit box. This has to be done after GetTriangleTransform
					scaleFactor = GetScaleFactor(dotsDistance, gui.editFloat);

					//Set the rotational and positional offset which is to be stored to the save file
					//The transform stored is relative to the slam dots object, not in world space.
					offsetVectorToRefMarker[0] = dotsPosition / scaleFactor;			
					offsetRotationToRefMarker[0] = Quaternion.LookRotation(dotsDirection, dotsNormal);
				}		
			}

			gui.SaveButtonPressed();		
			outlineType = OutlineType.MARKER_FOR_TRACKING;
		}

		else{

			ProcessRecordingUpdate();
		}
	}


	void CopyMultiToSingleArray(int[,] multiArray, ref int[] singleArray, int index){

		for(int i = 0; i < singleArray.Length; i++){

			singleArray[i] = multiArray[i, index];
		}
	}

	private static void CopyMultiArray(int[,] sourceArray, ref int[,] destinationArray, int lengthA, int lengthB){

		for(int i = 0; i < lengthA; i++){

			for(int e = 0; e < lengthB; e++){

				destinationArray[i,e] = sourceArray[i,e];
			}			
		}
	}

	private static void SetMultiDimensionalBoolArray(ref bool[,] array, int iLength, int eLength, bool value){

		for(int i = 0; i < iLength; i++){

			for(int e = 0; e < eLength; e++){
			
				array[i, e] = value;
			}
		}
	}


	
	void ReformatGeometryObjects2D(ref GameObject[,] gameObjects, int worldCenterSceneIndex){
	
		//copy array
		GameObject[] gameObjectsBuffer = new GameObject[geometryAmountMax];

		for(int i = 0; i < geometryAmountMax; i++){
		
			gameObjectsBuffer[i] = gameObjects[worldCenterSceneIndex, i];
		}
		
		//first reset
		for(int e=0; e < geometryAmountMax; e++){
		
			gameObjects[worldCenterSceneIndex, e] = null;
		}
		
		int index = 0;
		for(int e=0; e < geometryAmountMax; e++){
		
			if(gameObjectsBuffer[e] != null){
				
				gameObjects[worldCenterSceneIndex, index] = gameObjectsBuffer[e]; 
				index++;					
			}
		}
	}
	
	void ReformatGeometryTypeArray(ref int[] geometryTypeArray){
	
		//copy array
		int[] geometryTypeArrayBuffer = new int[geometryTypeArray.Length];		
		Array.Copy(geometryTypeArray, geometryTypeArrayBuffer, geometryTypeArray.Length);
		
		//first reset
		for(int e=0; e < geometryTypeArray.Length; e++){
		
			geometryTypeArray[e] = GeometryType.NONE;
		}
		
		int index = 0;
		for(int e=0; e < geometryTypeArray.Length; e++){
		
			if(geometryTypeArrayBuffer[e] != GeometryType.NONE){
				
				geometryTypeArray[index] = geometryTypeArrayBuffer[e]; 
				index++;					
			}
		}
	}
	
	void ReformatGeometryOBJNames(ref string[] geometryOBJNames){
	
		//copy array
		string[] geometryOBJNamesBuffer = new string[geometryOBJNames.Length];		
		Array.Copy(geometryOBJNames, geometryOBJNamesBuffer, geometryOBJNames.Length);
		
		//first reset
		for(int e=0; e < geometryOBJNames.Length; e++){
		
			geometryOBJNames[e] = "";
		}
		
		int index = 0;
		for(int e=0; e < geometryOBJNames.Length; e++){
		
			if(geometryOBJNamesBuffer[e] != ""){
				
				geometryOBJNames[index] = geometryOBJNamesBuffer[e]; 
				index++;					
			}
		}
	}



	//Delete all user created dots
	public void DeleteDots(bool deleteAll){
		
		//Delete all dots?
		if(deleteAll){
			
			for(int i = 0; i < 3; i++){
			
				selectedDots[i] = -1; 
				selectedDotsPositions[i] = Vector3.zero;
			}

			userDotAmount = 0;
		}
		
		//Delete only the selected user generated dots?
		else{

			//loop through all the user created dots
			int numberIndex = 0;
			int removedDotAmount = 0;

			for(int i = 0; i < userDotAmount; i++){
			
			
				//Is this dot selected?
				int indexEmptySlot;
				int index = -1;

				Math3d.GetNumberIndex(out index, out indexEmptySlot, selectedDots, i);

				if(index != -1){

					//delete the slot
					selectedDots[index] = -1; 
					selectedDotsPositions[index] = Vector3.zero;

					removedDotAmount++;
				}
				
				numberIndex++;
			}
			
			userDotAmount -= removedDotAmount;
		}
	}
	

	void DeleteGeometry(GameObject selectedGeometry, ref int geometryAmountFile){//tag: deleteObject destroyObject removeObject destroyGeometry removeGeometry
		
		if(selectedGeometry != null){
			
			ObjectLineManager lineManager = selectedGeometry.GetComponent<ObjectLineManager>();
		
			if(lineManager.objectType != ObjectLineManager.ObjectType.OBJ){
			
				if(geometryAmountFile > 0){
		
					ResetSelections(Reset.ALL);			
					Destroy(selectedGeometry);
					selectedGeometry = null;
					geometryAmountFile--;
					
					//When Destroy() is called, it doesn't remove the entry from the array right away. But the reformat
					//function does expect the array entry to be null, so force the array entry to be null here.
					//Find the entry:
					for(int i = 0; i < geometryAmountMax; i++){

						if(selectedGeometry == geometryObjectsScenes[worldCenterSceneIndex, i]){
						
							geometryObjectsScenes[worldCenterSceneIndex, i] = null;
							geometryTypeArray[i] = GeometryType.NONE;
							break;
						}
					}
				}
			}
			
			//is OBJ
			else{	
			
				if(geometryOBJAmountFile > 0){
				
					//detach this first to prevent it from being destroyed.
					DisableFast(markerLineOBJObject);
					DisableFast(markerDotOBJObject);
					
					ResetSelections(Reset.ALL);			
					Destroy(selectedGeometry);
					geometryOBJAmountFile--;
					
					//When Destroy() is called, it doesn't remove the entry from the array right away. But the reformat
					//function does expect the array entry to be null, so force the array entry to be null here.
					//Find the entry:
					for(int i = 0; i < geometryAmountMax; i++){

						if(selectedGeometry == geometryOBJObjectsScenes[worldCenterSceneIndex, i]){
						
							geometryOBJObjectsScenes[worldCenterSceneIndex, i] = null;
							geometryOBJNames[i] = "";
							break;
						}
					}
				}
			}
		}
		
		//Make sure there are no gaps in the middle of the array
		ReformatGeometryObjects2D(ref geometryObjectsScenes, worldCenterSceneIndex);
		ReformatGeometryObjects2D(ref geometryOBJObjectsScenes, worldCenterSceneIndex);
		ReformatGeometryTypeArray(ref geometryTypeArray);
		ReformatGeometryOBJNames(ref geometryOBJNames);
	}
	
	public void DeleteAllGeometryPressed(){
	
		DeleteAllGeometry(ref geometryAmountFile, ref geometryOBJAmountFile, worldCenterSceneIndex);
	}
	
	void DeleteAllGeometry(ref int geometryAmountFile, ref int geometryOBJAmountFile, int worldCenterSceneIndex){//tag: deleteObject destroyObject removeObject destroyGeometry removeGeometry
		
		ResetSelections(Reset.ALL);	
		
		//detach this first to prevent it from being destroyed.
		DisableFast(markerLineOBJObject);
		DisableFast(markerDotOBJObject);
		
		if(worldCenterSceneIndex != -1){
			
			for(int i = 0; i < geometryAmountMax; i++){
				
				if(geometryObjectsScenes[worldCenterSceneIndex, i] != null){
				
					Destroy(geometryObjectsScenes[worldCenterSceneIndex, i]);
					geometryObjectsScenes[worldCenterSceneIndex, i] = null;
					geometryTypeArray[i] = GeometryType.NONE;
				}
				
				if(geometryOBJObjectsScenes[worldCenterSceneIndex, i] != null){
				
					Destroy(geometryOBJObjectsScenes[worldCenterSceneIndex, i]);
					geometryOBJObjectsScenes[worldCenterSceneIndex, i] = null;
					geometryOBJNames[i] = "";
				}
			}
				
			geometryAmountFile = 0;
			geometryOBJAmountFile = 0;
		}
	}
	
	
	//NOTE: the order in which the child and parent are attached and detached and the order
	//of changing the scale and position is important. See the description in SingleLineManager.cs
	//for more information.
	void DetachAxis(){
		
		ResetAxisSelection(singleAxisLlinemanagers);
	
		//now detach		
		axisAll.transform.parent = null;
		axisAll.transform.localScale = new Vector3(1, 1, 1);

		DisableFast(axisAll);
		
		ResetAllPinch();

		moveBeginPosSet = false;
	}
	
	//This system is very messy and needs to be re-done
	public void AttachAxis(GameObject attachedObject){
	
		float factor = 2.0f;

		ObjectLineManager lineManager = attachedObject.GetComponent<ObjectLineManager>();
	
		if(lineManager.objectType == ObjectLineManager.ObjectType.PLANE){

			axisAll.transform.localScale = new Vector3(factor * lineManager.size.x * attachedObject.transform.localScale.x, factor * lineManager.size.x * attachedObject.transform.localScale.x, factor * lineManager.size.z * attachedObject.transform.localScale.z);
		}
		else{
			//set the scale according to the bounding box
			axisAll.transform.localScale = new Vector3(factor * lineManager.size.x * attachedObject.transform.localScale.x, factor * lineManager.size.y * attachedObject.transform.localScale.y, factor * lineManager.size.z * attachedObject.transform.localScale.z);
		}
		
		axisAll.transform.position = attachedObject.transform.position;
		axisAll.transform.rotation = attachedObject.transform.rotation;
		
		axisAll.transform.parent = attachedObject.transform;	
	}
	
	//Find out which markers are seen at least once during recording (markerSeen) but it's position and rotation
	//relative to the reference marker is not resolved after pathfinding.
	bool GetUnresolvedMarkers(out string unresolvedMarkers, bool[] markerSeen, bool[] markersForTracking){

		bool result = false;
		unresolvedMarkers = "";
		
		for(int i = 0; i < arWrapper.totalMarkerAmount; i++){
			
			if(markerSeen[i]){
				
				if(!markersForTracking[i]){
					
					unresolvedMarkers = unresolvedMarkers + i + ", ";
					result = true;
				}
			}				
		}
		
		return result;
	}
	



	//FileBrowser.js callback function
	void FileWindowRect(Rect fileRectSize){
		
		//set the global rect size
		fileRect = fileRectSize;
	}
	
	//FileBrowser.js callback function
	void FileWindowClosed(){
		
		//set the global file window state
		fileWindowOpen = false;
	}
	
	//figure out whether or not the triangle index is part of an object side
	bool IsTriangleIndexSide(ObjectLineManager.ObjectType objectType, int triangleIndex){
		
		if(objectType == ObjectLineManager.ObjectType.PLANE){
		
			if((triangleIndex == 0) || (triangleIndex == 1) || (triangleIndex == 8) || (triangleIndex == 10) || (triangleIndex == 14) || (triangleIndex == 15) || (triangleIndex == 21) || (triangleIndex == 23) ||
			   (triangleIndex == 18) || (triangleIndex == 19) || (triangleIndex == 11) || (triangleIndex == 12) || (triangleIndex == 2) || (triangleIndex == 3) || (triangleIndex == 26) || (triangleIndex == 25)){
				return true;
			}
			
			else{
				return false;
			}
		}
		
		if(objectType == ObjectLineManager.ObjectType.CUBE){
			return true;
		}
		
		if(objectType == ObjectLineManager.ObjectType.CYLINDER){
		
			if((triangleIndex == 60) || (triangleIndex == 65) || (triangleIndex == 64) || (triangleIndex == 63) || (triangleIndex == 68) || (triangleIndex == 67) || (triangleIndex == 66) || (triangleIndex == 71) || (triangleIndex == 70) || (triangleIndex == 69) || (triangleIndex == 74) || (triangleIndex == 73) || (triangleIndex == 72) || (triangleIndex == 77) || (triangleIndex == 76) || (triangleIndex == 75) || (triangleIndex == 79) || (triangleIndex == 78) || (triangleIndex == 62) || (triangleIndex == 61)){
				return true;
			}
			
			if((triangleIndex == 47) || (triangleIndex == 46) || (triangleIndex == 45) || (triangleIndex == 44) || (triangleIndex == 43) || (triangleIndex == 42) || (triangleIndex == 41) || (triangleIndex == 40) || (triangleIndex == 59) || (triangleIndex == 58) || (triangleIndex == 57) || (triangleIndex == 56) || (triangleIndex == 55) || (triangleIndex == 54) || (triangleIndex == 53) || (triangleIndex == 52) || (triangleIndex == 51) || (triangleIndex == 50) || (triangleIndex == 49) || (triangleIndex == 48)){
				return true;
			}
		}
			
		return false;
	}
	
	//convert the triangle index to a constraint side
	int TriangleIndexToConstraint(ObjectLineManager.ObjectType objectType, int triangleIndex){

		if(objectType == ObjectLineManager.ObjectType.PLANE){
		
			if((triangleIndex == 0) || (triangleIndex == 1) || (triangleIndex == 2) || (triangleIndex == 3)){ // 18,19
				return ConstraintSide.forward;
			}
			
			if((triangleIndex == 8) || (triangleIndex == 10) || (triangleIndex == 11) || (triangleIndex == 12)){//26,25
				return ConstraintSide.left;
			}
	
			if((triangleIndex == 21) || (triangleIndex == 23) || (triangleIndex == 26) || (triangleIndex == 25)){//11,12
				return ConstraintSide.right;
			}
	
			if((triangleIndex == 14) || (triangleIndex == 15) || (triangleIndex == 18) || (triangleIndex == 19)){//2,3
				return ConstraintSide.backward;
			}
		}
		
		if(objectType == ObjectLineManager.ObjectType.CUBE){
		
			if((triangleIndex == 9) || (triangleIndex == 8)){
				return ConstraintSide.left;
			}
			
			if((triangleIndex == 0) || (triangleIndex == 1)){
				return ConstraintSide.forward;
			}
	
			if((triangleIndex == 4) || (triangleIndex == 5)){
				return ConstraintSide.backward;
			}
	
			if((triangleIndex == 10) || (triangleIndex == 11)){
				return ConstraintSide.right;
			}
			
			if((triangleIndex == 2) || (triangleIndex == 3)){
				return ConstraintSide.up;
			}
			
			if((triangleIndex == 6) || (triangleIndex == 7)){
				return ConstraintSide.down;
			}
		}
		
		if(objectType == ObjectLineManager.ObjectType.CYLINDER){
		
			if((triangleIndex == 60) || (triangleIndex == 65) || (triangleIndex == 64) || (triangleIndex == 63) || (triangleIndex == 68) || (triangleIndex == 67) || (triangleIndex == 66) || (triangleIndex == 71) || (triangleIndex == 70) || (triangleIndex == 69) || (triangleIndex == 74) || (triangleIndex == 73) || (triangleIndex == 72) || (triangleIndex == 77) || (triangleIndex == 76) || (triangleIndex == 75) || (triangleIndex == 79) || (triangleIndex == 78) || (triangleIndex == 62) || (triangleIndex == 61)){
				return ConstraintSide.up;
			}
			
			if((triangleIndex == 47) || (triangleIndex == 46) || (triangleIndex == 45) || (triangleIndex == 44) || (triangleIndex == 43) || (triangleIndex == 42) || (triangleIndex == 41) || (triangleIndex == 40) || (triangleIndex == 59) || (triangleIndex == 58) || (triangleIndex == 57) || (triangleIndex == 56) || (triangleIndex == 55) || (triangleIndex == 54) || (triangleIndex == 53) || (triangleIndex == 52) || (triangleIndex == 51) || (triangleIndex == 50) || (triangleIndex == 49) || (triangleIndex == 48)){
				return ConstraintSide.down;
			}
		}
		
		return ConstraintSide.none;
	}
				
			
	//If a geometry object is scaled with a constraint set, we need to reposition the object so that one of the sides
	//seems to be fixed in space. Returned vector is in Object space.
	Vector3 GetConstraintCorrectedTranslation(ObjectLineManager.ObjectType objectType, Vector3 objectSize, Vector3 currentScale, Vector3 initialScale){
		
		float xOffset = 0.0f;
		float yOffset = 0.0f;
		float zOffset = 0.0f;
		Vector3 translationVector = Vector3.zero;

		if(constraintClass.IsSet(ConstraintSide.left)){
			
			xOffset = (currentScale.x * objectSize.x) - (initialScale.x * objectSize.x);
		}
		
		if(constraintClass.IsSet(ConstraintSide.right)){
			
			xOffset = -1 * ((currentScale.x * objectSize.x) - (initialScale.x * objectSize.x));
		}
		
		if(constraintClass.IsSet(ConstraintSide.backward)){
			
			zOffset = (currentScale.z * objectSize.z) - (initialScale.z * objectSize.z);
		}
		
		if(constraintClass.IsSet(ConstraintSide.forward)){
			
			zOffset = -1 * ((currentScale.z * objectSize.z) - (initialScale.z * objectSize.z));
		}
		
		if(constraintClass.IsSet(ConstraintSide.down)){
			
			yOffset = (currentScale.y * objectSize.y) - (initialScale.y * objectSize.y);
		}
		
		if(constraintClass.IsSet(ConstraintSide.up)){
			
			yOffset = -1 * ((currentScale.y * objectSize.y) - (initialScale.y * objectSize.y));
		}
		
		translationVector = new Vector3(xOffset, yOffset, zOffset);

		return translationVector;
	}
		


	

	//Do the camera and scene object placement.
	//At this stage an offset vector has been computed which is the offset from the current marker
	//to the reference marker. Add this this offset to the current location of the marker per
	//frame in game or geometry mode. Do this for every marker and average out the resulting coordinates.
	//The resulting coordinate is the location of the reference marker. So if a game object is
	//given those coordinates, it should end up at the reference marker.
	//Note: the offsetVectorToRefMarker and rotation variables when using PointCloud do not contain
	//the offset to the reference marker but to the plane created by 3 selected SLAM dots (if any).
	//Note: with PointCloud the camera transform is not calculated here but in the PointCloud.cs script.
	void PlaceCameraAndSceneObjects(int sceneIndex){		
	
		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && trackingMarkerInViewScenes[sceneIndex] && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){

			//loop through the markers used for tracking which are currently being tracked.
			sceneObjectTransformed.transform.position = Vector3.zero;
			sceneObjectTransformed.transform.rotation = Quaternion.identity;
			tempGameObject.transform.position = Vector3.zero;
			tempGameObject.transform.rotation = Quaternion.identity;
			Vector3 sceneObjectTransformedPositionAdd = Vector3.zero;		
			int addAmount = 0;
			Vector4 cumulativeRotation = Vector4.zero;

			Quaternion firstRotation = Quaternion.identity;
			bool firstRotationSet = false;

			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

				if(arWrapper.markerId[i] != -1){

					if(markersForTrackingScenes[sceneIndex, arWrapper.markerId[i]]){	
					
						//Get the transform of the current marker
						tempGameObject.transform.position = arWrapper.markerTransform[i].transform.position;

						//This is not redundant. Leave it.
						tempGameObject.transform.rotation = arWrapper.markerTransform[i].transform.rotation;
					
						//Translate the temp game object from the position of the current marker to the calculated offset from the reference marker,
						//so it ends up on top of the reference marker. This will give us a transform which can be averaged together with all
						//the other detected markers without touching the actual marker itself.
						tempGameObject.transform.Translate(offsetVectorToRefMarkerScenes[sceneIndex, arWrapper.markerId[i]]);
				
						//Calculate an average position from the calculated reference marker position from all markers in view
						//The average position (and rotation) is called sceneObjectTransformed.
						sceneObjectTransformedPositionAdd += tempGameObject.transform.position;
						addAmount++;
						sceneObjectTransformed.transform.position = sceneObjectTransformedPositionAdd / (float)addAmount;	
									
						//Do the same for the rotation. Note that the math is different. 
						tempGameObject.transform.rotation = arWrapper.markerTransform[i].transform.rotation * offsetRotationToRefMarkerScenes[sceneIndex, arWrapper.markerId[i]];
					
						//Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
						//q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
						if(!firstRotationSet){

							firstRotation = tempGameObject.transform.rotation;
							firstRotationSet = true;
						}

						sceneObjectTransformed.transform.rotation = Math3d.AverageQuaternion(ref cumulativeRotation, tempGameObject.transform.rotation, firstRotation, addAmount);
					}
				}
			}				
	
			//Calculate the camera transform. Only do this for the world center scene.
			if(sceneIndex == worldCenterSceneIndex){				

				//reset the camera
				Camera.main.transform.position = cameraStartPosition; 
				Camera.main.transform.rotation = cameraStartRotation;

				Quaternion cameraRotation;
				Vector3 cameraPosition;

				//We have to transform the scene object so the camera moves with it.
				Math3d.TransformWithParent(out cameraRotation, out cameraPosition, Quaternion.identity, Vector3.zero, sceneObjectTransformed.transform.rotation, sceneObjectTransformed.transform.position, cameraStartRotation, cameraStartPosition);

				Camera.main.transform.rotation = cameraRotation;
				Camera.main.transform.position = cameraPosition;		
			}
			
			//The scene is a merged scene but not the world center scene.
			//Calculate the scene transform.
			if(sceneIndex != worldCenterSceneIndex){
						
				Quaternion sceneObjectRotation;
				Vector3 sceneObjectPosition;

				//We have to transform the virtual camera so the scene object moves with it.
				Math3d.TransformWithParent(out sceneObjectRotation, out sceneObjectPosition, Camera.main.transform.rotation, Camera.main.transform.position, cameraStartRotation, cameraStartPosition, sceneObjectTransformed.transform.rotation, sceneObjectTransformed.transform.position);

				sceneObjects[sceneIndex].transform.rotation = sceneObjectRotation;
				sceneObjects[sceneIndex].transform.position = sceneObjectPosition;
			}
		}

		//for SLAM
		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && arWrapper.markerInView && (arWrapper.markerType == ARWrapper.MarkerType.SLAM)){

			//Calculate the camera transform. Only do this for the world center scene.
			if(sceneIndex == worldCenterSceneIndex){	
				
				//reset the camera
				Camera.main.transform.position = cameraStartPosition; 
				Camera.main.transform.rotation = cameraStartRotation;

				Quaternion cameraRotation;
				Vector3 cameraPosition;

				//We have to transform the scene object so the camera moves with it.				 		
				Math3d.TransformWithParent(out cameraRotation, out cameraPosition,  dotsObject.transform.rotation, dotsObject.transform.position, arWrapper.markerTransform[0].transform.rotation, arWrapper.markerTransform[0].transform.position, Quaternion.identity, Vector3.zero);

				Camera.main.transform.rotation = cameraRotation;
				Camera.main.transform.position = cameraPosition;	
			}
		}		
	}

	
	
	//At this stage the unified coordinate system creation has finished and geometry objects
	//do hot have to be parented to the marker objects anymore. However, markers are still free
	//to move around in the scene. The markers used for tracking should not be moved, but all
	//other markers can be used for geometry creation and need separate transform modification logic.
	void PlaceContent(int sceneIndex, bool somethingChanged){

		//Position the markers which are in view relative to the sceneObject.
		if(((mode == Mode.GEOMETRY) || (mode == Mode.GAME)) && (mergedScenes[sceneIndex] == true) && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){//&& trackingMarkerInViewScenes[sceneIndex]
			
			Quaternion markerObjectRotation;
			Vector3 markerObjectPosition;

			//TODO: don't call this every frame
			bool merged = AreScenesMerged(mergedScenes);

			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

				if(arWrapper.markerId[i] != -1){

					if((merged && (markersForTrackingScenes[sceneIndex, arWrapper.markerId[i]] == true)) || (!merged)){

						//The camera has been moved so now transform the markers so they appear to be at the
						//same position relative to the camera.
						Math3d.TransformWithParent(out markerObjectRotation, out  markerObjectPosition, Camera.main.transform.rotation, Camera.main.transform.position, cameraStartRotation, cameraStartPosition, arWrapper.markerTransform[i].transform.rotation, arWrapper.markerTransform[i].transform.position);
				
						arWrapper.markerTransform[i].transform.rotation = markerObjectRotation;
						arWrapper.markerTransform[i].transform.position =  markerObjectPosition;				
					}
				}
			}
		}
		
		if((mode != Mode.GEOMETRY) && (mode != Mode.GAME) && (arWrapper.markerType == ARWrapper.MarkerType.SLAM) && ((sceneIndex == worldCenterSceneIndex) || (worldCenterSceneIndex == -1))){ 

			if(!arWrapper.markerInView && somethingChanged){

				DisableFast(dotsObject);
				DisableFast(selectedDotsObject);
				DisableFast(userDotsObject);
			}

			if(arWrapper.markerInView && somethingChanged){

				EnableFast(dotsObject, null, true);
				EnableFast(selectedDotsObject, null, true);
				EnableFast(userDotsObject, null, true);
			}

			if(arWrapper.markerInView){
				
				dotsObject.transform.position = arWrapper.markerTransform[0].transform.position;
				dotsObject.transform.rotation = arWrapper.markerTransform[0].transform.rotation;			
				
				selectedDotsObject.transform.position = arWrapper.markerTransform[0].transform.position;
				selectedDotsObject.transform.rotation = arWrapper.markerTransform[0].transform.rotation;	
				
				userDotsObject.transform.position = arWrapper.markerTransform[0].transform.position;
				userDotsObject.transform.rotation = arWrapper.markerTransform[0].transform.rotation;
			}
		}
		
		//Remove the 3d scene from view if there is no marker visible during game mode
		//or no game mode is selected
		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && !trackingMarkerInViewScenes[sceneIndex] && somethingChanged && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){	

			//depending on which sceneObject has none of it's tracking markers in view,
			//remove that object
			if(mergedScenes[sceneIndex]){ 
							
				DisableFast(sceneObjects[sceneIndex]);
				DisableFast(geometryContainer);
			}
		}	
	
		//Remove the 3d scene from view if there is no marker visible during game mode
		//or no game mode is selected
		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && !arWrapper.markerInView && somethingChanged && (arWrapper.markerType == ARWrapper.MarkerType.SLAM)){	

			//depending on which sceneObject has none of it's tracking markers in view,
			//remove that object
			if(sceneIndex == worldCenterSceneIndex){ 
							
				DisableFast(sceneObjects[sceneIndex]);
				DisableFast(geometryContainer);
				
				if(forceOutlineSetting && (mode == Mode.GAME)){

					lineManagerDotsObject.EnableVectorLine(false);
					lineManagerSelectedDots.EnableVectorLine(false);
					lineManagerUserDots.EnableVectorLine(false);
				}
			}
		}	

		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && trackingMarkerInViewScenes[sceneIndex] && somethingChanged && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){	
		
			//depending on which sceneObject has one of it's tracking markers in view,
			//enable that object
			if(mergedScenes[sceneIndex]){
							
				if(sceneIndex == worldCenterSceneIndex){
					
					if(showContent){

						EnableFast(sceneObjects[worldCenterSceneIndex], worldCenterObject, true);						
					}

					EnableFast(geometryContainer, worldCenterObject, true);
				}
				
				else{
	
					EnableFast(sceneObjects[sceneIndex], null, false);
				}
			}
		}

		if(((mode == Mode.GAME) || (mode == Mode.GEOMETRY)) && arWrapper.markerInView && somethingChanged && (arWrapper.markerType == ARWrapper.MarkerType.SLAM)){	
		
			//depending on which sceneObject has one of it's tracking markers in view,
			//enable that object
			if(sceneIndex == worldCenterSceneIndex){
							
				if(showContent){

					EnableFast(sceneObjects[worldCenterSceneIndex], worldCenterObject, true);						
				}

				EnableFast(geometryContainer, worldCenterObject, true);
						
				if(forceOutlineSetting && (mode == Mode.GAME)){

					lineManagerDotsObject.EnableVectorLine(true);
					lineManagerSelectedDots.EnableVectorLine(true);
					lineManagerUserDots.EnableVectorLine(true);
				}
			}
		}


		//In home or recording mode, the UCS system is not used and subsequently the outlines visibility has to be
		//handled manually here.
		if((mode == Mode.HOME) || (mode == Mode.GEOMETRY) || ((mode == Mode.GAME) && forceOutlineSetting) && somethingChanged && (arWrapper.markerType != ARWrapper.MarkerType.SLAM)){
			
			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){	
				
				if(arWrapper.markerId[i] != -1){	
					
					EnableFast(outlineObjects[i], arWrapper.markerTransform[i], true);
				}
				
				else{
				
					DisableFast(outlineObjects[i]);
				}
			}
		}		

		if(somethingChanged && (mergedScenes[sceneIndex]) && (mode == Mode.GEOMETRY) && (arWrapper.markerType != ARWrapper.MarkerType.SLAM) ){ 

			if(!trackingMarkerInViewScenes[worldCenterSceneIndex]){

				DisableFast(dotsObject);
				DisableFast(selectedDotsObject);
				DisableFast(userDotsObject);
			}

			else{

				EnableFast(dotsObject, null, true);			
				EnableFast(selectedDotsObject, null, true);	
				EnableFast(userDotsObject, null, true);	
			}
		}


		if(somethingChanged && ((sceneIndex == worldCenterSceneIndex) || (worldCenterSceneIndex == -1)) && ((mode == Mode.HOME) || (mode == Mode.GEOMETRY)) && (arWrapper.markerType == ARWrapper.MarkerType.SLAM)){ 

			if(!arWrapper.markerInView){

				lineManagerDotsObject.EnableVectorLine(false);
				lineManagerSelectedDots.EnableVectorLine(false);
				lineManagerUserDots.EnableVectorLine(false);
			}

			else{

				lineManagerDotsObject.EnableVectorLine(true);
				lineManagerSelectedDots.EnableVectorLine(true);
				lineManagerUserDots.EnableVectorLine(true);
			}
		}
		
		
		
		if(modeHomeOneshot){			

			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				lineManagerDotsObject.EnableVectorLine(true);			
				EnableFast(dotsObject, null, true);
			
				lineManagerSelectedDots.EnableVectorLine(true);			
				EnableFast(selectedDotsObject, null, true);	

				lineManagerUserDots.EnableVectorLine(true);			
				EnableFast(userDotsObject, null, true);	
			}

			else{

				DisableFast(dotsObject);
				lineManagerDotsObject.EnableVectorLine(false);

				DisableFast(selectedDotsObject);
				lineManagerSelectedDots.EnableVectorLine(false);

				DisableFast(userDotsObject);
				lineManagerUserDots.EnableVectorLine(false);
			}
			
			if(worldCenterSceneIndex != -1){
			
				foreach(GameObject sceneObject in sceneObjects){
				
					sceneObject.SetActive(false);
				}

				DisableFast(sceneObjects[worldCenterSceneIndex]);
				DisableFast(geometryContainer);
			}
		}
		

		if(modeGameOneshot){				
			
			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				CalculateSLAMDotsTransform(out dotsObjectRotation, out dotsObjectPosition, offsetRotationToRefMarkerScenes[worldCenterSceneIndex, 0], offsetVectorToRefMarkerScenes[worldCenterSceneIndex, 0]);

				dotsObject.transform.rotation = dotsObjectRotation;
				dotsObject.transform.position = dotsObjectPosition;		
		
				selectedDotsObject.transform.rotation = dotsObjectRotation;
				selectedDotsObject.transform.position = dotsObjectPosition;	

				userDotsObject.transform.rotation = dotsObjectRotation;
				userDotsObject.transform.position = dotsObjectPosition;	
	
				if(!forceOutlineSetting){
			
					//do not disable LAMdotsObject!S
				
					//disable the SLAM points
					lineManagerDotsObject.EnableVectorLine(false);
					lineManagerSelectedDots.EnableVectorLine(false);
					lineManagerUserDots.EnableVectorLine(false);
				}
			
				//Position the dots object
				else{
					
					//enable the points
					EnableFast(dotsObject, null, false);
					lineManagerDotsObject.EnableVectorLine(true);

					EnableFast(selectedDotsObject, null, false);
					lineManagerSelectedDots.EnableVectorLine(true);

					EnableFast(userDotsObject, null, false);
					lineManagerUserDots.EnableVectorLine(true);
				}
			}
		
			DisableFast(arrowObject);
			DisableFast(triangleObject);
			
			if (worldCenterSceneIndex != -1){
			
				for(int i = 0; i < sceneObjects.Length; i++){
			
					if(mergedScenes[i] == true){
					
						sceneObjects[i].SetActive(true);
						sceneObjects[i].transform.localPosition = Vector3.zero;
					}
				}
				
				sceneObjects[worldCenterSceneIndex].SetActive(true);
				sceneObjects[worldCenterSceneIndex].transform.localPosition = Vector3.zero;
			}
			
			//Remove all the geometry lines and use the depth mask shader instead of the blue shader.
			EnableAllGeometry(true);
			EnableAllGeometryLines(false);
		}
		
		//Position the slam dots object
		if(modeGeometryOneshot){

			showContent = true;
			EnableFast(sceneObjects[worldCenterSceneIndex], worldCenterObject, true);
			EnableFast(geometryContainer, worldCenterObject, true);

			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				CalculateSLAMDotsTransform(out dotsObjectRotation, out dotsObjectPosition, offsetRotationToRefMarkerScenes[worldCenterSceneIndex, 0], offsetVectorToRefMarkerScenes[worldCenterSceneIndex, 0]);

				dotsObject.transform.rotation = dotsObjectRotation;
				dotsObject.transform.position = dotsObjectPosition;			

				selectedDotsObject.transform.rotation = dotsObjectRotation;
				selectedDotsObject.transform.position = dotsObjectPosition;

				userDotsObject.transform.rotation = dotsObjectRotation;
				userDotsObject.transform.position = dotsObjectPosition;

				EnableFast(dotsObject, null, false);
				EnableFast(selectedDotsObject, null, false);
				EnableFast(userDotsObject, null, false);
				
			}

			else{

				EnableFast(dotsObject, null, true);
				EnableFast(selectedDotsObject, null, true);
				EnableFast(userDotsObject, null, true);
			}

			lineManagerDotsObject.EnableVectorLine(true);
			lineManagerSelectedDots.EnableVectorLine(true);
			lineManagerUserDots.EnableVectorLine(true);

			//Enable all the geometry lines and use the blue shader instead of the depth mask
			EnableAllGeometry(true);
			EnableAllGeometryLines(true);
		}
		
		//In game mode (with force outline disabled) we do not need the outlines, so disable them.
		if(modeGameOneshot && !forceOutlineSetting){
			
			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){	

				DisableFast(outlineObjects[i]);
			}
		}	

		if(modeGameOneshot || modeHomeOneshot){
		
			//reset the camera
			Camera.main.transform.position = cameraStartPosition; 
			Camera.main.transform.rotation = cameraStartRotation;
		}
		

		//reset flags
		if(modeGameOneshot){
			modeGameOneshot = false;
		}
		
		if(modeHomeOneshot){
			modeHomeOneshot = false;
		}
	
		if(modeGeometryOneshot){
			modeGeometryOneshot = false;
		}
	}
	
	
	void SelectObject(GameObject selectedObject){
		
		GameObject[] geometryObjectsInView;
	
		//selectedGeometry can be another object at this stage, so first clear the other one.
		constraintClass.Reset();
		SetLineSegmentColor(selectedGeometry, colorForObject, colorForConstraint, constraintClass);
		
		//now set the new selected object
		selectedGeometry = selectedObject;
		
		//detach axis first
		DetachAxis();
		
		//There might be a more efficient way of doing this but this is to be implemented at a later time.
		geometryObjectsInView = GameObject.FindGameObjectsWithTag("Geometry");
		
		//select one geometry object only	
		SetSelections(selectedObject, geometryObjectsInView, true);

		//attach axis again.
		AttachAxis(selectedObject);
	}
	
	
	bool GetSmallestScreenDistance(Vector3 positionWorld, Vector2 mousePosition, ref float smallestDistanceFound, float distanceThreshold2d){
	
		bool isSmallest = false;
		
		//get the screen space position of the dot
		Vector3 screenPos3d = Camera.main.WorldToScreenPoint(positionWorld);
		Vector2 screenPos2d = new Vector2(screenPos3d.x, screenPos3d.y);
	
		//calculate the distance to the mouse position
		float distance  = Vector2.Distance(screenPos2d, mousePosition);
		
		//Is it a hit? 
		if(distance <= distanceThreshold2d){
		
			//Is it the closest one if there are multiple hits?
			if(distance <= smallestDistanceFound){
			
				smallestDistanceFound = distance;
				isSmallest = true;
			}	
		}
		
		return isSmallest;
	}

	int IsPositionSelected(int[] selectedDots, Vector3[] selectedDotsPositions, Vector3 position, float distanceThreshold3d){

		for(int i = 0; i < selectedDots.Length; i++){

			//is this not an empty slot?
			if(selectedDots[i] != -1){

				//get the distance between the two vectors
				float distance = Vector3.Distance(selectedDotsPositions[i], position);

				if(distance < distanceThreshold3d){

					return i;
				}
			}
		}

		return -1;
	}

	void MouseDotSelection(Vector2 mousePosition, Vector3[] dotsPositions, int currentDotAmount, int[] selectedDots, Vector3[] selectedDotsPositions)
	{

		bool found = false;
		float smallestDistanceFound = distanceThreshold2d; 
		Vector3 positionFound = Vector3.zero;
		bool isSmallest = false;
		Vector3 worldPosition;

		//get the smallest distance found to a dot
		for(int i = 0; i < currentDotAmount; i++){

			worldPosition = dotsObject.transform.TransformPoint(dotsPositions[i]);

			isSmallest = GetSmallestScreenDistance(worldPosition, mousePosition, ref smallestDistanceFound, distanceThreshold2d);
	
			if(isSmallest){

				positionFound = worldPosition;
				found = true;
			}	
		}

		for(int i = 0; i < selectedDotsPositions.Length; i++){

			if(selectedDots[i] != -1){
				
				worldPosition = selectedDotsObject.transform.TransformPoint(selectedDotsPositions[i]);

				isSmallest = GetSmallestScreenDistance(worldPosition, mousePosition, ref smallestDistanceFound, distanceThreshold2d);
	
				if(isSmallest){

					positionFound = worldPosition;
					found = true;
				}	
			}
		}

		for(int i = 0; i < userDotAmount; i++){

			worldPosition = userDotsObject.transform.TransformPoint(userDotsPositions[i]);

			isSmallest = GetSmallestScreenDistance(worldPosition, mousePosition, ref smallestDistanceFound, distanceThreshold2d);
	
			if(isSmallest){

				positionFound = worldPosition;
				found = true;
			}	
		}

		if(found){

			int slotIndex = -1;
			int emptySlotIndex = -1;

			Vector3 localPosition = selectedDotsObject.transform.InverseTransformPoint(positionFound);

			//is the position already selected?
			int selectedIndex = IsPositionSelected(selectedDots, selectedDotsPositions, localPosition, distanceThreshold3d);

			//the position is not yet selected
			if(selectedIndex == -1){

				//find an empty slot
				Math3d.GetNumberIndex(out slotIndex, out emptySlotIndex, selectedDots, -1);

				//an empty slot is found
				if(slotIndex != -1){

					//add a dot entry
					selectedDots[slotIndex] = 1;
					selectedDotsPositions[slotIndex] = localPosition;
				}

				//No empty slot is found, so 3 positions are selected.
				//Replace the last entry.
				else{

					//replace the last dot entry
					selectedDots[2] = 1;
					selectedDotsPositions[2] = localPosition;
				}
			}

			//the position is already selected
			else{

				//remove the dot entry
				selectedDots[selectedIndex] = -1;
			}

			//display the arrow and the triangle
			if(arWrapper.markerType == ARWrapper.MarkerType.SLAM){

				if(Are3DotsSelected()){
												
					float dotsDistance;
					Vector3 dotsNormal;
					Vector3 dotsPosition;
					Vector3 dotsDirection;
										
					EnableFast(arrowObject, dotsObject, true);
					EnableFast(triangleObject, dotsObject, true);
						
					Vector3[] pointsWorldSpace = new Vector3[selectedDotsPositions.Length];
					pointsWorldSpace = Math3d.PointsToSpace(selectedDotsPositions, selectedDotsObject, Space.World);

					GetTriangleTransform(out dotsDistance, out dotsNormal, out dotsPosition, out dotsDirection, pointsWorldSpace, flip);
					SetArrow(ref arrowObject, dotsPosition, dotsDirection, dotsNormal, dotsDistance);
					SetTriangle(pointsWorldSpace, dotsNormal, dotsPosition, dotsDirection);
				}
					
				else{
								
					DisableFast(arrowObject);
					DisableFast(triangleObject);
				}
			}
		}
	}

	
	void PopulateDotsSelections(ref Vector3[] selectedDotsPositions, int[] selectedDots, Vector3 hidePosition){

		for(int i = 0; i < selectedDotsPositions.Length; i++){

			//hide the dot(s)
			if(selectedDots[i] == -1){

				selectedDotsPositions[i] = hidePosition;
			}
		}
	}

	void PopulateDotsUser(ref Vector3[] userDotsPositions, int userDotAmount, Vector3 hidePosition){

		for(int i = userDotAmount; i < userDotsPositions.Length; i++){

			//hide the rest of the dots
			userDotsPositions[i] = hidePosition;
		}
	}
	
	
	int PopulateDotsSLAM(ref Vector3[] dotsPositions, Vector3[] SLAMpoints, Vector3 hidePosition){

		for(int i = 0; i < SLAMpoints.Length; i++){

			dotsPositions[i] = SLAMpoints[i];
		}

		//Hide the rest of the dots
		for(int i = SLAMpoints.Length; i < dotsPositions.Length; i++){

			dotsPositions[i] = hidePosition;
		}

		return SLAMpoints.Length;
	}

	int PopulateDotsMarkers(ref Vector3[] dotsPositions, Vector3 hidePosition)
	{
		int index = 0;
		int dotAmount = 0;

		//populate the marker dots
		for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

			for(int e = 0; e < 4; e++){

				//is the marker tracking?
				if(arWrapper.markerId[i] != -1){

					dotsPositions[index] = GetDotPositionGlobal(i, e, arWrapper.markerTransform[i], arWrapper.markerSize);
				}

				//marker is not tracking
				else{

					//hide the dot
					dotsPositions[index] = hidePosition;
				}

				index++;
			}
		}

		dotAmount = index;

		//hide the rest of the dots
		for(int i = index; i < dotsPositions.Length; i++){

			dotsPositions[index] = hidePosition;
			index++;
		}

		return dotAmount;
	}

	//Get a dot position in world coordinates. Because the center of a dot object is the same as the
	//center of the marker (in case of marker based AR, not SLAM), the actual coordinates of the dot
	//has to be calculated and cannot be derived from transform.position directly.
	Vector3 GetDotPositionGlobal(int markerId, int corner_index, GameObject markerObject, Vector2[] markerSize)
	{

		//get the position of the dot relative to the marker, in object space.
		Vector3 dotPosition = GetDotPositionLocal(corner_index, markerId , markerSize);
	
		//Get the world space location of the dot
		Vector3 translationVector = new Vector3(dotPosition.x, 0, dotPosition.y); 
		Vector3 positionWorld = markerObject.transform.TransformPoint(translationVector);
		
		return positionWorld;
	}

	//Get a dot position relative to the marker
	Vector2 GetDotPositionLocal(int corner_index, int markerId, Vector2[] markerSize){
	
		if(corner_index == 0){
			return new Vector2(markerSize[markerId].x, markerSize[markerId].y);
		}
		if(corner_index == 1){
			return new Vector2(-markerSize[markerId].x, markerSize[markerId].y);
		}
		if(corner_index == 2){
			return new Vector2(markerSize[markerId].x, -markerSize[markerId].y);
		}
		if(corner_index == 3){
			return new Vector2(-markerSize[markerId].x, -markerSize[markerId].y);
		}
		
		return Vector2.zero;
	}


	//Flip (or leave alone) a normal with reference to the camera.
	Vector3 SetNormaltoCameraDirection(Vector3 normal){
	
		//Get the angle between the camera up vector and the normal.
		float dot = Vector3.Dot(normal, Camera.main.transform.up);
		
		//Is the angle more then 90 degrees?
		if(dot < 0.0f){
			
			//Reverse the normal vector
			return normal * -1;
		}
		
		//Leave the normal as it is.
		return normal;
	}
	
	
	void ToggleFreezeCameraAndTracking(){
			
		if(!trackingFrozen){	

			arWrapper.Freeze(true);

			if(mode != Mode.HOME){

				specialEffect.zoomScreen.SetActive(true);
			}

			trackingFrozen = true;	
		}
		
		else{

			arWrapper.Freeze(false);
		
			//disable the zoom screen
			specialEffect.zoomScreen.SetActive(false);
			trackingFrozen = false;
		}
	}
	
	
	//debug
	public void Debug1ButtonPressed(){	

		if(debug1){
			
			debug1 = false;
		}
		
		else{
			
			debug1 = true;
		}	
	}
	

	public void Debug2ButtonPressed(){

		ToggleFreezeCameraAndTracking();

		if(debug2){
			
			debug2 = false;

		}
		else{
			debug2 = true;
		}
	}
	
	public void Debug3ButtonPressed(){

	
		if(debug3){
			
			debug3 = false;
		}
		else{
			debug3 = true;
		}
	}
	
	//This function makes the input object a child of the disableObject which in turn is placed
	//behind the camera. This is a fast way of getting an object out of view.
	public void DisableFast(GameObject objectToDisable){
					
		if(objectToDisable != null){
						
			if(objectToDisable.transform.parent != disableGameObject.transform){
		
				objectToDisable.transform.parent = disableGameObject.transform;
				objectToDisable.transform.localPosition = Vector3.zero;
				objectToDisable.transform.localRotation = Quaternion.identity;
			}
		}			
	}
	
	void EnableFast(GameObject objectToEnable, GameObject parentObject, bool forceIdentity){
	
		if(objectToEnable != null){
							
			if(parentObject != null){
			
				if(objectToEnable.transform.parent != parentObject.transform){
				
					objectToEnable.transform.parent = parentObject.transform;
				}
			}
			
			else{
			
				objectToEnable.transform.parent = null;		
			}
			
			if(forceIdentity){
			
				objectToEnable.transform.localPosition = Vector3.zero;
				objectToEnable.transform.localRotation = Quaternion.identity;
			}
		}
	}
	
	
	public void EnableAllGeometry(bool enableGeometry){
	
		if(enableGeometry){
		
			if(worldCenterSceneIndex != -1){
			
				sceneObjects[worldCenterSceneIndex].SetActive(true);
				sceneObjects[worldCenterSceneIndex].transform.localPosition = Vector3.zero;
			}
		}
		
		for(int sceneIndex = 0; sceneIndex < sceneObjects.Length; sceneIndex++){
			
			if(mergedScenes[sceneIndex] == true){
			
				for(int i = 0; i < geometryAmountMax; i++){
				
					if(geometryObjectsScenes[sceneIndex, i] != null){
					
						geometryObjectsScenes[sceneIndex, i].SetActive(enableGeometry);
					}
					
					if(geometryOBJObjectsScenes[sceneIndex, i] != null){
					
						geometryOBJObjectsScenes[sceneIndex, i].SetActive(enableGeometry);
					}
				}
			}
		}
	}
	
	public void SaveWorldCenterScene(){
	
		string pathToFile = Application.persistentDataPath + "/" + sceneObjects[worldCenterSceneIndex].name + ".ucs";
		SaveFile(pathToFile);
	}
	
	//Find out if some scenes are merged or not.
	public bool AreScenesMerged(bool[] mergedScenes){
	
		int amount = 0;
	
		foreach(bool scene in mergedScenes){
		
			if(scene == true){
			
				amount++;
			}
		}
		
		if(amount > 1){
		
			return true;
		}
		
		else{
		
			return false;
		}
	}
	
	public void OpenFile(string path){
	
		fileIO.OpenFile(path, mode, ref pathfindingFinished, ref geometryOBJAmountFile, geometryAmountMax, ref geometryOBJObjectsScenes, ref worldCenterSceneIndex, objPrefab, cubePrefab, planePrefab, cylinderPrefab, ref sceneObjects, ref geometryOBJNames, versionFile, ref mergedScenes, ref mergedScenesAmountFile, ref mergedScenesNames, ref worldCenterSceneName, arWrapper.totalMarkerAmount, ref markersForTracking, ref offsetVectorToRefMarker, ref offsetRotationToRefMarker, ref referenceMarkerNumber, ref geometryAmountFile, ref geometryTypeArray, ref geometryObjectsScenes, outlineObjects, ref markersForTrackingScenes, ref offsetVectorToRefMarkerScenes, ref offsetRotationToRefMarkerScenes, ref referenceMarkerNumberScenes, worldCenterObject, ref selectedGeometry, ref scaleFactor);
	}
	
	public void SaveFile(string path){
	
		fileIO.SaveFile(path, mode, ref pathfindingFinished, ref geometryOBJAmountFile, geometryAmountMax, ref geometryOBJObjectsScenes, ref worldCenterSceneIndex, objPrefab, cubePrefab, planePrefab, cylinderPrefab, ref sceneObjects, ref geometryOBJNames, versionFile, ref mergedScenes, ref mergedScenesAmountFile, ref mergedScenesNames, ref worldCenterSceneName, arWrapper.totalMarkerAmount, ref markersForTracking, ref offsetVectorToRefMarker, ref offsetRotationToRefMarker, ref referenceMarkerNumber, ref geometryAmountFile, ref geometryTypeArray, ref geometryObjectsScenes, outlineObjects, ref markersForTrackingScenes, ref offsetVectorToRefMarkerScenes, ref offsetRotationToRefMarkerScenes, ref referenceMarkerNumberScenes, worldCenterObject, ref selectedGeometry, ref scaleFactor);
	}
	

	public bool Are3DotsSelected(){

		int selectedDotAmount = 0;
		
		for(int i = 0; i < 3; i++){

			if(selectedDots[i] != -1){

				selectedDotAmount++;
			}
		}

		if(selectedDotAmount == 3){

			return true;
		}

		else{

			return false;
		}
	}


	//get the amount of dots selected
	public int GetSelectionAmount(){
		
		int selectedDotAmount = 0;
		
		for(int i = 0; i < 3; i++){
			

			if(selectedDots[i] != -1){

				selectedDotAmount++;
			}
		}

		return selectedDotAmount;			
	}
	
	
	public int GetSceneIndex(string sceneName, GameObject[] sceneObjects){

		int index = 0;
		
		//find the appropriate 3d scene and attach it to the worldCenterObject
		foreach(GameObject sceneObject in sceneObjects){
			
			//Is the scene object name the same as the ucs name?
			//Case un-sensitive string compare.
			if(string.Equals(sceneName, sceneObject.name, StringComparison.CurrentCultureIgnoreCase)){

				return index;
			}
			
			index++;
		}
		
		return -1;
	}
	
	
	public void GuiMessage(string message){
	
		gui.message = message;
	}

	


	void SetArrow(ref GameObject arrowObject, Vector3 positionVector, Vector3 directionVector, Vector3 normalVector, float dotsDistance){
	
		arrowObject.transform.rotation = Math3d.VectorsToQuaternion(directionVector, normalVector);
		arrowObject.transform.position = positionVector;
		
		arrowObject.transform.localScale = new Vector3(dotsDistance, dotsDistance, dotsDistance);
	}
	
	
	void SetTriangle(Vector3[] selectedDotsPositions, Vector3 dotsNormal, Vector3 dotsPosition, Vector3 dotsDirection){

		triangleObject.transform.position = dotsPosition;
		triangleObject.transform.rotation = Math3d.VectorsToQuaternion(dotsDirection, dotsNormal);
	
		if(Are3DotsSelected()){

			//set the triangle
			Vector3[] vertices = new Vector3[3];
			
			for(int i = 0; i < vertices.Length; i++){

				vertices[i] = triangleObject.transform.InverseTransformPoint(selectedDotsPositions[i]);
			}			
			
			triangleLineManager.SetVerticesAndColors(vertices, colorForObject, colorForConstraint);	
		}
	}
	
	
	public void GetTriangleTransform(out float dotsDistance, out Vector3 dotsNormal, out Vector3 dotsPosition, out Vector3 dotsDirection, Vector3[] vertices, int flip){

		//the vertex indices used for the distance and dot vector
		int vertexA = 0;
		int vertexB = 1;
	
		Math3d.PlaneFrom3Points(out dotsNormal, out dotsPosition, vertices[0], vertices[1], vertices[2]);
		
		dotsNormal *= (float)flip;
		
		//get the dot vector
		dotsDirection = Vector3.Normalize(vertices[vertexB] - vertices[vertexA]);
		
		//get the distance between the first two vertices
		dotsDistance = Vector3.Distance(vertices[vertexB], vertices[vertexA]);
	}


	
	//This function takes the selected triangle from the slam dots object and outputs the transform of the slam dots object
	//which will make the selected triangle is located in the world centre.
	void CalculateSLAMDotsTransform(out Quaternion outputRotation, out Vector3 outputPosition, Quaternion inputRotation, Vector3 inputPosition){
	
		Vector3 direction = Math3d.GetForwardVector(inputRotation);
		Vector3 normal = Math3d.GetUpVector(inputRotation);

		Math3d.PreciseAlign(ref tempGameObject, Vector3.forward, Vector3.up, Vector3.zero, direction, normal, inputPosition);

		outputRotation = tempGameObject.transform.rotation;
		outputPosition = tempGameObject.transform.position;
	}
	

	//calculate the scale factor and set it for pointCloud logic usage
	float GetScaleFactor(float dotsDistance, float editBoxFloat){
	
		float realDistance;
		
		//the edit box is left empty or is set to 0
		if(gui.editFloat == 0.0f){
		
			realDistance = 1.0f;
		}
		
		else{
		
			realDistance = editBoxFloat;
		}
		
		return dotsDistance / realDistance;
	}

	void CreateMarkerAccessories(){
		
		if(arWrapper.markerType != ARWrapper.MarkerType.SLAM){

			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){	
	
				//create a line object (square outline) 
				outlineObjects[i] = Instantiate(outlinePrefab, Vector3.zero, outlinePrefab.transform.rotation) as GameObject;
 	
				ChangeColorAllLines(outlineObjects[i], colorForObject);
			}
		}
	}
}

