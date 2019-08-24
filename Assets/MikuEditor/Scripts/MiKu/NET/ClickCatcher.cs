using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MiKu.NET {

    public class ClickCatcher : MonoBehaviour, IPointerClickHandler {
        public void OnPointerClick(PointerEventData eventData) {
            Vector2 clickPosition;
            var transform = GetComponent<RectTransform>();
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform, eventData.position, null, out clickPosition))
                return;


            float originY = 0.5643f*transform.sizeDelta.y;

            float yCoord = (float)transform.sizeDelta.y/2f - clickPosition.y;

            float delta = originY - yCoord;
            float deltaPercent = delta/transform.sizeDelta.y;
            float deltaRealWorld = 1383f*deltaPercent;

            float time = Track.CurrentTime + deltaRealWorld;
            float convertedTime = 0;
            if(eventData.button == PointerEventData.InputButton.Left)
                convertedTime = Track.s_instance.SnapToPeak(time, Track.PlacerClickSnapMode.MinorBar);
            else
                convertedTime = Track.s_instance.SnapToPeak(time, Track.PlacerClickSnapMode.MajorBar);
            Track.s_instance.AddPlaceholderToChart(convertedTime);

            print("clicked position on the gameobject  is :" + clickPosition.x);
        }


    }
}