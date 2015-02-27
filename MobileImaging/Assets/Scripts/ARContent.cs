//Part of ARWrapper
//This is not used by UCS but is to be used for a standalone application only using ARWrapper.

using UnityEngine;
using System.Collections;

public class ARContent : MonoBehaviour {

	private float scale = 0.8f;
	public GameObject content;
	private ARWrapper arWrapper;

	private GameObject[] contentArray;
	private MeshRenderer[] mrArray;

	public GUIText message;
	
	private bool markerAccessoriesDone = false;
	
	// Use this for initialization
	void Start () {

		arWrapper = gameObject.GetComponent<ARWrapper>();

		contentArray = new GameObject[arWrapper.maxMarkerAmountInView];
		mrArray = new MeshRenderer[arWrapper.maxMarkerAmountInView];

		arWrapper.Init();		
	}

	
	// Update is called once per frame
	void Update () {	

		if(arWrapper.ready){
			
			InitMarkerAccessories();

			//get the marker data
			arWrapper.GetTrackingData(false, true, 1);

			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){

				//is the marker in view?
				if(arWrapper.markerId[i] != -1){

					//set the 3d content to the marker position
					contentArray[i].transform.parent = arWrapper.markerTransform[i].transform;

					//make sure the cube sits on top of the marker, not the middle
					contentArray[i].transform.localPosition = new Vector3(0, contentArray[i].transform.localScale.y / 2f, 0);
					contentArray[i].transform.localRotation = Quaternion.identity;

					//make the object visible
					mrArray[i].enabled = true;
				}

				//the marker is not in view
				else{

					//make the object invisible
					mrArray[i].enabled = false;
					contentArray[i].transform.parent = null;
				}
			}
		}

		message.text = arWrapper.message;
	}
	
	
	void InitMarkerAccessories(){
		
		if(!markerAccessoriesDone){
			
			for(int i = 0; i < arWrapper.maxMarkerAmountInView; i++){
	
				contentArray[i] = Instantiate(content, Vector3.zero, Quaternion.identity) as GameObject;
	
				//set the scale
				contentArray[i].transform.localScale = new Vector3(arWrapper.markerSize[i].x * 2f * scale, arWrapper.markerSize[i].x * 2f * scale, arWrapper.markerSize[i].y * 2f * scale);
	
				//hide the cube
				mrArray[i] = contentArray[i].GetComponent<MeshRenderer>();	
				mrArray[i].enabled = false;
			}	
			
			markerAccessoriesDone = true;
		}
	}
}
