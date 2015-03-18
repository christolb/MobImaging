using UnityEngine;
using System.Collections;

public class Primary : MonoBehaviour {
	public Transform game_cam;
	public GameObject ball_prefab;
	private char mode = 's';
	
	private float sliderValue = 1.0f;
	private float maxSliderValue = 10.0f;
	public GUISkin customSkin = null;
	
	private int player = 1;
	private int player1score = 0;
	private int player2score = 0;
	
	
	void Start () {
		game_cam = (GameObject.Find("BallCam")).GetComponent<Transform>();
	}
	void OnGUI () {
		GUIStyle costumButton = new GUIStyle ("button");
		costumButton.fontSize = 30;
		GUI.skin = customSkin;	
		
		switch (mode) {
		case 's':
			if (GUI.Button (new Rect (Screen.width/10, Screen.height/10 + Screen.height/7, Screen.width/6, Screen.height/8), "Play Game")) {
				mode = 'g';
				
			}
			if (GUI.Button (new Rect (Screen.width/10, Screen.height/10, Screen.width/6, Screen.height/8), "New Recording")) {
				mode = 'r';
				GameObject.Find("wall_placer").GetComponent<wall_setup>().recording = true;
			}
			break;
		case 'r':
			GUI.Box(new Rect(0, 0, Screen.width, 30), "Pann the camera over all the markers, in increasing order (starting at 0)."); 
			
			if (GUI.Button (new Rect (Screen.width/20, Screen.height/20, Screen.width/8, Screen.height/14), "Stop")) {
				mode = 's';
				GameObject.Find("wall_placer").GetComponent<wall_setup>().recording = false;
			}
			break;
		case 'g':
			
			GUI.Box(new Rect(0, 0, Screen.width, 30), "Player 1 score: "+player1score+"\t\t\tPlayer "+player+" turn\t\t\t"+"Player 2 score: "+player2score); 
			
			if (GUI.Button (new Rect (Screen.width/2 - Screen.width/12, Screen.height-Screen.height/8, Screen.width/6, Screen.height/10), "Shoot!",costumButton)) {
				Shoot ();
			}
			if (GUI.Button (new Rect (Screen.width/20, Screen.height/20, Screen.width/14, Screen.height/14), "Back")) {
				mode = 's';
			}
			
			
			GUILayout.BeginArea (new Rect (Screen.width - Screen.width/13,Screen.height/8,Screen.width/15,Screen.height - Screen.height/4));
			GUILayout.BeginVertical();	
			
			
			sliderValue = GUILayout.VerticalSlider (sliderValue, maxSliderValue,1.0f);
			
			// End the Groups and Area
			GUILayout.EndVertical();
			//GUILayout.EndHorizontal();	
			GUILayout.EndArea();
			
			break;
		}
		
		
	}
	void Update (){
		
		//check if goal --> send type of ball to updateScore
		
	}
	void UpdateScore(int player, GameObject ball_prefab){
		
		
		//check which players ball it is
		//update the score of the right player and switch players turn if needed
		
	}
	void Shoot () {
		if (player == 1) {
			player = 2;
		} else {
			player = 1;
		}
		Vector3 shooting_position = game_cam.localPosition;
		Vector3 shooting_direction = game_cam.forward;
		GameObject go = (GameObject)Instantiate(ball_prefab, shooting_position, Quaternion.identity);
		go.GetComponent<Rigidbody>().AddForce(shooting_direction * sliderValue*100);
		
	}
}
