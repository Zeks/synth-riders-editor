using UnityEngine;
using System.Collections;

/// <summary>
/// Line Wave v2.0
/// Use with Unity's LineRenderer
/// by Adriano Bini.
/// </summary>

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class LineWave : MonoBehaviour {
	float ampT;
	public Material traceMaterial;
	public float traceWidth = 0.3f;
	public GameObject targetOptional;
	public float altRotation;
	public enum Origins{Start, Middle};
	public LineWave.Origins origin = Origins.Start;
	public int size = 300;
	public float lengh = 10.0f;
	public float freq = 2.5f;
	public float amp = 1;
	public bool ampByFreq;
	public bool centered = true;
	public bool centCrest = true;
	public bool warp = true;
	public bool warpInvert;
	public float warpRandom;
	public float walkManual;
	public float walkAuto;
	public bool spiral;
	float start;
	float warpT;
	float angle;
	float sinAngle;
	float sinAngleZ;
	double walkShift;
	Vector3 posVtx2;
	Vector3 posVtxSizeMinusOne;
	LineRenderer lrComp;

	void Awake(){
		lrComp = GetComponent<LineRenderer>();
		lrComp.useWorldSpace = false;
		lrComp.material = traceMaterial;
	}
	
	void Update () {
		lrComp.SetWidth(traceWidth, traceWidth);
		
		if (targetOptional != null) {
			origin = Origins.Start;
			lengh = (transform.position - targetOptional.transform.position).magnitude;
			transform.LookAt(targetOptional.transform.position);
//			transform.rotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y - 90, transform.localRotation.eulerAngles.z);
			transform.Rotate(altRotation, -90, 0);
		}
		
		if (warpRandom<=0){warpRandom=0;}
		if (size<=2){size=2;}
		lrComp.SetVertexCount(size);
		
		if (ampByFreq) {ampT = Mathf.Sin(freq*Mathf.PI);}
		else {ampT = 1;}
		ampT = ampT * amp;
		if (warp && warpInvert) {ampT = ampT/2;}
		
		for (int i = 0; i < size; i++) {
			angle = (2*Mathf.PI/size*i*freq);
			if (centered) {
				angle -= freq*Mathf.PI; 	//Center
				if (centCrest) {
					angle -= Mathf.PI/2;	//Crest/Knot
				}
			}
			else {centCrest = false;}
			
			walkShift -= walkAuto/size*Time.deltaTime;
			angle += (float)walkShift - walkManual;
			sinAngle = Mathf.Sin(angle);
			if (spiral) {sinAngleZ = Mathf.Cos(angle);}
			else {sinAngleZ = 0;}
			
			if (origin == Origins.Start) {start = 0;}
			else {start = lengh/2;}
			
			if (warp) {
				warpT = size - i;
				warpT = warpT / size;
				warpT = Mathf.Sin(Mathf.PI * warpT * (warpRandom+1));
				if (warpInvert) {warpT = 1/warpT;}
				lrComp.SetPosition(i, new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT));
			}
			else {
				lrComp.SetPosition(i, new Vector3(lengh/size*i - start, sinAngle * ampT, sinAngleZ * ampT));
				warpInvert = false;
			}
			
			if (i == 1) {posVtx2 = new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT);}
			if (i == size-1) {posVtxSizeMinusOne = new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT);}
		}
		
		if (warpInvert) {  //Fixes pinned limits when WarpInverted
			lrComp.SetPosition(0, posVtx2);
			lrComp.SetPosition(size-1, posVtxSizeMinusOne);
		}
	}
}
