#define VUFORIA
//#define USE_ALTERNATE_FREEZE

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

//Vuforia bug: "Don't use for play mode" must be switched
//off, otherwise no markers will be created.

#if !VUFORIA
public class Vuforia : MonoBehaviour {
}
#endif


#if VUFORIA
public class Vuforia : MonoBehaviour, ITrackerEventHandler {

	private bool trackingFrozen = false;
	private Texture2D videoTexture;
	private int markerType = 0;
	
	private MarkerAbstractBehaviour[] markerBehaviours;
	private ImageTargetBehaviour[] imageTargetBehaviours;
 
	private DataSet dataSet;

	private ARWrapper arWrapper;
	
	private bool IsVideoBackgroundInfoAvailable = false;

	[HideInInspector]
	public bool markersReady = false;
	private bool componentsReady = false;
	private bool videoReady = false;
	private bool vuforiaInitReady = false;
	private bool tryToInit = false;
	
	private int frameIndex = 0;
	private int frameIndexBuf = 0;

	private QCARBehaviour qcarBehaviour;

	public void OnInitialized(){

		vuforiaInitReady = true;
	}

	public void Init(int mkrType){

		//simple id markers are not supported
		if(mkrType == (int)ARWrapper.MarkerType.SIMPLE_ID_MARKER){

			mkrType = (int)ARWrapper.MarkerType.FRAME_MARKER;
		}

		markerType = mkrType;
		tryToInit = true;
	}

	void Update(){

		//Vuforia "feature" fix. Vuforia sets the camera clear flags to solid color
		//at some point, so set it back to dept only.
		if(Camera.main.clearFlags != CameraClearFlags.Depth){

			Camera.main.clearFlags = CameraClearFlags.Depth;
		}
		

		if(tryToInit){
			
			InitComponents();

			if(componentsReady){

				if(vuforiaInitReady){

					InitMarkers(ref arWrapper.markerSize, arWrapper.maxMarkerAmountInView, arWrapper.totalMarkerAmount);
					
					//Bugfix: due to a Vuforia bug/lack of a feature, the video background is not ready when OnInitialized is called. fix it here.
					InitVideo();
				}

				if(videoReady && markersReady){

					arWrapper.ready = true;
					tryToInit = false;
				}
			}
		}
	}


	public void OnTrackablesUpdated(){
		
		frameIndex++;
	}
	


