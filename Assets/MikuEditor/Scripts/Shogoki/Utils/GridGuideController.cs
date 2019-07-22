using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shogoki.Utils {
    public class GridGuideController : MonoBehaviour
    {
        public enum GridGuideType {
            Solid,
            Outline,
            Circles
        }
        
        private const string GRID_GUIDE_TYPE = "com.synth.editor.GRID_GUIDE_TYPE";

        [SerializeField]
        private GameObject GridOutline;

        [SerializeField]
        private GameObject GridSolid;

        [SerializeField]
        private GameObject GridCircles;

        private GridGuideType currentGuideType = GridGuideType.Solid;

        public GridGuideType CurrentGuideType
        {
            get
            {
                return currentGuideType;
            }

            private set
            {
                currentGuideType = value;
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            GridOutline.SetActive(false);
            GridSolid.SetActive(false);
            GridCircles.SetActive(false);

            CurrentGuideType = (GridGuideController.GridGuideType)(PlayerPrefs.GetInt(GRID_GUIDE_TYPE, 0));
        }

        void OnDestroy() {
            PlayerPrefs.SetInt(GRID_GUIDE_TYPE, (int)CurrentGuideType);
        }   

        void OnEnable() {
            DisplayGridGuide();
        }

        private void DisplayGridGuide()
        {
            if(CurrentGuideType == GridGuideType.Solid) {
                GridOutline.SetActive(false);
                GridSolid.SetActive(true);
                GridCircles.SetActive(false);
            } else if (CurrentGuideType == GridGuideType.Outline) {
                GridOutline.SetActive(true);
                GridSolid.SetActive(false);
                GridCircles.SetActive(false);
            } else if (CurrentGuideType == GridGuideType.Circles) { 
                GridOutline.SetActive(false);
                GridSolid.SetActive(false);
                GridCircles.SetActive(true);
            }
        }

        public void SwitchGridGuideType() {
            CurrentGuideType++;
            // Debug.Log("Current type "+CurrentGuideType);
            if((int)CurrentGuideType >= Enum.GetNames(typeof(GridGuideType)).Length) {
                CurrentGuideType = 0;
            }
               
            DisplayGridGuide();
        }
    }
}