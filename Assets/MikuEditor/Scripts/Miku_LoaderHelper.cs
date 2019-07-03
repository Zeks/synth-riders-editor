using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miku_LoaderHelper : MonoBehaviour {

	public static Miku_LoaderHelper s_instance;

	public string m_PreloadPrefab = "Stock_Style";
	public string m_SceneToLoad = "Editor";

	// Use this for initialization
	void Start () {
		s_instance = this;
	}
	
	public static void LauchPreloader() {
		LO_LoadingScreen.prefabName = s_instance.m_PreloadPrefab;
		LO_LoadingScreen.LoadScene(s_instance.m_SceneToLoad);
	}
}
