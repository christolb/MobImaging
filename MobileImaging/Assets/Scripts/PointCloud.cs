//#define POINTCLOUD

using UnityEngine;
using System.Collections;

public class PointCloud : MonoBehaviour {
	
#if POINTCLOUD

	[HideInInspector]
	public bool init1Ready = false;
	
	[HideInInspector]
	public bool init2Ready = false;
	
	// Coordinate transform
	private Matrix4x4 convert = new Matrix4x4();
	private Matrix4x4 pixelTransform;	

	bool texture_updated = false;
	bool transforms_updated = false;
	private Texture2D videoTexture;

#if !UNITY_EDITOR
	
	private bool trackingFrozen = false;

	private int textureSizeInt;
	private float textureSizeInv;
	
	private int videoTextureID; // Native texture ID.

	private Rect videoTextureCoordinates;
	private Rect screenRect;
	private Matrix4x4 frustum, cam;
	
	private int textureSize;

	int flags = 0;
	
	static public pointcloud_state PreviousState { get; private set; }
	public pointcloud_state State { get; private set; }
#endif

	private ARWrapper arWrapper;
	

	void OnDestroy()
	{
#if !UNITY_EDITOR
		//destroy point cloud if necessary
		PointCloudAdapter.pointcloud_destroy();
#endif
	}
	


