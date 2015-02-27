
//#define VECTROSITY

using UnityEngine;
using System.Collections;

#if VECTROSITY
using Vectrosity;
#endif

public class ObjectLineManager : MonoBehaviour {
	
	//NOTE: this should be the same as in MarkerRecorder.cs
	public enum ObjectType{
		PLANE,
		CUBE,
		CYLINDER,
		OBJ
	}	
	public ObjectType objectType;
	
	
	//Half the object width, length, and height, when scale is 1 and not a child of another object.
	//This variable is not used in this script, just fetched. It is used in MarkerRecorder.cs
	[HideInInspector]
	public Vector3 size;	

	public TextAsset vectorObject;
	public Material lineMaterial;
	
	//Do not set the scale using the object transform because that will loose it's original value
	//when when it is re-scaled
	public Vector3 scale;
	
#if VECTROSITY
	public VectorLine lineObjectVectorline;
	
	//size will expand later
	private Vector3[] linePoints = {new Vector3(0, 0, 0), new Vector3(0, 0, 0)};
	
	private float lineWidth = 3.0f;
	
	private float selectWidthFactor = 2.0f;
	private float expansion = 1.005f; //0.01f
	
	private Color[] colors;
#else
	private Color materialColor;
#endif
	
	public bool useMaterial;
	private bool selected = false;

	private bool init = false;
	private bool lineObjectVectorlineActive = true;

	//If you call the instantiate function to create a clone of this prefab, the Awake function is called. The Start
	//function is not, so the init code has to be placed in Awake.
	void Awake(){
				
		//get the size		
		size = GetObjectSize();
		
#if VECTROSITY

		if(objectType == ObjectType.OBJ){
		
			lineWidth = 5.0f;
		}
		
		if(objectType != ObjectType.OBJ){
			
			linePoints = VectorLine.BytesToVector3Array(vectorObject.bytes);
		}		
		
		lineObjectVectorline = new VectorLine("LineObject", linePoints, MarkerRecorder.colorForObject, lineMaterial, lineWidth);
		
		if(objectType != ObjectType.OBJ){
			
			ExpandLines(linePoints, expansion);
		}
			
		VectorLine.SetCamera3D(Camera.main);
		VectorManager.useDraw3D = true;		
		
		VectorManager.ObjectSetup(gameObject, lineObjectVectorline, Visibility.Dynamic, Brightness.None, !useMaterial); //Visibility.Always // Visibility.Dynamic
		
		//For obj, the line segment number doesn't exist here yet, so init the color array later
		if(objectType != ObjectType.OBJ){
		
			//get the amount of segments
			int segments = lineObjectVectorline.GetSegmentNumber();
			
			//initialize the array 
			colors = new Color[segments];
		}
		
		//The vector needs to be de-activated this way because if the game object where the vector is 
		//attached to is de-activated, the vectors will still be shown. This is the effect of a Unity3d
		//bug or "feature" (to do with OnBecameVisible). Anyway, adding this line here solves the problem.
		lineObjectVectorline.active = false; 
#else
		//store the material alpha
		materialColor = gameObject.renderer.material.color;
#endif
	}
	
	void Update(){
		
		//The line wireframe cannot be generated right after the object is instantiated. For some reason that
		//doesn't work. So init it at a later stage.
		if((objectType == ObjectType.OBJ) && (!init)){
			
			//get the size		
			size = GetObjectSize();
		
			GenerateWireframe(gameObject.GetComponent<MeshFilter>().mesh);
			
			//get the color array
#if VECTROSITY
			int segments = lineObjectVectorline.GetSegmentNumber();
			colors = new Color[segments];
#endif
			
			
			init = true;
		}
		
		//Is the game object marked as invisible but the object itself invisible? If so, do not make the 
		//object visible. This is a workaround for the fact that if the game object becomes active, that it
		//enables the vector lines as well, even though the vector lines are marked as inactive.
		if(!lineObjectVectorlineActive){
#if VECTROSITY
			lineObjectVectorline.active = false; 
#endif
		}
	}
	
	
	//Modify the linePoints so it scales up. This is to prevent the lines from touching the object it is attached 
	//to which creates visual artifacts.
	void ExpandLines(Vector3[] linePoints, float expansion){

		for(int i = 0; i < linePoints.Length; i++){
			linePoints[i] *= expansion;
		}
	}

