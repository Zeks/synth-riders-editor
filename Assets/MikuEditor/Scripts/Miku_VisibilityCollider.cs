using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using UnityEngine;

public class Miku_VisibilityCollider : MonoBehaviour {

	public string m_validColliderTag = "SphereMarker";

	void OnTriggerEnter(Collider other) {
		if(other.gameObject.tag.Equals(m_validColliderTag)) {
			Track.AddNoteToDisabledList(other.gameObject);
		}
	}
}
