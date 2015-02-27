using UnityEngine;
using System.Collections;

public class ResetCam : MonoBehaviour {

	[HideInInspector]
	public GameObject backupCam;
	private Camera cam;

	void Start(){

		backupCam = new GameObject("backupCam");
		cam = backupCam.AddComponent<Camera>();

		//this camera is just needed to store variables and it doesn't render anything
		cam.enabled = false;
	}

	void OnPostRender() {

		cam.CopyFrom(Camera.main);

		Camera.main.transform.position = Vector3.zero;
		Camera.main.transform.rotation = Quaternion.identity;
	}
}
