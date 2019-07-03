using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shogoki.Utils {
    public class BeatNumberHelper : MonoBehaviour {
		
        [SerializeField]
        private TextMeshPro numberField;

        [SerializeField]
        private TextMeshPro miniMapdField;

        public void SetText(string number) {
            numberField.SetText(number);
            miniMapdField.SetText(number);
        }
	}
}