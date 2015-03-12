#pragma strict

function Start () {

}

function Update () {
}

function OnCollisionEnter(collision : Collision) {
    // can put sound effect, particle effect
    Debug.Log("collision!");
}

function OnTriggerEnter (other : Collider) {
    Debug.Log("trigger enter");
    if (other.gameObject.CompareTag("hole")) {
        Destroy(gameObject);
    }
}