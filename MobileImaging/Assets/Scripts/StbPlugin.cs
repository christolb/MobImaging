//StudierStube Tracker plugin for Unity. 
//Bit Barrel Media
//v1.0

//#define STUDIERSTUBE 

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class StbPlugin : MonoBehaviour {

#if STUDIERSTUBE
	public Texture2D videoTexture;
	public string message;
	private MarkerData[] markerData;
	private int frameIndexBuf = -1;

	private float[] markerSizeFactor;
	private int totalMarkerAmount;
		
	private bool trackingFrozen = false;	
	private bool init = true;

	[DllImport ("StbPlugin")]   
	private static extern void StbInit(ref bool init, ref bool flip_v, ref int width, ref int height, [In, Out] float[] projectionMatrix, int totalMarkerAmount, int maxMarkerAmountInView, int keepOnMS, int markerType);

	[DllImport ("StbPlugin")]   
	private static extern void StbGetVideoResolution(ref int x, ref int y);

	[DllImport ("StbPlugin")]   
	private static extern void StbSetTexturePointer(System.IntPtr texture);
	
	[DllImport ("StbPlugin")]  
	public static extern void StbGetMarkerData([In, Out] MarkerData[] markerData, ref int markerAmountInView);

	[DllImport ("StbPlugin")]   
	private static extern int StbGetFrameIndex();

	[DllImport("StbPlugin", CharSet = CharSet.Ansi)]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string StbGetString(int stringType);

	[DllImport ("StbPlugin")]   
	private static extern void StbEnableVideoUpdate(bool enableVideoUpdate);

	[DllImport ("StbPlugin")]   
	private static extern void StbClose();

	private static class MarkerType{

		public static int FRAME_MARKER			= 0;
		public static int SIMPLE_ID_MARKER		= 1;
		public static int DATA_MATRIX_MARKER	= 2;
		public static int IMAGE_TARGET			= 3;
	}

	public static class StringType{

		public static int ERROR					= 0;
		public static int DATA_MATRIX_MESSAGE	= 1;
	}


	IEnumerator Start () {

		yield return StartCoroutine("CallPluginLoop");
	}
	
	private IEnumerator CallPluginLoop()
	{
		while (true) {

			// Wait until all frame rendering is done
			yield return new WaitForEndOfFrame();
			
			//Calling this will call the UnityRenderEvent() function in the DLL
			GL.IssuePluginEvent(1);
		}
	}

	public void Init(ref bool init, ref bool flip_v, float[] mkrSizeFactor, int totalMkrAmount, int maxMarkerAmountInView, int markerType, Vector2 markerBaseResolution, float markerBaseXsizeMeters){

		int keepOnMS = 0;
		markerSizeFactor = mkrSizeFactor;
		totalMarkerAmount = totalMkrAmount;

		markerData = new MarkerData[maxMarkerAmountInView];
		projectionMatrix = new float[16];

		int width = 0;
		int height = 0;

		//A frame marker in use with StudierStube displays heavy flickering when 
		//the target->setFilterStrength(...) keepOnDuration variable is set to zero.
		if(markerType == (int)ARWrapper.MarkerType.FRAME_MARKER){

			keepOnMS = 200;
		}

		else{

			keepOnMS = 0;
		}

		//get the project root directory
		string root = Application.dataPath;
			
		for(int i = Application.dataPath.Length - 1; i >= 0; i--){

			if(root[i] == '/'){

				root = root.Substring(0, i + 1);
				break;
			}
		}

		string StbPluginPath = Application.dataPath + "/Plugins/StbPlugin.dll";
		string DSVLmodDPath = root + "DSVLmodD.dll";
		string StbCoreDPath = root + "StbCoreD.dll";
		string StbIODPath = root + "StbIOD.dll";
		string StbMathDPath = root + "StbMathD.dll";
		string StbTrackerDPath = root + "StbTrackerD.dll";
		string StbTrackerNFT2DPath = root + "StbTrackerNFT2D.dll";

		if(File.Exists(StbPluginPath) && File.Exists(DSVLmodDPath) && File.Exists(StbCoreDPath) && File.Exists(StbIODPath) && File.Exists(StbMathDPath) && File.Exists(StbTrackerDPath) && File.Exists(StbTrackerNFT2DPath)){

			StbInit(ref init, ref flip_v, ref width, ref height, projectionMatrix, totalMkrAmount, maxMarkerAmountInView, keepOnMS, markerType);
			message = StbGetString(StringType.ERROR);
		}

		else{

			init = false;
		}

		if(!File.Exists(StbPluginPath)){

			message = "Can not find Plugin: " + StbPluginPath;
		}

		if(!File.Exists(DSVLmodDPath)){

			message = "Can not find Plugin: " + DSVLmodDPath;
		}

		if(!File.Exists(StbCoreDPath)){

			message = "Can not find Plugin: " + StbCoreDPath;
		}

		if(!File.Exists(StbIODPath)){

			message = "Can not find Plugin: " + StbIODPath;
		}

		if(!File.Exists(StbMathDPath)){

			message = "Can not find Plugin: " + StbMathDPath;
		}

		if(!File.Exists(StbTrackerDPath)){

			message = "Can not find Plugin: " + StbTrackerDPath;
		}

		if(!File.Exists(StbTrackerNFT2DPath)){

			message = "Can not find Plugin: " + StbTrackerNFT2DPath;
		}
		
		if(init){

			CreateVideoTexture(width, height);		

			SetProjectionMatrix(projectionMatrix, flip_v);
		}
	}


	public void GetMarkerData(ref int[] markerId, ref GameObject[] markerTransform, out bool cameraUpdate){

		//get the frame index to figure out whether the camera frame is a new one
		int frameIndex = StbGetFrameIndex();

		if(frameIndex != frameIndexBuf){

			cameraUpdate = true;
		}

		else{

			cameraUpdate = false;
		}

		frameIndexBuf = frameIndex;

		if(init){

			if(!trackingFrozen){

				int markerAmountInView = 0;

				//get all the marker data
				StbGetMarkerData(markerData, ref markerAmountInView);

				// Handle detected markers
				for (int i = 0; i < markerId.Length; i++){

					//Get the marker ID
					int id = markerData[i].id;

 					if((id >= 0) && (id < totalMarkerAmount)){

						//get marker text
						if(markerData[i].type == MarkerType.DATA_MATRIX_MARKER){
						
							message = StbGetString(StringType.DATA_MATRIX_MESSAGE);
						}

						//Get the marker position. Note that the raw position from StudierStube is not compatible with the
						//Unity format and has to be modified.
						Vector3 position = markerData[i].GetPosition();
						markerTransform[i].transform.position = new Vector3(position.x, -position.y, position.z);

						//Account for the marker size. This can be set in the DLL as well, but modifying it here gives 
						//us more flexibility.
						markerTransform[i].transform.position *=  markerSizeFactor[id];

						//Get the marker rotation. Note that the raw quaternion from StudierStube is not compatible with the
						//Unity format and has to be modified.
						Quaternion rotation = markerData[i].GetRotation();
						markerTransform[i].transform.rotation = new Quaternion(-rotation.x, rotation.y, -rotation.z, rotation.w);		
						markerTransform[i].transform.Rotate(new Vector3(90.0f, 0, 0));
					}

					markerId[i] = id;
				}
			}
		}
	}



	private void CreateVideoTexture(int width, int height)
	{
		if((width != 0) && (height != 0)){

			Vector2 textureSize = new Vector2(width, height);

			// Create a texture
			videoTexture = new Texture2D((int)textureSize.x, (int)textureSize.y,TextureFormat.ARGB32,false);
			videoTexture.filterMode = FilterMode.Bilinear;
			videoTexture.wrapMode = TextureWrapMode.Clamp;

			// Pass texture pointer to the plugin
			StbSetTexturePointer(videoTexture.GetNativeTexturePtr());
		}
	}

	void OnApplicationQuit() {
	
		try{
			StbClose();	
		}
		catch(Exception e){
			message = e.Message;
		}
		
	}

	void SetProjectionMatrix(float[] projectionMatrix, bool flip_v){

		//Convert the projection matrix to Unity format
		Matrix4x4 mat = new Matrix4x4();

		//Note that the raw projection matrix from StudierStube is not compatible with the
		//Unity format and has to be modified.
		mat[0] = projectionMatrix[0];
		mat[1] = projectionMatrix[1];
		mat[2] = projectionMatrix[2];
		mat[3] = projectionMatrix[3];
		mat[4] = projectionMatrix[4];
		mat[5] = -projectionMatrix[5];
		mat[6] = projectionMatrix[6];
		mat[7] = projectionMatrix[7];
		mat[8] = -projectionMatrix[8];
		mat[9] = -projectionMatrix[9];
		mat[10] = -projectionMatrix[10];
		mat[11] = -projectionMatrix[11];
		mat[12] = projectionMatrix[12];
		mat[13] = projectionMatrix[13];
		mat[14] = projectionMatrix[14];
		mat[15] = projectionMatrix[15];

		//if the video texture is flipped vertically, 
		//we need to modify the projection matrix
		if(!flip_v){

			mat *= Matrix4x4.Scale(new Vector3(1, -1, 1));
		}

		//Set the projection matrix
		Camera.main.projectionMatrix = mat;			
	}

	public void FreezeTracking(bool freeze){

		StbEnableVideoUpdate(!freeze);
		trackingFrozen = freeze;
	}

	//Marshal the projection matrix
	[MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_R4)] float[] projectionMatrix ;

	//Marshal the marker data structure
	[StructLayout(LayoutKind.Sequential)]
	public struct MarkerData
	{		
		//Marshal the marker ID
		[MarshalAs(UnmanagedType.I4)]
		public int id;

		//Marshal the marker type
		[MarshalAs(UnmanagedType.I4)]
		public int type;

		//Marshal the marker transform matrix array. 
		[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)] public float[] m;

		//Extract the position from the marker transform matrix
		public Vector3 GetPosition(){
			
			/*
			//convert from float[16] to Matrix4x4
			Matrix4x4 stbMatrix = new Matrix4x4();
			for(int i = 0; i < 16; i++){

				stbMatrix[i] = m[i];
			}
			*/

			//convert from float[16] to Matrix4x4
			Matrix4x4 stbMatrix = new Matrix4x4();
			Float16ToMatrix4x4(ref stbMatrix, m);

			Vector3 position =  Math3d.PositionFromMatrix(stbMatrix);
			return position;
		}

		//Extract the rotation from the marker transform matrix
		public Quaternion GetRotation(){

			//convert from float[16] to Matrix4x4
			Matrix4x4 stbMatrix = new Matrix4x4();
			Float16ToMatrix4x4(ref stbMatrix, m);

			Quaternion rotation = Math3d.QuaternionFromMatrix(stbMatrix);
			return rotation;
		}	

		//convert from float[16] to Matrix4x4
		private void Float16ToMatrix4x4(ref Matrix4x4 matrix, float[] array){

			for(int i = 0; i < 16; i++){

				matrix[i] = array[i];
			}
		}
	}
#endif
}
