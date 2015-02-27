//#define OBJREADER
//#define POINTCLOUD


using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;
using System;

public class FileIO : MonoBehaviour {


	private int referenceMarkerNumberWCS = -1;
	private int geometryAmountFileWCS = 0;
	private int geometryOBJAmountFileWCS = 0;
	
	private int[] geometryTypeArrayWCS;
	private string[] geometryOBJNamesWCS;	
	
	[HideInInspector]
	public string mergedScenesString = "";
	
	private MarkerRecorder markerRecorder;
	private Gui gui;
	
	void Start(){
	
		markerRecorder = gameObject.GetComponent<MarkerRecorder>();
		gui = gameObject.GetComponent<Gui>();
		
		geometryTypeArrayWCS = new int[markerRecorder.geometryAmountMax];
		geometryOBJNamesWCS = new string[markerRecorder.geometryAmountMax];
	}
	
	
	public void AttachScene(int sceneIndex, ref GameObject[] sceneObjects, bool isWorldCenterScene, GameObject worldCenterObject){

		//The current scene is the world center scene
		if(isWorldCenterScene){
		
			sceneObjects[sceneIndex].transform.parent = worldCenterObject.transform;
		}
		
		sceneObjects[sceneIndex].transform.localPosition = Vector3.zero;
		sceneObjects[sceneIndex].transform.localRotation = Quaternion.identity;
	}
	

	
	public void StoreCurrentTrackingDataIntoScenes(int sceneIndex, bool[] markersForTracking, ref bool[,] markersForTrackingScenes, Vector3[] offsetVectorToRefMarker, ref Vector3[,] offsetVectorToRefMarkerScenes, ref Quaternion[,] offsetRotationToRefMarkerScenes, Quaternion[] offsetRotationToRefMarker, int referenceMarkerNumber, ref int[] referenceMarkerNumberScenes){
			
		for(int i = 0; i < markersForTracking.Length; i++){
		
			markersForTrackingScenes[sceneIndex, i] = markersForTracking[i];
		}
	
		for(int i = 0; i < offsetVectorToRefMarker.Length; i++){

			offsetVectorToRefMarkerScenes[sceneIndex, i] = offsetVectorToRefMarker[i];
		}
		
		for(int i = 0; i < offsetVectorToRefMarker.Length; i++){

			offsetRotationToRefMarkerScenes[sceneIndex, i] = offsetRotationToRefMarker[i];
		}
		
		referenceMarkerNumberScenes[sceneIndex] = referenceMarkerNumber;
	}
	
	public void InstantiateAllGeometry(int geometryAmountFile, ref GameObject[,] geometryObjectsScenes, int[] geometryTypeArray, int sceneIndex, GameObject cubePrefab, GameObject planePrefab, GameObject cylinderPrefab, int worldCenterSceneIndex, GameObject[] sceneObjects){
		
		for(int i = 0; i < geometryAmountFile; i++){
		
			if(geometryObjectsScenes[sceneIndex, i] == null){
				
				//instantiate the geometry
				if(geometryTypeArray[i] == MarkerRecorder.GeometryType.CUBE){				
					geometryObjectsScenes[sceneIndex, i] = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity) as GameObject;
				}
				
				if(geometryTypeArray[i] == MarkerRecorder.GeometryType.PLANE){				
					geometryObjectsScenes[sceneIndex, i] = Instantiate(planePrefab, Vector3.zero, Quaternion.identity) as GameObject;
				}
				
				if(geometryTypeArray[i] == MarkerRecorder.GeometryType.CYLINDER){	

					geometryObjectsScenes[sceneIndex, i] = Instantiate(cylinderPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				}
			}
			
			//Make the object a child of the sceneObject.
			if(sceneIndex == worldCenterSceneIndex){
			
				geometryObjectsScenes[sceneIndex, i].transform.parent = markerRecorder.geometryContainer.transform;
			}
			
			else{
			
				geometryObjectsScenes[sceneIndex, i].transform.parent = sceneObjects[sceneIndex].transform;
			}

			//reset the local position and rotation
			geometryObjectsScenes[sceneIndex, i].transform.localPosition = Vector3.zero; 
			geometryObjectsScenes[sceneIndex, i].transform.localRotation = Quaternion.identity; 
		}
	}
	

