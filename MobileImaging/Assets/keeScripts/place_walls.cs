using UnityEngine;
using System.Collections;

public class place_walls : MonoBehaviour {
	public GameObject ar_camera;
	public GameObject[] ar_objects;
	public GameObject[] scene_objects;
	private Vector3[,] relative_pos;
	private Quaternion[,] relative_rot;

	void Start () {
		int num_ar = ar_objects.Length;
		relative_pos = new Vector3[num_ar, num_ar];
		relative_rot = new Quaternion[num_ar, num_ar];
		for (int i = 0; i < num_ar; i++) {
			for (int j = 0; j < num_ar; j++) {
				relative_pos[i, j] = Vector3.one * 5000;
				relative_rot[i, j] = Quaternion.identity;
			}
		}
		for (int i = 0; i < num_ar; i++) {
			scene_objects[i].transform.position = Vector3.one * 5000;
		}
	}

	int debug_count = 0;
	void Update () {
		// always put oth object in world center as reference
		scene_objects[0].transform.position = Vector3.zero;
		scene_objects[0].transform.rotation = Quaternion.identity;

		// loop through enabled (whose marker is found) ar objects
		// with respect to other ar objects
		// compare their relative position and rotation and save them
		for (int i = 0; i < ar_objects.Length; i++) {
			GameObject obji = ar_objects[i];
            if (debug_count >= 60) {
                //Debug.Log(obji.transform.rotation);
            }
			bool enabledi = obji.transform.GetChild(0).GetComponent<MeshRenderer>().enabled;
			for (int j = i+1; j < ar_objects.Length; j++) {
				GameObject objj = ar_objects[j];
				bool enabledj = objj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled;
				if (enabledi && enabledj) {
					// ith row has relative value of i w.r.t. all j
					// so if i needed, [i][j] could be added / multiplied to j to get i
					// Vector3 relative_world_pos = obji.transform.position - objj.transform.position;
					// InverseTransformDirection :
                    // Transforms a direction from world space to local space.
                    // relative_pos[i, j] = objj.transform.InverseTransformDirection(relative_world_pos);
					relative_pos[i, j] = obji.transform.position - objj.transform.position;;
					// The relative orientation is obtained simply by division:
                    // q = q0 / q1 = q0 * inverse(q1)
					relative_rot[i, j] = obji.transform.rotation
                            * Quaternion.Inverse(objj.transform.rotation);
				}
			}
		}

		// since only upper right triangle of the relative value is filled
		// fill out the lower left one with negative val
		for (int i = 0; i < ar_objects.Length; i++) {
			for (int j = 0; j < i; j++) {
				relative_pos[i, j] = -relative_pos[j, i];
				relative_rot[i, j] = Quaternion.Inverse(relative_rot[j, i]);
			}
		}

		// loop through game scene obejcts
		for (int i = 1; i < scene_objects.Length; i++) {
			GameObject obji = scene_objects[i];
			Vector3 result_pos = Vector3.zero;
			Quaternion result_rot = Quaternion.identity;
			int add_count = 0;
			// first slerp should go all the way away from identity
			// that means: slerp factor t = 1 / slerp_count = 1;
			int slerp_count = 1;
			for (int j = 0; j < scene_objects.Length; j++) {
				if (i == j) continue;
				// if object for referencing was never placed, pass
				GameObject objj = scene_objects[j];
				if (objj.transform.position.x >= 5000) {
					if (debug_count >= 60) {
						//Debug.Log("object " + j + " not placed, not using "
                        //        + j + " for relative position");
					}
					continue;
				}
				// first save relative rotations, add them to het average later
				// if relative position was not get, pass
				Vector3 rel_pos = relative_pos[i, j];
				if (Mathf.Abs(rel_pos.x) >= 5000) {
					if (debug_count >= 60) {
						//Debug.Log("relative position not get: " + i + ", " + j);
					}
					continue;
				}
				// for i, add relative position [i, j] and j
				// [!] applying local rotation of j for relative position
				result_pos += objj.transform.position + objj.transform.InverseTransformDirection(rel_pos);
				add_count++;

                // slerp between rotations
                // 1/2 for first two, 1/3 for the result and third one and so on...
				Quaternion rel_rot = relative_rot[i, j];
				Quaternion temp_result_rot = rel_rot * objj.transform.rotation;
				float slerp_t = 1 / (float)slerp_count;
				result_rot = Quaternion.Slerp(result_rot, temp_result_rot, slerp_t);
				slerp_count++;
			}

			if (add_count > 0) {
				// average the current averaged position with previous position
				result_pos /= (float)add_count;
				obji.transform.position += result_pos;
				obji.transform.position = obji.transform.position * 0.5f;
			}

            if (slerp_count > 1) {
                Quaternion new_rot = Quaternion.Slerp(obji.transform.localRotation, result_rot, 0.5f);
                obji.transform.localRotation = new_rot;
            }
		}

		if (debug_count >= 60) {
			debug_count = 0;
		} else {
			debug_count++;
		}
	}
}
