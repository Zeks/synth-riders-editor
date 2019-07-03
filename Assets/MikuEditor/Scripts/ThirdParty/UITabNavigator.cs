using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
public class UITabNavigator : MonoBehaviour
{
    private void Awake()
    {
        this._orderedSelectables = new List<Selectable>();
    }
 
    private void Start()
    {
        this.SortSelectables();
    }
 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Navigate backward when holding shift, else navigate forward.
            this.HandleHotkeySelect(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift), false);
        }
    }
 
    /// <summary>
    /// Iterates through all selectables in scene and orders them based on their position.
    /// </summary>
	public void SortSelectables()
	{
		List<Selectable> originalSelectables = Selectable.allSelectables;
		int totalSelectables = originalSelectables.Count;
		this._orderedSelectables = new List<Selectable>(totalSelectables);
		int sortIndex = 0;
		for (int index = 0; index < totalSelectables; ++index)
		{
			Selectable selectable = originalSelectables[index];
			
			if (!selectable.IsInteractable()) continue;
			
			this._orderedSelectables.Insert(
				this.FindSortedIndexForSelectable(sortIndex, selectable), selectable);
			sortIndex++;
		}
	}
 
    private void HandleHotkeySelect(bool isNavigateBackward, bool isWrapAround)
    {
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject != null && selectedObject.activeInHierarchy) // Ensure a selection exists and is not an inactive object.
        {
            Selectable currentSelection = selectedObject.GetComponent<Selectable>();
            if (currentSelection != null)
            {
                Selectable nextSelection = this.FindNextSelectable(
                    this._orderedSelectables.IndexOf(currentSelection), isNavigateBackward, isWrapAround);
                if (nextSelection != null)
                {
                    nextSelection.Select();
                }
            }
            else
            {
                this.SelectFirstSelectable();
            }
        }
        else
        {
            this.SelectFirstSelectable();
        }
    }
 
    private void SelectFirstSelectable()
    {
        if (this._orderedSelectables != null && this._orderedSelectables.Count > 0)
        {
            Selectable firstSelectable = this._orderedSelectables[0];
            firstSelectable.Select();
        }
    }
 
    /// <summary>
    /// Looks at ordered selectable list to find the selectable we are trying to navigate to and returns it.
    /// </summary>
    private Selectable FindNextSelectable(int currentSelectableIndex, bool isNavigateBackward, bool isWrapAround)
    {
        Selectable nextSelection = null;
 
        int totalSelectables = this._orderedSelectables.Count;
        if (totalSelectables > 1)
        {
            if (isNavigateBackward)
            {
                if (currentSelectableIndex == 0)
                {
                    nextSelection = (isWrapAround) ? this._orderedSelectables[totalSelectables - 1] : null;
                }
                else
                {
                    nextSelection = this._orderedSelectables[currentSelectableIndex - 1];
                }
            }
            else // Navigate forward.
            {
                if (currentSelectableIndex == (totalSelectables - 1))
                {
                    nextSelection = (isWrapAround) ? this._orderedSelectables[0] : null;
                }
                else
                {
                    nextSelection = this._orderedSelectables[currentSelectableIndex + 1];
                }
            }
        }
 
        return nextSelection;
    }
 
    /// <summary>
    /// Recursively finds the sorted index by positional order within _orderedSelectables (positional order is determined from left-to-right followed by top-to-bottom).
    /// </summary>
    private int FindSortedIndexForSelectable(int selectableIndex, Selectable selectableToSort)
    {
        int sortedIndex = selectableIndex;
        if (selectableIndex > 0)
        {
            int previousIndex = selectableIndex - 1;
            Vector3 previousSelectablePosition = this._orderedSelectables[previousIndex].transform.position;
            Vector3 selectablePositionToSort = selectableToSort.transform.position;
 
            if ((previousSelectablePosition.y < selectablePositionToSort.y)
                || (previousSelectablePosition.y == selectablePositionToSort.y
                    && previousSelectablePosition.x > selectablePositionToSort.x))
            {
                // Previous selectable is in front, try the previous index:
                sortedIndex = this.FindSortedIndexForSelectable(previousIndex, selectableToSort);
            }
        }
 
        return sortedIndex;
    }
 
    private List<Selectable> _orderedSelectables = null;
}