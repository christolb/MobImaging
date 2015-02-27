//Augmented Reality engine wrapper for Unity.
//No copyright applies.
//Bit Barrel Media
//v1.0

//AR engine to use
//#define POINTCLOUD
//#define STRING
#define VUFORIA
//#define DESKTOPSTATIC
//#define STUDIERSTUBE

/*
Export: 
-Scenes
-StreamingAssets
*/

using UnityEngine;
using System.Collections;
using System.Globalization; //for number format
using System.IO;
using System;

public class ARWrapper : MonoBehaviour {

	public enum MarkerType{

		FRAME_MARKER = 0,
		SIMPLE_ID_MARKER = 1,
		DATA_MATRIX_MARKER = 2,
		IMAGE_TARGET = 3,
		SLAM = 4
	}
	public MarkerType markerType;

	//debug
//	public GameObject debugObject;
	
	//maximum amount of markers which can be in view at the same time
	//Note: if Vuforia is used, the MaxSimultaneousImageTargets variable 
	//on the AR Camera must be set to mach this number. 
	//Note: the AR engine might limit this number
	public int maxMarkerAmountInView = 0;
	
	//the total amount of markers used. 
	public int totalMarkerAmount = 0;
	
	public Material videoScreenMaterial;

	private float[] markerSizeFactor;

	[HideInInspector]
	public GameObject[] markerTransform;
	private GameObject[] markerTransformBuf;
	private GameObject[] markerTransformFil;

	//to store the marker ID to be used for object selection
	private MarkerData[] markerData;

	[HideInInspector]
	public Vector3[] SLAMpoints;


	[HideInInspector]
	public bool markerCompositionInViewChanged;

	private bool trackingFrozen = false;

	//This is the file format version and not the program version
	private float versionFile = 1.0f;

	//Value should be between 0.01f and 0.99f. Smaller value means more damping. //0.7f
	private float lowPassFactor = 0.4f; //0.2f

	[HideInInspector]
	public bool markerInView = false;

	[HideInInspector]
	public int[] markerId;

	private int[] markerIdBuf;
	private int[] markerIdFil;
	private int[] markerIdBufUnmodified;

	[HideInInspector]
	public string markerName = "Marker ";

	private float RAD2DEG = 180.0f / 3.14159265358979323846f;

	//All marker sizes are calculated with reference to this value.
	private Vector2 markerBaseResolution; 

	//Set this variable to the size in meters of the base marker x size. This makes sure the augmentations sizes fit
	//real world objects and accelerations work correctly.
	private float markerBaseXsizeMeters;

	[HideInInspector]
	public Vector2[] markerSize;

	[HideInInspector]
	public Vector2 imageSizeAR;
	[HideInInspector]
	public Vector2 textureSizeAR;

	private GameObject videoTexCam;

	[HideInInspector]
	public GameObject videoScreen;

	[HideInInspector]
	public string message;

	[HideInInspector]
	public bool ready = false;

	private bool flash = false;

	public TextAsset markerSizeFile;

#if STRING			
	private StringAR stringAR;
#endif
	
#if VUFORIA
	private Vuforia vuforia;
	
	QCARBehaviour qcarBehaviour;
#endif


#if POINTCLOUD			
	private PointCloud pointCloud;
#endif

#if DESKTOPSTATIC			
	private DesktopStatic desktopStatic;
#endif

#if STUDIERSTUBE			
	private StbPlugin stbPlugin;
#endif

	
	// Use this for initialization
	void Start () {

		InitComponents();

		//this cannot be empty
		SLAMpoints = new Vector3[1];
	}


	// Update is called once per frame
	void Update () {
		
#if POINTCLOUD

		pointCloud.Init2();
		
		if(pointCloud.init2Ready){
			
			ready = true;
		}
#endif
	}


