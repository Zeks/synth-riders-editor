using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiKu.NET {
	public class NotesArea : MonoBehaviour {

		public static NotesArea s_instance;

		[SerializeField]
		private GridManager grid;

		[Space(20)]
		[SerializeField]
		private GameObject m_boundBox;
		private Transform boundBoxTransform;

		[Space(20)]
		[Header("Confort Boundaries")]
		[SerializeField]
		private float m_confortableBoundarie = 0.35f;

		[SerializeField]
		private float m_moderateBoundarie = 0.36f;

		/* [SerializeField]
		private float m_intense = 0.76f; */

		[Space(20)]
		[Header("Colors")]
		[SerializeField]
		private Color m_confortableColor = Color.blue;
		[SerializeField]
		private Color m_moderateColor = Color.yellow;
		[SerializeField]
		private Color m_intenseColor = Color.red;		

		private GameObject selectedNote;
		private GameObject mirroredNote;

		private bool snapToGrip = true;

		RaycastHit hit;
		Ray ray;
		Vector3 rayPos = Vector3.zero;

		/* bool rayEnabled = false; */

		SpriteRenderer boundBoxSpriteRenderer;

		public LayerMask targetMask = 11;
        private bool isCTRLDown;

		Vector3 finalPosition, mirroredPosition;
		GameObject[] multipleNotes;

        public bool SnapToGrip
        {
            get
            {
                return snapToGrip;
            }

            set
            {
                snapToGrip = value;
            }
        }

        void Awake() {
			s_instance = this;

			multipleNotes = new GameObject[2];
		}

		void Start() {
			boundBoxTransform = m_boundBox.transform;
			boundBoxSpriteRenderer = m_boundBox.GetComponent<SpriteRenderer>();
			m_boundBox.SetActive(false);
		}		

		void OnDisable() {
			if(selectedNote != null) {
				GameObject.DestroyImmediate(selectedNote);
			}

			if(mirroredNote != null) {
				GameObject.DestroyImmediate(mirroredNote);
			}

			m_boundBox.SetActive(false);
		}

		void EnabledSelectedNote() {
			if(selectedNote == null) {
				selectedNote = Track.GetSelectedNoteMarker();
				SphereCollider coll = selectedNote.GetComponent<SphereCollider>();
				if(coll == null) {
					coll = selectedNote.GetComponentInChildren<SphereCollider>();
				}

				coll.enabled = false;

				m_boundBox.SetActive(true);	

				if(Track.IsOnMirrorMode) {
					mirroredNote = Track.GetMirroredNoteMarker();
				}
			}					
		}

		void DisableSelectedNote() {
			if(selectedNote != null) {
				GameObject.DestroyImmediate(selectedNote);
				m_boundBox.SetActive(false);
			}	

			if(mirroredNote != null) {
				GameObject.DestroyImmediate(mirroredNote);
			}		
		}

		void OnApplicationFocus(bool hasFocus)
		{
			if(hasFocus) {
				isCTRLDown = false;
			} 
		}

		void Update()
		{
			if(Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
			{
				isCTRLDown = true;
			}

			if(Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt)) {
				isCTRLDown = false;
			}
			
			if (!isCTRLDown && Input.GetMouseButtonDown(0) && selectedNote != null) {
				if(Track.IsOnMirrorMode) {
                    System.Array.Clear(multipleNotes, 0, 2);
					multipleNotes[0] = selectedNote;
					multipleNotes[1] = mirroredNote;
					Track.AddNoteToChart(multipleNotes);
				} else {
					Track.AddNoteToChart(selectedNote);
				}				
			}
		}

		void FixedUpdate() {	
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray, out hit, Mathf.Infinity, targetMask.value)) {
				EnabledSelectedNote();

				rayPos = hit.point;
				rayPos.z = (float)Track.CurrentUnityUnit;

				finalPosition = (SnapToGrip) ? grid.GetNearestPointOnGrid(rayPos) : rayPos;

				selectedNote.transform.position = finalPosition;
				boundBoxTransform.position = finalPosition;

				if(Track.IsOnMirrorMode) {
					mirroredPosition = finalPosition;

					if(Track.XAxisInverse) {
						mirroredPosition.x *= -1;
					}

					if(Track.YAxisInverse) {
						mirroredPosition.y *= -1;
					}
					
					mirroredNote.transform.position = mirroredPosition;
				}

				//float toCenter = Mathf.Abs(Vector3.Distance(transform.position, finalPosition));
				SetBoundaireBoxColor(DistanceToCenter(finalPosition));					
			} else {
				DisableSelectedNote();
			}
		}

		public void RefreshSelectedObjec() {
			if(selectedNote != null) {
				GameObject.DestroyImmediate(selectedNote);
				selectedNote = Track.GetSelectedNoteMarker();
				if(Track.IsOnMirrorMode) {
					GameObject.DestroyImmediate(mirroredNote);
					mirroredNote = Track.GetMirroredNoteMarker();
				} else {
					if(mirroredNote != null) {
						GameObject.DestroyImmediate(mirroredNote);
					}
				}

				SphereCollider coll = selectedNote.GetComponent<SphereCollider>();
				if(coll == null) {
					coll = selectedNote.GetComponentInChildren<SphereCollider>();
				}

				coll.enabled = false;
			}			
		}

		private void SetBoundaireBoxColor(float distanceToCenter) {
			boundBoxSpriteRenderer.color = GetColorToDistance(distanceToCenter);
		}

#region Static Methods
		public static float DistanceToCenter(Vector3 targetPoint) {
			return Mathf.Abs(Vector3.Distance(s_instance.transform.position, targetPoint));
		}

		public static Color GetColorToDistance(float distanceToCenter) {
			if(distanceToCenter <= s_instance.m_confortableBoundarie) {
				return s_instance.m_confortableColor;
				;
			}

			if(distanceToCenter <= s_instance.m_moderateBoundarie) {
				return s_instance.m_moderateColor;
			}

			return s_instance.m_intenseColor;
		}
#endregion
		
	}
}