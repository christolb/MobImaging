/*
Note:
-If you have Unity Android/IOS Pro, enable this as it is much faster then using ReadPixels.
-RenderTexture does not work in the editor if a mobile platform is selected.
*/
//#define USE_RENDERTEXTURE


using UnityEngine;
using System.Collections;

public class SpecialEffect : MonoBehaviour {

	//Drop the video texture deform special effect game object here
	public GameObject vidTexDeform;

	//Drop the top lid of the box effect here
	public GameObject vidTexPatch;

	//Drop the Box effect here
	public GameObject vidTexObject;

	[HideInInspector]
	public GameObject zoomScreen;	
	private GameObject backgroundCameraZoomScreen;

	//This will set the zoom of the zoomScreen. 1 is no zoom, 0.5 is 2x zoom in, -0.5 is 2x zoom out
	private float zoomScreenFactor = 0.5f;

	//The size and position of the zoom screen. Values are in pixels.
	private Vector2 zoomScreenSize = new Vector2(150.0f, 150.0f); 
	private Vector2 zoomScreenPosition = new Vector2(20.0f, 300.0f); //20.0f, 300.0f

	private bool startRotationSet = false;

	private ARWrapper arWrapper;
	private MarkerRecorder markerRecorder;

	private Quaternion startRotation = Quaternion.identity;
	private Quaternion endRotation = Quaternion.identity;

	private bool isFrozen = false;

	private float rate = 1f / 3f; //3 seconds
	private float t = 0.0f;

	private RenderTexture renderTexture;
	private Texture2D renderTextureReadPix;
	private GameObject renderTexCam;

	private GameObject backupCam;

	private bool initReady = false;

	void Start(){

		InitComponents();

		backupCam = GameObject.Find("backupCam");
	}

	// Update is called once per frame
	void Update () {

		Init();

		if(initReady){
	
			if(isFrozen){

				t += Time.deltaTime * rate;
				vidTexPatch.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
			}

			else{

				vidTexPatch.transform.localRotation = startRotation;
			}
		}
	}

	void Init(){

		if(arWrapper.ready && !initReady){

			float ScaleFacYa = 0.0f;
			float ScaleFacYb = 0.0f;
			float ScaleFacXa = 0.0f;
			float ScaleFacXb = 0.0f;

			backgroundCameraZoomScreen = Instantiate(markerRecorder.backgroundCameraZoomScreenPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			zoomScreen = Instantiate(markerRecorder.zoomScreenPrefab, Vector3.zero, Quaternion.identity) as GameObject;

			//create a render texture
			renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32); 
			renderTextureReadPix = new Texture2D(Screen.width, Screen.height);

			renderTexture.Create();
			renderTexCam = new GameObject("renderTexCam");

			Vector3 cameraPosition = new Vector3(0,1,0);
			Quaternion cameraRotation = Quaternion.Euler(new Vector3(90, 0, 0));

			//set the render tex camera properties
			renderTexCam.AddComponent<Camera>();
			renderTexCam.camera.targetTexture = renderTexture;
			renderTexCam.camera.cullingMask = backgroundCameraZoomScreen.camera.cullingMask;
			renderTexCam.transform.position = cameraPosition;
			renderTexCam.transform.rotation = cameraRotation;
			renderTexCam.camera.nearClipPlane = 0.5f;
			renderTexCam.camera.farClipPlane = 2.0f;
			renderTexCam.camera.clearFlags = CameraClearFlags.Depth;

			//this camera has to be set to inactive because we only 
			//want to render the current frame on command
			renderTexCam.SetActive(false);

			//clean up the Hierarchy editor window
			renderTexCam.transform.parent = arWrapper.videoScreen.transform;

			float screenAspect = (float)Screen.height / (float)Screen.width;

			renderTexCam.camera.orthographic = true;
			renderTexCam.camera.orthographicSize = screenAspect; 

			//this is the actual special effect game object
			if(vidTexObject){

				vidTexObject.SetActive(false);
			}

			//set VidTexDeform shader variables
			if(vidTexDeform){

				arWrapper.CalculateShaderUVMapping(out ScaleFacYa, out ScaleFacYb, out ScaleFacXa, out ScaleFacXb, arWrapper.textureSizeAR, arWrapper.imageSizeAR, Screen.width, Screen.height);
				arWrapper.SetShaderUVMapping(vidTexDeform, ScaleFacYa, ScaleFacYb, ScaleFacXa, ScaleFacXb);

				vidTexDeform.renderer.material.mainTexture = arWrapper.videoScreen.renderer.material.mainTexture;
			}	

			//set zoomscreen shader variables
			arWrapper.CalculateShaderUVMapping(out ScaleFacYa, out ScaleFacYb, out ScaleFacXa, out ScaleFacXb, arWrapper.textureSizeAR, arWrapper.imageSizeAR, Screen.width, Screen.height);
			arWrapper.SetShaderUVMapping(zoomScreen, ScaleFacYa, ScaleFacYb, ScaleFacXa, ScaleFacXb);

			//set the layers
			zoomScreen.layer = 8;
			backgroundCameraZoomScreen.camera.cullingMask = 1 << zoomScreen.layer;

			Vector2 orthoCameraWorldSize = new Vector2(2.0f, ((float)Screen.height / (float)Screen.width) * 2.0f); 
			PositionZoomMesh(zoomScreen, orthoCameraWorldSize);
		
			backgroundCameraZoomScreen.transform.position = new Vector3(0,5,0);
			backgroundCameraZoomScreen.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));

