using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltMetronome : MonoBehaviour {

	public double bpm = 140.0F;

    double nextTick = 0.0F; // The next tick in dspTime
    double sampleRate = 0.0F; 
    bool ticked = false;
	public bool playMetronome = false;

	AudioSource _audioSource;

	public AudioClip TickClip {
		get; set;
	}

    void Start() {
        double startTick = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;

        nextTick = startTick + (60.0 / bpm);

		_audioSource = GetComponent<AudioSource>();
		_audioSource.playOnAwake = false;
		_audioSource.loop = false;	
    }

    void LateUpdate() {
		if(playMetronome) {
			if ( !ticked && nextTick >= AudioSettings.dspTime ) {
				ticked = true;
				BroadcastMessage( "OnTick" );
			}
		}
        
    }

    // Just an example OnTick here
    void OnTick() {
        Debug.Log( "Tick" );
        // GetComponent<AudioSource>().Play();
		_audioSource.clip = TickClip;
		_audioSource.PlayOneShot(_audioSource.clip);
    }

    void FixedUpdate() {
		if(playMetronome) {
			double timePerTick = 60.0f / bpm;
			double dspTime = AudioSettings.dspTime;

			while ( dspTime >= nextTick ) {
				ticked = false;
				nextTick += timePerTick;
			}
		}
    }
}
