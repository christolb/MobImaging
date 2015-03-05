using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class OnTouchDown : MonoBehaviour {
		// Update is called once per frame
	void Update () {
		
		int touchCorrection = 1;
		
		RaycastHit hit = new RaycastHit();
		for (int i = 0; i+touchCorrection < Input.touchCount; ++i) {
			if (Input.GetTouch(i).phase.Equals(TouchPhase.Began)) {
				// Construct a ray from the current touch coordinates
				Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
				if (Physics.Raycast(ray, out hit)) {
					hit.transform.gameObject.SendMessage("OnMouseDown");
				}
			}
		}
		
	}
}