	public void InitMarkers(ref Vector2[] markerSize, int maxMarkerAmountInView, int totalMarkerAmount){

		if(!markersReady){

			markerBehaviours = new MarkerBehaviour[totalMarkerAmount];
			imageTargetBehaviours = new ImageTargetBehaviour[totalMarkerAmount];
			
			if(markerType == (int)ARWrapper.MarkerType.FRAME_MARKER){

				CreateFrameMarkers(markerSize, totalMarkerAmount);
			}
			
			if(markerType == (int)ARWrapper.MarkerType.IMAGE_TARGET){

				CreateImageTargets(ref markerSize, totalMarkerAmount);
			}
		}
	}
	

	
	void InitVideo(){

		IsVideoBackgroundInfoAvailable = QCARRenderer.Instance.IsVideoBackgroundInfoAvailable();

		//Setup the geometry and orthographic camera as soon as the video
		//background info is available.
		if(IsVideoBackgroundInfoAvailable && !videoReady && componentsReady){

			//Create a texture. The parameters are set by Vuforia
			videoTexture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
			
	        videoTexture.filterMode = FilterMode.Bilinear;
	        videoTexture.wrapMode = TextureWrapMode.Clamp;
			
			QCARRenderer.Instance.SetVideoBackgroundTexture(videoTexture);

			QCARRenderer.VideoTextureInfo mTextureInfo = QCARRenderer.Instance.GetVideoTextureInfo();
			Vector2 imageSize = new Vector2(mTextureInfo.imageSize.x, mTextureInfo.imageSize.y);
			Vector2 textureSize = new Vector2(mTextureInfo.textureSize.x, mTextureInfo.textureSize.y);

			arWrapper.SetupVideoBackground(true, imageSize, textureSize,  Quaternion.identity, false, false, false);

			arWrapper.videoScreen.renderer.material.mainTexture = videoTexture;

			videoReady = true;
		}
	}

	
	void CreateFrameMarkers(Vector2[] markerSize, int totalMarkerAmount){
		
		if(!markersReady){
			
			//MarkerTracker markerTracker = (MarkerTracker)TrackerManager.Instance.GetTracker(Tracker.Type.MARKER_TRACKER);	
			MarkerTracker markerTracker = TrackerManager.Instance.GetTracker<MarkerTracker>();

			if(markerTracker != null){
			
				for(int i = 0; i < totalMarkerAmount; i++){
				
					MarkerAbstractBehaviour mb = markerTracker.CreateMarker(i, arWrapper.markerName + i, markerSize[i].x * 2.0f);

					//store for later use
					markerBehaviours[i] = mb;
				}

				markersReady = true;
			}			
			
			else{

				TrackerManager.Instance.InitTracker<MarkerTracker>();
			}
		}
	}

	
	void CreateImageTargets(ref Vector2[] markerSize, int totalMarkerAmount){

		if(!markersReady){

			ImageTracker imageTracker = TrackerManager.Instance.GetTracker<ImageTracker>();
			
			if(imageTracker != null){
			
				string dataSetName = "QCAR/MobileImaging.xml";
				DataSet.StorageType storageType = DataSet.StorageType.STORAGE_APPRESOURCE;
				
				if(!DataSet.Exists(dataSetName, storageType)){

					arWrapper.message = "Marker dataset not found. Program can not run.";
				}
				
				dataSet = imageTracker.CreateDataSet();			
				dataSet.Load(dataSetName, storageType);
				imageTracker.ActivateDataSet(dataSet);						
				
				StateManager stateManager = TrackerManager.Instance.GetStateManager();
				
				foreach(TrackableBehaviour tb in stateManager.GetTrackableBehaviours()){

					int index = Math3d.IntParseFast(tb.TrackableName);

					if(index < markerSize.Length){

						tb.gameObject.name = arWrapper.markerName + index;
		
						//get and store the image target size
						ImageTargetBehaviour ib = tb.gameObject.GetComponent<ImageTargetBehaviour>();
						markerSize[index] = ib.GetSize() / 2.0f;

						//store for later use
						imageTargetBehaviours[index] = ib;
					}
				}

				markersReady = true;
			}
			
			else{

				TrackerManager.Instance.InitTracker<ImageTracker>();
			}
		}
	}


	//The marker transform changes if the sofware camera changes. It should only change if the 
	//hardware camera chanes. Fix it here. Alternatively, restting the camera to its identity transform
	//in OnPostRender (must be attached to the camera) will work as well.
	private void FixMarkerTransform(out Vector3 position, out Quaternion rotation, GameObject markerObject){

		// Get the trackable position in World coordinates
		Vector3 tbPos_WordRef = markerObject.transform.position;
		
		// Get the local orientation axis (U,V,W) in World coordinates
		Vector3 tbV_WordRef = markerObject.transform.TransformDirection(Vector3.up);
		Vector3 tbW_WordRef = markerObject.transform.TransformDirection(Vector3.forward);
		
		// Normalize axis to have unit length
		tbV_WordRef.Normalize();
		tbW_WordRef.Normalize();
		
		// Compute position and orientation axis in Camera coordinates
		// i.e. with respect to the camera
		position = Camera.main.transform.InverseTransformPoint(tbPos_WordRef);
		Vector3 tbV_CamRef = Camera.main.transform.InverseTransformDirection(tbV_WordRef);
		Vector3 tbW_CamRef = Camera.main.transform.InverseTransformDirection(tbW_WordRef);
		
		// Normalize axis to have unit length
		tbV_CamRef.Normalize();
		tbW_CamRef.Normalize();

		rotation = Quaternion.LookRotation(tbW_CamRef, tbV_CamRef);
	}



