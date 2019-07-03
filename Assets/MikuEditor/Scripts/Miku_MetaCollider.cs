using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using UnityEngine;

public class Miku_MetaCollider : MonoBehaviour {

	public List<string> m_validColliderTag;
	//"SphereMarker"

	public GameObject m_CollisionEffect;

	public Transform m_parentHolder;

	private List<GameObject> gameObjectsStack;
	private List<SpriteRenderer> spritesStack;

	private bool isInitialized = false;

	void Awake() {
		if(!isInitialized) {
			gameObjectsStack = new List<GameObject>();
			spritesStack = new List<SpriteRenderer>();

			isInitialized = true;
		}
	}

	void OnTriggerEnter(Collider other) {
		if(m_validColliderTag.Contains(other.gameObject.tag)) {
			
			bool isNoteMarker = other.gameObject.tag.Equals("SphereMarker");
			Track.AddNoteToDisabledList(other.gameObject, isNoteMarker);

			if(isNoteMarker) {
				int nextOnQueue = GetNextQueueIndex();

				Transform targetTransform = !other.gameObject.transform.parent.name.Equals("[NotesHolder]") ? other.gameObject.transform.parent : other.gameObject.transform;

				GameObject collisionEffect = gameObjectsStack[nextOnQueue];
				collisionEffect.transform.localPosition = new Vector3(
					targetTransform.localPosition.x,
					targetTransform.localPosition.y,
					0
				);
				collisionEffect.transform.parent = m_parentHolder;

				SpriteRenderer sprite = spritesStack[nextOnQueue];
				sprite.color = NotesArea.GetColorToDistance(NotesArea.DistanceToCenter(collisionEffect.transform.position));

				collisionEffect.SetActive(true);
			}			
		}
	}

	int GetNextQueueIndex() {

		for(int i = 0; i < gameObjectsStack.Count; ++i) {
			GameObject nextOnLine = gameObjectsStack[i];
			if(!nextOnLine.activeSelf) {
				return i;
			}
		}

		GameObject newToStackGO = GameObject.Instantiate(m_CollisionEffect);
		newToStackGO.transform.rotation =	Quaternion.identity;
		newToStackGO.transform.parent = m_parentHolder;		

		SpriteRenderer newToStackSpr = newToStackGO.GetComponent<SpriteRenderer>();

		gameObjectsStack.Add(newToStackGO);
		spritesStack.Add(newToStackSpr);

		newToStackGO.SetActive(false);
		return gameObjectsStack.Count - 1;
	}
}
