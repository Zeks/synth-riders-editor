using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace MiKu.NET.Charting {

    [Serializable]
    /// <summary>
    /// Notes Class for the note representation
    /// </summary>
    public class EditorNote {
        public enum NoteDirection {
            None,
            Right,
            RightBottom,
            Bottom,
            LeftBottom,
            Left,
            LeftTop,
            Top,
            RightTop,
        }

        public enum NoteHandType {
            RightHanded,
            LeftHanded,
            OneHandSpecial,
            BothHandsSpecial,
            SeparateHandSpecial,
            NoHand,
        };

        public enum NoteUsageType {
            None,
            Ball,
            Line,
            Breaker
        };

        ~EditorNote() {
            IdDictionaries.RemoveNote(noteId);
            TimeDictionaries.RemoveNote(this);
        }

        public EditorNote Clone() {
            EditorNote newNote = new EditorNote();
            newNote.timePoint = this.timePoint;
            newNote.ComboId = this.ComboId;
            newNote.HandType = this.HandType;
            newNote.UsageType = this.UsageType;
            newNote.Position = this.Position;
            newNote.Direction = this.Direction;
            newNote.nullNote = this.nullNote;
            return newNote;
        }

        private string _id;

        public int noteId = -1;
        public int railId = -1;

        public bool nullNote = false;

        private static int noteCounter = 0;

        private float timePoint;

        /// <value>
		/// ID for cache use, when set the format used is Note_{value passed}
        /// there's literally no point in using anything but the id tho
        /// a proper name can just as well be formatted where it's needed
        /// but for object find purposes Note_UNIQUEID will suffice
		/// </value>
        public string name
        {
            get
            {
                return _id;
            }

            set
            {
                _id = string.Format("Note_{0}", value);
            }
        }

        public GameObject GameObject { get; set; }


        /// <value>
        /// Combo Id that the note bellow to, is 0 based meaing that -1 means that the note doesnt belong to any combo
        /// </value>
        public int ComboId { get; set; }

        /// <value>
		/// Position of the note on the space
		/// </value>
        public float[] Position { get; set; }



        /// <value>
        /// Segments of the line tha form the note
        /// </value>
        public float[,] Segments { get; set; }

        /// <value>
		/// Type of the note's hand
		/// </value>
        public NoteHandType HandType { get; set; }

        /// <value>
        /// Type of the note usage
        /// </value>
        public NoteUsageType UsageType { get; set; }

        /// <value>
		/// Direction to hit the note
		/// </value>
        public NoteDirection Direction { get; set; }

        public float TimePoint
        {
            get
            {
                return timePoint;
            }

            set
            {
                timePoint = value;
            }
        }

        public EditorNote(UnityEngine.Vector3 pos, float time = default(float), int idCmb = -1, NoteHandType t = NoteHandType.OneHandSpecial, NoteDirection d = NoteDirection.None) {
            noteId = noteCounter++;
            name = noteId.ToString();

            IdDictionaries.AddNote(noteId, this);
            if(time != default(float))
                TimeDictionaries.AddNote(time, this);
            timePoint = time;
            ComboId = idCmb;
            HandType = t;
            UsageType = EditorNote.NoteUsageType.Ball;
            Position = new float[3] { pos.x, pos.y, pos.z };
            Direction = d;
        }

        public EditorNote(float time, UnityEngine.Vector3 pos, NoteHandType handType, NoteUsageType usageType) {
            noteId = noteCounter++;
            name = noteId.ToString();

            IdDictionaries.AddNote(noteId, this);
            if(time != default(float))
                TimeDictionaries.AddNote(time, this);
            timePoint = time;
            HandType = handType;
            UsageType = usageType;
            Position = new float[3] { pos.x, pos.y, pos.z };
        }

        public EditorNote() {
            noteId = noteCounter++;
            UsageType = EditorNote.NoteUsageType.Ball;
        }
        public void Log() {
            Trace.WriteLine("Note ID: " + noteId + "time: " + TimePoint + "position:"  + Position + " handtype:" + HandType+  " usagetype:" + UsageType +  " combo:" + ComboId);
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the beats Representaion
    /// </summary>
    public class EditorBeats {

        /// <value>
        /// Notes for the easy dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Easy { get; set; }

        /// <value>
        /// Notes for the normal dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Normal { get; set; }

        /// <value>
        /// Notes for the hard dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Hard { get; set; }

        /// <value>
        /// Notes for the expert dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Expert { get; set; }

        /// <value>
        /// Notes for the Master dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Master { get; set; }

        /// <value>
        /// Notes for the Custom dificulty
        /// </value>
        public Dictionary<float, List<EditorNote>> Custom { get; set; }
    }


    /// <summary>
    /// Class for the beats Representaion
    /// </summary>
    public class EditorRails {

        /// <value>
        /// Notes for the easy dificulty
        /// </value>
        public  List<Rail> Easy { get; set; }

        /// <value>
        /// Notes for the normal dificulty
        /// </value>
        public List<Rail> Normal { get; set; }

        /// <value>
        /// Notes for the hard dificulty
        /// </value>
        public List<Rail> Hard { get; set; }

        /// <value>
        /// Notes for the expert dificulty
        /// </value>
        public  List<Rail> Expert { get; set; }

        /// <value>
        /// Notes for the Master dificulty
        /// </value>
        public List<Rail> Master { get; set; }

        /// <value>
        /// Notes for the Custom dificulty
        /// </value>
        public List<Rail> Custom { get; set; }
    }

    [Serializable]
    /// <summary>
    /// Class for the Effects Representaion
    /// </summary>
    public class EditorEffects {
        /// <value>
        /// Effects for the easy dificulty
        /// </value>
        public List<float> Easy { get; set; }

        /// <value>
        /// Effects for the Normal dificulty
        /// </value>
        public List<float> Normal { get; set; }

        /// <value>
        /// Effects for the Hard dificulty
        /// </value>
        public List<float> Hard { get; set; }

        /// <value>
        /// Effects for the Expert dificulty
        /// </value>
        public List<float> Expert { get; set; }

        /// <value>
        /// Effects for the Master dificulty
        /// </value>
        public List<float> Master { get; set; }

        /// <value>
        /// Effects for the Custom dificulty
        /// </value>
        public List<float> Custom { get; set; }
    }

    [Serializable]
    /// <summary>
    /// Class for the Lights Representaion
    /// </summary>
    public class EditorLights {
        /// <value>
        /// Lights for the easy dificulty
        /// </value>
        public List<float> Easy { get; set; }

        /// <value>
        /// Lights for the Normal dificulty
        /// </value>
        public List<float> Normal { get; set; }

        /// <value>
        /// Lights for the Hard dificulty
        /// </value>
        public List<float> Hard { get; set; }

        /// <value>
        /// Lights for the Expert dificulty
        /// </value>
        public List<float> Expert { get; set; }

        /// <value>
        /// Lights for the Master dificulty
        /// </value>
        public List<float> Master { get; set; }

        /// <value>
        /// Lights for the Custom dificulty
        /// </value>
        public List<float> Custom { get; set; }
    }

    [Serializable]
    public struct EditorBookmark {
        public float time;
        public string name;
    }

    [Serializable]
    /// <summary>
    /// Class for the Bookmars Representaion
    /// </summary>
    public class EditorBookmarks {
        /// <value>
        /// Effects for the beatmap
        /// </value>
        public List<EditorBookmark> BookmarksList { get; set; }

        public EditorBookmarks() {
            if(BookmarksList == null) {
                BookmarksList = new List<EditorBookmark>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the Jumps Representaion
    /// </summary>
    public class EditorJumps {
        /// <value>
        /// Jumps for the easy dificulty
        /// </value>
        public List<float> Easy { get; set; }

        /// <value>
        /// Jumps for the Normal dificulty
        /// </value>
        public List<float> Normal { get; set; }

        /// <value>
        /// Jumps for the Hard dificulty
        /// </value>
        public List<float> Hard { get; set; }

        /// <value>
        /// Jumps for the Expert dificulty
        /// </value>
        public List<float> Expert { get; set; }

        /// <value>
        /// Jumps for the Master dificulty
        /// </value>
        public List<float> Master { get; set; }

        /// <value>
        /// Jumps for the Custom dificulty
        /// </value>
        public List<float> Custom { get; set; }
    }

    [Serializable]
    /// <summary>
    /// Class for the Crouch Representaion
    /// </summary>
    public class EditorCrouchs {
        /// <value>
        /// Crouchs for the easy dificulty
        /// </value>
        public List<float> Easy { get; set; }

        /// <value>
        /// Crouchs for the Normal dificulty
        /// </value>
        public List<float> Normal { get; set; }

        /// <value>
        /// Crouchs for the Hard dificulty
        /// </value>
        public List<float> Hard { get; set; }

        /// <value>
        /// Crouchs for the Expert dificulty
        /// </value>
        public List<float> Expert { get; set; }

        /// <value>
        /// Crouchs for the Master dificulty
        /// </value>
        public List<float> Master { get; set; }

        /// <value>
        /// Crouchs for the Custom dificulty
        /// </value>
        public List<float> Custom { get; set; }
    }

    [Serializable]
    public struct EditorSlide {
        public float time;
        public EditorNote.NoteHandType slideType;

        public bool initialized;
    }

    [Serializable]
    /// <summary>
    /// Class for the Slides Representaion
    /// </summary>
    public class EditorSlides {
        /// <value>
        /// Slides for the easy dificulty
        /// </value>
        public List<EditorSlide> Easy { get; set; }

        /// <value>
        /// Slides for the Normal dificulty
        /// </value>
        public List<EditorSlide> Normal { get; set; }

        /// <value>
        /// Slides for the Hard dificulty
        /// </value>
        public List<EditorSlide> Hard { get; set; }

        /// <value>
        /// Slides for the Expert dificulty
        /// </value>
        public List<EditorSlide> Expert { get; set; }

        /// <value>
        /// Slides for the Expert dificulty
        /// </value>
        public List<EditorSlide> Master { get; set; }

        /// <value>
        /// Slides for the Custom dificulty
        /// </value>
        public List<EditorSlide> Custom { get; set; }
    }

    [Serializable]
    public struct EditorDrum {
        public float time;
        public int playType;
        public string audio;
    }

    [Serializable]
    /// <summary>
    /// Class for the Slides Representaion
    /// </summary>
    public class EditorDrumData {
        /// <value>
        /// Drum for the easy dificulty
        /// </value>
        public List<EditorDrum> Easy { get; set; }

        /// <value>
        /// Drum for the Normal dificulty
        /// </value>
        public List<EditorDrum> Normal { get; set; }

        /// <value>
        /// Drum for the Hard dificulty
        /// </value>
        public List<EditorDrum> Hard { get; set; }

        /// <value>
        /// Drum for the Expert dificulty
        /// </value>
        public List<EditorDrum> Expert { get; set; }

        /// <value>
        /// Drum for the Expert dificulty
        /// </value>
        public List<EditorDrum> Master { get; set; }

        /// <value>
        /// Drum for the Custom dificulty
        /// </value>
        public List<EditorDrum> Custom { get; set; }
    }

    [Serializable]
    /// <summary>
    /// Serilazable class of the Chart made from the user
    /// </summary>
    public class EditorChart {

        /// <value>
		/// Name of the chart
		/// </value>
        public string Name { get; set; }

        /// <value>
		/// Author of the chart/song
		/// </value>
        public string Author { get; set; }

        /// <value>
		/// Artwork name to use for the chart
		/// </value>
        public string Artwork { get; set; }

        /// <value>
		/// Artwork base64 data
		/// </value>
        public string ArtworkBytes { get; set; }

        /// <value>
		/// Name of the song that belong to the chart 
		/// </value>
        public string AudioName { get; set; }

        /// <value>
		/// Audio Data of the song that belong to the chart 
		/// </value>
        public float[] AudioData { get; set; }

        /// <value>
		/// Audio Frecuency of the song that belong to the chart 
		/// </value>
        public int AudioFrecuency { get; set; }

        /// <value>
		/// Number of channels of the song that belong to the chart 
		/// </value>
        public int AudioChannels { get; set; }

        /// <value>
		/// BPM of the Chart
		/// </value>
        public float BPM { get; set; }

        /// <value>
        /// Offset on seconds befor the Song start playing
        /// </value>
        public float Offset { get; set; }

        /// <value>
		/// List of beats that made the Chart
		/// </value>
        public EditorBeats Track { get; set; }
        
        /// <value>
		/// List of Rails that made the Chart
		/// </value>
        public EditorRails Rails { get; set; }

        /// <value>
        /// List of the effects for the chart
        /// </value>
        public EditorEffects Effects { get; set; }

        /// <value>
        /// List of the bookmars for the chart
        /// </value>
        public EditorBookmarks Bookmarks { get; set; }

        /// <value>
        /// List of the Jumps for the chart
        /// </value>
        public EditorJumps Jumps { get; set; }

        /// <value>
        /// List of the Crouchs for the chart
        /// </value>
        public EditorCrouchs Crouchs { get; set; }

        /// <value>
        /// List of the Slides for the chart
        /// </value>
        public EditorSlides Slides { get; set; }

        /// <value>
        /// List of the Lights for the chart
        /// </value>
        public EditorLights Lights { get; set; }

        /// <value>
        /// List of the Drum for the chart
        /// </value>
        public EditorDrumData DrumSamples { get; set; }

        /// <value>
        /// The path of the file on disk
        /// </value>
        public string FilePath { get; set; }

        /// <value>
        /// Is true the chart can only be edited with Admin Mode
        /// <value>
        public bool IsAdminOnly { get; set; }

        /// <value>
        /// Version of the Editor in what the Chart was made
        /// </value>
        public string EditorVersion { get; set; }

        /// <value>
        /// Name of the creator of the beatmap
        /// </value>
        public string Beatmapper { get; set; }

        public string CustomDifficultyName { get; set; }

        public float CustomDifficultySpeed { get; set; }

        public List<string> Tags { get; set; }
    }
}