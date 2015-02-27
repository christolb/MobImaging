//#define DESKTOPSTATIC

using UnityEngine;
using System.Collections;


#if DESKTOPSTATIC
public class DesktopStatic : MonoBehaviour {

	private Vector2 webcamResolution = new Vector2(640, 480);
	private ARWrapper arWrapper;
	private WebCamTexture webcamTexture;
	private GameObject plane;
	private bool trackingFrozen = false;

	private Quaternion markerRotation = Quaternion.identity;
	private Vector3 markerPosition = Vector3.zero;

	// Update is called once per frame
	void Update () {
	
		//Bugfix: due to a bug in Unity, the webcam texture size is reported incorrectly initially
		//but it will be correct after some time.
		if((webcamTexture.width > 16) && !arWrapper.ready){

			CreateTexture();
			arWrapper.ready = true;
		}
	}

	public void Init(bool useMarkerPose, Quaternion rotation, Vector3 position, Matrix4x4 projectionMatrix){

		InitComponents();

		if(useMarkerPose){

			//set the transform of the marker
			markerRotation = rotation;
			markerPosition = position;
		}
		
		else{

			//if the transform of the marker is not known, set the transform 
			//of the camera instead. 
			Quaternion cameraRotation = rotation;
			Vector3 cameraPosition = position;

			Math3d.TransformWithParent(out markerRotation, out markerPosition, Quaternion.identity, Vector3.zero, cameraRotation, cameraPosition, Quaternion.identity, Vector3.zero);
		}

		Camera.main.projectionMatrix = projectionMatrix;
		
		//setup the webcam
		webcamTexture = new WebCamTexture((int)webcamResolution.x, (int)webcamResolution.y, 30);
		webcamTexture.Play();  
	}

	public void FreezeTracking(bool freeze){

		if(freeze){

			webcamTexture.Pause();
		}

		else{

			webcamTexture.Play();
		}

		trackingFrozen = freeze;
	}

	//This has to be called only once
	public void GetMarkerData(ref GameObject[] markerTransform){

		markerTransform[0].transform.rotation = markerRotation;
		markerTransform[0].transform.position = markerPosition;
	}

	void InitComponents(){

		if(!arWrapper){

			arWrapper = gameObject.GetComponent<ARWrapper>();
		}
	}


	private void CreateTexture()
	{

		//set the texture size (this might be modified by the AR engine).		
		int width = webcamTexture.width;
		int height = webcamTexture.height;

		if((width != 0) && (height != 0)){

			Vector2 imageSize = new Vector2(width, height);
			Vector2 textureSize = new Vector2(width, height);

			arWrapper.SetupVideoBackground(true, imageSize, textureSize, Quaternion.Euler(new Vector3(0, 180f, 0)), true, false, false);
			arWrapper.videoScreen.renderer.material.mainTexture = webcamTexture;
		}
	}
}
#endif