	public void Init(){
		
		//This has to be set first because some AR engines (Vuforia) will take
		//ownership of the projection matrix and once that is done, modifying
		//anything relating to the projection matrix can have unexpected results.
		Camera.main.nearClipPlane = 0.1f;

		markerTransform = new GameObject[maxMarkerAmountInView];
		markerId = new int[maxMarkerAmountInView];
		markerIdBuf = new int[maxMarkerAmountInView]; 
		markerIdFil = new int[maxMarkerAmountInView]; 
		markerIdBufUnmodified = new int[maxMarkerAmountInView]; 
		markerSize = new Vector2[totalMarkerAmount];	
		markerSizeFactor = new float[totalMarkerAmount];
	
		markerTransformBuf = new GameObject[maxMarkerAmountInView];
		markerTransformFil = new GameObject[maxMarkerAmountInView];

		markerData = new MarkerData[maxMarkerAmountInView];
		
		for(int i = 0; i < maxMarkerAmountInView; i++){
			
			markerTransform[i] = new GameObject("MarkerTransform");

			//add an ID to the marker game object so it can be identified when it is selected in the scene
			markerData[i] = markerTransform[i].AddComponent("MarkerData") as MarkerData;

			markerTransformBuf[i] = new GameObject("Buffer " + markerName);
			markerTransformFil[i] = new GameObject("Filter " + markerName);
			markerIdBuf[i] = -1;
			markerIdFil[i] = -1;
			markerIdBufUnmodified[i] = -1;
		}

		GetMarkerSizeFromFile(ref markerSize);
					
#if POINTCLOUD		
		pointCloud.Init1();
		
		//Note: ready is set from within the pointcloud script as the tracking is not ready as soon 
		//as the function returns
#endif


#if STRING
		stringAR.Init(markerSizeFactor, totalMarkerAmount, maxMarkerAmountInView, markerBaseResolution, markerBaseXsizeMeters);
#endif

#if STUDIERSTUBE

		bool initOK = false;
		bool flip_v = false;

		stbPlugin.Init(ref initOK, ref flip_v, markerSizeFactor, totalMarkerAmount, maxMarkerAmountInView, (int)markerType, markerBaseResolution, markerBaseXsizeMeters);

		if(initOK){

			Vector2 textureSize = new Vector2(stbPlugin.videoTexture.width, stbPlugin.videoTexture.height);

			SetupVideoBackground(true, textureSize, textureSize, Quaternion.identity, false, !flip_v, true);

			if(stbPlugin.videoTexture != null){

				videoScreen.renderer.material.mainTexture = stbPlugin.videoTexture;
			}

			ready = true;
		}

		//do not overwrite existing messages
		if((stbPlugin.message != "") && (message == "")){

			message = stbPlugin.message;
		}
#endif


#if VUFORIA
		vuforia.Init((int)markerType);

		//Note: ready is set from within the vuforia script because the tracking is not ready right away.
#endif	
		
#if DESKTOPSTATIC

		//TODO: set the marker transform here
		Quaternion rotation = new Quaternion(0.376522f, -0.4590046f, 0.5079004f, -0.624166f);
		Vector3 position = new Vector3(-0.1822489f, 0.05792517f, 0.5775005f);

		desktopStatic.Init(true, rotation, position, Camera.main.projectionMatrix);		

		//Note: ready is set from within the desktopstatic script as the tracking is not ready as soon 
		//as the function returns
#endif
	
		
	}



	public void GetTrackingData(bool usePoseFilter, bool recording, float scaleFactor){

		bool cameraUpdate = false;

		if(ready){

			bool useCompareFilter;
			bool useFrustumFilter;
			bool useLowPassilter;

#if (POINTCLOUD && UNITY_EDITOR) || DESKTOPSTATIC
			usePoseFilter = false;
#endif
			
			if(usePoseFilter){

				if(recording){

#if STUDIERSTUBE
					useCompareFilter = false;
					useFrustumFilter = false;
					useLowPassilter = false;
#else
					useCompareFilter = true;
					useFrustumFilter = true;
					useLowPassilter = false;
#endif
				}

				else{

					useCompareFilter = false;
					useFrustumFilter = false;
					useLowPassilter = true;
				}
			}

			else{

				if(recording){

#if STUDIERSTUBE
					useCompareFilter = false;
					useFrustumFilter = false;
					useLowPassilter = false;
#else
					useCompareFilter = true;
					useFrustumFilter = true;
					useLowPassilter = false;
#endif
				}

				else{

					useCompareFilter = false;
					useFrustumFilter = false;
					useLowPassilter = false;
				}
			}

	#if VUFORIA 	
		vuforia.GetMarkerData(markerSize, ref markerId, ref markerTransform, out cameraUpdate);
	#endif
		
	#if STRING	
			stringAR.GetMarkerData(ref markerId, ref markerTransform);
	#endif

	#if POINTCLOUD		
			pointCloud.GetSLAMData(recording, ref SLAMpoints, scaleFactor, ref markerId, ref markerTransform);		
	#endif

	#if DESKTOPSTATIC		
			desktopStatic.GetMarkerData(ref markerTransform);
	#endif
    
  	#if STUDIERSTUBE		
			stbPlugin.GetMarkerData(ref markerId, ref markerTransform, out cameraUpdate);

			if((stbPlugin.message != "") && (message == "")){

				message = stbPlugin.message;
			}
	#endif

			if(!Math3d.CompareIntArray(markerId, markerIdBuf)){

				markerCompositionInViewChanged = true;
			}

			else{

				markerCompositionInViewChanged = false;
			}

			if(usePoseFilter){

				PoseFilter(useCompareFilter, useFrustumFilter, useLowPassilter, trackingFrozen, markerSize, ref markerId, ref markerTransform, cameraUpdate);
			}

			markerInView = false;
			for(int i = 0; i < maxMarkerAmountInView; i++){

				//is the marker tracking?
				if(markerId[i] != -1){

					//add an ID to the marker game object so it can be identified when it is selected in the scene
					markerData[i].id = markerId[i];
					markerInView = true;
				}

				else{

					//add an ID to the marker game object so it can be identified when it is selected in the scene
					markerData[i].id = -1;
				}
			}

			//if tracking is frozen, the previous value must be fetched because the current value might not be valid
			if(trackingFrozen){

				for(int i = 0; i < maxMarkerAmountInView; i++){

					if(markerId[i] != -1){

						markerTransform[i].transform.position = markerTransformBuf[i].transform.position;
						markerTransform[i].transform.rotation = markerTransformBuf[i].transform.rotation;	
					}
				}
			}

				
			else{

				for(int i = 0; i < maxMarkerAmountInView; i++){

					if(markerId[i] != -1){

						markerTransformBuf[i].transform.position = markerTransform[i].transform.position;
						markerTransformBuf[i].transform.rotation = markerTransform[i].transform.rotation;
					}
				}
			}
			
			
			Array.Copy(markerId, markerIdBuf, markerId.Length);
		}
	}