	//Note that originally PointCloud outputs a camera transform with the slam points being stationary. This is modified so that
	//the camera is at the identity transform and the slam points move around it instead. This is to make the tracking
	//logic he same across all AR engines.
	public void GetSLAMData(bool drawPoints, ref Vector3[] SLAMpoints, float scaleFactor, ref int[] markerId, ref GameObject[] markerTransform){
		
		if(init1Ready && init2Ready){
			
  #if !UNITY_EDITOR
			if(!trackingFrozen){
				
				int flags = PointCloudAdapter.update(videoTextureID, Camera.main.nearClipPlane, Camera.main.farClipPlane, drawPoints, ref cam, ref frustum);
				State = PointCloudAdapter.pointcloud_get_state();

				texture_updated = (flags & 1) > 0;
				transforms_updated = (flags & 2) > 0;
			}
			
			else{
				
				texture_updated = true;
				transforms_updated = true;
			}
#endif
			
#if UNITY_EDITOR
			texture_updated = true;
			transforms_updated = true;	
#endif
			
			
#if !UNITY_EDITOR
			if(texture_updated && (State != pointcloud_state.POINTCLOUD_NOT_CREATED)){ 
#endif		
#if UNITY_EDITOR
			if(texture_updated){
#endif
				
				
				if (transforms_updated)
				{
#if !UNITY_EDITOR
					Matrix4x4 camera_matrix = convert * cam;
					Matrix4x4 camera_pose = camera_matrix.inverse;

					Vector3 cameraPosePosition = camera_pose.GetColumn(3) / scaleFactor;
					Quaternion cameraPoseRotation = QuaternionFromMatrixColumns(camera_pose);	

					Quaternion rotation = Quaternion.identity;
					Vector3 position = Vector3.zero;
					Math3d.TransformWithParent(out rotation, out position, Quaternion.identity, Vector3.zero, cameraPoseRotation, cameraPosePosition, Quaternion.identity, Vector3.zero);
					markerTransform[0].transform.rotation = rotation;
					markerTransform[0].transform.position = position;
						
					Camera.main.projectionMatrix = frustum * convert;
					
					switch(Screen.orientation)
					{
						default:
						case ScreenOrientation.LandscapeLeft:
							RotateProjectionMatrix(90);
							break;
						case ScreenOrientation.LandscapeRight:
							RotateProjectionMatrix(-90);
							break;
						case ScreenOrientation.Portrait:
							break;
						case ScreenOrientation.PortraitUpsideDown:
							RotateProjectionMatrix(180);
							break;
					}
#endif

#if UNITY_EDITOR

					Quaternion cameraPoseRotation;
					Vector3 cameraPosePosition;
					
					cameraPoseRotation = Quaternion.Euler(new Vector3(5.0f, 350.0f, 0.0f));
					cameraPosePosition = new Vector3(0.0f, 2.0f, -3.4f) / scaleFactor;					

			//		cameraPoseRotation = arWrapper.debugObject.transform.rotation;
			//		cameraPosePosition = arWrapper.debugObject.transform.position / scaleFactor;

					Quaternion rotation = Quaternion.identity;
					Vector3 position = Vector3.zero;

					//PointCloud gives us a camera transform. Convert this to a marker transform
					Math3d.TransformWithParent(out rotation, out position, Quaternion.identity, Vector3.zero, cameraPoseRotation, cameraPosePosition, Quaternion.identity, Vector3.zero);
					markerTransform[0].transform.rotation = rotation;
					markerTransform[0].transform.position = position;
#endif
				}
			}
			
			UpdateTracking(ref markerId);
		}

		//get the position of the SLAM feature points
		SLAMpoints = GetSlamPoints(scaleFactor);		
	}
	
	
	
		
	public void Init1()
	{		
		Application.targetFrameRate = 30;
		
		InitComponents();
				
#if !UNITY_EDITOR		
		
		if (PointCloudAppKey.AppKey == "")
		{
			Debug.Log("No PointCloud Application Key provided!");
			DebugStreamer.message = ("Debug: " + "No app key");
		}
		
		
		screenRect = new Rect(0, 0, Screen.height, Screen.width);
#endif
		
		convert.SetRow(0, new Vector4(0,-1,0,0));
		convert.SetRow(1, new Vector4(-1,0,0,0));
		convert.SetRow(2, new Vector4(0,0,-1,0));
		convert.SetRow(3, new Vector4(0,0,0,1));
		
		pixelTransform = Matrix4x4.identity;
	
		pixelTransform[0,0] = 0;
		pixelTransform[0,1] = 1;
		pixelTransform[1,0] = 1;
		pixelTransform[1,1] = 0;
		
#if !UNITY_EDITOR
		int bigDim = Mathf.Max(Screen.width, Screen.height);
		int smallDim = Mathf.Min(Screen.width, Screen.height);
		
		init1Ready = PointCloudAdapter.init(smallDim, bigDim, PointCloudAppKey.AppKey);;
#endif	
			
#if UNITY_EDITOR
		init1Ready = true;
#endif	
	}
		
		
		
	public void Init2(){
	
		if(!init2Ready && init1Ready){
				
#if !UNITY_EDITOR
			int flags = PointCloudAdapter.update(videoTextureID, Camera.main.nearClipPlane, Camera.main.farClipPlane, false, ref cam, ref frustum);
			texture_updated = (flags & 1) > 0;
#endif
				
#if UNITY_EDITOR
			texture_updated = true;	
#endif
			if (!videoTexture && texture_updated)
			{
					
#if UNITY_EDITOR
				texture_updated = true;
					
				videoTexture = new Texture2D(640,480);
				int size = videoTexture.width * videoTexture.height;
				Color[] colors = new Color[size];
				for(int i = 0; i < size; i++){
						
					colors[i] = Color.black;		
				}
					
				videoTexture.SetPixels(colors);
				videoTexture.Apply();		
#endif
				Vector2 imageSize = new Vector2(0, 0);
				Vector2 textureSize = new Vector2(0, 0);
				InitializeTexture(out imageSize, out textureSize);

				arWrapper.SetupVideoBackground(true, imageSize, textureSize, Quaternion.identity, false, false, false);

				arWrapper.videoScreen.renderer.material.mainTexture = videoTexture;

				init2Ready = true;
			}	
		}
	}
	
	public void Restart()
	{
#if !UNITY_EDITOR
		PointCloudAdapter.pointcloud_reset();
#endif
		Init1();
	}
		
							
	static Quaternion QuaternionFromMatrixColumns(Matrix4x4 m) { 
						
		return Quaternion.LookRotation(-m.GetColumn(2), m.GetColumn(1)); // forward, up
	}
		
				
	public void UpdateTracking(ref int[] markerId)
	{
														

#if !UNITY_EDITOR

		if(State == pointcloud_state.POINTCLOUD_TRACKING_SLAM_MAP){
		
			markerId[0] = 0;				
		}
						
		else{
							
			markerId[0] = -1;				
		}
		
			
#endif

#if UNITY_EDITOR

		markerId[0] = 0;
#endif
	}
				
	


	void InitializeTexture(out Vector2 imageSize, out Vector2 textureSize) {

#if UNITY_EDITOR
		textureSize = new Vector2(480,360);
		imageSize = textureSize;
#endif
			
#if !UNITY_EDITOR
		
	//	480x360
		int videoWidth = PointCloudAdapter.pointcloud_get_video_width();
		int videoHeight = PointCloudAdapter.pointcloud_get_video_height();
			
	//	0x0
		float videoCropX = PointCloudAdapter.pointcloud_get_video_crop_x();
		float videoCropY = PointCloudAdapter.pointcloud_get_video_crop_y();
		
		int bigDim = Mathf.Max(videoWidth, videoHeight);
		
		textureSizeInt = GetPowerOfTwo(bigDim);
		textureSizeInv = 1.0f/textureSizeInt;
		
		float cx = videoCropX / textureSizeInt;
		float cy = videoCropY / textureSizeInt;
		
		//512x512
		videoTexture = new Texture2D(textureSizeInt, textureSizeInt, TextureFormat.BGRA32, false);
		videoTextureID = videoTexture.GetNativeTextureID();
			
		//0.9375, 0, -0.9375, 0.703125
		videoTextureCoordinates = new Rect(videoWidth * textureSizeInv - cx, cy, -videoWidth * textureSizeInv + 2 * cx, videoHeight * textureSizeInv - 2 * cy);
			
		imageSize = new Vector2(videoWidth, videoHeight);
		textureSize = new Vector2(videoTexture.width, videoTexture.height);
#endif
	}

				
	private void RotateProjectionMatrix(float angleDegrees) {
					
		Quaternion correction_q = Quaternion.AngleAxis(angleDegrees, new Vector3(0, 0, 1));
		Matrix4x4 correction_rot = Matrix4x4.TRS(Vector3.zero, correction_q, new Vector3(1, 1, 1));
		Camera.main.projectionMatrix = correction_rot * Camera.main.projectionMatrix;
	}
	
	private int GetPowerOfTwo(int pow2) {
	
		pow2--;
		pow2 |= pow2 >> 1;
		pow2 |= pow2 >> 2;
		pow2 |= pow2 >> 4;
		pow2 |= pow2 >> 8;
		pow2 |= pow2 >> 16;
		pow2++;
		
		return pow2;
	}
	

	
	//Note: The SLAM points from PointCloud are in the wrong format (not Vector3 but pointcloud_vector_3),
	//in the wrong orientation (X has to be swapped), and not affected by scale. This function fixes that.
	//Note: the index versus the coordinate of a point is not static. The index values will be shuffled
	//multiple times during point cloud construction. Therefore selected points cannot be stored using
	//the original point cloud array.
	public Vector3[] GetSlamPoints(float scaleFactor){

#if !UNITY_EDITOR
		if(State == pointcloud_state.POINTCLOUD_TRACKING_SLAM_MAP){
		
			//Get the raw points
			pointcloud_vector_3[] points = PointCloudAdapter.pointcloud_get_points();
			
			//Change the format to Unity compatible
			Vector3[] pointsUnity = new Vector3[points.Length];
		
			for(int i = 0; i < points.Length; i++){
			
				//Swap the X coordinate
				pointsUnity[i] = new Vector3(-points[i].x, points[i].y, points[i].z);
				

				//Apply scale changes
				pointsUnity[i] /= scaleFactor;

			}
				
			return pointsUnity;
		}
			
		else{
				
			return null;		
		}
#endif

#if UNITY_EDITOR
		Vector3[] pointsUnity = new Vector3[11];

		//create fake SLAM points for debugging
		pointsUnity[0] = new Vector3(-1.77f, 0.0f, 3.2f);
		pointsUnity[1] = new Vector3(-2.43f, 0.64f, 4.86f);
		pointsUnity[2] = new Vector3(-1.81f, -0.77f, 4.27f);
		pointsUnity[3] = new Vector3(-1.57f, 0.0f, 4.58f);
		pointsUnity[4] = new Vector3(-1.0f, 0.78f, 4.12f);
		pointsUnity[5] = new Vector3(-3.20f, -0.87f, 5.32f);
				
		pointsUnity[6] = new Vector3(0.0f, 1.0f, 2.0f);
		pointsUnity[7] = new Vector3(1.0f, 1.0f, 2.0f);
		pointsUnity[8] = new Vector3(1.0f, 0.0f, 2.0f);
		pointsUnity[9] = new Vector3(0.0f, 0.0f, 2.0f);
		pointsUnity[10] = new Vector3(0.5f, 0.5f, 2.0f);

		/*
		//create fake SLAM points for debugging
		pointsUnity[5] = new Vector3(-1.77f, 0.0f, 3.2f);
		pointsUnity[3] = new Vector3(-2.43f, 0.64f, 4.86f);
		pointsUnity[2] = new Vector3(-1.81f, -0.77f, 4.27f);
		pointsUnity[1] = new Vector3(-1.57f, 0.0f, 4.58f);
		pointsUnity[4] = new Vector3(-1.0f, 0.78f, 4.12f);
		pointsUnity[0] = new Vector3(-3.20f, -0.87f, 5.32f);
				
		pointsUnity[8] = new Vector3(0.0f, 1.0f, 2.0f);
		pointsUnity[7] = new Vector3(1.0f, 1.0f, 2.0f);
		pointsUnity[6] = new Vector3(1.0f, 0.0f, 2.0f);
		pointsUnity[10] = new Vector3(0.0f, 0.0f, 2.0f);
		pointsUnity[9] = new Vector3(0.5f, 0.5f, 2.0f);
		*/
		

		//apply scale
		for(int i = 0; i < pointsUnity.Length; i++){
			
			pointsUnity[i] /= scaleFactor;
		}

		return pointsUnity;
#endif
	}

	public void FreezeTracking(bool freeze){

#if !UNITY_EDITOR
		trackingFrozen = freeze;
#endif
	}

	void InitComponents(){

		if(!arWrapper){

			arWrapper = gameObject.GetComponent<ARWrapper>();
		}
	}

#endif
}
