using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Shogoki.Utils {
	 
	public class TagItem : MonoBehaviour {

		[SerializeField]
		private TextMeshProUGUI label;

		private bool removed = false;
		private string Tag;

		public void DisplayTag(string Tag) {
			this.Tag = Tag;
			label.SetText(Tag);
		}

		public void RemoveMe() {
			if(!removed) {
				removed = true;
				TagController.RemoveTag(Tag);
			}			
		}
	}
}