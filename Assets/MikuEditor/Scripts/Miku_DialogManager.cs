using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Miku_DialogManager : MonoBehaviour {

	private static Miku_DialogManager s_instance;

	public enum DialogType {
		Info,
		Alert
	}

	[Header("Objects References")]
	public GameObject m_DialogObject;

	public Material m_InfoColor;

	public Material m_AlertColor;

	[Header("Dialog Elements")]
	public TextMeshProUGUI m_DialogMessage;

	public Animator m_DiagAnimator;

	public Image m_DialogBG;
	private float prevSpeed;
	bool prevetOut = false;
	WaitForSeconds lateOutWait;
	WaitForSeconds waitForRead;

	// Use this for initialization
	void Start () {
		m_DialogObject.SetActive(false);
		lateOutWait = new WaitForSeconds(0.5f);
		waitForRead = new WaitForSeconds(5f);

		s_instance = this;
	}
	
	public static void ShowDialog(DialogType type, string message, bool preventOut = false) {
		if(s_instance == null) return;

		// First disable dialog to reset animations
		s_instance.m_DialogObject.SetActive(false);

		// Set dialog data
		// TODO
		// Not workin on 2018, save for later
		/* Material selectedSprite = (type == DialogType.Info) ? s_instance.m_InfoColor : s_instance.m_AlertColor;
		s_instance.m_DialogBG.material = selectedSprite; */

		s_instance.m_DialogMessage.SetText(message);

		s_instance.prevSpeed = s_instance.m_DiagAnimator.speed;
		s_instance.prevetOut = preventOut;
		// Enable dialog to play animations
		s_instance.StartCoroutine(s_instance.EnableDialogWindow());
		
	}

	// To give enoungh time for the animation to run correctly
	IEnumerator EnableDialogWindow() {
		yield return null;

		s_instance.m_DialogObject.SetActive(true);
		if(prevetOut) {
			StartCoroutine(LateOut());
		}
	}

	IEnumerator LateOut() {
		yield return lateOutWait;
		m_DiagAnimator.speed = 0;

		yield return waitForRead;
		m_DiagAnimator.speed = prevSpeed;
	}
}
