using UnityEngine;
using System.Collections;

public class placeWalls : MonoBehaviour {

    // Use this for initialization
    void Start () {
         
    }
    
    // Update is called once per frame
    void Update () {
        GameObject refObj = GameObject.Find("Marker 0");
        if (refObj != null) {
            // Debug.Log("found marker 0");
            // Debug.Log(refObj.transform.position.z);
            // GameObject placedObj = GameObject.Find("/worldCenterObject/Sphere/Sphere");
            GameObject placedObj = GameObject.Find("Wall");
            if( placedObj != null) {
                Debug.Log("placing wall");
                placedObj.transform.position = refObj.transform.position + Vector3.forward * 2.0f;
            }
        }
    }
}
