using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
	private int sizeX = 40;

	private int sizeY = 40;

    [SerializeField]
    private float separationSize = 1f;

    [SerializeField]
    private GameObject gridLines;

    [SerializeField]
    private Color lineColor = Color.white;

    private List<GameObject> GridLines;
    private GameObject GridLinesParent;

    private const int MAX_SIZE_X = 16;

    private const int MAX_SIZE_Y = 12;

    private const float MAX_SEPARATION = 0.1365f;
    private const float MIN_SEPARATION = 0.05f;
    private const float SEPARATION_STEP = 0.005f;

    public float SeparationSize
    {
        get
        {
            return separationSize;
        }

        set
        {
            separationSize = value;
        }
    }

    void Start() {
        GridLines = new List<GameObject>();  
        InstantiateLinesParent();      
        DrawGridLines();       
    }

    void InstantiateLinesParent() {
        GridLinesParent = new GameObject();
        GridLinesParent.name = "[Grid Lines]";
        GridLinesParent.transform.parent = gameObject.transform;
        GridLinesParent.transform.localPosition = Vector3.zero;
        GridLinesParent.transform.rotation = Quaternion.identity;
    }

    public Vector3 GetNearestPointOnGrid(Vector3 position)
    {
        position -= transform.position;

        int xCount = Mathf.RoundToInt(position.x / SeparationSize);
        int yCount = Mathf.RoundToInt(position.y / SeparationSize);
        int zCount = Mathf.RoundToInt(position.z / SeparationSize);

        Vector3 result = new Vector3(
            (float)xCount * SeparationSize,
            (float)yCount * SeparationSize,
            (float)zCount * SeparationSize);

        result += transform.position;

        return result;
    }

    private void OnDrawGizmos()
    {
        SeparationSize = Mathf.Clamp(SeparationSize, MIN_SEPARATION, MAX_SEPARATION);
        CalucalteGridSize();

        Gizmos.color = Color.yellow;
        for (float x = 0; x <= sizeX; ++x)
        {            
            Gizmos.DrawLine(
                transform.TransformPoint(new Vector3(x * SeparationSize, 0, 0)),
                transform.TransformPoint(new Vector3(x * SeparationSize, (sizeY * SeparationSize) * -1, 0))
            );                         
        }

        for (float y = 0; y <= sizeY; ++y)
        {            
            Gizmos.DrawLine(
                transform.TransformPoint(new Vector3(0, (y * SeparationSize) * -1, 0)),
                transform.TransformPoint(new Vector3(sizeX * SeparationSize, (y * SeparationSize) * -1, 0))
            );                         
        }
    }

    private void CalucalteGridSize() {
        float maxX = MAX_SIZE_X * MAX_SEPARATION;
        int index = -1;
        float currX = 0;        
        while(currX < maxX)
        {       
            index++;     
            currX = index * SeparationSize;
            sizeX = index;                      
        }

        float maxY = MAX_SIZE_Y * MAX_SEPARATION;
        index = -1;
        float currY = 0;
        while(currY < maxY)
        {            
            index++;     
            currY = (index * SeparationSize);
            sizeY = index;                       
        }
    }

    public void DrawGridLines() {
        SeparationSize = Mathf.Clamp(SeparationSize, MIN_SEPARATION, MAX_SEPARATION);
        CalucalteGridSize();

        if(GridLines.Count > 0) {
            if(GridLines.Count != sizeX * sizeY) {
                ClearLines();
            }
        }        

        GameObject lineObj;
        LineRenderer targetRenderer;

        for (int x = 0; x <= sizeX; ++x)
        {            
            lineObj = GameObject.Instantiate(gridLines, Vector3.zero, Quaternion.identity, GridLinesParent.transform);
            lineObj.transform.localPosition = Vector3.zero;
            targetRenderer = lineObj.GetComponent<LineRenderer>();

            targetRenderer.SetPosition(0, new Vector3(0 + x * SeparationSize, 0, 0));
            targetRenderer.SetPosition(1, new Vector3(0 + x * SeparationSize, (sizeY * SeparationSize) * -1, 0));     
            GridLines.Add(lineObj);
        }

        for (int y = 0; y <= sizeY; ++y)
        {            
            lineObj = GameObject.Instantiate(gridLines, Vector3.zero, Quaternion.identity, GridLinesParent.transform);
            lineObj.transform.localPosition = Vector3.zero;
            targetRenderer = lineObj.GetComponent<LineRenderer>();

            targetRenderer.SetPosition(0, new Vector3(0, (y * SeparationSize) * -1, 0));
            targetRenderer.SetPosition(1, new Vector3(sizeX * SeparationSize, (y * SeparationSize) * -1, 0)); 
            GridLines.Add(lineObj);               
        }

        GridLines[0].GetComponent<LineRenderer>().sharedMaterial.SetColor("_Color", lineColor);
    }

    private void ClearLines()
    {
        GameObject.DestroyImmediate(GridLinesParent);
        InstantiateLinesParent();
        GridLines.Clear();
    }

    public void ChangeGridSize(bool increment = true) {
        if(increment) {
            SeparationSize += SEPARATION_STEP;
        } else {
            SeparationSize -= SEPARATION_STEP;
        }

        SeparationSize = Mathf.Clamp(SeparationSize, MIN_SEPARATION, MAX_SEPARATION);
        DrawGridLines();
    }
}