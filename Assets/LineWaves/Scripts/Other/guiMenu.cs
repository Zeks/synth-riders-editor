using UnityEngine;
using System.Collections;

public class guiMenu : MonoBehaviour {
	public GameObject sphere1;
	public GameObject sphere2;
	public GameObject sphere3;
	public GameObject cam;
	public GUIStyle tgStyle;
	
	GameObject activeSphere;
	float guiWidth = 200;
	float guiHeight = 150;
	string[] tbContent = {"Red", "Green", "Blue"};
	int tgInt;
	
	void OnGUI(){	
		tgInt = GUI.Toolbar(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+20, guiWidth-30, 30), tgInt, tbContent);
		switch (tgInt) {
		case 0:
			sphere1.GetComponent<LineWave>().freq = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+40, Screen.height-guiHeight+65, guiWidth-70, 20), sphere1.GetComponent<LineWave>().freq, 0, 20);
			sphere1.GetComponent<LineWave>().warpRandom = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+30, Screen.height-guiHeight+85, guiWidth-60, 20), sphere1.GetComponent<LineWave>().warpRandom, 0, 20);
			sphere1.GetComponent<LineWave>().walkAuto = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+50, Screen.height-guiHeight+105, guiWidth-80, 20), sphere1.GetComponent<LineWave>().walkAuto, -80, 80);
			sphere1.GetComponent<LineWave>().amp = GUI.VerticalSlider(new Rect(Screen.width-20, Screen.height-guiHeight+20, 20, guiHeight-30), sphere1.GetComponent<LineWave>().amp, 15, 0);
			sphere1.GetComponent<LineWave>().spiral = GUI.Toggle(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+120, guiWidth/3, 20), sphere1.GetComponent<LineWave>().spiral, "Spiral");
			activeSphere = sphere1;
			break;
		case 1:
			sphere2.GetComponent<LineWave>().freq = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+40, Screen.height-guiHeight+65, guiWidth-70, 20), sphere2.GetComponent<LineWave>().freq, 0, 20);
			sphere2.GetComponent<LineWave>().warpRandom = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+30, Screen.height-guiHeight+85, guiWidth-60, 20), sphere2.GetComponent<LineWave>().warpRandom, 0, 20);
			sphere2.GetComponent<LineWave>().walkAuto = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+50, Screen.height-guiHeight+105, guiWidth-80, 20), sphere2.GetComponent<LineWave>().walkAuto, -80, 80);
			sphere2.GetComponent<LineWave>().amp = GUI.VerticalSlider(new Rect(Screen.width-20, Screen.height-guiHeight+20, 20, guiHeight-30), sphere2.GetComponent<LineWave>().amp, 15, 0);
			sphere2.GetComponent<LineWave>().spiral = GUI.Toggle(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+120, guiWidth/3, 20), sphere2.GetComponent<LineWave>().spiral, "Spiral");
			activeSphere = sphere2;
			break;
		case 2:
			sphere3.GetComponent<LineWave>().freq = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+40, Screen.height-guiHeight+65, guiWidth-70, 20), sphere3.GetComponent<LineWave>().freq, 0, 20);
			sphere3.GetComponent<LineWave>().warpRandom = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+30, Screen.height-guiHeight+85, guiWidth-60, 20), sphere3.GetComponent<LineWave>().warpRandom, 0, 20);
			sphere3.GetComponent<LineWave>().walkAuto = GUI.HorizontalSlider(new Rect(Screen.width-guiWidth+50, Screen.height-guiHeight+105, guiWidth-80, 20), sphere3.GetComponent<LineWave>().walkAuto, -80, 80);
			sphere3.GetComponent<LineWave>().amp = GUI.VerticalSlider(new Rect(Screen.width-20, Screen.height-guiHeight+20, 20, guiHeight-30), sphere3.GetComponent<LineWave>().amp, 15, 0);
			sphere3.GetComponent<LineWave>().spiral = GUI.Toggle(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+120, guiWidth/3, 20), sphere3.GetComponent<LineWave>().spiral, "Spiral");
			activeSphere = sphere3;
			break;
		}
		
		GUI.Label(new Rect(Screen.width*0.44f, Screen.height-35, 150, 20), "  Mouse1: CamOrbit");
		GUI.Label(new Rect(Screen.width*0.44f, Screen.height-20, 150, 20), "Mouse2: MoveSpheres");
		GUI.Label(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+60, 50, 20), "Freq.");
		GUI.Label(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+60, 50, 20), "Freq.");
		GUI.Label(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+80, 50, 20), "Var.");
		GUI.Label(new Rect(Screen.width-guiWidth, Screen.height-guiHeight+100, 50, 20), "Speed.");
		GUI.Label(new Rect(Screen.width-guiWidth+140, Screen.height-guiHeight+120, 50, 20), "Amp.");
		
	}
	
	void Update() {
		if (Input.GetMouseButton(1)) {
			activeSphere.transform.Translate(Input.GetAxis("Mouse X")/2, Input.GetAxis("Mouse Y")/2, 0, cam.transform);
		}	
	}
}
