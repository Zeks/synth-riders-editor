using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shogoki.Utils {
    [RequireComponent(typeof(EventTrigger))]
    public class ButtonDownHelper : MonoBehaviour {
		
        /// <summary>
        /// Seconds between every button down action
        /// </summary>
        [SerializeField]
        private float buttonDownHoldDelta = 0.5f;

        /// <summary>
        /// How many seconds must to pass to do the Button Down Action
        /// </summary>
		private float nextButtonHold = 0.5f;

        /// <summary>
        /// How many seconds the button has been holdDown
        /// </summary>
		private float buttonHoldTime = 0;

        /// <summary>
        /// Is the button beign hold Down?
        /// </summary>
		private bool buttonIsDown = false;

        /// <summary>
        /// The Button Object to get the click action, if null it will be try to get the component
        /// </summary>
        [SerializeField]
        Button m_TargetButton;

        Button.ButtonClickedEvent clickEvent;
        /// TODO
        /// Add PointerDown and Pointer Up automatically
        void Start() {
            if(m_TargetButton == null) {
                m_TargetButton = gameObject.GetComponent<Button>();
            }

            
            if(m_TargetButton != null) {
                clickEvent = m_TargetButton.onClick;
            }
        }
        public void OnButtonDown() {
            if(!buttonIsDown) buttonIsDown = true;
        }     

        public void OnButtonUp() {
            if(buttonIsDown) buttonIsDown = false;
            nextButtonHold = buttonDownHoldDelta;
            buttonHoldTime = 0.0f;
        }

        void Update() {
            if(m_TargetButton == null) return;

            buttonHoldTime += Time.deltaTime;
            
			if(buttonIsDown && buttonHoldTime > nextButtonHold) {
				nextButtonHold = buttonHoldTime + buttonDownHoldDelta;
                // Invoke the click action
                clickEvent.Invoke();
				nextButtonHold -= buttonHoldTime;
            	buttonHoldTime = 0.0f;
			}
        }   
	}
}