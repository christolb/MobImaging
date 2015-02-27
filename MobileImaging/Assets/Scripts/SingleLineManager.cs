
//#define VECTROSITY

using UnityEngine;
using System.Collections;
using System;

#if VECTROSITY
using Vectrosity;
#endif

public class SingleLineManager : MonoBehaviour {
	
	public enum ObjectType{
		X,
		Y,
		Z,
		Line,
		Dot1,
		Dots
	}	
	public ObjectType objectType;

	public Material lineMaterial;

	public bool selection;

	//length of line
	public static float lineLength = 3.0f;

#if VECTROSITY
	//factor for increase in width when selected
	private float selectSizeFactor = 3.0f;

	private float lineWidth = 4.0f;
	private float dotSize = 10.0f; //15
	
	//half the object width;
	private float objectSize = 0.5f;
	
	public VectorLine lineObjectVectorline;
	public VectorPoints lineObjectVectorpoints;
	
	private Vector3[] vectorPoints;  //= new Vector3[4];
	private Vector3[] linePoints = new Vector3[2];
	
	private Color[] dotColors;
	
	private bool lineObjectVectorlineActive = true;

	private Vector3 hidePosition;
	private int dotAmount;
#endif

	private bool selected = false;
	
	//If you call the instantiate function to create a clone of this prefab, the Awake function is called. The Start
	//function is not, so the init code has to be placed in Awake.
	void Awake(){

#if VECTROSITY

		if(selection){
			
			dotAmount = 3;
		}

		else{

			dotAmount = MarkerRecorder.maxDotAmount;
		}
		
		if(objectType == ObjectType.Dots){
		
			dotColors = new Color[dotAmount]; 
		}

		if(objectType == ObjectType.Dot1){

			dotColors = new Color[1]; 
		}

		if(objectType == ObjectType.X){
			
			linePoints[0] = new Vector3(-objectSize, 0.0f, 0.0f);
			linePoints[1] = new Vector3(objectSize, 0.0f, 0.0f);
			lineObjectVectorline = new VectorLine("LineObjectX", linePoints, Color.red, lineMaterial, lineWidth);
		}

		if(objectType == ObjectType.Y){
			
			linePoints[0] =  new Vector3(0.0f, -objectSize, 0.0f);
			linePoints[1] =  new Vector3(0.0f, objectSize, 0.0f);
			lineObjectVectorline = new VectorLine("LineObjectY", linePoints, Color.green, lineMaterial, lineWidth);
		}

		if(objectType == ObjectType.Z){
			
			linePoints[0] = new Vector3(0.0f, 0.0f, -objectSize);
			linePoints[1] = new Vector3(0.0f, 0.0f, objectSize);
			lineObjectVectorline = new VectorLine("LineObjectZ", linePoints, Color.blue, lineMaterial, lineWidth);
		}
		
		if(objectType == ObjectType.Line){

			linePoints[0] = Vector3.zero;
			linePoints[1] = Vector3.zero;
			lineObjectVectorline = new VectorLine("LineObjectLine", linePoints, MarkerRecorder.colorForSelected, lineMaterial, lineWidth);
		}
		
		if(objectType == ObjectType.Dot1){
			
			vectorPoints = new Vector3[1]; 
			vectorPoints[0] = Vector3.zero;
			
			lineObjectVectorpoints = new VectorPoints("Dot1PointObject", vectorPoints, MarkerRecorder.colorForTracking, lineMaterial, dotSize);

			//The vector needs to be de-activated this way because if the game object where the vector is 
			//attached to is de-activated, the vectors will still be shown.
			lineObjectVectorpoints.active = false;
		}
		
		//We have to draw the SLAM dots manually and can't use ObjectSetup due to some limitations.
		if(objectType == ObjectType.Dots){
			
			string name;

			if(selection){

				name = "DotsSelectionObject";
				dotSize *= 1.5f;
			}

			else{

				name = "DotsPointObject";
			}

			vectorPoints = new Vector3[dotAmount]; 

			for(int i = 0; i < vectorPoints.Length; i++){

				vectorPoints[i] = gameObject.transform.InverseTransformPoint(Camera.main.transform.position);
			}

			lineObjectVectorpoints = new VectorPoints(name, vectorPoints, MarkerRecorder.colorForTracking, lineMaterial, dotSize);
		}
		
		if((objectType != ObjectType.Dot1) && (objectType != ObjectType.Dots)){
			
			VectorManager.ObjectSetup(gameObject, lineObjectVectorline, Visibility.Dynamic, Brightness.None); //Visibility.Always // Visibility.Dynamic

			//The vector needs to be de-activated this way because if the game object where the vector is 
			//attached to is de-activated, the vectors will still be shown.
			lineObjectVectorline.active = false;	
		}
		
		VectorManager.useDraw3D = true;
		VectorLine.SetCamera3D(Camera.main);			

		if(objectType == ObjectType.X){
			gameObject.transform.localScale = new Vector3(lineLength, 0.1f, 0.1f);
		}

		if(objectType == ObjectType.Y){
			gameObject.transform.localScale = new Vector3(0.1f, lineLength, 0.1f);
		}

		if(objectType == ObjectType.Z){
			gameObject.transform.localScale = new Vector3(0.1f, 0.1f, lineLength);
		}		

#endif
	}

