using UnityEngine;
using System.Collections;

public class placeBallCam : MonoBehaviour {
	
	int i;
	int numMarkers;
	int foundMarker=0, current = 0, numTargetsInView=0;
	int[] inViewMarkers = new int[5];
	GameObject[] image_targets =  new GameObject[5];
	public GameObject[] scene_objects;
	public Camera ballCam;
	public GameObject ArCam;
	Quaternion rot;
	Vector3 worldCenterPos;
	Quaternion worldCenterRot;
	public bool recording = false;

	
	// Use this for initialization+
	void Start () {
		
		ArCam = GameObject.Find ("ARCamera");
		
		image_targets[0] = GameObject.Find ("ImageTarget_00");
		image_targets[1] = GameObject.Find ("ImageTarget_01");
		image_targets[2] = GameObject.Find ("ImageTarget_02");
		image_targets[3] = GameObject.Find ("ImageTarget_03");
		
	}
	
	// Update is called once per frame
	void Update () {
		if (recording) {

			TrackableBehaviour t = ArCam.GetComponent<QCARBehaviour> ().WorldCenter;
		
			worldCenterPos = t.gameObject.transform.position;
			worldCenterRot = t.gameObject.transform.rotation;
		
			foundMarker = 0;
			numTargetsInView = 0;
		
			Vector3 acv = Vector3.zero;
		
			// Record all in view markers
			for (int i = 0; i < 4; i++) {
				if (image_targets [i].transform.GetChild (0).GetComponent<MeshRenderer> ().enabled) {
					Debug.Log ("Marker" + i + " Coordinates are: " + image_targets [i].transform.position);
					numTargetsInView++;
				}
			}
		
			Debug.Log ("Ar Cam Coordinates are: " + ArCam.transform.position);
		
			for (int i = 0; i < 4; i++) {
				// Check if a particular Image Target is active
				if (image_targets [i].transform.GetChild (0).GetComponent<MeshRenderer> ().enabled) {
				
					// Find the AR Camera vector
					acv = -(image_targets [i].transform.position - ArCam.transform.position);
				
					foundMarker = i;
				
					Debug.Log ("Test:" + acv.ToString () + "foundMarker: " + foundMarker);
				
					break;
				
					//				if( image_targets[i].GetInstanceID() == t.gameObject.GetInstanceID()) {
					//				}
				}
			}
		
			//Specify ball Cam position and rotation
			ballCam.transform.position = scene_objects [foundMarker].transform.position + scene_objects [foundMarker].transform.TransformDirection (t.gameObject.transform.InverseTransformDirection (acv));
			//ballCam.transform.position = scene_objects [foundMarker].transform.position + acv;//scene_objects[foundMarker].transform.TransformDirection(t.gameObject.transform.InverseTransformDirection(acv));
			ballCam.transform.rotation = ArCam.transform.rotation * Quaternion.Inverse (worldCenterRot) * scene_objects [foundMarker].transform.rotation;
		
			Debug.Log (" Ball Cam position : " + ballCam.transform.position + " Scene Objects position " + scene_objects [foundMarker].transform.position);
			Debug.Log (" Ball Cam rotation : " + ballCam.transform.rotation + "ArCam rotation: " + ArCam.transform.rotation + "Inv World Center Rot = " + Quaternion.Inverse (worldCenterRot) + "Scene Obj rot = " + scene_objects [foundMarker].transform.rotation);
		
			// Specify ball Cam position and rotation
			//ballCam.transform.position = scene_objects[foundMarker].transform.position + scene_objects[foundMarker].transform.TransformDirection(image_targets[foundMarker].transform.InverseTransformDirection(acv));
			//ballCam.transform.rotation = ArCam.transform.rotation * Quaternion.Inverse(image_targets[foundMarker].transform.rotation) * scene_objects[foundMarker].transform.rotation;
		
		}
	}
}

