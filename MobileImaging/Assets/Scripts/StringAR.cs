//#define STRING

using UnityEngine;
using System.Collections;
using System;


#if !STRING
public class StringAR : MonoBehaviour { 
}
#endif

#if STRING
public class StringAR : StringCam { 


	private float[] markerSizeFactor;
	private bool trackingFrozen = false;
	private ARWrapper arWrapper;
	private Texture2D videoTexture;
	private bool videoTextureSetup = false;
	private bool tryToInit = false;
	
	//This is currently not being used but it needs to be present as a return variable.
	private Matrix4x4 viewToVideoTextureTransform;

	new void Update () {
		
		if(!trackingFrozen){
			
			base.Update();
			
			if(!videoTextureSetup){
				
				//String bug: the video texture stutters when the camera is in motion. Even if this function
				//is called every frame (the video texture is double buffered with String), it still doesn`t 
				//make a difference
				videoTexture = GetCurrentVideoTexture(out viewToVideoTextureTransform);
			}
			
			//It will take a while before the video texture becomes active, so wait for that
			if((videoTexture != null) && !videoTextureSetup && tryToInit){
			
				Vector2 textureSize = new Vector2(Screen.width, Screen.height);
				arWrapper.SetupVideoBackground(false, textureSize, textureSize,  Quaternion.identity, false, false, true);
				arWrapper.videoScreen.renderer.material.mainTexture = videoTexture;
				
				videoTextureSetup = true;
				arWrapper.ready = true;
			}
		}
	
	}
	
	//This is not used but it needs to be present
	//to avoid a String error
	new void Start(){

	}
	
	
	/*
	void Awake(){
		
		base.Start();	
	}
	 */
	
	public void Init(float[] mkrSizeFactor, int totalMarkerAmount, int maxMarkerAmountInView, Vector2 markerBaseResolution, float markerBaseXsizeMeters){
		
		base.Start();
		
		InitComponents();

		//Load image targets
		for (uint i = 0; i < totalMarkerAmount; i++){
		
			LoadMarkerImage(i + ".png");
		}
		
		markerSizeFactor = mkrSizeFactor;
		
		tryToInit = true;
	}


	public void GetMarkerData(ref int[] markerId, ref GameObject[] markerTransform){

		//reset the index array
		for(int i = 0; i < markerId.Length; i++){

			markerId[i] = -1;
		}

		if(!trackingFrozen){
			
			//get the amount of markers currently being tracked
			uint markerAmountInView = GetDetectedMarkerCount();

			// Handle detected markers
			for (uint i = 0; ((i < markerAmountInView) && (i < arWrapper.maxMarkerAmountInView)); i++)
			{
				// Fetch tracker data for this marker
				StringCam.MarkerInfo markerInfo = GetDetectedMarkerInfo(i);
	
				//update the marker information
				//Note that we need to manually extract the marker transform, unlike with Vuforia
				markerTransform[i].transform.position = markerInfo.position;
				markerTransform[i].transform.rotation = markerInfo.rotation;	

				//The markers are positioned in relation to the camera, so just change the distance to the camera to 
				//change the size of the marker
				markerTransform[i].transform.position *= markerSizeFactor[i];
				
				//have to rotate by 90 degrees for compatibility with Vuforia
				markerTransform[i].transform.Rotate(new Vector3(90.0f, 0, 0));	

				markerId[i] = (int)markerInfo.imageID;	
			}
		}
	}

	public void FreezeTracking(bool freeze){
	
		trackingFrozen = freeze;
	}


	void InitComponents(){

		if(!arWrapper){
			
			//find the ARwrapper script
	        GameObject[] gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
			
	        foreach (GameObject someObject in gameObjects) {
				
	   			arWrapper = someObject.GetComponent<ARWrapper>();
				
				if(arWrapper != null){
				
					break;
				}
			}
		}
	}

}
#endif