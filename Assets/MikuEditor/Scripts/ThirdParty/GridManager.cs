using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
	[SerializeField]
	private float sizeX = 40;

	[SerializeField]
	private float sizeY = 40;

    [SerializeField]
    private float separationSize = 1f;

    public Vector3 GetNearestPointOnGrid(Vector3 position)
    {
        position -= transform.position;

        int xCount = Mathf.RoundToInt(position.x / separationSize);
        int yCount = Mathf.RoundToInt(position.y / separationSize);
        int zCount = Mathf.RoundToInt(position.z / separationSize);

        Vector3 result = new Vector3(
            (float)xCount * separationSize,
            (float)yCount * separationSize,
            (float)zCount * separationSize);

        result += transform.position;

        return result;
    }

    private void OnDrawGizmos()
    {
        /*Gizmos.color = Color.yellow;
        for (float x = 0; x < sizeX; x += separationSize)
        {
            for (float y = 0; y < sizeY; y += separationSize)
            {
                var point = GetNearestPointOnGrid(transform.position - new Vector3(x, y, 0f));
                Gizmos.DrawSphere(point, 0.05f);
            }
                
        }*/
        //Gizmos.DrawSphere(new Vector3(transform.localPosition.x, transform.localPosition.y, 0), 0.05f);
        //Gizmos.DrawSphere(GetNearestPointOnGrid(transform.position - new Vector3(0, 0, 0f)), 0.05f);
    }
}