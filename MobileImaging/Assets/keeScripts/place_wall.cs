using UnityEngine;
using System.Collections;

public class place_wall : MonoBehaviour {
	// set this field in inspector
	public GameObject ar_wall;
	// public int id_marker;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		// if reference ar wall is active i.e. marker is seen in scene,
		// place this wall there

		// GameObject marker = GameObject.Find(System.String.Concat("Marker ", id_marker.ToString()));
		// Debug.Log (marker.activeInHierarchy);

		if (ar_wall.activeInHierarchy && ar_wall.GetComponentInParent<Transform>().localPosition.z > -5000) {
			transform.localPosition = ar_wall.transform.localPosition;
			transform.rotation = ar_wall.transform.rotation;
		}
	}
}
