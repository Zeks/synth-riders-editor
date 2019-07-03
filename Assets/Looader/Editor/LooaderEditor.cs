using System.Collections;
using UnityEditor;
using UnityEngine;

public class LooaderEditorWindow : EditorWindow {

	private static LooaderEditorWindow instance = null;

	[MenuItem("Tools/Looader/Create Looader Manager")]
	static void CreateLooaderSystem()
	{
		instance = Instantiate(Resources.Load<GameObject>("Looader Manager")).GetComponent<LooaderEditorWindow>();
	}

	[MenuItem("Tools/Looader/Create Trigger Object")]
	static void CreateTriggerObject()
	{
		instance = Instantiate(Resources.Load<GameObject>("Trigger Object")).GetComponent<LooaderEditorWindow>();
	}

	public static void OnCustomWindow()
	{
		EditorWindow.GetWindow(typeof(LooaderEditorWindow));
	}
}