	public void SetupVideoBackground(bool useLiveCam, Vector2 imageSize, Vector2 textureSize, Quaternion videoScreenRotation, bool mirrorX, bool mirrorY, bool stretchToFit){

		imageSizeAR = imageSize;
		textureSizeAR = textureSize;

		//set the camera parameters
		Vector3 cameraPosition = new Vector3(0,1,0);
		Quaternion cameraRotation = Quaternion.Euler(new Vector3(90, 0, 0));
		int videoScreenLayer = 9;
		Camera.main.cullingMask = 1 << 0 | 1 << 1 | 1 << 2 | 1 << 4;;
		Camera.main.depth = 1;
		Camera.main.transform.position = Vector3.zero;
		Camera.main.transform.rotation = Quaternion.identity;
		
		
		Camera.main.clearFlags = CameraClearFlags.Depth;

		//create the camera
		videoScreen = new GameObject("videoScreen");

		// Create the video mesh
		MeshFilter meshFilter = videoScreen.AddComponent<MeshFilter>();
		meshFilter.mesh = CreateVideoMesh(2, 2, imageSize, textureSize, mirrorX, mirrorY);

		//set the video screen properties
		videoScreen.AddComponent<MeshRenderer>();
	//	videoScreen.renderer.material.shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
		videoScreen.renderer.material = videoScreenMaterial;
		videoScreen.transform.position = Vector3.zero;
		videoScreen.transform.rotation = videoScreenRotation;
		videoScreen.layer = videoScreenLayer;

		//create a camera which displays the live camera feed
		if(useLiveCam){

			videoTexCam = new GameObject("videoTexCam");
			videoTexCam.AddComponent<Camera>();
			videoTexCam.camera.cullingMask =  1 << videoScreenLayer;
			videoTexCam.camera.depth = -2;

			videoTexCam.transform.position = cameraPosition;
			videoTexCam.transform.rotation = cameraRotation;

			videoTexCam.camera.nearClipPlane = 0.5f;
			videoTexCam.camera.farClipPlane = 2.0f;

			//clean up the Hierarchy editor window
			videoTexCam.transform.parent = videoScreen.transform;
		}

		// Position the video mesh
		PositionVideoMeshAndCamera(useLiveCam, videoScreen, videoTexCam, imageSize, stretchToFit);
	}



	public void StartRecording(bool update){

#if POINTCLOUD && !UNITY_EDITOR
		
		if(!update){

			pointCloud.Restart();

			PointCloudAdapter.pointcloud_start_slam();
		}
		

		PointCloudAdapter.pointcloud_enable_map_expansion();
#endif
		
	}