	public void GetMarkerData(Vector2[] markerSize, ref int[] markerId, ref GameObject[] markerTransform, out bool cameraUpdate){

	//	Vector3 position;
	//	Quaternion rotation;
	
		if(frameIndex != frameIndexBuf){
			
			cameraUpdate = true;
		}
		
		else{
		
			cameraUpdate = false;
		}
		
		frameIndexBuf = frameIndex;

		if(!trackingFrozen && markersReady){

			//reset the index array
			int index = 0;
			for(int i = 0; i < markerId.Length; i++){

				markerId[i] = -1;
			}

			if(markerType == (int)ARWrapper.MarkerType.FRAME_MARKER){

				foreach(MarkerBehaviour mb in markerBehaviours){
						
					//Bugfix: due to a Vuforia bug, mb.Marker will be null if camera "don't use for play mode" is used. Fix it here.
					if(mb.Marker != null){

						//is the marker detected?
						if(mb.CurrentStatus == TrackableBehaviour.Status.DETECTED || mb.CurrentStatus == TrackableBehaviour.Status.TRACKED){
							
							if(index < markerId.Length){

								int id = mb.Marker.MarkerID;

								markerTransform[index].transform.position = markerBehaviours[id].gameObject.transform.position;
								markerTransform[index].transform.rotation = markerBehaviours[id].gameObject.transform.rotation;

								/*
								FixMarkerTransform(out position, out rotation, markerTransform[index]);
								markerTransform[index].transform.position = position;
								markerTransform[index].transform.rotation = rotation;
								*/

								markerId[index] = id;
								index++;
							}							
						}
					}					
				}
			}
			

			if(markerType == (int)ARWrapper.MarkerType.IMAGE_TARGET){

				foreach(ImageTargetBehaviour ib in imageTargetBehaviours){	
						
					//if the dataset is not found, ib will be null
					if(ib != null){

						//is the marker detected?
						if(ib.CurrentStatus == TrackableBehaviour.Status.DETECTED || ib.CurrentStatus == TrackableBehaviour.Status.TRACKED){
							
							if(index < markerId.Length){

								//convert the marker string name into an integer
								int id = Math3d.IntParseFast(ib.TrackableName);
							
								markerTransform[index].transform.position = imageTargetBehaviours[id].gameObject.transform.position;
								markerTransform[index].transform.rotation = imageTargetBehaviours[id].gameObject.transform.rotation;

								/*
								FixMarkerTransform(out position, out rotation, markerTransform[index]);
								markerTransform[index].transform.position = position;
								markerTransform[index].transform.rotation = rotation;
								*/

								markerId[index] = id;
								index++;
							}
						}	
					}
				}
			}
		}
	}
	
	

	
	public void FreezeTracking(bool freeze){
	
		if(freeze){

			//freeze the camera image and stop tracking.
#if USE_ALTERNATE_FREEZE
			qcarBehaviour.enabled = false;
#else
			CameraDevice.Instance.Stop();
#endif
		}
		
		else{
		
			//start the camera image and start tracking.
#if USE_ALTERNATE_FREEZE
			qcarBehaviour.enabled = true;
#else
			CameraDevice.Instance.Start();
#endif
		}

		trackingFrozen = freeze;
	}
	



	void InitComponents(){

		if(!componentsReady){

			qcarBehaviour = (QCARBehaviour)FindObjectOfType(typeof(QCARBehaviour));
			arWrapper = gameObject.GetComponent<ARWrapper>();

			if(qcarBehaviour != null){

				//Setup the Vuforia initialized callback function
				qcarBehaviour.RegisterTrackerEventHandler(this);
			
				//If we want access to the video texture, we have to disable native rendering of the video texture 
				QCARRenderer.Instance.DrawVideoBackground = false;	

				//TODO: set the world center mode to CAMERA in code when Vuforia supports this.

				componentsReady = true;
			}

			else{

				arWrapper.message = "Init Error. Make sure the Vuforia Camera Prefab is enabled";
			}
		}
	}
}
#endif

