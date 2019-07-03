using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using TMPro;
using UnityEngine;

namespace Shogoki.Utils {
    public class BeforeQuit : MonoBehaviour {
		
        /// <summary>
        /// Flag to know if the app can be quitted
        /// </summar>
        private bool canQuit = false;
        
        [Header("Dialog Elements")]
        /// <summary>
        /// GameObject of the popup promt to show
        /// </summary>
        [SerializeField]
	    private GameObject m_DialogObject;

        /// <summary>
        /// If the GameObject had a animation, pass the animator to use
        /// </summary>
        [SerializeField]
        private Animator m_DialogAnimator;
        
        /// <summary>
        /// Animation to show when the propmt is visible
        /// </summary>
        [SerializeField]
        private string m_DialogAnimationName = "Panel In";

        /// <summary>
        /// Text field for the promt message
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI m_DialogTextField;

        void Start() {
            /// Hide the dialog if is visible
            m_DialogObject.SetActive(false);
        }


        void OnApplicationQuit() {
            /// If the app can't be quitted, show a popup promt
            if (!canQuit) {
                Application.CancelQuit();
                ShowDialog();
            }               
            
        }

        /// Show the popup promt
        private void ShowDialog() 
        {

            m_DialogTextField.SetText(
                StringVault.Promt_ExitApp +(
					Track.NeedSaveAction() 
					?
					"\n" +
					StringVault.Promt_NotSaveChanges 
					:
					""
				)
            );

            // First disable dialog to reset animations
            m_DialogObject.SetActive(false);

            // Enable dialog to play animations
            StartCoroutine(EnableDialogWindow());
        }

        // To give enoungh time for the animation to run correctly
        IEnumerator EnableDialogWindow() {
            yield return new WaitForEndOfFrame();

            m_DialogObject.SetActive(true);

            if(m_DialogAnimator != null) {
                m_DialogAnimator.Play(m_DialogAnimationName);
            }
        }

        /// <summary>
        /// Quit action to be used when the used accept the promt
        /// </summary>
        public void DoQuitAction() {
            canQuit = true;
            Application.Quit();
        }
	}
}