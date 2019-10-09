// Credit to damien_oconnell from http://forum.unity3d.com/threads/39513-Click-drag-camera-movement
// for using the mouse displacement for calculating the amount of camera movement and panning code.

using UnityEngine;
using System.Collections;
using MiKu.NET;

public class MoveCamera : MonoBehaviour 
{
	
	public float turnSpeed = 4.0f;		// Speed of camera turning when mouse moves in along an axis
	public float panSpeed = 4.0f;		// Speed of the camera when being panned

	private Vector2 mouseOrigin;	// Position of cursor when mouse dragging starts

	private Camera attachedCamera;

	private Vector3 defaultPosition;
    private Quaternion defaulRotation; 
	private float defaultFOV;

    private bool defaultSaved = false;

    void Start() {
        attachedCamera = gameObject.GetComponent<Camera>();

        if(!defaultSaved) {
            defaultSaved = true;

            defaultPosition = transform.localPosition;
            defaulRotation = transform.localRotation;
			defaultFOV = attachedCamera.fieldOfView;
        }
        
    }
    
    private void PanCamera(Vector3 direction) {
		transform.position += Time.deltaTime * panSpeed * 8f * direction;
	}
	
	void LateUpdate() 
	{
		if(!gameObject.activeSelf || !gameObject.activeInHierarchy) return;

		Vector3 curPos = transform.position;
		
		if (Input.GetKey(KeyCode.A)) {
			PanCamera(-transform.right);
		}
		
		if (Input.GetKey(KeyCode.D)) {
			PanCamera(transform.right);
		}
		
		if (Input.GetKey(KeyCode.W)) {
			PanCamera(transform.forward);
		}
		
		if (Input.GetKey(KeyCode.S)) {
			PanCamera(-transform.forward);
		}
		
		if (Input.GetKey(KeyCode.Q)) {
			PanCamera(-transform.up);
		}
		
		if (Input.GetKey(KeyCode.E)) {
			PanCamera(transform.up);
		}

		//Start rotating the camera.
		if (Input.GetMouseButtonDown(1)) {
			mouseOrigin = Input.mousePosition;
		}

		if (Input.GetMouseButton(1)) {
			Vector2 curMousePos = Input.mousePosition;

			float rightTurnSpeed = (curMousePos.y - mouseOrigin.y) * turnSpeed * Time.deltaTime;
			float upTurnSpeed = (curMousePos.x - mouseOrigin.x) * turnSpeed * Time.deltaTime;
			
			transform.Rotate(Vector3.right * -rightTurnSpeed, Space.Self);
			transform.Rotate(Vector3.up * upTurnSpeed, Space.World);

			mouseOrigin = curMousePos;
		}


		if( Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) {
            ResetCamera();
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, "Camera position was reset");
        }
		
	}

    void ResetCamera() {
        transform.localPosition = defaultPosition;
        transform.localRotation = defaulRotation;
		attachedCamera.fieldOfView = defaultFOV;
    }
}