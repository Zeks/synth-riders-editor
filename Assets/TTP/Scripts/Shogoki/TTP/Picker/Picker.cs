using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shogoki.TTP.Picker {
	public class Picker : MonoBehaviour {
		public enum ButtonID {
			Null,
			UP_BUTTON,
			DOWN_BUTTON
		}

		[Header("Picker Fields")]
		[Header("Buttons")]
		[SerializeField]
		protected Button m_UpButton;
		[SerializeField]
		protected Button m_DownButton;
		[SerializeField]
		protected Button[] m_TopButtons;
		[SerializeField]
		protected Button[] m_BottomButtons;
		[Space(20)]
		[Header("TextFields")]
		[SerializeField]
		protected GameObject m_SelectedValueFieldContainer;
		private Text selectedValueField;
		private Text[] topTextFields;
		private Text[] bottomTextFields;

		[Space(20)]
		[Header("Buttons Hold Sensibility Settings")]
        [SerializeField]
		[Tooltip("Seconds between every button down action")]
        protected float m_ButtonDownHoldDelta = 0.5f;

		[Space(40)]
		[Header("Picker Value Settings")]
		[Tooltip("The max value that the picker will have, be aware that the picker is base 0")]
		[SerializeField]
		protected int m_PickerLenght = 60;
		
		[Tooltip("The picker start value")]
		[SerializeField]
		protected int m_PickerStartAt = 0;

		[Tooltip("Number format that the picker will have")]
		[SerializeField]
		protected string m_ValueFormat = "00";

        /// <summary>
        /// How many seconds must to pass to do the Button Down Action
        /// </summary>
		protected float nextButtonHold = 0.5f;

        /// <summary>
        /// How many seconds the button has been holdDown
        /// </summary>
		protected float buttonHoldTime = 0;

        /// <summary>
        /// Is the button beign hold Down?
        /// </summary>
		protected bool buttonIsDown = false;

		protected ButtonID currentHoldButton = ButtonID.Null;

        /// <summary>
        /// Current Value the picker had
        /// </summary>
        protected int pickerValue = 0;

        public int PickerValue
        {
            get
            {
                return pickerValue;
            }
        }


        // Use this for initialization
        protected virtual void Start () {
			// Intitialize the selectedValueField
			selectedValueField = m_SelectedValueFieldContainer.GetComponentInChildren<Text>();

			// Initialze the content of the Text[] arrays base on the m_TopButtons and DownButtons content
			// Top Buttons
			topTextFields = new Text[m_TopButtons.Length];
			for(int i = 0; i < m_TopButtons.Length; ++i) {
				topTextFields[i] = m_TopButtons[i].GetComponentInChildren<Text>();
				int steepUp = i + 1;
				m_TopButtons[i].onClick.AddListener(
					delegate {
						NumeredButtonClicked(steepUp, ButtonID.UP_BUTTON);
					}
				);
			}

			// Bottom Buttons
			bottomTextFields = new Text[m_BottomButtons.Length];
			for(int j = 0; j < m_BottomButtons.Length; ++j) {
				bottomTextFields[j] = m_BottomButtons[j].GetComponentInChildren<Text>();
				int steepDown = j + 1;
				m_BottomButtons[j].onClick.AddListener(					
					delegate {
						NumeredButtonClicked(steepDown, ButtonID.DOWN_BUTTON);
					}
				);
			}

			if(m_ValueFormat == null || m_ValueFormat == string.Empty) {
				m_ValueFormat = "00";
			}

			AddEvents();
			SetPickerValue(m_PickerStartAt);
		}

        protected void NumeredButtonClicked(int steep, ButtonID buttonId)
        {
            if(buttonId == ButtonID.UP_BUTTON) {
				SetPickerValue(PickerValue - steep);
			} else if(buttonId == ButtonID.DOWN_BUTTON) {
				SetPickerValue(PickerValue + steep);
			}
        }

        protected void AddEvents()
        {
			// Add Triggers for the Up button;
			AddTriggers(m_UpButton.gameObject, ButtonID.UP_BUTTON);
            
			// Add Triggers for the Down button;
			AddTriggers(m_DownButton.gameObject, ButtonID.DOWN_BUTTON);
        }

		protected void AddTriggers(GameObject buttonObject, ButtonID buttonID) {
			EventTrigger eventTrigger = buttonObject.AddComponent<EventTrigger>();
			EventTrigger.Entry pointerDownEvent = new EventTrigger.Entry();
			pointerDownEvent.eventID = EventTriggerType.PointerDown;
			pointerDownEvent.callback.AddListener(
				(data) => { OnPointerDown( (PointerEventData)data, buttonID); }
			);
			eventTrigger.triggers.Add(pointerDownEvent);

			EventTrigger.Entry pointerUpEvent = new EventTrigger.Entry();
			pointerUpEvent.eventID = EventTriggerType.PointerUp;
			pointerUpEvent.callback.AddListener(
				(data) => { OnPointerUp( (PointerEventData)data, buttonID); }
			);
			eventTrigger.triggers.Add(pointerUpEvent);
		}

		protected void OnPointerDown(PointerEventData data, ButtonID buttonID) {
			if(!buttonIsDown) {
				currentHoldButton = buttonID;
				buttonIsDown = true;
			} 			
		} 

		protected void OnPointerUp(PointerEventData data, ButtonID buttonID) {
			if(buttonIsDown) {
				buttonIsDown = false;
				nextButtonHold = m_ButtonDownHoldDelta;
				buttonHoldTime = 0.0f;
				currentHoldButton = ButtonID.Null;
			} 
		} 

        // Update is called once per frame
        protected virtual void Update () {
			buttonHoldTime += Time.deltaTime;
            
			if(buttonIsDown && currentHoldButton != ButtonID.Null && buttonHoldTime > nextButtonHold) {
				nextButtonHold = buttonHoldTime + m_ButtonDownHoldDelta;
                // Invoke the click action
                // TODO
				if(currentHoldButton == ButtonID.UP_BUTTON) {
					SetPickerValue(PickerValue - 1);
				} else if(currentHoldButton == ButtonID.DOWN_BUTTON) {
					SetPickerValue(PickerValue + 1);
				}
				nextButtonHold -= buttonHoldTime;
            	buttonHoldTime = 0.0f;
			}
		}

		/// <summary>
        /// Set the picker value
        /// </summary>
		/// <param name="value">The value to set the picker to</param>
		public virtual void SetPickerValue(int value) {
			pickerValue = value;
			if(PickerValue < 0) pickerValue = GetLength() - 1;
			if(PickerValue > GetLength() - 1) pickerValue = 0;
			
			// Set top values
			for(int i = 0; i < topTextFields.Length; ++i) {
				topTextFields[i].text = GetCalcualteValue(i+1, false).ToString(m_ValueFormat);
			} 

			// Set bottom values
			for(int j = 0; j < bottomTextFields.Length; ++j) {
				bottomTextFields[j].text = GetCalcualteValue(j+1).ToString(m_ValueFormat);
			} 

			selectedValueField.text = PickerValue.ToString(m_ValueFormat);
		}


		/// <summary>
        /// Calcualte the next/previous value, of the picker
        /// </summary>
		/// <param name="steep">How much the value will be increment/decremented</param>
		/// <param name="isIncrement">indicate if the value is to be increment or decremented by <paramref name="steep"/></param>
		/// <returns>Value after calculations</returns> 
		protected int GetCalcualteValue(int steep, bool isIncrement = true) {
			if(!isIncrement) {
				int decrementBy = PickerValue - steep;
				return (decrementBy < 0 ) ? GetLength() + decrementBy : decrementBy;
			} else {
				int incrementBy = PickerValue + steep;
				return (incrementBy >= GetLength()) ? incrementBy - GetLength() : incrementBy;
			}
		}

		/// <summary>
		/// Get the lenght of the picker
		/// </summary>
		/// <returns>lenght</returns> 
		public int GetLength() {
			return m_PickerLenght;
		}

		/// <summary>
		/// Set the lenght of the picker and recalculate values
		/// </summary>
		/// <param name="newSize">The new lenght of the picker</param>
		public void SetLenght(int newSize) {
			m_PickerLenght = newSize;
			SetPickerValue(PickerValue);
		}
	}
}