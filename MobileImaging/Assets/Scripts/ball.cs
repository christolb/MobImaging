using UnityEngine;
using System.Collections;

public class ball : MonoBehaviour {
	Primary primary = new Primary();
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	   Vector3 pos = transform.localPosition;
       if ((pos.x > 10 || pos.x < -10)
                || (pos.y > 10 || pos.y < -10)
                || (pos.z > 10 || pos.z < -10)) {
            Debug.Log("I'm dead");
            Destroy(gameObject);
        }
	}
	void OnCollisionEnter(Collision collision) {
		// can put sound effect, particle effect
		Debug.Log("collision!");
	}
	
	void OnTriggerEnter (Collider other) {
		Debug.Log("trigger enter");
		if (other.gameObject.CompareTag("hole")) {
			Destroy(gameObject);
		}
		//primary.
	}
}