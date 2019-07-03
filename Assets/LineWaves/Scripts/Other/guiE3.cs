using UnityEngine;
using System.Collections;

public class guiE3 : MonoBehaviour {

    public GameObject waveSphere;

	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        waveSphere.GetComponent<LineWaveCollider>().walkAuto = GUI.HorizontalSlider(new Rect(Screen.width * 0.4f, Screen.height - 20, Screen.width * 0.2f, 10), waveSphere.GetComponent<LineWaveCollider>().walkAuto, -7, 7);
    }
}