	//Get the path to the file (including file name) and output just the 
	//file name. If extension is set to true, it the output name will include
	//the extension too.
	public string GetFileNameFromPath(string pathToFile, bool extension){
		
		string filename= "";

		for(int i = pathToFile.Length-1; i >= 0; i--){

			if((pathToFile[i] == '/') || (pathToFile[i] == '\\')){
			
				filename = pathToFile.Substring(i + 1);
				break;
			}
		}
		
		//remove extension?
		if(!extension){
		
			//get the ucs name without the extension			
			filename = RemoveExtension(filename);
		}
		
		return filename;
	}



	private string GetExtension(string pathToFile){
		
		//seperate all strings with a dot
		string[] lines = pathToFile.Split('.');
		
		//get the last line
		return lines[lines.Length-1];
	}
	
	private string RemoveExtension(string pathToFile){
	
		string[] lines = pathToFile.Split('.');	
		return lines[0];
	}
	
	
#if OBJREADER
	private void LoadAllGeometryOBJ(int geometryOBJAmountFile, ref GameObject[,] geometryOBJObjectsScenes, string[] geometryOBJNames, int sceneIndex, int worldCenterSceneIndex, GameObject[] sceneObjects, GameObject objPrefab){
		
		for(int i = 0; i < geometryOBJAmountFile; i++){
			
			//Read the object data
			GameObject[] objObjects = ObjReader.use.ConvertFile(geometryOBJNames[i], false);

			//The object file can contain multiple objects, so get them all.
			if(geometryOBJObjectsScenes[sceneIndex, i] == null){
					
				geometryOBJObjectsScenes[sceneIndex, i] = Instantiate(objPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				geometryOBJObjectsScenes[sceneIndex, i].GetComponent<MeshFilter>().mesh = objObjects[0].GetComponent<MeshFilter>().mesh;// result.objMesh;
				geometryOBJObjectsScenes[sceneIndex, i].GetComponent<MeshFilter>().mesh.RecalculateBounds();

				geometryOBJObjectsScenes[sceneIndex, i].transform.localScale = new Vector3(1, 1, 1); 
				geometryOBJObjectsScenes[sceneIndex, i].transform.localPosition = Vector3.zero;
				geometryOBJObjectsScenes[sceneIndex, i].transform.localRotation = Quaternion.identity;
					
				//This is not redundant. Leave it.
				if(sceneIndex == worldCenterSceneIndex){
					
					geometryOBJObjectsScenes[sceneIndex, i].transform.parent = markerRecorder.geometryContainer.transform;
				}
					
				else{
					
					geometryOBJObjectsScenes[sceneIndex, i].transform.parent = sceneObjects[sceneIndex].transform;
				}
					
				//set the collider 
				geometryOBJObjectsScenes[sceneIndex, i].transform.GetComponent<MeshCollider>().sharedMesh = objObjects[0].GetComponent<MeshFilter>().mesh; //result.objMesh;

				//The size of the object via the line manager is not available yet (it takes at least one frame to init the object),
				//so set the size manually here
				Bounds bounds = objObjects[0].GetComponent<MeshFilter>().mesh.bounds; //result.objMesh.bounds;	
				Vector3 size = new Vector3(bounds.size.x / 2.0f, bounds.size.y / 2.0f, bounds.size.z / 2.0f);
				ObjectLineManager lineManager = geometryOBJObjectsScenes[sceneIndex, i].GetComponent<ObjectLineManager>();
				lineManager.size = size;

				//destroy the temporary object
				Destroy(objObjects[0]);
			}
		}
	}
	
	
	public void LoadOBJObject(ref GameObject[,] geometryOBJObjectsScenes, int geometryOBJIndex, string pathToFile, int worldCenterSceneIndex, GameObject objPrefab, GameObject[] sceneObjects, ref GameObject selectedGeometry){
		
		//Read the object data
		GameObject[] objObjects = ObjReader.use.ConvertFile(pathToFile, false);
		
		if(objObjects != null){

			//The object file can contain multiple objects, so get them all.
			if(geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex] == null){

				int selectedDotAmount = markerRecorder.GetSelectionAmount();
				
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex] = Instantiate(objPrefab, Vector3.zero, Quaternion.identity) as GameObject;
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].GetComponent<MeshFilter>().mesh = objObjects[0].GetComponent<MeshFilter>().mesh; //result.objMesh;
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].GetComponent<MeshFilter>().mesh.RecalculateBounds();

				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].transform.localScale = new Vector3(1, 1, 1); 
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].transform.localPosition = Vector3.zero;
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].transform.localRotation = Quaternion.identity;
				
				//This is not redundant. Leave it.
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].transform.parent = markerRecorder.geometryContainer.transform;
				
				//set the collider 
				geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].transform.GetComponent<MeshCollider>().sharedMesh = objObjects[0].GetComponent<MeshFilter>().mesh; //result.objMesh;

				//The size of the object via the line manager is not available yet (it takes at least one frame to init the object),
				//so set the size manually here
				Bounds bounds = objObjects[0].GetComponent<MeshFilter>().mesh.bounds; //result.objMesh.bounds;	
				Vector3 size = new Vector3(bounds.size.x / 2.0f, bounds.size.y / 2.0f, bounds.size.z / 2.0f);
				ObjectLineManager lineManager = geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex].GetComponent<ObjectLineManager>();
				lineManager.size = size;

				//destroy the temporary object
				Destroy(objObjects[0]);
				
				//Only valid if 3 dots are selected
				if(selectedDotAmount == 3){
					
					//set the selection
					markerRecorder.ResetSelections(MarkerRecorder.Reset.ALL);				
					selectedGeometry = geometryOBJObjectsScenes[worldCenterSceneIndex, geometryOBJIndex];
					markerRecorder.SetSelections(selectedGeometry, null, true);			
					markerRecorder.AttachAxis(selectedGeometry);
				}
			}
		}
	}
	#endif
	
	
	//callback function from file browsing menu
	//NOTE: storing game state to file is done by reading/writing the variables one by one (extracting arrays). 
	//It is not easy to store a class instead as all the custom Unity3d datatypes cannot be easily serialized.
	//Also note that the order in which the items are read should be exactly the same as the order in which
	//the items are saved.
	//WorldCenterScene indicates whether or not the scene is the reference for the world center (in case of merged scenes).
	//If it is not the world center, then some modifications to the tracking logic have to be made.
	public void OpenFile(string pathToFile, int mode, ref bool pathfindingFinished, ref int geometryOBJAmountFile, int geometryAmountMax, ref GameObject[,] geometryOBJObjectsScenes, ref int worldCenterSceneIndex, GameObject objPrefab, GameObject cubePrefab, GameObject planePrefab, GameObject cylinderPrefab, ref GameObject[] sceneObjects, ref string[] geometryOBJNames, float versionFile, ref bool[] mergedScenes, ref int mergedScenesAmountFile, ref string[] mergedScenesNames, ref string worldCenterSceneName, int markerAmount, ref bool[] markersForTracking, ref Vector3[] offsetVectorToRefMarker, ref Quaternion[] offsetRotationToRefMarker, ref int referenceMarkerNumber, ref int geometryAmountFile, ref int[] geometryTypeArray, ref GameObject[,] geometryObjectsScenes, GameObject[] outlineObjects, ref bool[,] markersForTrackingScenes, ref Vector3[,] offsetVectorToRefMarkerScenes, ref Quaternion[,] offsetRotationToRefMarkerScenes, ref int[] referenceMarkerNumberScenes, GameObject worldCenterObject, ref GameObject selectedGeometry, ref float scaleFactor){ //tag = LoadFile openFile loadFile

		//Do not set this. It is used as a temporary variable only.
		float versionInFile = 0.0f;

		//get extension
		string extension = GetExtension(pathToFile);
		
		int sceneIndex = -1;
		string sceneName = "";
		
		//set string format
		NumberFormatInfo n = CultureInfo.InvariantCulture.NumberFormat;
		
		//Load a 3d object in OBJ format.
		if(extension == "obj"){		
			
			if(mode == MarkerRecorder.Mode.GEOMETRY){
			
				//is a scene loaded?
				if(pathfindingFinished){
					
#if OBJREADER
					if(geometryOBJAmountFile < geometryAmountMax){
					
						   
						LoadOBJObject(ref geometryOBJObjectsScenes, geometryOBJAmountFile, pathToFile, worldCenterSceneIndex, objPrefab, sceneObjects, ref selectedGeometry);
						
						//store the name
						geometryOBJNames[geometryOBJAmountFile] = pathToFile;
						
						geometryOBJAmountFile++;
					}
					
					else{
					
						markerRecorder.GuiMessage("Maximum amount of geometry objects reached");				
					}
#endif
				}
			}
			
			//not in geometry mode
			else{
			
				markerRecorder.GuiMessage( "OBJ file can only be loaded in geometry mode.");
	
			}
		}
		
		//load the Unified Coordinate System (ucs) scene file.
		if(extension == "ucs"){
			
			if(mode == MarkerRecorder.Mode.HOME){
				
				if(File.Exists(pathToFile)){

					float x;
					float y;
					float z;
					float w;
		
					StreamReader dataIn = new StreamReader(pathToFile); 
					
					//get the file version
					versionInFile = float.Parse(dataIn.ReadLine(), n);
					
					//If the loaded file version is not the same of that of the program, display a warning and exit.
					if(versionInFile != versionFile){
						
						markerRecorder.GuiMessage("ucs file is incorrect version");
				
						dataIn.Close();	
						return;
					}
					

#if POINTCLOUD && !UNITY_EDITOR
					//Get the pathToFile, modify the extension, and save it as a SLAM map
					string noExtension = RemoveExtension(pathToFile);
					string SLAMMapPath = noExtension + ".slm";

					PointCloudAdapter.pointcloud_load_map(SLAMMapPath);
#endif
					
					//get the scale 
					scaleFactor = float.Parse(dataIn.ReadLine(), n);
					
					bool[] mergedScenesFile = new bool[mergedScenes.Length];
					
					//get the merged scenes from the file
					for(int i = 0; i < sceneObjects.Length; i++){
					
						mergedScenesFile[i] = bool.Parse(dataIn.ReadLine());
						
						if(mergedScenesFile[i] == true){
							 
							mergedScenesAmountFile++;
						}
					}
					
					//If the scene contains merged scenes, we have to call this
					//function externally a few times to load all the separate scenes. Again, this 
					//logic is not ideal but it will do for now.					
					
					//This is the worldCenterSceneIndex saved to file
					sceneIndex = int.Parse(dataIn.ReadLine(), n);	

					//NOTE: mergedScenes and worldCenterSceneIndex are set from gui.cs if scenes 
					//are merged in the scene menu. This is not very object orientated but it is 
					//a bit of a chicken and egg problem. To be changed some time later.
					if(gui.mergedScenesAmount <= 1){

						Array.Copy(mergedScenesFile, mergedScenes, mergedScenes.Length);
						worldCenterSceneIndex = sceneIndex;							
					}
					
					if((gui.mergedScenesAmount <= 1) && (gui.worldCenterScene)){
						
						//Set all the outlines to non-tracking marker
						markerRecorder.outlineType = MarkerRecorder.OutlineType.FORCE_NONTRACKING;
					}
					
					//We end up here if we are loading a saved merged scene and this iteration is not the world
					//center scene.
					//Note: this type of "complex" logic is really asking for trouble. I need it re-do it at some
					//point but for now this will do.
					if((gui.mergedScenesAmount <= 1) && (gui.worldCenterScene) && (mergedScenesAmountFile > 1)){
						
						//get the world center scene name. We cannot use the path to file here because it is the
						//merged scenes save file so we have to extract the world center scene name another way.
						sceneName = sceneObjects[worldCenterSceneIndex].name;
						
						mergedScenesNames[sceneIndex] = sceneName;
					}

					else{
						
						sceneName = GetFileNameFromPath(pathToFile, false);
						mergedScenesNames[sceneIndex] = sceneName;
					}
					
					if(gui.worldCenterScene){
					
						worldCenterSceneName = sceneName;
						
						//TODO: is this redundant?
						worldCenterSceneIndex = sceneIndex;

						//Delete all user created dots.
						markerRecorder.DeleteDots(true);
					}

					AttachScene(sceneIndex, ref sceneObjects, gui.worldCenterScene, worldCenterObject);

					if(sceneIndex != -1){

						for(int i = 0; i < markerAmount; i++){
							markersForTracking[i] = bool.Parse(dataIn.ReadLine());
						}
						
						for(int i = 0; i < markerAmount; i++){
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);
							
							offsetVectorToRefMarker[i] = new Vector3(x,y,z);
						}
						
						for(int i = 0; i < markerAmount; i++){
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);
							w = float.Parse(dataIn.ReadLine(), n);
							
							offsetRotationToRefMarker[i] = new Quaternion(x,y,z,w);
						}
						
						referenceMarkerNumber = int.Parse(dataIn.ReadLine(), n);
						
						//Get the amount of geometry objects which are to be instantiated
						geometryAmountFile = int.Parse(dataIn.ReadLine(), n);
						
						//Get the amount of OBJ objects which are to be instantiated
						geometryOBJAmountFile = int.Parse(dataIn.ReadLine(), n);
						
						//Get all the geometry types in the scene
						for(int i = 0; i < geometryAmountFile; i++){
							geometryTypeArray[i] = int.Parse(dataIn.ReadLine(), n);
						}
						
						//Instantiate all the geometry
						InstantiateAllGeometry(geometryAmountFile, ref geometryObjectsScenes, geometryTypeArray, sceneIndex, cubePrefab, planePrefab, cylinderPrefab, worldCenterSceneIndex, sceneObjects);
						
						//Set the transform of all the geometry objects
						for(int i = 0; i < geometryAmountFile; i++){	
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);		
							geometryObjectsScenes[sceneIndex, i].transform.localPosition = new Vector3(x,y,z);	
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);
							w = float.Parse(dataIn.ReadLine(), n);			
							geometryObjectsScenes[sceneIndex, i].transform.localRotation = new Quaternion(x,y,z,w);
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);		
							geometryObjectsScenes[sceneIndex, i].transform.localScale= new Vector3(x,y,z);	
						}
						
						//Get all the geometry types in the scene
						for(int i = 0; i < geometryOBJAmountFile; i++){
							
							//is already a string
							geometryOBJNames[i] = dataIn.ReadLine();
						}
