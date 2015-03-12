using UnityEngine;
using System.Collections;

public class wall_setup : MonoBehaviour {
  public GameObject ar_camera;
  public GameObject[] ar_objects;
  public GameObject[] scene_objects;
  private Vector3[,] relative_pos;
  private Quaternion[,] relative_rot;
  private int [,] rel_rec;

  void Start () {
    int num_ar = ar_objects.Length;
    relative_pos = new Vector3[num_ar, num_ar];
    relative_rot = new Quaternion[num_ar, num_ar];
    rel_rec = new int[num_ar, num_ar];
    for (int i = 0; i < num_ar; i++) {
      for (int j = 0; j < num_ar; j++) {
        relative_pos[i, j] = Vector3.zero;
        relative_rot[i, j] = Quaternion.identity;
        rel_rec[i, j] = 0;
      }
    }
    for (int i = 0; i < num_ar; i++) {
      scene_objects[i].transform.position = Vector3.zero;
      scene_objects[i].transform.rotation = Quaternion.identity;
    }
  }

  void Update () {
    // loop through enabled (whose marker is found) ar objects
    // with respect to other ar objects
    // compare their relative position and rotation and save them
    for (int i = 0; i < ar_objects.Length; i++) {
      GameObject obji = ar_objects[i];
      // whether it's enabled is checked by looking into mesh renderer (Vuforia does it)
      bool enabledi = obji.transform.GetChild(0).GetComponent<MeshRenderer>().enabled;
      for (int j = i+1; j < ar_objects.Length; j++) {
        if (!enabledi) {
          rel_rec[i, j] = 0;
          continue;
        }
        GameObject objj = ar_objects[j];
        bool enabledj = objj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled;
        if (enabledi && enabledj) {
          // ith row has relative value of i w.r.t. all j
          // so if i needed, [i][j] could be added / multiplied to j to get i
          relative_pos[i, j] = obji.transform.position - objj.transform.position;
          // The relative orientation is obtained simply by division:
          // q = q0 / q1 = q0 * inverse(q1)
          relative_rot[i, j] = obji.transform.rotation * Quaternion.Inverse(objj.transform.rotation);
          rel_rec[i, j] = 1;
        }
      }
    }

    for (int i = 0; i < ar_objects.Length; i++) {
      for (int j = 0; j < i; j++) {
        relative_pos[i, j] = -relative_pos[j, i];
        relative_rot[i, j] = Quaternion.Inverse(relative_rot[j, i]);
        if (rel_rec[j, i] > 0) rel_rec[i, j] = 1;
      }
    }

    // always put 0th object in world center as reference
    scene_objects[0].transform.position = Vector3.zero;
    scene_objects[0].transform.rotation = Quaternion.identity;

    // loop through game scene obejcts
    // also only the left corner
    // nth object will reference: 1st ... (n-1)th objects
    for (int i = 1; i < scene_objects.Length; i++) {
      GameObject obji = scene_objects[i];
      Vector3 result_pos = Vector3.zero;
      Quaternion result_rot = Quaternion.identity;
      int add_count = 0;
      // first slerp should go all the way away from identity
      // that means: slerp factor t = (1 / slerp_count) = 1
      int slerp_count = 1;
      for (int j = 0; j < scene_objects.Length; j++) {
        if (rel_rec[i, j] > 0) {
          GameObject objj = scene_objects[j];
          Vector3 rel_pos = relative_pos[i, j];
          // InverseTransformDirection : Transforms a direction from world space to local space.
          // result_pos += objj.transform.position + objj.transform.InverseTransformDirection(rel_pos);
          result_pos += objj.transform.position + rel_pos;
          add_count++;
          // slerp between rotations
          // 1/2 for first two, 1/3 for the result and third one and so on...
          Quaternion rel_rot = relative_rot[i, j];
          Quaternion temp_result_rot = rel_rot * objj.transform.rotation;
          float slerp_t = 1 / (float)slerp_count;
          result_rot = Quaternion.Slerp(result_rot, temp_result_rot, slerp_t);
          slerp_count++;
        }
      }

      if (add_count > 0) {
        // lowpass the new averaged position with previous position
        result_pos /= (float)add_count;
        obji.transform.position += result_pos;
        obji.transform.position = obji.transform.position * 0.5f;
      }

      if (slerp_count > 1) {
        // slerp with prev position to smoothen the jitter
        Quaternion new_rot = Quaternion.Slerp(obji.transform.localRotation, result_rot, 0.5f);
        obji.transform.rotation = new_rot;
      }
    }
  }
}
