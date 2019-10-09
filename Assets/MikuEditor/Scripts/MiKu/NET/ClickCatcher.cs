using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace MiKu.NET {

    public class ClickCatcher : MonoBehaviour, IPointerClickHandler {

        private void OffsetTheGridToTime(TimeWrapper time) {
            TimeWrapper convertedTime = 0;
            StepDataHolder stepHolder = Track.s_instance. GetDataForCurrentStepMode();
            int savedStepsInBeat = stepHolder.stepsInBeat;
            stepHolder.stepsInBeat = 64;
            convertedTime = Track.s_instance.SnapToStep(time);
            time = convertedTime;
            stepHolder.stepsInBeat = savedStepsInBeat;
            convertedTime = Track.s_instance.SnapToStep(time);
            if(convertedTime == 0)
                return;

            float diff = time.FloatValue - convertedTime.FloatValue;
            Track.s_instance.SetNewGridOffset(diff);
        }
        
        private void PlaceWithSnapToStep(TimeWrapper time, PointerEventData.InputButton button) {
            TimeWrapper convertedTime = 0;
            convertedTime = Track.s_instance.SnapToStep(time);
            if(convertedTime == 0)
                return;

            if(button == PointerEventData.InputButton.Left)
                Track.s_instance.AddPlaceholderToChart(convertedTime);
            else
                Track.s_instance.RemovePlaceholderFromChart(convertedTime);
        }

        private void PlaceWithSnapToBars(TimeWrapper time, PointerEventData.InputButton button) {
            TimeWrapper convertedTime = 0;
            convertedTime = FrequencyData.SnapToBar(Track.s_instance.frequencyData, Track.s_instance.StartOffset.FloatValue, time, Track.PlacerClickSnapMode.MajorBar);
            if(convertedTime == 0)
                return;

            if(button == PointerEventData.InputButton.Left)
                Track.s_instance.AddPlaceholderToChart(convertedTime);
            else
                Track.s_instance.RemovePlaceholderFromChart(convertedTime);
        }

        public void OnPointerClick(PointerEventData eventData) {
            Vector2 clickPosition;
            var transform = GetComponent<RectTransform>();
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform, eventData.position, null, out clickPosition))
                return;


            float originY = 0.56777f*transform.sizeDelta.y;

            float yCoord = (float)transform.sizeDelta.y/2f - clickPosition.y;

            float delta = originY - yCoord;
            float deltaPercent = delta/transform.sizeDelta.y;
            float deltaRealWorld = 1482*deltaPercent;

            TimeWrapper time = Track.CurrentTime + deltaRealWorld;
            if(Track.s_instance.isSHIFTDown)
                OffsetTheGridToTime(time);
            else if(Track.s_instance.isALTDown)
                PlaceWithSnapToBars(time, eventData.button);
            else
                PlaceWithSnapToStep(time, eventData.button);

            //print("clicked position on the gameobject  is :" + clickPosition.x);
        }


    }
}