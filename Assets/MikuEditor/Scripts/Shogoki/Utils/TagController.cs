using System;
using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using UnityEngine;
using UnityEngine.UI;

namespace Shogoki.Utils {

	public class TagController : MonoBehaviour {

		public static TagController s_instance;

		public GameObject TagPrefab;
		public InputField TextInput;
		private bool initializated = false;

		// Use this for initialization
		void Start () {
			s_instance = this;
		}	

		public static void InitContainer() {
			if(Track.CurrentChart == null) { return; }

			if(!s_instance.initializated) {
				foreach(string tag in Track.CurrentChart.Tags) {
					AddTagToList(tag);
				}
				s_instance.initializated = true;
			}
		}	

		private void RefreshTagList()
        {
            int totalChildren = transform.childCount;
			for(int i = totalChildren - 1; i >= 0; --i) {
				Transform child = transform.GetChild(i);
				GameObject.DestroyImmediate(child.gameObject);
			}

			initializated = false;
			InitContainer();
        }
		
		public static void AddTag(string Tag) {
			Tag = Tag.Trim();
			Tag = Tag.ToUpper();

			s_instance.TextInput.text = string.Empty;
			s_instance.TextInput.ActivateInputField();

			if(Track.CurrentChart == null || Track.CurrentChart.Tags.Contains(Tag) || Track.CurrentChart.Tags.Count >= Track.MAX_TAG_ALLOWED) {
				return;
			}		
			
			Track.CurrentChart.Tags.Add(Tag);
			AddTagToList(Tag);
		}

		public static void RemoveTag(string Tag) {
			if(Track.CurrentChart == null || !Track.CurrentChart.Tags.Contains(Tag)) {
				return;
			}

			Track.CurrentChart.Tags.Remove(Tag);
			s_instance.RefreshTagList();
		}       

        private static void AddTagToList(string Tag)
        {
            GameObject tagObject = Instantiate(s_instance.TagPrefab, Vector3.zero, Quaternion.identity, s_instance.transform);
			TagItem item = tagObject.GetComponent<TagItem>();
			if(item != null) {
				item.DisplayTag(Tag);
			}			
        }
    }
}
