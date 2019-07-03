using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Line Wave v2.0
/// Use with Unity's LineRenderer
/// by Adriano Bini.
/// </summary>

//[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class LineWaveCollider : MonoBehaviour {
	float ampT;
	public Material traceMaterial;
	public float traceWidth = 0.3f;
	public GameObject targetOptional;
	public float altRotation;
	public enum Origins{Start, Middle};
    public LineWaveCollider.Origins origin = Origins.Start;
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
    List<Vector3> linePositions;

    public enum WaveColliders { Box, Sphere};
    public LineWaveCollider.WaveColliders waveCollider = WaveColliders.Box;
    List<GameObject> colliders;
    GameObject Colliders;
    GameObject colliderGO;
    System.Type colType;
    public float colliderSize = 0.2f;
    public int collidersGap = 5;
    
	void Awake(){
		lrComp = GetComponent<LineRenderer>();
		lrComp.useWorldSpace = false;
		lrComp.material = traceMaterial;

        linePositions = new List<Vector3>();
        colliders = new List<GameObject>();
        Colliders = new GameObject("Colliders");
        Colliders.transform.parent = transform;
        colliderGO = new GameObject("col");
        colliderGO.transform.position = transform.position;
        colliderGO.transform.parent = Colliders.transform;
	}
	
	void Update () {

		lrComp.SetWidth(traceWidth, traceWidth);
        gameObject.transform.localScale = Vector3.one;
		
		if (targetOptional != null) {
			origin = Origins.Start;
			lengh = (transform.position - targetOptional.transform.position).magnitude;
			transform.LookAt(targetOptional.transform.position);
//			transform.rotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y - 90, transform.localRotation.eulerAngles.z);
			transform.Rotate(altRotation, -90, 0);
		}
		
		if (warpRandom<=0){warpRandom=0;}
        if (size <= 2) { size = 2; }
        lrComp.SetVertexCount(size);
		
		if (ampByFreq) {ampT = Mathf.Sin(freq*Mathf.PI);}
		else {ampT = 1;}
		ampT = ampT * amp;
		if (warp && warpInvert) {ampT = ampT/2;}

        foreach (GameObject go in colliders)
        {
            Destroy(go);
        }
        linePositions.Clear();
        colliders.Clear();

		
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
                linePositions.Add(new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT));
			}
			else {
                linePositions.Add(new Vector3(lengh / size * i - start, sinAngle * ampT, sinAngleZ * ampT));
				warpInvert = false;
			}


			if (i == 1) {posVtx2 = new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT);}
			if (i == size-1) {posVtxSizeMinusOne = new Vector3(lengh/size*i - start, sinAngle * ampT * warpT, sinAngleZ * ampT * warpT);}
		}



        switch (waveCollider)
        {
            case WaveColliders.Box:
                colType = typeof(BoxCollider);
                break;
            case WaveColliders.Sphere:
                colType = typeof(SphereCollider);
                break;
            default:
                break;
        }

        if (colliderGO.GetComponent(colType) == null)
        {
            Destroy(colliderGO.GetComponent(typeof(Collider)));
            colliderGO.AddComponent(colType);
        }
        colliderGO.SetActive(true);
        colliderGO.transform.localScale = new Vector3(colliderSize, colliderSize, colliderSize);
        collidersGap = Mathf.Clamp(collidersGap, 1, 20);

        for (int j = 0; j < linePositions.Count; j++)
        {
            lrComp.SetPosition(j, linePositions[j]);
            if (j % collidersGap == 0)
            {
                GameObject newCol = (GameObject)Instantiate(colliderGO);
                newCol.transform.parent = Colliders.transform; ;
                if (targetOptional)
                {
                    newCol.transform.position = Vector3.Lerp(gameObject.transform.position, targetOptional.transform.position, (float)j / (float)linePositions.Count);
                    newCol.transform.Translate(0, linePositions[j].y, linePositions[j].z, gameObject.transform);
                }
                else
                {
                    newCol.transform.position = gameObject.transform.position;
                    newCol.transform.Translate(linePositions[j].x, linePositions[j].y, linePositions[j].z, gameObject.transform);
                }
                newCol.transform.rotation = gameObject.transform.rotation;
                //newCol.transform.Rotate(0, 0, linePositions[j].y, Space.Self); //2do: Align colliders' Zs
                colliders.Add(newCol);
            }
        }
        colliderGO.SetActive(false);


		
		if (warpInvert) {  //Fixes pinned limits when WarpInverted
			lrComp.SetPosition(0, posVtx2);
			lrComp.SetPosition(size-1, posVtxSizeMinusOne);
		}
        

    }
 }