#if OBJREADER
						//load all the geometry								
						LoadAllGeometryOBJ(geometryOBJAmountFile, ref geometryOBJObjectsScenes, geometryOBJNames, sceneIndex, worldCenterSceneIndex, sceneObjects, objPrefab);
#endif
						//Set the transform of all the geometry objects
						for(int i = 0; i < geometryOBJAmountFile; i++){	
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);			
							geometryOBJObjectsScenes[sceneIndex, i].transform.localPosition = new Vector3(x,y,z);	
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);
							w = float.Parse(dataIn.ReadLine(), n);		
							geometryOBJObjectsScenes[sceneIndex, i].transform.localRotation = new Quaternion(x,y,z,w);
							
							x = float.Parse(dataIn.ReadLine(), n);
							y = float.Parse(dataIn.ReadLine(), n);
							z = float.Parse(dataIn.ReadLine(), n);			
							geometryOBJObjectsScenes[sceneIndex, i].transform.localScale= new Vector3(x,y,z);	
						}
			
						dataIn.Close();

						markerRecorder.HomeButtonPressed();	

						if(gui.worldCenterScene){
						
							referenceMarkerNumberWCS = referenceMarkerNumber;
							geometryAmountFileWCS = geometryAmountFile;
							geometryOBJAmountFileWCS = geometryOBJAmountFile;
							
							Array.Copy(geometryTypeArray, geometryTypeArrayWCS, geometryTypeArray.Length);
							Array.Copy(geometryOBJNames, geometryOBJNamesWCS, geometryOBJNames.Length);
						}						
						
						pathfindingFinished = true;		

						//Set part of the outlines to tracking marker
						markerRecorder.outlineType = MarkerRecorder.OutlineType.MARKER_FOR_TRACKING;

						bool merged = markerRecorder.AreScenesMerged(mergedScenes);
						
						//are none of the scenes merged?
						if(!merged){
						
							markerRecorder.GuiMessage("Scene loaded: " + worldCenterSceneName); 
						}
						
						//Some scenes are merged
						else{
						
							//Create a string of all the merged scenes. Note that this string increases with each iteration.
							string allMergedSceneNames = "";
							foreach(string sceneNameMerged in mergedScenesNames){
							
								if((sceneNameMerged != "") && (sceneNameMerged != null)){
								
									allMergedSceneNames += sceneNameMerged;
									allMergedSceneNames += ", ";
								}
							}

							mergedScenesString = "Merged scenes loaded: " + allMergedSceneNames + "  World Center Scene = " + worldCenterSceneName;
							
							markerRecorder.GuiMessage(mergedScenesString);
						}						
						
						StoreCurrentTrackingDataIntoScenes(sceneIndex, markersForTracking, ref markersForTrackingScenes, offsetVectorToRefMarker, ref offsetVectorToRefMarkerScenes, ref offsetRotationToRefMarkerScenes, offsetRotationToRefMarker, referenceMarkerNumber, ref referenceMarkerNumberScenes);

						markerRecorder.DisableFast(sceneObjects[sceneIndex]);
					}
				}
			}
			
			//not in home mode
			else{
			
				markerRecorder.GuiMessage("UCS scene can only be loaded in home mode.");

			}
		}
	}
	
	
	
	
	//callback function from file browsing menu
	//NOTE: storing game state to file is done by reading/writing the variables one by one (extracting arrays). 
	//It is not easy to store a class as all the custom Unity3d datatypes cannot be easily serialized.
	//Also note that the order in which the items are saved should be exactly the same as the order in which
	//the items are read.
	public void SaveFile(string pathToFile, int mode, ref bool pathfindingFinished, ref int geometryOBJAmountFile, int geometryAmountMax, ref GameObject[,] geometryOBJObjectsScenes, ref int worldCenterSceneIndex, GameObject objPrefab, GameObject cubePrefab, GameObject planePrefab, GameObject cylinderPrefab, ref GameObject[] sceneObjects, ref string[] geometryOBJNames, float versionFile, ref bool[] mergedScenes, ref int mergedScenesAmountFile, ref string[] mergedScenesNames, ref string worldCenterSceneName, int markerAmount, ref bool[] markersForTracking, ref Vector3[] offsetVectorToRefMarker, ref Quaternion[] offsetRotationToRefMarker, ref int referenceMarkerNumber, ref int geometryAmountFile, ref int[] geometryTypeArray, ref GameObject[,] geometryObjectsScenes, GameObject[] outlineObjects, ref bool[,] markersForTrackingScenes, ref Vector3[,] offsetVectorToRefMarkerScenes, ref Quaternion[,] offsetRotationToRefMarkerScenes, ref int[] referenceMarkerNumberScenes, GameObject worldCenterObject, ref GameObject selectedGeometry, ref float scaleFactor){ //tag: closeFile, saveFile, writeFile
		
		int sceneIndex = -1;
		string sceneName = "";
		bool merged = markerRecorder.AreScenesMerged(mergedScenes);	
		
		//set string format
		NumberFormatInfo n = CultureInfo.InvariantCulture.NumberFormat;

#if POINTCLOUD && !UNITY_EDITOR
		//Get the pathToFile, modify the extension, and save it as a SLAM map
		string noExtension = RemoveExtension(pathToFile);
		string SLAMMapPath = noExtension + ".slm";

		PointCloudAdapter.pointcloud_save_current_map(SLAMMapPath);
#endif		
		
		if(!merged){
			
			sceneName = GetFileNameFromPath(pathToFile, false);
			sceneIndex = markerRecorder.GetSceneIndex(sceneName, sceneObjects);
			
			//This is not redundant. Leave it.
			worldCenterSceneIndex = sceneIndex;
			
			AttachScene(sceneIndex, ref sceneObjects, true, worldCenterObject);
			
			//First clear merged scenes array
			Array.Clear(mergedScenes, 0, mergedScenes.Length);
			
			//This is not redundant. Leave it.
			mergedScenes[worldCenterSceneIndex] = true;
	
			StoreCurrentTrackingDataIntoScenes(sceneIndex, markersForTracking, ref markersForTrackingScenes, offsetVectorToRefMarker, ref offsetVectorToRefMarkerScenes, ref offsetRotationToRefMarkerScenes, offsetRotationToRefMarker, referenceMarkerNumber, ref referenceMarkerNumberScenes);	
		}
		
		//Scenes are merged. 
		else{

			//Fetch the data from the world center scene.
			string path = Application.persistentDataPath + "/" + sceneObjects[worldCenterSceneIndex].name + ".ucs";
			gui.worldCenterScene = true;
			
			//Get the amount of merged scenes.
			gui.mergedScenesAmount = 0;
			for(int i = 0; i < mergedScenes.Length; i++){
			
				if(mergedScenes[i]){
				
					gui.mergedScenesAmount++;
				}
			}
	
			OpenFile(path, mode, ref pathfindingFinished, ref geometryOBJAmountFile, geometryAmountMax, ref geometryOBJObjectsScenes, ref worldCenterSceneIndex, objPrefab, cubePrefab, planePrefab, cylinderPrefab, ref sceneObjects, ref geometryOBJNames, versionFile, ref mergedScenes, ref mergedScenesAmountFile, ref mergedScenesNames, ref worldCenterSceneName, markerAmount, ref markersForTracking, ref offsetVectorToRefMarker, ref offsetRotationToRefMarker, ref referenceMarkerNumber, ref geometryAmountFile, ref geometryTypeArray, ref geometryObjectsScenes, outlineObjects, ref markersForTrackingScenes, ref offsetVectorToRefMarkerScenes, ref offsetRotationToRefMarkerScenes, ref referenceMarkerNumberScenes, worldCenterObject, ref selectedGeometry, ref scaleFactor);
		
			//Set the world center scene data so it can be saved into the merged scene file.
			referenceMarkerNumber = referenceMarkerNumberWCS;
			geometryAmountFile = geometryAmountFileWCS;
			geometryOBJAmountFile = geometryOBJAmountFileWCS;
			
			Array.Copy(geometryTypeArrayWCS, geometryTypeArray, geometryTypeArray.Length);
			Array.Copy(geometryOBJNamesWCS, geometryOBJNames, geometryOBJNames.Length);
		}

		//Enable all the geometry lines and use the blue shader instead of the depth mask.
		markerRecorder.EnableAllGeometry(true);
		markerRecorder.EnableAllGeometryLines(true);
		
		//We shouldn't end up here but check anyway.
		if((!merged) && (sceneIndex == -1)){

			markerRecorder.GuiMessage("This scene is not recorded. Will not save.");
		}
		
		//sceneIndex is valid
		else{		
			
			//set the marker outline colors (tracking, non-tracking) for the merged scenes.
			bool[] markersForTrackingBuf = new bool[markerAmount];

			for(int e = 0; e < markerAmount; e++){
			
				markersForTrackingBuf[e] = markersForTrackingScenes[worldCenterSceneIndex, e];
			}	
			
			if(!merged){

				markerRecorder.outlineType = MarkerRecorder.OutlineType.FORCE_NONTRACKING;
			}
			
			//Set part of the outlines to tracking marker
	//		markerRecorder.SetOutlineObjectColors(outlineObjects, markersForTrackingBuf, MarkerRecorder.colorForObject, MarkerRecorder.colorNotForTracking, true);
			markerRecorder.outlineType = MarkerRecorder.OutlineType.MARKER_FOR_TRACKING;

			StreamWriter dataOut; 
			
			try{ 
				//create the file and overwrite if it already exists
				dataOut = new StreamWriter(pathToFile); 
			} 
			catch(Exception){ 
				//cannot create file
				return;
			} 
			
			dataOut.WriteLine(versionFile.ToString(n));
			
			dataOut.WriteLine(scaleFactor.ToString(n));
			
			for(int i = 0; i < mergedScenes.Length; i++){
				dataOut.WriteLine(mergedScenes[i].ToString(n));
			}
			
			dataOut.WriteLine(worldCenterSceneIndex.ToString(n));	
			
			for(int i = 0; i < markerAmount; i++){
				dataOut.WriteLine(markersForTrackingScenes[worldCenterSceneIndex, i].ToString(n));
			}
			
			for(int i = 0; i < markerAmount; i++){
				dataOut.WriteLine(offsetVectorToRefMarkerScenes[worldCenterSceneIndex, i].x.ToString(n));
				dataOut.WriteLine(offsetVectorToRefMarkerScenes[worldCenterSceneIndex, i].y.ToString(n));
				dataOut.WriteLine(offsetVectorToRefMarkerScenes[worldCenterSceneIndex, i].z.ToString(n));
			}
			
			for(int i = 0; i < markerAmount; i++){
				dataOut.WriteLine(offsetRotationToRefMarkerScenes[worldCenterSceneIndex, i].x.ToString(n));
				dataOut.WriteLine(offsetRotationToRefMarkerScenes[worldCenterSceneIndex, i].y.ToString(n));
				dataOut.WriteLine(offsetRotationToRefMarkerScenes[worldCenterSceneIndex, i].z.ToString(n));
				dataOut.WriteLine(offsetRotationToRefMarkerScenes[worldCenterSceneIndex, i].w.ToString(n));
			}
			
			dataOut.WriteLine(referenceMarkerNumber.ToString(n));		
			dataOut.WriteLine(geometryAmountFile.ToString(n));
			dataOut.WriteLine(geometryOBJAmountFile.ToString(n));
			
			for(int i = 0; i < geometryAmountFile; i++){			
				
				dataOut.WriteLine(geometryTypeArray[i].ToString(n));
			}

			for(int i = 0; i < geometryAmountFile; i++){			
					
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.x.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.y.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.z.ToString(n));
				
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.x.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.y.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.z.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.w.ToString(n));
				
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localScale.x.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localScale.y.ToString(n));
				dataOut.WriteLine(geometryObjectsScenes[worldCenterSceneIndex, i].transform.localScale.z.ToString(n));
			}

			for(int i = 0; i < geometryOBJAmountFile; i++){			
				
				//is already a string
				dataOut.WriteLine(geometryOBJNames[i]);
			}

			for(int i = 0; i < geometryOBJAmountFile; i++){			
					
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.x.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.y.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localPosition.z.ToString(n));
				
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.x.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.y.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.z.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localRotation.w.ToString(n));
				
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localScale.x.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localScale.y.ToString(n));
				dataOut.WriteLine(geometryOBJObjectsScenes[worldCenterSceneIndex, i].transform.localScale.z.ToString(n));
			}
			
			dataOut.Close();
			
			if(mode != MarkerRecorder.Mode.GEOMETRY){
			
				markerRecorder.HomeButtonPressed();
			}
			
			else{
			
				gui.menuToShow = Gui.MenuMode.GEOMETRY;
			}
		}
	}
}
