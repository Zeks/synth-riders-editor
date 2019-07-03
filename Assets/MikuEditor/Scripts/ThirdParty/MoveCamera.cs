// Credit to damien_oconnell from http://forum.unity3d.com/threads/39513-Click-drag-camera-movement
// for using the mouse displacement for calculating the amount of camera movement and panning code.

using UnityEngine;
using System.Collections;
using MiKu.NET;

public class MoveCamera : MonoBehaviour 
{
	//
	// VARIABLES
	//
	
	public float turnSpeed = 4.0f;		// Speed of camera turning when mouse moves in along an axis
	public float panSpeed = 4.0f;		// Speed of the camera when being panned
	public float zoomSpeed = 4.0f;		// Speed of the camera going back and forth
	
	private Vector3 mouseOrigin;	// Position of cursor when mouse dragging starts
	private bool isPanning;		// Is the camera being panned?
	private bool isRotating;	// Is the camera being rotated?
	
    private Camera attachedCamera;
    private bool isCTRLDown = false;

	private bool isSHIFTDown = false;

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

	void OnApplicationFocus(bool hasFocus)
	{
		if(hasFocus) {
			isCTRLDown = false;
			isSHIFTDown = false;
		} 
	}
	
	void LateUpdate() 
	{
		if(!gameObject.activeSelf || !gameObject.activeInHierarchy) return;

        if(Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt) 
			|| Input.GetKeyDown(KeyCode.RightControl) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCTRLDown = true;
        }

        if(Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt)
			|| Input.GetKeyUp(KeyCode.RightControl) || Input.GetKeyUp(KeyCode.LeftControl)) {
            isCTRLDown = false;
        }

		if(Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) {
			isSHIFTDown = true;
		}

		if(Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)) {
			isSHIFTDown = false;
		}

        if( (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) && isCTRLDown ) {
            ResetCamera();
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, "Camera position was reset");
        }

		// Get the right mouse button
		if(Input.GetMouseButtonDown(1))
		{
			// Get mouse origin
			mouseOrigin = Input.mousePosition;
            isRotating = true;
		}
		
		// Get the middle mouse button
		/* if(Input.GetMouseButtonDown(0) && isCTRLDown)
		{
			// Get mouse origin
			mouseOrigin = Input.mousePosition;
            isPanning = true;            
		} */

		float horizontalPanning = Input.GetAxis("Horizontal Free Camera");
		float verticalPanning = Input.GetAxis("Vertical Free Camera");

		if( (horizontalPanning != 0 || verticalPanning !=0) && !Track.PromtWindowOpen && !isCTRLDown && !isSHIFTDown) {
			isPanning = true;		
		} else {
			isPanning = false;
		}

		/* if (Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
		{
			DoZoom( -zoomSpeed );
		} 
		else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
		{
			DoZoom( zoomSpeed );
		} */
		
		// Disable movements on button release
		if (!Input.GetMouseButton(1)) { isRotating = false; }
		//if (!Input.GetMouseButton(0)) { isPanning = false; }
		
		// Rotate camera along X and Y axis
		if (isRotating)
		{
            Vector3 pos = attachedCamera.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

			transform.RotateAround(transform.position, transform.right, -pos.y * turnSpeed);
			transform.RotateAround(transform.position, Vector3.up, pos.x * turnSpeed);
		}
		
		// Move the camera on it's XY plane
		if (isPanning)
		{
            //Vector3 pos = attachedCamera.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);
			/* Vector3 pos = transform.localPosition;
			pos.x += horizontalPanning * 0.001f;
			pos.y += verticalPanning * 0.001f;

            Vector3 move = new Vector3(pos.x * panSpeed, pos.y * panSpeed, 0);
            transform.Translate(move, Space.Self); */

			float translateY = verticalPanning * panSpeed;
			float translateX = horizontalPanning * panSpeed;
            transform.Translate(translateX, translateY, 0, Space.Self);
		}
	}

    void ResetCamera() {
        transform.localPosition = defaultPosition;
        transform.localRotation = defaulRotation;
		attachedCamera.fieldOfView = defaultFOV;
    }

	void DoZoom(float zoomFactor) {
		/*Vector3 pos = attachedCamera.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

		Vector3 move = pos.y * ( zoomSpeed * zoomFactor ) * transform.forward; 
		transform.Translate(move, Space.Self);*/
		float zoomTarget = attachedCamera.fieldOfView + zoomFactor;

		attachedCamera.fieldOfView = Mathf.Clamp(zoomTarget, 10, 50);
	}
}