using UnityEngine;
using System.Collections;

public class meanPos : MonoBehaviour {
	public GameObject[] objects;
	Vector3 posSum;
	
	// Update is called once per frame
	void Update () {
		posSum = Vector3.zero;
		foreach (GameObject go in objects) {
			posSum += go.transform.position;
		}
		transform.position = posSum/objects.Length;
	}
}
