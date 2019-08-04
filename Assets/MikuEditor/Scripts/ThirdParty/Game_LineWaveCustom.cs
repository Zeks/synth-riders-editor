using UnityEngine;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace ThirdParty.Custom {
    /// <summary>
    /// Line Wave v2.0
    /// Use with Unity's LineRenderer
    /// by Adriano Bini.
    /// custom by Jhean Ceballos
    /// </summary>

    //[ExecuteInEditMode]
    [RequireComponent(typeof(LineRenderer))]
    public class Game_LineWaveCustom : MonoBehaviour
    {
        float ampT;
        public Material[] traceMaterial;
        public float traceWidth = 0.3f;
        public float[,] targetOptional;
        public GameObject endPointGO;
        public float altRotation;
        public LineWaveCollider.Origins origin = LineWaveCollider.Origins.Start;
        public int size = 700;
        public float lengh = 10.0f;
        public float freq = 2.5f;
        public float amp = 1;
        public bool ampByFreq;
        public bool centered = true;
        public bool centCrest = true;
        public bool warp = true;
        public bool warpInvert;
        public float warpRandom;
        private float walkManual;
        private float walkAuto;
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
        
        List<GameObject> colliders;
        GameObject Colliders;
        GameObject colliderGO;
        System.Type colType;
        public float colliderSize = 0.2f;
        public int collidersGap = 5;

        [SerializeField]
        private string collitionTag = "ContactPoint";

        //[SerializeField]
        //protected int[] m_queues = new int[] { 3000 };

        private Gradient defaultColor;

        private int startIndex = -1;
        private bool areColliderInit = false;
        BoxCollider t_Collider = null;
        GameObject newCol;
        private bool isLineInit = false;
        private bool isChopping = false;

        [Header("Ring Settings")]
        public GameObject m_RingObject;
        public float m_RingScale = 2f;
        public int m_ringEvery = 1;
        public bool m_randomizeScale = false;
        private int lastStartIndex = 0;
        
        private int currentCycle = 1;
        private Vector3 lastPointPosition = Vector3.zero;
        int lastYVariation = 1;
        int[] ramdomSeeds = new int[2]{1, -1};
        Vector3[] smoothedPoints;

        void Awake()
        {
            lrComp = GetComponent<LineRenderer>();
            lrComp.useWorldSpace = true;

            /*for (int i = 0; i < traceMaterial.Length && i < m_queues.Length; ++i)
            {
                traceMaterial[i].renderQueue = m_queues[i];
            }*/

            lrComp.materials = traceMaterial;

            linePositions = new List<Vector3>();
            colliders = new List<GameObject>();
            Colliders = new GameObject("Colliders");
            Colliders.transform.parent = transform;
            Colliders.transform.localPosition = Vector3.zero;
            colliderGO = new GameObject("col");
            // colliderGO.transform.position = transform.position;
            colliderGO.transform.localPosition = Vector3.zero;
            colliderGO.transform.parent = Colliders.transform;

            defaultColor = lrComp.colorGradient;
            lastPointPosition = Vector3.zero;
        }

        public void RenderLine(bool refreshLine = false)
        {
            Trace.WriteLine("00000000000000000000LINE RENDER00000000000000");
            Trace.WriteLine("Entered Render Line");
            if(!isChopping || refreshLine)
            {
                Trace.WriteLine("Passed first if");
                isChopping = true;
                if (!isLineInit || refreshLine)
                {
                    Trace.WriteLine("Passed second if");
                    linePositions.Clear();
                    isLineInit = true;
                    lrComp.startWidth = traceWidth;
                    lrComp.endWidth = traceWidth;

                    gameObject.transform.localScale = Vector3.one;  
                    int startOptional = targetOptional.GetLength(0) > size ? targetOptional.GetLength(0) - size : 0;
                    Trace.WriteLine("size is:" + size);
                    Trace.WriteLine("targetOptional.GetLength(0) is:" + targetOptional.GetLength(0));
                    Trace.WriteLine("startOptional is:" + startOptional);
                    size = Mathf.RoundToInt( size / (targetOptional.GetLength(0) - startOptional) );
                    Trace.WriteLine("rounded size is:" + size);

                    linePositions.Add(transform.position);

                    int lengthOfArray = targetOptional.GetLength(0);
                    Trace.WriteLine("Passed array is of size:" + lengthOfArray);
                    try {
                        for(int index = startOptional; index < targetOptional.GetLength(0); ++index) {
                            //InitLinePositions(new Vector3(targetOptional[index, 0], targetOptional[index, 1], targetOptional[index, 2])); 
                            Trace.WriteLine("Adding point:" + targetOptional[index, 0] + " " +  targetOptional[index, 1] + " " + targetOptional[index, 2]);
                            linePositions.Add(new Vector3(targetOptional[index, 0], targetOptional[index, 1], targetOptional[index, 2]));
                        }
                    } catch {
                        Trace.WriteLine("!!!!!!!!!!!!!!!!CRASHED ADDING POINTS TO RENDERER!!!!!!!!!!!!!!!!!!!!");
                    }

                    smoothedPoints = LineSmoother.SmoothLine( linePositions.ToArray(), 0.8f );
                    Trace.WriteLine("Smoothed points is of size: " + smoothedPoints.Length);


                    lrComp.positionCount = smoothedPoints.Length;  
                    lrComp.SetPositions( smoothedPoints );                       

                    if(targetOptional.GetLength(0) > 0) {

                        if(m_RingObject != null && endPointGO != null) {
                            GameObject ring = GetRingElement(endPointGO.transform);

                            ring.transform.localPosition = Vector3.zero;
                            ring.transform.localScale = Vector3.one * m_RingScale;
                            ring.transform.localEulerAngles = new Vector3(90, 0, 0);
                        }
                    }    
                            
                }

                /* if (!areColliderInit)
                {
                    t_Collider = colliderGO.AddComponent<BoxCollider>();
                    t_Collider.isTrigger = true;

                    colliderGO.SetActive(true);
                    colliderGO.transform.localScale = Vector3.one;
                    collidersGap = Mathf.Clamp(collidersGap, 1, 20);
                } */

                /* for (int j = 0; j < smoothedPoints.Length; j++)
                {
                    //if(j > 0 && !areColliderInit)
                    t_Collider.size = new Vector3(colliderSize, colliderSize, colliderSize);
                    //lrComp.SetPosition(j, linePositions[j]);
                    if (startIndex == -1) startIndex = j;

                    if (j % collidersGap == 0)
                    {

                        if (!areColliderInit)
                        {
                            newCol = GameObject.Instantiate(colliderGO);
                            newCol.transform.parent = Colliders.transform;
                            newCol.tag = collitionTag;
                            
                            startIndex = -1;

                            Vector3 endPointPosition = transform.InverseTransformPoint(
                                    smoothedPoints[j].x,
                                    smoothedPoints[j].y, 
                                    smoothedPoints[j].z
                            );
                            newCol.transform.localPosition = endPointPosition;

                            colliders.Add(newCol);

                            // Rings Instantiation
                            if(m_RingObject != null && j > 0 && (j ==1 || j % m_ringEvery == 0)) {
                                GameObject ring = GetRingElement(newCol.transform);

                                ring.transform.localPosition = Vector3.zero;                            
                                ring.transform.localEulerAngles = new Vector3(0, 0, 90);
                                if(m_randomizeScale) {
                                    ring.transform.localScale = Vector3.one * UnityEngine.Random.Range(1f, m_RingScale);
                                } else {
                                    ring.transform.localScale = Vector3.one * m_RingScale;
                                }
                            }
                        }
                    }
                } */

                if (!areColliderInit)
                {
                    colliderGO.SetActive(false);
                    areColliderInit = true;
                }

                if ((endPointGO && !areColliderInit))
                {
                    int index = smoothedPoints.Length - 1;
                    endPointGO.transform.position = gameObject.transform.position;
                    endPointGO.transform.Translate(smoothedPoints[index].x, smoothedPoints[index].y, smoothedPoints[index].z, gameObject.transform);
                }

                // this.LineRenderer.enabled = false;
            }

            isChopping = false;
        }

        private void InitLinePositions(Vector3 endPointPosition)
        {
            
            lastYVariation = ramdomSeeds[UnityEngine.Random.Range(0, ramdomSeeds.Length)];

            endPointPosition = transform.InverseTransformPoint(
                endPointPosition.x,
                endPointPosition.y, 
                endPointPosition.z
            );

            /*Vector3 peakHandle = new Vector3(
                ( endPointPosition.x / UnityEngine.Random.Range(2, 8) ) * ( endPointPosition.x == lastPointPosition.x ? 0 : ( endPointPosition.x > lastPointPosition.x ? -1 : 1) ), 
                ( endPointPosition.y / UnityEngine.Random.Range(2, 8) ) * ( endPointPosition.y == lastPointPosition.x ? 0 : ( endPointPosition.y > lastPointPosition.x ? -1 : 1) ), 
                endPointPosition.z - UnityEngine.Random.Range(0.0f, 1.2f)
            );*/
            Vector3 peakHandle = new Vector3(
                ( endPointPosition.x / UnityEngine.Random.Range(1.5f, 2.0f) ) * lastYVariation, 
                ( endPointPosition.y - UnityEngine.Random.Range(0.0f, 0.8f) ) * lastYVariation, 
                0
            );
            //float rimAngleRadians = UnityEngine.Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            //float rimAngleRadians = (UnityEngine.Random.Range(15f, 45f) * ( endPointPosition.x == lastPointPosition.x ? lastYVariation : ( endPointPosition.x > lastPointPosition.x ? -1 : 1) ) ) * Mathf.Deg2Rad;
            float rimAngleRadians = (UnityEngine.Random.Range(30, 35f) * lastYVariation ) * Mathf.Deg2Rad;
            Vector3 rimHandle = new Vector3(Mathf.Cos(rimAngleRadians), Mathf.Sin(rimAngleRadians), 0.0f);
            rimHandle *= UnityEngine.Random.Range(0.1f, (size >= GetRealSize() ? 1.8f : 0.5f) ) * lastYVariation;

            for (int i = 0; i <= size; ++i)
            {
                float heightNormalised = (float)i / size;
                Vector3 bezier = Bezier(lastPointPosition, lastPointPosition + rimHandle, endPointPosition + peakHandle, endPointPosition, heightNormalised);
                if(!linePositions.Contains(bezier)) {
                    linePositions.Add(
                        bezier
                    );
                }            

                if(i == size) {
                    lastPointPosition = bezier;
                }
            }
            
            lastStartIndex = (size*currentCycle);

            currentCycle++;    
            //lastYVariation *= -1;
        }

        Vector3 Bezier(Vector3 start, Vector3 controlMid1, Vector3 controlMid2, Vector3 end, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            float mt = 1 - t;
            float mt2 = mt * mt;
            float mt3 = mt2 * mt;

            return start * mt3 + controlMid1 * mt2 * t * 3.0f + controlMid2 * mt * t2 * 3.0f + end * t3;
        }

        private GameObject GetRingElement(Transform parent) {
            return GameObject.Instantiate(
                m_RingObject,
                Vector3.zero,
                Quaternion.identity,
                parent
            );        
        }

        public void ChopPoint()
        {
            if (isChopping) return;

            try
            {
                linePositions.RemoveAt(0);            
            } catch (ArgumentOutOfRangeException)
            {
                
            }
                    
            RenderLine();        
        }

        public GameObject CollidersWrap {
            get { return Colliders; }
        }

        public GameObject EndPoint {
            get { return endPointGO; }
        }

        public int TotalColliders
        {
            get { return colliders.Count; }
        }

        public void ResetGradient()
        {
            lrComp.colorGradient = defaultColor;
        }

        public void SetGradient(Gradient color)
        {
            lrComp.colorGradient = color;
        }

        public LineRenderer LineRenderer
        {
            get { return lrComp; }
        }

        private int GetRealSize() {
            if(targetOptional.GetLength(0) > 0) {
                return size * targetOptional.GetLength(0);
            }

            return size;
        }
    }
}