			backgroundCameraZoomScreen.camera.orthographicSize = screenAspect;
		
			//set to middle of screen
			UpdateZoomScreenUVMapping(new Vector2(Screen.width / 2.0f, Screen.height / 2.0f));
		
			zoomScreen.renderer.material.mainTexture = arWrapper.videoScreen.renderer.material.mainTexture;
			zoomScreen.SetActive(false);

			initReady = true;
		}
	}

	public void TriggerAnimation(){

		if(!startRotationSet){

			//Set the start and end rotation of the lid of the box
			startRotation = vidTexPatch.transform.localRotation;
			endRotation = vidTexPatch.transform.localRotation * Quaternion.Euler(-90.0f, 0, 0);

			//set the texture of the effect object to the render texture so it displays a
			//frozen frame of the video.
			#if USE_RENDERTEXTURE
			vidTexPatch.renderer.material.mainTexture = renderTexture;
			#else
			vidTexPatch.renderer.material.mainTexture = renderTextureReadPix;
			#endif

			startRotationSet = true;
		}

		if(!isFrozen){

			//Save the current camera frame
			renderTexCam.camera.Render();

			//Get the matrix which is equivalent to UNITY_MATRIX_MVP in a shader
			Matrix4x4 MVP = GetMVPMatrix(backupCam, vidTexPatch);

			//copy the matrix to the shader
			vidTexPatch.renderer.material.SetMatrix("_MATRIX_MVP", MVP);

		#if !USE_RENDERTEXTURE
			RenderTexture.active = renderTexCam.camera.targetTexture;
			renderTextureReadPix.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			renderTextureReadPix.Apply();
			RenderTexture.active = null;
		#endif

			vidTexObject.SetActive(true);

			t = 0.0f;
			isFrozen = true;
		}

		else{

			vidTexObject.SetActive(false);
			isFrozen = false;
		}
	}
	
	void InitComponents(){

		if(!arWrapper){

			arWrapper = gameObject.GetComponent<ARWrapper>();
			markerRecorder = gameObject.GetComponent<MarkerRecorder>();
		}
	}

	//Note that this does not work correctly in the Editor when using String.
	public void UpdateZoomScreenUVMapping(Vector2 mousePosition){
	
		Vector2 mouseScreenSpace = PixelPositionToScreenSpace(mousePosition);

		//Get the position of the zoom screen in pixel coordinates
		Vector3 zoomScreenPosPixels = backgroundCameraZoomScreen.camera.WorldToScreenPoint(zoomScreen.transform.position);
		
		//Convert the position of the zoom screen to screen space
		Vector2 zoomScreenPosScreenSpace = PixelPositionToScreenSpace(zoomScreenPosPixels);

		Vector2 displaceVectorScreenSpace = mouseScreenSpace - zoomScreenPosScreenSpace;
		
		//TODO: figure out if I can transfer these 4 variables via 
		//a color vector instead.
		zoomScreen.renderer.material.SetFloat("_DisplaceVectorX", displaceVectorScreenSpace.x);
		zoomScreen.renderer.material.SetFloat("_DisplaceVectorY", displaceVectorScreenSpace.y);
		zoomScreen.renderer.material.SetFloat("_ObjectPositionScreenSpaceX", zoomScreenPosScreenSpace.x);
		zoomScreen.renderer.material.SetFloat("_ObjectPositionScreenSpaceY", zoomScreenPosScreenSpace.y);
		
		zoomScreen.renderer.material.SetFloat("_ZoomFactor", zoomScreenFactor);
	}
	
	//Give the zoom screen a size zoomScreenSize and a position zoomScreenPosition. The position is an offset from the
	//left and top side of the screen to the edge of the zoom screen.
	public void PositionZoomMesh(GameObject zoomScreen, Vector2 orthoCameraWorldSize){

		// Reset the transform so the mesh faces the camera
		zoomScreen.transform.rotation = Quaternion.identity;	
		
		Vector2 worldPosition =  Vector2.zero;
		Vector2 newZoomScreenWorldSize = Vector2.zero;
		Vector2 zoomScreenScale =  Vector2.zero;
		Vector2 zoomScreenPositionWorld =  Vector2.zero;
		Vector2 ScreenResolution = new Vector2(Screen.width, Screen.height);
		Bounds bounds;
		
		//get the default size of the zoomScreen
		bounds = zoomScreen.GetComponent<MeshFilter>().mesh.bounds;
		Vector2 DefaultZoomScreenWorldSize = new Vector2(bounds.size.x, bounds.size.z);
		
		//calculate the world size the zoom screen should get
		newZoomScreenWorldSize.x = PixelsToWorldDistance(zoomScreenSize.x, orthoCameraWorldSize.x, ScreenResolution.x);
		newZoomScreenWorldSize.y = PixelsToWorldDistance(zoomScreenSize.y, orthoCameraWorldSize.y, ScreenResolution.y);

		//Calculate the scale of the zoom screen so it ends up having the correct world size.
		//This assumes that the zoom screen mesh is a square, not a rectangle.
		zoomScreenScale.x = newZoomScreenWorldSize.x / DefaultZoomScreenWorldSize.x;
		zoomScreenScale.y = newZoomScreenWorldSize.y / DefaultZoomScreenWorldSize.y;
		
		//get the distance from the edge of the video screen to the edge of the zoom screen in world space size instead of pixels
		zoomScreenPositionWorld.x = PixelsToWorldDistance(zoomScreenPosition.x, orthoCameraWorldSize.x, ScreenResolution.x);
		zoomScreenPositionWorld.y = PixelsToWorldDistance(zoomScreenPosition.y, orthoCameraWorldSize.y, ScreenResolution.y);

		//calculate the world position of the zoom screen
		worldPosition.x = -((orthoCameraWorldSize.x / 2.0f) - ((newZoomScreenWorldSize.x / 2.0f) + zoomScreenPositionWorld.x));
		worldPosition.y = (orthoCameraWorldSize.y / 2.0f) - ((newZoomScreenWorldSize.y / 2.0f) + zoomScreenPositionWorld.y);
		
		//Set the zoom screen position and scale.
		//This assumes that the mesh used is square, not a rectangle.
		zoomScreen.transform.position = new Vector3(worldPosition.x, 0.0f, worldPosition.y);
		zoomScreen.transform.localScale = new Vector3(zoomScreenScale.x, 1.0f, zoomScreenScale.x);
	}

	//convert from range "0 to 1024" to "-1 to 1")
	Vector2 PixelPositionToScreenSpace(Vector2 pixelPosition){
	
		Vector2 factor = Vector2.zero;
		Vector2 screenSpace = Vector2.zero;
		
		//calculate the factor
		factor.x = 2.0f / Screen.width;
		factor.y = 2.0f / Screen.height;
		
		screenSpace.x = (pixelPosition.x * factor.x) - 1.0f;
		screenSpace.y = (pixelPosition.y * factor.y) - 1.0f;
		
		return screenSpace;
	}

	//Convert a distance in pixels to a distance in world space. Only valid for the ortho camera setup.
	float PixelsToWorldDistance(float pixelDistance, float worldScreenDistance, float pixelScreenDistance){
	
		return (pixelDistance * worldScreenDistance) / pixelScreenDistance;
	}

	//Get the matrix which is equivalent to UNITY_MATRIX_MVP in a shader
	private Matrix4x4 GetMVPMatrix(GameObject cameraObject, GameObject shaderObject){

		Matrix4x4 P = GL.GetGPUProjectionMatrix(cameraObject.camera.projectionMatrix, false);
		Matrix4x4 V = cameraObject.camera.worldToCameraMatrix;
		Matrix4x4 M = shaderObject.renderer.localToWorldMatrix;
		Matrix4x4 MVP = P * V * M;

		return MVP;
	}
}