	//Instead of using a predetermined vector line layout, use the object mesh for this instead.
	//Only use this function on simple a mesh for performance reasons.
	public void GenerateWireframe(Mesh mesh){
		
		//If OBJ object is set, use a mesh to generate the lines
		if(objectType == ObjectType.OBJ){
			
#if VECTROSITY
			lineObjectVectorline.MakeWireframe(mesh);
			ExpandLines(linePoints, expansion);
#endif
			
		}
	}
	
	
	//Change the line size of the entire object
	public void ChangeSelection(bool select, Color color) {

		if(select){
#if VECTROSITY
			lineObjectVectorline.lineWidth = lineWidth * selectWidthFactor;
#else
			gameObject.renderer.material.color = new Color(color.r, color.g, color.b, 255);
#endif
			selected = true;
		}
		
		else{
#if VECTROSITY
			lineObjectVectorline.lineWidth = lineWidth;
#else
			gameObject.renderer.material.color = new Color(color.r, color.g, color.b, materialColor.a);
#endif
			selected = false;
		}
	}
	
	public bool isSelected(){
		
		return selected;
	}
	
	
	//Change the color of the entire object
	public void ChangeColorAllLines(Color color){
#if VECTROSITY
		lineObjectVectorline.SetColor(color);
#else
		if(!selected){
			
			gameObject.renderer.material.color = new Color(color.r, color.g, color.b, materialColor.a);
		}
		
		else{
			
			gameObject.renderer.material.color = new Color(color.r, color.g, color.b, 255);
		}
#endif
	}	
	
	//Change the color of only one line. For debugging only.
	public void SetColorOneLine(Color color, int lineIndex){		
	
#if VECTROSITY
		colors[lineIndex] = color;
		lineObjectVectorline.SetColors(colors);
#endif
	}
	

	//change the color of a specific line segment
	//PLANE: 
	//0 = left
	//1 = backward
	//2 = forward
	//3 = right
	//
	//CUBE:
	//0 = bottom plane forward
	//1 = left plane forward
	//2 = right plane forward
	//3 = top plane forward
	//4 = top plane left
	//5 = top plane right
	//6 = top plane backward
	//7 = left plane backward
	//8 = right plane backward
	//9 = bottom plane backward
	//10 = bottom plane left
	//11 = bottom plane right
	public void SetLineSegmentColor(Color oldColor, Color newColor, MarkerRecorder.ConstraintClass constraintClass){
#if VECTROSITY
		//this is not valid for OBJ
		if(objectType == ObjectType.OBJ){
			
			return;
		}
		
		//first fill the array with the normal color
		for(int i=0; i < colors.Length; i++){
		
			colors[i] = oldColor;
		}
		
		if(objectType == ObjectType.CUBE){
			
			//now change the color of a few lines
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.right)){
			
				colors[2] = newColor;
				colors[8] = newColor;
				colors[11] = newColor;
				colors[5] = newColor;
			}	

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.left)){
			
				colors[1] = newColor;
				colors[7] = newColor;
				colors[10] = newColor;
				colors[4] = newColor;
			}

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.forward)){
			
				colors[1] = newColor;
				colors[2] = newColor;
				colors[0] = newColor;
				colors[3] = newColor;
			}		
	
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.backward)){
			
				colors[7] = newColor;
				colors[8] = newColor;
				colors[9] = newColor;
				colors[6] = newColor;
			}

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.up)){
			
				colors[3] = newColor;
				colors[4] = newColor;
				colors[5] = newColor;
				colors[6] = newColor;
			}

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.down)){
			
				colors[0] = newColor;
				colors[9] = newColor;
				colors[10] = newColor;
				colors[11] = newColor;
			}
		}
		
		if(objectType == ObjectType.PLANE){
		
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.right)){
			
				colors[16] = newColor;
				colors[20] = newColor;
				colors[23] = newColor;
			}	

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.left)){
			
				colors[6] = newColor;
				colors[9] = newColor;
				colors[19] = newColor;
			}
	
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.forward)){
			
				colors[0] = newColor;
				colors[4] = newColor;
				colors[22] = newColor;
			}		

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.backward)){
			
				colors[11] = newColor;
				colors[18] = newColor;
				colors[21] = newColor;
			}
		}
		
		if(objectType == ObjectType.CYLINDER){
			
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.side)){
			
				colors[0] = newColor;
				colors[2] = newColor;
				colors[5] = newColor;
				colors[8] = newColor;
				colors[11] = newColor;
				colors[14] = newColor;
				colors[17] = newColor;
				colors[20] = newColor;
				colors[23] = newColor;
				colors[26] = newColor;
				colors[28] = newColor;
				colors[30] = newColor;
				colors[33] = newColor;
				colors[36] = newColor;
				colors[39] = newColor;
				colors[42] = newColor;
				colors[45] = newColor;
				colors[48] = newColor;
				colors[51] = newColor;
				colors[54] = newColor;
			}
			
			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.up)){
				
				//top ring
				colors[3] = newColor;
				colors[6] = newColor;
				colors[9] = newColor;
				colors[12] = newColor;
				colors[15] = newColor;
				colors[18] = newColor;
				colors[21] = newColor;
				colors[24] = newColor;
				colors[27] = newColor;
				colors[31] = newColor;
				colors[34] = newColor;
				colors[37] = newColor;
				colors[40] = newColor;
				colors[43] = newColor;
				colors[46] = newColor;
				colors[49] = newColor;
				colors[52] = newColor;
				colors[55] = newColor;
				colors[57] = newColor;
				colors[59] = newColor;
				
				//top fan
				colors[80] = newColor;
				colors[81] = newColor;
				colors[82] = newColor;
				colors[83] = newColor;
				colors[84] = newColor;
				colors[85] = newColor;
				colors[86] = newColor;
				colors[87] = newColor;
				colors[88] = newColor;
				colors[89] = newColor;
				colors[90] = newColor;
				colors[91] = newColor;
				colors[92] = newColor;
				colors[93] = newColor;
				colors[94] = newColor;
				colors[95] = newColor;
				colors[96] = newColor;
				colors[97] = newColor;
				colors[98] = newColor;
				colors[99] = newColor;
			}

			if(constraintClass.IsSet(MarkerRecorder.ConstraintSide.down)){
				
				//bottom ring
				colors[1] = newColor;
				colors[4] = newColor;
				colors[7] = newColor;
				colors[10] = newColor;
				colors[13] = newColor;
				colors[16] = newColor;
				colors[19] = newColor;
				colors[22] = newColor;
				colors[25] = newColor;
				colors[29] = newColor;
				colors[32] = newColor;
				colors[35] = newColor;
				colors[38] = newColor;
				colors[41] = newColor;
				colors[44] = newColor;
				colors[47] = newColor;
				colors[50] = newColor;
				colors[53] = newColor;
				colors[56] = newColor;
				colors[58] = newColor;
				
				//bottom fan
				colors[60] = newColor;
				colors[61] = newColor;
				colors[62] = newColor;
				colors[63] = newColor;
				colors[64] = newColor;
				colors[65] = newColor;
				colors[66] = newColor;
				colors[67] = newColor;
				colors[68] = newColor;
				colors[69] = newColor;
				colors[70] = newColor;
				colors[71] = newColor;
				colors[72] = newColor;
				colors[73] = newColor;
				colors[74] = newColor;
				colors[75] = newColor;
				colors[76] = newColor;
				colors[77] = newColor;
				colors[78] = newColor;
				colors[79] = newColor;
			}
		}
		
		lineObjectVectorline.SetColors(colors);
