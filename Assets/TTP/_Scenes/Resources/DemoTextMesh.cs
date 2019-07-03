using System.Collections;
using System.Collections.Generic;
using Shogoki.TTP.Picker;
using UnityEngine;

public class DemoTextMesh : MonoBehaviour {

	[SerializeField]
	PickerTextMesh hoursPicker;

	[SerializeField]
	PickerTextMesh minutePicker;

	[SerializeField]
	PickerTextMesh secondsPicker;

	public void PrintPickersValue() {
		print(string.Format("{0:00}:{1:00}:{2:00}",
			hoursPicker.PickerValue,
			minutePicker.PickerValue,
			secondsPicker.PickerValue
		));
	}
}
