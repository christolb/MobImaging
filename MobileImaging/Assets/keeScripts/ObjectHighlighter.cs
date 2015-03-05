using UnityEngine;
using System.Collections;

public class ObjectHighlighter : MonoBehaviour {
	
	bool isHighlighted = false;
	Material originalMaterial;
	Material redMaterial;
	MeshRenderer meshRenderer;
	
	GameObject baseObject;
	string obj_name;
	// Use this for initialization
	void Start () {
		
		obj_name        = this.gameObject.name;
		baseObject      = GameObject.Find( obj_name );
		meshRenderer        = baseObject.GetComponent<MeshRenderer>();
		originalMaterial    = meshRenderer.material;
		
		Color red           = new Color(255.0f,0.0f,0.0f, 0.5f);
		redMaterial         = new Material(Shader.Find("Transparent/Parallax Specular"));
		redMaterial.color   = red;
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	void OnMouseDown(){
		
		Debug.Log("OMD "+obj_name);
		isHighlighted = !isHighlighted;
		
		if( isHighlighted == true ){
			
			HighlightRed();
			
		}
		if ( isHighlighted==false ){
			
			RemoveHighlight();
		}
	}
	
	void HighlightRed(){
		
		meshRenderer.material = redMaterial;
		Debug.Log("IT SHOULD BE RED");
	}
	
	void RemoveHighlight(){
		
		meshRenderer.material = originalMaterial;
	}
}