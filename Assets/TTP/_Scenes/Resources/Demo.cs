using System.Collections;
using System.Collections.Generic;
using Shogoki.TTP.Picker;
using UnityEngine;

public class Demo: MonoBehaviour {

	[SerializeField]
	Picker hoursPicker;

	[SerializeField]
	Picker minutePicker;

	[SerializeField]
	Picker secondsPicker;

	public void PrintPickersValue() {
		print(string.Format("{0:00}:{1:00}:{2:00}",
			hoursPicker.PickerValue,
			minutePicker.PickerValue,
			secondsPicker.PickerValue
		));
	}
}
