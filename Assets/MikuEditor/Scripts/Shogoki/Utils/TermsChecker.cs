using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shogoki.Utils {
	public class TermsChecker : MonoBehaviour {
		/// <summary>
        /// Game Object with the Terms text
        /// </summary>
        [SerializeField]
		GameObject m_TermsHolder;

		/// <summary>
        /// Dialog Animator
        /// </summary>
		[SerializeField]
        private Animator m_termsAnimator;

		/// <summary>
        /// Current App Version
        /// </summary>
		[SerializeField]
        private string editorVersion = "1.1-alpha.3";

		/// <summary>
        /// Current App Name
        /// </summary>
		[SerializeField]
        private string editorName = "SynthRiders Editor v{0}";

		private string _termVersionKeyy = "com.synth.miku.Terms";

		void Awake() {
			m_TermsHolder.SetActive(false);
		}

		// Use this for initialization
		void Start() {
			string lastTerms = PlayerPrefs.GetString(
            	this._termVersionKeyy,
				"null"
			);

			if(!lastTerms.Equals(editorVersion)) {
				m_TermsHolder.SetActive(true);
				m_termsAnimator.Play("Panel In");
			}
		}

		public void TermsAccepted(bool accepted) {
			if(!accepted) {
#if UNITY_EDITOR
				// Application.Quit() does not work in the editor so
				// UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
				UnityEditor.EditorApplication.isPlaying = false;
#else
				UnityEngine.Application.Quit();
#endif
			} else {

#if UNITY_EDITOR
				print("Therms Accepted but not saved, because is editor");
#else
				PlayerPrefs.SetString(
					this._termVersionKeyy,
					editorVersion
				);
				PlayerPrefs.Save();
				StartCoroutine(TermsOff());
#endif			

				m_termsAnimator.Play("Panel Out");
			}
		}

		IEnumerator TermsOff() {
			yield return new WaitForSeconds(1.5f);
			m_TermsHolder.SetActive(false);
		}
	}
}
