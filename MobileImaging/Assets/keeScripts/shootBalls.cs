	 using UnityEngine;
using System.Collections;

public class shootBalls : MonoBehaviour {
    public Transform scene;
    public Transform AR_cam;
    public GameObject ball_prefab;

    // Use this for initialization
    void Start () {
        AR_cam = (GameObject.Find("ARCamera")).GetComponent<Transform>();
		//AR_cam = Camera.current.GetComponent<Transform> ();
        scene = transform.parent;


    }
    
    // Update is called once per frame
    void Update () {
       // if (Time.frameCount % 30 == 0) {
		if (Input.GetMouseButtonDown (0)) {
				scene = transform.parent;
				Vector3 shooting_position = AR_cam.localPosition;
				Vector3 shooting_direction = AR_cam.forward;
				((GameObject)Instantiate (
                	    ball_prefab, shooting_position, Quaternion.identity
				)).GetComponent<Rigidbody> ().AddForce (shooting_direction * 200);

		}
    }
}
