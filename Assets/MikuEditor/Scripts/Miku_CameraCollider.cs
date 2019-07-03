using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using UnityEngine;

public class Miku_CameraCollider : MonoBehaviour {

	public List<string> m_validColliderTag;

	/* MovementObstacle "SphereMarker"*/

	void OnTriggerEnter(Collider other) {
		if(m_validColliderTag.Contains(other.gameObject.tag)) {
			// other.gameObject.tag.Equals("MovementObstacle")
			Track.AddNoteToReduceList(other.gameObject, true);
		}
		
	}

	void OnTriggerExit(Collider other) {
		if(m_validColliderTag.Contains(other.gameObject.tag)) {
			// other.gameObject.tag.Equals("MovementObstacle")
			Track.RemoveNoteToReduceList(other.gameObject, true);
		}
	}
}