	void Update(){
		
#if VECTROSITY		

		if(!lineObjectVectorlineActive){
			
			//Is the game object marked as invisible but the object itself invisible? If so, do not make the 
			//object visible. This is a workaround for the fact that if the game object becomes active, that it
			//enables the vector lines as well, even though the vector lines are marked as inactive.
			if((objectType != ObjectType.Dot1) && (objectType != ObjectType.Dots)){
				
				lineObjectVectorline.active = false; 
			}
			
			else{
				
				lineObjectVectorpoints.active = false; 
			}
		}
		
		else{
		
			if((objectType == ObjectType.Dots) && (gameObject.activeInHierarchy)){
			
				//TODO: change to normal draw. Draw3d is only used for debugging.
				lineObjectVectorpoints.Draw(gameObject.transform);
			}
		}

		hidePosition = gameObject.transform.InverseTransformPoint(Camera.main.transform.position);
#endif	
	}



	public void ChangeLineSelection(bool select) {

		if(select){
#if VECTROSITY
			lineObjectVectorline.lineWidth = lineWidth * selectSizeFactor;
#endif
			selected = true;
		}
		
		else{
#if VECTROSITY
			lineObjectVectorline.lineWidth = lineWidth;
#endif
			selected = false;
		}
	}

	public void SetDotsSelectionColor(int index, Color color){
		
#if VECTROSITY
		//change the color of the dot
		dotColors[index] = color;			
		lineObjectVectorpoints.SetColors(dotColors);
#endif
	}
	
	//for the entire object, not compatible if object
	//has individual selectable items.
	public bool IsSelected(){
	
		return selected;
	}
	
	public void SetLinePoints(Vector3 linePoint0, Vector3 linePoint1){
#if VECTROSITY
		linePoints[0] = linePoint0;
		linePoints[1] = linePoint1;		
#endif
	}
	
	//For OBJ
	public void SetDot1PointLocation(Vector3 dotPoint){
#if VECTROSITY
		vectorPoints[0] = dotPoint;
#endif
	}


	public void SetDotsPositions(Vector3[] positions){
		
#if VECTROSITY
		//is the array available?
		if(positions != null){

			//TODO: find out if the array reference can be passed instead.
			Array.Copy(positions, vectorPoints, positions.Length);
		}
#endif
		
	}


	//Disable or enable line drawing. Note that only the global flag is set here. This is due to the fact that
	//Vectrosity itself might decide to enable the lines when it should not, so we have to force it to be disabled again.
	//For points also.
	public void EnableVectorLine(bool enable){
#if VECTROSITY	
		if(enable){
		
			lineObjectVectorlineActive = true;
			
			if(objectType == ObjectType.Dots){

				lineObjectVectorpoints.active = true; 

			}
		}
		
		else{
		
			lineObjectVectorlineActive = false;
		}
#endif
	}
	

	public void ResetDotsSelections(){
	
#if VECTROSITY
		//set the colors
		for(int i = 0; i < dotColors.Length; i++){
				
			dotColors[i] = MarkerRecorder.colorForTracking;
		}
#endif
		
#if VECTROSITY
		lineObjectVectorpoints.SetColors(dotColors);
#endif

		if(selection){

			for(int i = 0; i < 3; i++){
#if VECTROSITY
				vectorPoints[i] = hidePosition;
#endif
			}
		}
	}

	
	//Reformat the array so that the empty slots are at the end. 
	void ReformatSelectedDots(ref int[] selectedDots){
	
		//copy array
		int[] numberBuffer = new int[selectedDots.Length];
		Array.Copy(selectedDots, numberBuffer, selectedDots.Length);
		
		//first reset
		for(int e=0; e < selectedDots.Length; e++){
		
			selectedDots[e] = -1;
		}
		
		int index = 0;
		for(int e=0; e < selectedDots.Length; e++){
		
			if(numberBuffer[e] != -1){
				
				selectedDots[index] = numberBuffer[e]; 
				index++;					
			}
		}
	}
	
}



