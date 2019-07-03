using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiKu.NET.Utils {

	/** Initialite the Metronome and play the sound given the BPM and the max meassure(1/1, 1/2, 1/4, etc) */
	[RequireComponent(typeof(AudioSource))]
	public class Metronome : MonoBehaviour {

		// Global Tick Sound Settings
		private int TICK_SAMPLE_RATE = 44100 / 16;
		private const float TICK_FRECUENCY = 440;
        private int TICK_POSITION = 0;

		// Low Tick
		public AudioClip TickClip {
			get; set;
		}

		private const int TICK_MEASURE_LENGHT = 64;

		// AudioSource
		AudioSource _audioSource;

		private float lastTick = 0;

		// Clip for the metronome sound
		[SerializeField]
		private AudioClip m_tickSound;

		// Beats per Minute
		[SerializeField]
		[Range(40, 218)]
		private double bpm = 120;

		// For the conversiton of beats to miliseconds
		private double miliseconds_unit = 1000;
		private double minute_unit = 60;

		private double beat = 1/1;
		private double BEATS_TO_MS;

		private bool playMetronome = false;

        // Use this for initialization
        void Start () {
			_audioSource = GetComponent<AudioSource>();
			_audioSource.playOnAwake = false;
			_audioSource.loop = false;	

			if(m_tickSound == null) {
				TickClip = AudioClip.Create("MetronomeTick", TICK_SAMPLE_RATE*2, 1, 
					TICK_SAMPLE_RATE*TICK_MEASURE_LENGHT, true, OnAudioRead, OnAudioSetPosition);
			} else {
				TickClip = m_tickSound;
			}	
		}
		
		void OnAudioRead(float[] data)
		{
			int count = 0;
			while (count < data.Length)
			{
				data[count] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * TICK_FRECUENCY * TICK_POSITION / TICK_SAMPLE_RATE));
				TICK_POSITION++;
				count++;
			}
		}

		void OnAudioSetPosition(int newPosition)
		{
			TICK_POSITION = newPosition;
		}

		void PlayTick() {
			TICK_POSITION = 0;
			_audioSource.clip = TickClip;
			_audioSource.PlayOneShot(_audioSource.clip);
		}

		void LateUpdate() {
			if(playMetronome) {
				if(lastTick >= BEATS_TO_MS) {
					// print(lastTick);
					lastTick = 0;
					PlayTick();
				}
			}
		}

		void FixedUpdate() {
			if(playMetronome) {
				lastTick += Time.fixedDeltaTime;
			}
		}

		public void Play(float startingAt = 0) {
			BEATS_TO_MS = ( ( (miliseconds_unit*minute_unit / BPM) * (4.0f*(beat)) ) / 4.0f ) / miliseconds_unit;

			/*if(!playMetronome) {
				PlayTick();
			}*/

			lastTick = startingAt;
			playMetronome = true;
		}

		public void Stop() {			
			lastTick = 0;
			playMetronome = false;
		}

	#region Setters and Getters
			
        public double BPM
        {
            get
            {
                return bpm;
            }

            set
            {
                bpm = value;//Mathf.Clamp(value, 40, 218);
            }
        }

	#endregion
	}
}