#endif
	}
	

	
	//Disable or enable line drawing. Note that only the global flag is set here. This is due to the fact that
	//Vectrosity itself might decide to enable the lines when it should not, so we have to force it to be disabled again.
	public void EnableVectorLine(bool enable){
		
		if(enable){
			lineObjectVectorlineActive = true;
		}
		
		else{
			lineObjectVectorlineActive = false;
		}
	}
	

	public void SetVerticesAndColors(Vector3[] vertices, Color baseColor, Color selectedColor){

		Mesh mesh = new Mesh();
		
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

		//build vertices
		mesh.vertices = vertices;

		//Build triangles
		mesh.triangles = new int[6];

		int[] triangles = mesh.triangles;
		
		//front
		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		
		//back
		triangles[3] = 0;
		triangles[4] = 2;
		triangles[5] = 1;

		mesh.triangles = triangles;

		mesh.normals = new Vector3[mesh.vertices.Length];
		mesh.RecalculateNormals();

		meshFilter.mesh = mesh;
		
		gameObject.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
		gameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
		
#if VECTROSITY
		//set the colors
		//first fill the array with the normal color
		for(int i=0; i < colors.Length; i++){
		
			colors[i] = baseColor;
		}
		
		//set the color of the segment
		colors[0] = selectedColor;		
		
		lineObjectVectorline.SetColors(colors);
#endif
	
		//we have to re-init the object if the mesh has changed
		init = false;
	}
	
	private Vector3 GetObjectSize(){
		
		Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;	
		Vector3 size = new Vector3(bounds.size.x / 2.0f, bounds.size.y / 2.0f, bounds.size.z / 2.0f);
		
		return size;
	}
}
