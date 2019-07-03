using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_AutoTurnOff : MonoBehaviour {

	public float TurnOffAfter = 0.35f;

	// Use this for initialization
	void OnEnable () {
		Invoke("TurnOffMe", TurnOffAfter);
	}
	
	void TurnOffMe () {
		gameObject.SetActive(false);
	}
}
