using UnityEngine;
using System.Collections;

public class placeBallCam : MonoBehaviour {
	
	int i;
	int foundMarker=0, prevNumTargetsinView = 1000, numTargetsInView=0;
	GameObject[] image_targets =  new GameObject[4];
	public GameObject[] scene_objects;
	public Camera ballCam;
	public GameObject ArCam;
	public Camera ARCaaaamera;
	
	Quaternion rot;
	
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
		ballCam.projectionMatrix = ARCaaaamera.projectionMatrix;
		
		numTargetsInView = 0;
		
		Vector3 acv = Vector3.zero;
		
		// Record all in view markers
		for (int i = 0; i < 4; i++) {
			if( image_targets[i].transform.GetChild(0).GetComponent<MeshRenderer>().enabled ) {
				Debug.Log ("Marker"+i+" Coordinates are: "+ image_targets[i].transform.position);
				numTargetsInView++;
			}
		}
		
		for (int i = 0; i < 4; i++) {
			// Check if a particular Image Target is active
			if ( image_targets[i].transform.GetChild(0).GetComponent<MeshRenderer>().enabled && ((prevNumTargetsinView > 1) && (numTargetsInView==1)) ) {
				
				foundMarker = i;
				
				prevNumTargetsinView = 1;
				
				break;
			}
		}
		
		// Find the AR Camera vector
		acv = -(image_targets[foundMarker].transform.position - ArCam.transform.position);
		
		prevNumTargetsinView = numTargetsInView;
		
		Debug.Log ("Test:" + acv.ToString () + "foundMarker: "+ foundMarker);
		
		// Scene objects location                                   // Convert from world space to local space
		ballCam.transform.position = scene_objects[foundMarker].transform.position + scene_objects[foundMarker].transform.TransformDirection(image_targets[foundMarker].transform.InverseTransformDirection(acv));
		
		ballCam.transform.rotation = ArCam.transform.rotation * image_targets[foundMarker].transform.rotation * Quaternion.Inverse(scene_objects[foundMarker].transform.rotation);
		
		Debug.Log (" Ball Cam position : "+ ballCam.transform.position +" Scene Objects position " + scene_objects [foundMarker].transform.position+"Image Target position= "+image_targets[foundMarker].transform.position);
		Debug.Log (" ArCam   position : "+ ArCam.transform.position);
	}
}

