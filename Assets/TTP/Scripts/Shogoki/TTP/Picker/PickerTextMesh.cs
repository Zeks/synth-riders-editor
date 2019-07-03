using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Shogoki.TTP.Picker {
	public class PickerTextMesh : Picker {

		private TextMeshProUGUI selectedValueFieldTextMesh;
		private TextMeshProUGUI[] topTextFieldsTextMesh;
		private TextMeshProUGUI[] bottomTextFieldsTextMesh;


        // Use this for initialization
        protected override void Start () {
			// Intitialize the selectedValueField
			selectedValueFieldTextMesh = m_SelectedValueFieldContainer.GetComponentInChildren<TextMeshProUGUI>();

			// Initialze the content of the Text[] arrays base on the m_TopButtons and DownButtons content
			// Top Buttons
			topTextFieldsTextMesh = new TextMeshProUGUI[m_TopButtons.Length];
			for(int i = 0; i < m_TopButtons.Length; ++i) {
				topTextFieldsTextMesh[i] = m_TopButtons[i].GetComponentInChildren<TextMeshProUGUI>();
				int steepUp = i + 1;
				m_TopButtons[i].onClick.AddListener(
					delegate {
						NumeredButtonClicked(steepUp, ButtonID.UP_BUTTON);
					}
				);
			}

			// Bottom Buttons
			bottomTextFieldsTextMesh = new TextMeshProUGUI[m_BottomButtons.Length];
			for(int j = 0; j < m_BottomButtons.Length; ++j) {
				bottomTextFieldsTextMesh[j] = m_BottomButtons[j].GetComponentInChildren<TextMeshProUGUI>();
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

		/// <summary>
        /// Set the picker value
        /// </summary>
		/// <param name="value">The value to set the picker to</param>
		public override void SetPickerValue(int value) {
			pickerValue = value;
			if(PickerValue < 0) pickerValue = m_PickerLenght - 1;
			if(PickerValue > m_PickerLenght - 1) pickerValue = 0;
			
			// Set top values
			for(int i = 0; i < topTextFieldsTextMesh.Length; ++i) {
				topTextFieldsTextMesh[i].SetText(GetCalcualteValue(i+1, false).ToString(m_ValueFormat));
			} 

			// Set bottom values
			for(int j = 0; j < bottomTextFieldsTextMesh.Length; ++j) {
				bottomTextFieldsTextMesh[j].SetText(GetCalcualteValue(j+1).ToString(m_ValueFormat));
			} 

			selectedValueFieldTextMesh.SetText(PickerValue.ToString(m_ValueFormat));
		}
	}
}