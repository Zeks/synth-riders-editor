using System;
using MiKu.NET;
using Shogoki.TTP.Picker;
using UnityEngine;

public class Miku_JumpToTime : MonoBehaviour {	
	public static Miku_JumpToTime s_instance;

	[SerializeField]
	PickerTextMesh minutePicker;

	[SerializeField]
	PickerTextMesh secondsPicker;

	PickerTextMesh millisecondsPicker;

	void Start() {
		s_instance = this;
	}

	public void DoGoToTime() {
		float timeInMS = ( ( (minutePicker.PickerValue * Track.secondsInMinute) + secondsPicker.PickerValue ) * Track.msInSecond );
		Track.JumpToTime(timeInMS);
	}

	public static void SetMinutePickerLenght(int newLenght) {
		s_instance.minutePicker.SetLenght(newLenght);
	}

	/// <summary>
	/// Given the milliseconds set the pickers value
	/// </summary>
	/// <param name="ms">Milliseconds</ms>
	public static void SetPickersValue(double ms) {
		if(s_instance == null) return;

		TimeSpan t = TimeSpan.FromMilliseconds(ms);
		
		s_instance.minutePicker.SetPickerValue(t.Minutes);
		s_instance.secondsPicker.SetPickerValue(t.Seconds);
	}

	public static void GoToTime() {
		if(s_instance == null) return;
		s_instance.DoGoToTime();
	}
}