	public void Focus(){

#if VUFORIA
		CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_TRIGGERAUTO);
#endif
	}


	public void StopRecording(){

#if POINTCLOUD && !UNITY_EDITOR

		if(pointCloud.State != pointcloud_state.POINTCLOUD_NOT_CREATED){
			
			PointCloudAdapter.pointcloud_disable_map_expansion();
		}
#endif
	}

	public void Reset(){

#if POINTCLOUD
		pointCloud.Restart();
#endif
	}


	public void Freeze (bool freeze){

#if VUFORIA
			vuforia.FreezeTracking(freeze);
#endif

#if STRING
			stringAR.FreezeTracking(freeze);
#endif

#if POINTCLOUD
			pointCloud.FreezeTracking(freeze);
#endif

#if STUDIERSTUBE
			stbPlugin.FreezeTracking(freeze);
#endif

#if DESKTOPSTATIC
			desktopStatic.FreezeTracking(freeze);
#endif

		trackingFrozen = freeze;
	}


	public void ToggleFlash(){
	
		if(flash){
		
#if VUFORIA
			CameraDevice.Instance.SetFlashTorchMode(false);
#endif
			flash = false;			
		}
		
		else{
		
#if VUFORIA
			CameraDevice.Instance.SetFlashTorchMode(true);
#endif
			flash = true;
		}
	}


	// Scale and position the video mesh to fill the screen
	private void PositionVideoMeshAndCamera(bool useLiveCam, GameObject videoScreen, GameObject videoTexCam, Vector2 imageSize, bool stretchToFit)
	{
		// Reset the rotation so the mesh faces the camera
		videoScreen.transform.position = Vector3.zero;
		videoScreen.transform.rotation = Quaternion.identity;

		// Scale game object for full screen video image
		if(!stretchToFit){
			
			videoScreen.transform.localScale = new Vector3(1, 1, (float)imageSize.y / (float)imageSize.x);
		}

		else{

			videoScreen.transform.localScale = new Vector3(1, 1, (float) Screen.height / (float) Screen.width);
		}

		// Visible portion of the image:
		float screenAspect;
		
		if(ShouldFitWidth(imageSize)){
			
			screenAspect = (float)Screen.height / (float)Screen.width;
		}

		else{

			screenAspect = (float)imageSize.y / (float)imageSize.x;
		}

		if(useLiveCam){

			videoTexCam.camera.orthographic = true;
			videoTexCam.camera.orthographicSize = screenAspect;
		}
	}


	private bool ShouldFitWidth(Vector2 imageSize){

		float screenAspect = (float)Screen.width / (float)Screen.height;
		float cameraAspect = (float)imageSize.x / (float)imageSize.y;
		float difference = screenAspect - cameraAspect;

		//Account for rounding errors
		//difference is (close to) 0 or positive
		if(difference > -0.02f){

			return true;
		}

		else{

			return false;
		}
	}


	// Create a video mesh with the given number of rows and columns
	// Minimum two rows and two columns
	private Mesh CreateVideoMesh(int rows, int clomns, Vector2 imageSize, Vector2 textureSize, bool mirrorX, bool mirrorY)
	{
		Mesh mesh = new Mesh();

		// Build mesh:
		mesh.vertices = new Vector3[rows * clomns];
		Vector3[] vertices = mesh.vertices;

		for (int r = 0; r < rows; ++r)
		{
			for (int c = 0; c < clomns; ++c)
			{
				float x = (((float)c) / (float)(clomns - 1)) - 0.5f;
				float z = (1.0F - ((float)r) / (float)(rows - 1)) - 0.5f;

				vertices[r * clomns + c].x = x * 2.0f;
				vertices[r * clomns + c].y = 0.0f;
				vertices[r * clomns + c].z = z * 2.0f;
			}
		}
		mesh.vertices = vertices;

		// Builds triangles:
		mesh.triangles = new int[rows * clomns * 2 * 3];
		int triangleIndex = 0;

		// Setup UVs to match texture info:
		float scaleFactorX = (float)imageSize.x / (float)textureSize.x;
		float scaleFactorY = (float)imageSize.y / (float)textureSize.y;

		mesh.uv = new Vector2[rows * clomns];

		int[] triangles = mesh.triangles;
		Vector2[] uvs = mesh.uv;


		for (int r = 0; r < rows - 1; ++r)
		{
			for (int c = 0; c < clomns - 1; ++c)
			{
				int point0Index = r * clomns + c;
				int point1Index = r * clomns + c + clomns + 1;
				int point2Index = r * clomns + c + clomns;
				int point3Index = r * clomns + c + 1;

				triangles[triangleIndex++] = point0Index;
				triangles[triangleIndex++] = point1Index;
				triangles[triangleIndex++] = point2Index;

				triangles[triangleIndex++] = point1Index;
				triangles[triangleIndex++] = point0Index;
				triangles[triangleIndex++] = point3Index;

				uvs[point0Index] = new Vector2(((float)c) / ((float)(clomns - 1)) * scaleFactorX, ((float)r) / ((float)(rows - 1)) * scaleFactorY);
				uvs[point1Index] = new Vector2(((float)(c + 1)) / ((float)(clomns - 1)) * scaleFactorX, ((float)(r + 1)) / ((float)(rows - 1)) * scaleFactorY);
				uvs[point2Index] = new Vector2(((float)c) / ((float)(clomns - 1)) * scaleFactorX, ((float)(r + 1)) / ((float)(rows - 1)) * scaleFactorY);
				uvs[point3Index] = new Vector2(((float)(c + 1)) / ((float)(clomns - 1)) * scaleFactorX, ((float)r) / ((float)(rows - 1)) * scaleFactorY);

#if VUFORIA
				// mirror UV coordinates if necessary
				if (qcarBehaviour.VideoBackGroundMirrored){

					mirrorX = true;
				}
#endif
				if(mirrorX){

					uvs[point0Index] = new Vector2(scaleFactorX - uvs[point0Index].x, uvs[point0Index].y);
					uvs[point1Index] = new Vector2(scaleFactorX - uvs[point1Index].x, uvs[point1Index].y);
					uvs[point2Index] = new Vector2(scaleFactorX - uvs[point2Index].x, uvs[point2Index].y);
					uvs[point3Index] = new Vector2(scaleFactorX - uvs[point3Index].x, uvs[point3Index].y);
				}

				if(mirrorY){

					uvs[point0Index] = new Vector2(uvs[point0Index].x, scaleFactorY - uvs[point0Index].y);
					uvs[point1Index] = new Vector2(uvs[point1Index].x, scaleFactorY - uvs[point1Index].y);
					uvs[point2Index] = new Vector2(uvs[point2Index].x, scaleFactorY - uvs[point2Index].y);
					uvs[point3Index] = new Vector2(uvs[point3Index].x, scaleFactorY - uvs[point3Index].y);
				}
			}
		}

		mesh.triangles = triangles;
		mesh.uv = uvs;

		mesh.normals = new Vector3[mesh.vertices.Length];
		mesh.RecalculateNormals();

		return mesh;
	}



	public void CalculateShaderUVMapping(out float ScaleFacYa, out float ScaleFacYb, out float ScaleFacXa, out float ScaleFacXb, Vector2 textureSize, Vector2 imageSize, float screenWidth, float screenHeight){

		float textureAspect = textureSize.y / textureSize.x;
		float screenAspect = screenHeight / screenWidth;
		float scaleFactorXtop = imageSize.x / textureSize.x;
		
		//The scaleFactorY variable depends on the X screen size in relation to the X camera image size.
		//Basically, we want to know how much smaller the Y size is going to be, since the X side of the
		//camera and the video texture will always be scaled so they fit together. If the screen is wider
		//then the camera image, part of the top and bottom video feed will be cut off.
		float scaleFactorYOriginal = imageSize.y / textureSize.y;
		float clippedHeightFac = (scaleFactorXtop * screenAspect) / textureAspect;
		float scaleFactorYbottom = (scaleFactorYOriginal - clippedHeightFac) / 2.0f;
		float scaleFactorYtop = scaleFactorYbottom + clippedHeightFac;
		
		ScaleFacYa = (scaleFactorYtop - scaleFactorYbottom) / 2.0f;
		ScaleFacYb = ((scaleFactorYtop - scaleFactorYbottom) / 2.0f) + scaleFactorYbottom;
		ScaleFacXa = scaleFactorXtop / 2.0f;
		ScaleFacXb = ScaleFacXa;
	}	
	
	
	public void SetShaderUVMapping(GameObject shaderObject, float ScaleFacYa, float ScaleFacYb, float ScaleFacXa, float ScaleFacXb){

		if(shaderObject){

			shaderObject.renderer.material.SetFloat("_ScaleFacXa", ScaleFacXa);
			shaderObject.renderer.material.SetFloat("_ScaleFacYa", ScaleFacYa);
			shaderObject.renderer.material.SetFloat("_ScaleFacXb", ScaleFacXb);
			shaderObject.renderer.material.SetFloat("_ScaleFacYb", ScaleFacYb);
		}
	}
	


	void PoseFilter(bool useCompareFilter, bool useFrustumFilter, bool useLowPassilter, bool freeze, Vector2[] markerSize, ref int[] markerId, ref GameObject[] markerTransform, bool cameraUpdate){

		//Pose filter
		if(!freeze){			

			int indexNumber = -1;
			int indexEmptySlot = -1;
	
			for(int i = 0; i < maxMarkerAmountInView; i++){	
				
				if(markerId[i] != -1){

					//Previous compared to current pose filtering.
					//Note: Only use this during recording as it might cause marker and content flickering.
					//Note: this is a "feature" fix for Vuforia. It is the build in marker de-recognition delay
					//which needs to be made accessible from the API.
					if(useCompareFilter && cameraUpdate){

						Math3d.GetNumberIndex(out indexNumber, out indexEmptySlot, markerIdBufUnmodified, markerId[i]);

						//Is the current pose different or the same as the previous one?
						bool poseDifferent = false;
						if(indexNumber != -1){

							poseDifferent = ComparePoseWithPrevious(markerTransform[i], markerTransformBuf[indexNumber]);
						}

						else{

							poseDifferent = true;
						}

						Array.Copy(markerId, markerIdBufUnmodified, markerId.Length);
					//	markerTransformBuf[i].transform.position = markerTransform[i].transform.position;
					//	markerTransformBuf[i].transform.rotation = markerTransform[i].transform.rotation;
								
						if(!poseDifferent){

							//pretend marker marker not tracked
							markerId[i] = -1;
						}
					}
					
					//Frustum filter
					//This part of the Vuforia "feature" fix of the problem described above.
					//If the marker is not fully in the view frustum then set the marker as lost.
					//This only makes sense for Frame Markers since Image Targets track fine when partially occluded.
					if(useFrustumFilter){

						if((markerType == (int)ARWrapper.MarkerType.FRAME_MARKER) && (markerId[i] != -1)){
									
							if(!IsMarkerInFrustum(markerSize[i], markerTransform[i])){
										
								//pretend marker marker not tracked
								markerId[i] = -1;
							}	
						}
					}
					
					//Low Pass filter for rotational movements
					if(useLowPassilter){

						//Find the marker in the low pass filtered array
						//Note: indexNumber will be -1 if the marker is found for the first time after it was lost
						Math3d.GetNumberIndex(out indexNumber, out indexEmptySlot, markerIdFil, markerId[i]);

						//If a marker is mis-identified it can happen that there is no empty slot available
						//so check for this and ignore the current frame.
						if (!((indexNumber == -1) && (indexEmptySlot == -1))){

							if(indexNumber == -1){

								//The marker is not already present in the low pass filtered array, so make an entry
								markerIdFil[indexEmptySlot] = markerId[i];
								indexNumber = indexEmptySlot;

								//initialize the rotation for first usage, it cannot be zero.
								markerTransformFil[indexNumber].transform.rotation = markerTransform[i].transform.rotation;
							}

							//calculate the low pass filtered rotation
							markerTransformFil[indexNumber].transform.rotation = LowPassFilterQuaternion(markerTransformFil[indexNumber].transform.rotation, markerTransform[i].transform.rotation, lowPassFactor);
						
							//set the output
							markerTransform[i].transform.rotation = markerTransformFil[indexNumber].transform.rotation;
						}
					}
				}
			}

			
			for(int i = 0; i < maxMarkerAmountInView; i++){

				//Remove all the filtered entries of markers which are not in view anymore
				//Does the marker exist in the filtered array?
				if(markerIdFil[i] != -1){

					//is the filtered marker still tracking? If not, indexNumber will be -1
					Math3d.GetNumberIndex(out indexNumber, out indexEmptySlot, markerId, markerIdFil[i]);

					if(indexNumber == -1){

						markerIdFil[i] = -1;
						markerTransformFil[i].transform.rotation = Quaternion.identity;
					}
				}			
			}
		}
	}


	
	//Quaternion Low pass filter. Requires lastValue to be stored externally.
	//factor should be between 0.01 and 0.99 
	Quaternion LowPassFilterQuaternion(Quaternion intermediateValue, Quaternion targetValue, float factor){

		intermediateValue = Quaternion.Slerp(intermediateValue, targetValue, factor);
		
		return intermediateValue;
	}


	
	//Figures out whether the present marker pose makes sense compared to the previous frame pose. If the pose is exactly
	//the same or if the difference is too great, the pose most likely is invalid.
	//Returns true if the current pose is ok, false if it is invalid.
	bool ComparePoseWithPrevious(GameObject presentObject, GameObject previousObject){
		
		//pose is the same
		if((presentObject.transform.position == previousObject.transform.position) && (presentObject.transform.rotation == previousObject.transform.rotation)){
			
			return false;
		}
		
		//pose is different
		return true;
	}


	//Figure out whether one of the marker corners is outside the camera view frustum or not.
	bool IsMarkerInFrustum(Vector2 markerSize, GameObject markerObject){
		
		Vector3[] points = new Vector3[4];
		
		//Get the camera view frustum planes
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
		
		//Calculate the location of the marker dots. Note that with Vuforia, the marker object will have a scale
		//but this should not be taken into account when calculating the edge positions, hence TransformDirection
		//is used instead of TransformPoint.		
		Vector3 translationVector = new Vector3(markerSize.x, 0, markerSize.y);
		points[0] = markerObject.transform.position + markerObject.transform.TransformDirection(translationVector);
		
		translationVector = new Vector3(-markerSize.x, 0, markerSize.y);
		points[1] = markerObject.transform.position + markerObject.transform.TransformDirection(translationVector);
		
		translationVector = new Vector3(markerSize.x, 0, -markerSize.y);
		points[2] = markerObject.transform.position + markerObject.transform.TransformDirection(translationVector);
		
		translationVector = new Vector3(-markerSize.x, 0, -markerSize.y);
		points[3] = markerObject.transform.position + markerObject.transform.TransformDirection(translationVector);
		
		//loop through all points
		for(int i = 0; i < points.Length; i++){
			
			//loop through all frustum planes
			for(int e = 0; e < planes.Length; e++){
				
				//Only plane indices 0,1,2, and 3 are the side planes. The front and back plane are not important.
				if((e == 0) || (e == 1)|| (e == 2)|| (e == 3)){
				
					//is the point outside the frustum?
					if(planes[e].GetSide(points[i]) == false){;
						
						return false;
					}
				}
			}
		}
		
		return true;
	}
	
	//Bugfix: due to a bug/lack of feature in Unity, the field of view from the Editor
	//or from Camera.main.fieldOfView will be wrong if the camera projection matrix is modified
	//directly. This function will get the correct vertical field of view directly from
	//the camera projection matrix
	public float GetFieldOfView(){

		Matrix4x4 mat = Camera.main.projectionMatrix;
		return RAD2DEG * (2.0f * (float)Math.Atan(1.0f / mat[5]));
	}


	//If the marker size is based on the diagonal instead of the x axis, use this function
	void ResolutionToMarkerSize(ref Vector2 markerSize, ref float markerSizeFactor, Vector2 markerBaseResolution, Vector2 markerResolution, float markerBaseXsizeMeters){
		
		//TODO: only calculate base diagonal once
		float baseDiagonal = Mathf.Sqrt((markerBaseResolution.x * markerBaseResolution.x) + (markerBaseResolution.y * markerBaseResolution.y));
		float markerDiagonal = Mathf.Sqrt((markerResolution.x * markerResolution.x) + (markerResolution.y * markerResolution.y));
		float diagonalFacBaseCorrected = markerDiagonal / baseDiagonal;
		
		//get the marker size with no reference to the base resolution
		float markerSizeUncorrectedX = (markerResolution.x / markerDiagonal);
		float markerSizeUncorrectedY = (markerResolution.y / markerDiagonal);
		
		//Make augmentations fit the x size instead of the diagonal with String. In order to do this, we have to place
		//the marker detected position further away from the camera.
		markerSizeFactor = diagonalFacBaseCorrected / markerSizeUncorrectedX;
		
		//Correct for a user defined unit size. Normally the x size of the base marker will be 1, which is one meter. The
		//graphics can be scaled accordingly, but the physics effects such as acceleration depend on size, so the size in
		//real world units has to be correct. This modification allows for that.
		markerSizeFactor *= markerBaseXsizeMeters;
		
		//calculate the marker size
		markerSize.x = markerSizeFactor * markerSizeUncorrectedX;
		markerSize.y = markerSizeFactor * markerSizeUncorrectedY;

		//we need half the size
		markerSize /= 2.0f;
	}


	private void GetMarkerSizeFromFile(ref Vector2[] markerSize){

		bool markerSizeFileValid = false;
		
		if(markerSizeFile != null){

			if(markerSizeFile.text != ""){

				markerSizeFileValid = true;
			}
		}
		
		if(!markerSizeFileValid){

			message = "markerSize file not loaded";

			for(int i = 0; i < markerSize.Length; i++){

				markerSize[i] = Vector2.one;
			}

			return;
		}
		
		
		//set string format
		NumberFormatInfo n = CultureInfo.InvariantCulture.NumberFormat;

		string[] lines = markerSizeFile.text.Split('\n');

		//Loop through all the lines		
		for(int stringIndex = 0; stringIndex < lines.Length; stringIndex++){

			string line = lines[stringIndex];

			//get the type of line
			//Comment: double slashes (//), or semicolon (;)
			//Variable designator: ([)
			bool isComment = false;
			for(int i = 0; i < line.Length - 1; i++){

				if(((line[i] == '/') && (line[i+1] == '/')) || (line[i] == ';')){

					isComment = true;
					break;
				}
			}

			if(!isComment){

				string[] lines1 = line.Split('=');
				string variableName1 = lines1[0];

				string numberString1 = "";
				string numberString2 = "";

				if(lines1.Length >= 2){

					numberString1 = lines1[1];
				}

				string[] lines2 = line.Split('.');
				string variableName2 = lines2[0];

				if(lines2.Length >= 2){

					numberString2 = lines2[1];
				}

				//get the file version
				if(variableName1 == "fileVersion"){

					float versionInFile = float.Parse(numberString1, n);

					//If the loaded file version is not the same of that of the program, display a warning and exit.
					if(versionInFile != versionFile){
						
						message = "Marker size file is incorrect version";
						return;
					}
				}

				//find the correct marker type
				if(variableName1 == "markerType"){

					int markerTypeFile = int.Parse(numberString1, n);

					if((int)markerType == markerTypeFile){

						//The correct marker type has been found, so now loop through
						//the variable entries until we find another marker type entry
						//and then bail out.
						for(stringIndex++; stringIndex < lines.Length; stringIndex++){

							line = lines[stringIndex];

							lines1 = line.Split('=');
							variableName1 = lines1[0];

							if(lines1.Length >= 2){

								numberString1 = lines1[1];
							}

							lines2 = line.Split('.');
							variableName2 = lines2[0];

							if(lines2.Length >= 2){

								numberString2 = lines2[1];
							}

							//another marker type is found, so bail out
							if(variableName1 == "markerType"){

								break;
							}

							if(variableName1 == "markerBaseResolution"){

								lines1 = numberString1.Split('x');
								markerBaseResolution.x = float.Parse(lines1[0], n);
								markerBaseResolution.y = float.Parse(lines1[1], n);
							}

							if(variableName1 == "markerBaseXsizeMeters"){

								markerBaseXsizeMeters = float.Parse(numberString1, n);
									
								float aspectRatio = markerBaseResolution.y / markerBaseResolution.x;
									
								Vector2 size = new Vector2();
								size.x = markerBaseXsizeMeters * 0.5f;
								size.y = (markerBaseXsizeMeters * aspectRatio) * 0.5f;	
								
								//Now all required marker size information is available.
								//First fill the entire marker size array with the default value
								//and later some individual marker sizes will be overwritten
								for(int i = 0; i < markerSize.Length; i++){

#if !STRING
									//get the size
									markerSize[i] = size;
									markerSizeFactor[i] = markerBaseXsizeMeters;
#else
											
									//The marker size with String is based on the diagonal instead of the x side, so use different logic here
									ResolutionToMarkerSize(ref markerSize[i], ref markerSizeFactor[i], markerBaseResolution, markerBaseResolution, markerBaseXsizeMeters);
#endif
								}
							}

							//An individual marker size is found, so overwrite the default value
							if(variableName2 == "marker"){
								 
								//get the marker id
								lines1 = numberString2.Split('=');
								int markerId = int.Parse(lines1[0], n);
									
								//get the size
								lines1 = numberString1.Split('x');
									
								Vector2 size = new Vector2();

#if !STRING
								size.x = float.Parse(lines1[0], n) * 0.5f;
								size.y = float.Parse(lines1[1], n) * 0.5f;
									
								markerSize[markerId] = size;
								markerSizeFactor[markerId] = markerSize[markerId].x * 2.0f;								
#else
								size.x = float.Parse(lines1[0], n);
								size.y = float.Parse(lines1[1], n);
									
								//convert meters to resolution
								size.x = size.x * (markerBaseResolution.x / markerBaseXsizeMeters);
								size.y = size.y * (markerBaseResolution.y / markerBaseXsizeMeters);;
										
								//The marker size with String is based on the diagonal instead of the x side, so use different logic here
								ResolutionToMarkerSize(ref markerSize[markerId], ref markerSizeFactor[markerId], markerBaseResolution, size, markerBaseXsizeMeters);
#endif
							}
						}
					}
				}
			}
		}
		
	}




	void InitComponents(){	

#if VUFORIA
		vuforia = gameObject.AddComponent("Vuforia") as Vuforia;
		
		qcarBehaviour = (QCARBehaviour)FindObjectOfType(typeof(QCARBehaviour));
#endif

#if STUDIERSTUBE
		stbPlugin = gameObject.AddComponent("StbPlugin") as StbPlugin;
#endif
		
#if POINTCLOUD
		pointCloud = gameObject.AddComponent("PointCloud") as PointCloud;
#endif

#if STRING
		stringAR = Camera.main.gameObject.AddComponent("StringAR") as StringAR;
#endif

#if DESKTOPSTATIC
		desktopStatic = gameObject.AddComponent("DesktopStatic") as DesktopStatic;
#endif

	}
}
