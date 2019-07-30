using System.Collections;
using System.Collections.Generic;
using MiKu.NET;
using MiKu.NET.Charting;
using UnityEngine;

/// <summary>
/// For the copy and paste operations
/// </summary>
public class Miku_Clipboard : MonoBehaviour {

	public static Miku_Clipboard s_instance;
    
    public static bool Initialized { get; set; }   

    // The clipboard dictionary
    private Dictionary<float, List<EditorNote>> clipboardDict;

    private float clipboardBPM = 0;

	// Use this for initialization
	void Start () {
		if(s_instance != null) {
            DestroyImmediate(this.gameObject);
            return;
        }

        this.transform.parent = null;
        s_instance = this;
        Initialized = true;
        DontDestroyOnLoad(this.gameObject);
	}

    /// <summary>
    /// Save a copy of the passed Dictionary to the clipboard	
    /// </summary>
    /// <param name="workingTrack">The Dictionary to be copied</param>
    public static void CopyTrackToClipboard( Dictionary<float, List<EditorNote>> workingTrack, float pasteBPM ) {
        if(s_instance == null) return;
        
        ClipboardBPM = pasteBPM;
        
        // Check if there are Entries to be copied
        if(workingTrack != null && workingTrack.Count > 0) {
            // Dictionary on where the new data will be copied
            if(CopiedDict != null) {
                CopiedDict.Clear();
            } else {
                CopiedDict = new Dictionary<float, List<EditorNote>>();
            }            

            // Iterate each entry on the Dictionary and get the note to copy
            foreach( KeyValuePair<float, List<EditorNote>> kvp in workingTrack )
            {
                List<EditorNote> _notes = kvp.Value;
                List<EditorNote> copiedList = new List<EditorNote>();

                // Iterate each note and update its info
                for(int i = 0; i < _notes.Count; i++) {
                    EditorNote n = _notes[i];
                    EditorNote newNote = new EditorNote(Vector3.zero);
                    newNote.Position = n.Position;
                    newNote.Id = Track.FormatNoteName(kvp.Key, i, n.HandType);
                    newNote.HandType = n.HandType;
                    newNote.UsageType = n.UsageType;
                    newNote.ComboId = n.ComboId;
                    newNote.Segments = n.Segments;

                    copiedList.Add(newNote);
                }
                
                // Add copied note to the list
                CopiedDict.Add(kvp.Key, copiedList);
            }
        }
    }

    public static Dictionary<float, List<EditorNote>> CopiedDict
    {
        get
        {
            return s_instance.clipboardDict;
        }

        set
        {
            s_instance.clipboardDict = value;
        }
    }

    public static float ClipboardBPM
    {
        get
        {
            return s_instance.clipboardBPM;
        }

        set
        {
            s_instance.clipboardBPM = value;
        }
    }
}
