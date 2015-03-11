using UnityEngine;
using System.Collections;

public class ball : MonoBehaviour {

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
            Destroy(this);
        }
	}
}
