using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiKu.NET.Charting
{

    [Serializable]
    /// <summary>
    /// Notes Class for the note representation
    /// </summary>
    public class Note
    {
        public enum NoteDirection
        {
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

        public enum NoteType
        {
            RightHanded,
            LeftHanded,
            OneHandSpecial,
            BothHandsSpecial,
            SeparateHandSpecial,
            NoHand,
        };

        private string _id;

        /// <value>
		/// ID for cache use, when set the format used is Note_{value passed}
		/// </value>
        public string Id
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
		/// Type of the note
		/// </value>
        public NoteType Type { get; set; }

        /// <value>
		/// Direction to hit the note
		/// </value>
        public NoteDirection Direction { get; set; }

        public Note(UnityEngine.Vector3 pos, string idRoot = "", int idCmb = -1, NoteType t = NoteType.OneHandSpecial, NoteDirection d = NoteDirection.None)
        {
            if (idRoot != null)
            {
                Id = idRoot.ToString();
            }

            ComboId = idCmb;
            Type = t;
            Position = new float[3] { pos.x, pos.y, pos.z };
            Direction = d;
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the beats Representaion
    /// </summary>
    public class Beats
    {

        /// <value>
        /// Notes for the easy dificulty
        /// </value>
        public Dictionary<float, List<Note>> Easy { get; set; }

        /// <value>
        /// Notes for the normal dificulty
        /// </value>
        public Dictionary<float, List<Note>> Normal { get; set; }

        /// <value>
        /// Notes for the hard dificulty
        /// </value>
        public Dictionary<float, List<Note>> Hard { get; set; }

        /// <value>
        /// Notes for the expert dificulty
        /// </value>
        public Dictionary<float, List<Note>> Expert { get; set; }

        /// <value>
        /// Notes for the Master dificulty
        /// </value>
        public Dictionary<float, List<Note>> Master { get; set; }

        /// <value>
        /// Notes for the Custom dificulty
        /// </value>
        public Dictionary<float, List<Note>> Custom { get; set; }

        public Beats()
        {
            if (Easy == null)
            {
                Easy = new Dictionary<float, List<Note>>();
            }
            if (Normal == null)
            {
                Normal = new Dictionary<float, List<Note>>();
            }
            if (Hard == null)
            {
                Hard = new Dictionary<float, List<Note>>();
            }
            if (Expert == null)
            {
                Expert = new Dictionary<float, List<Note>>();
            }
            if (Master == null)
            {
                Master = new Dictionary<float, List<Note>>();
            }
            if (Custom == null)
            {
                Custom = new Dictionary<float, List<Note>>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the Effects Representaion
    /// </summary>
    public class Effects
    {
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

        public Effects()
        {
            if (Easy == null)
            {
                Easy = new List<float>();
            }
            if (Normal == null)
            {
                Normal = new List<float>();
            }
            if (Hard == null)
            {
                Hard = new List<float>();
            }
            if (Expert == null)
            {
                Expert = new List<float>();
            }
            if (Master == null)
            {
                Master = new List<float>();
            }
            if (Custom == null)
            {
                Custom = new List<float>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the Lights Representaion
    /// </summary>
    public class Lights
    {
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

        public Lights()
        {
            if (Easy == null)
            {
                Easy = new List<float>();
            }
            if (Normal == null)
            {
                Normal = new List<float>();
            }
            if (Hard == null)
            {
                Hard = new List<float>();
            }
            if (Expert == null)
            {
                Expert = new List<float>();
            }
            if (Master == null)
            {
                Master = new List<float>();
            }
            if (Custom == null)
            {
                Custom = new List<float>();
            }
        }
    }

    [Serializable]
    public struct Bookmark
    {
        public float time;
        public string name;
    }

    [Serializable]
    /// <summary>
    /// Class for the Bookmars Representaion
    /// </summary>
    public class Bookmarks
    {
        /// <value>
        /// Effects for the beatmap
        /// </value>
        public List<Bookmark> BookmarksList { get; set; }

        public Bookmarks()
        {
            if (BookmarksList == null)
            {
                BookmarksList = new List<Bookmark>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the Jumps Representaion
    /// </summary>
    public class Jumps
    {
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

        public Jumps()
        {
            if (Easy == null)
            {
                Easy = new List<float>();
            }
            if (Normal == null)
            {
                Normal = new List<float>();
            }
            if (Hard == null)
            {
                Hard = new List<float>();
            }
            if (Expert == null)
            {
                Expert = new List<float>();
            }
            if (Master == null)
            {
                Master = new List<float>();
            }
            if (Custom == null)
            {
                Custom = new List<float>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Class for the Crouch Representaion
    /// </summary>
    public class Crouchs
    {
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

        public Crouchs()
        {
            if (Easy == null)
            {
                Easy = new List<float>();
            }
            if (Normal == null)
            {
                Normal = new List<float>();
            }
            if (Hard == null)
            {
                Hard = new List<float>();
            }
            if (Expert == null)
            {
                Expert = new List<float>();
            }
            if (Master == null)
            {
                Master = new List<float>();
            }
            if (Custom == null)
            {
                Custom = new List<float>();
            }
        }
    }

    [Serializable]
    public struct Slide
    {
        public float time;
        public Note.NoteType slideType;

        public bool initialized;
    }

    [Serializable]
    /// <summary>
    /// Class for the Slides Representaion
    /// </summary>
    public class Slides
    {
        /// <value>
        /// Slides for the easy dificulty
        /// </value>
        public List<Slide> Easy { get; set; }

        /// <value>
        /// Slides for the Normal dificulty
        /// </value>
        public List<Slide> Normal { get; set; }

        /// <value>
        /// Slides for the Hard dificulty
        /// </value>
        public List<Slide> Hard { get; set; }

        /// <value>
        /// Slides for the Expert dificulty
        /// </value>
        public List<Slide> Expert { get; set; }

        /// <value>
        /// Slides for the Expert dificulty
        /// </value>
        public List<Slide> Master { get; set; }

        /// <value>
        /// Slides for the Custom dificulty
        /// </value>
        public List<Slide> Custom { get; set; }

        public Slides()
        {
            if (Easy == null)
            {
                Easy = new List<Slide>();
            }
            if (Normal == null)
            {
                Normal = new List<Slide>();
            }
            if (Hard == null)
            {
                Hard = new List<Slide>();
            }
            if (Expert == null)
            {
                Expert = new List<Slide>();
            }
            if (Master == null)
            {
                Master = new List<Slide>();
            }
            if (Custom == null)
            {
                Custom = new List<Slide>();
            }
        }
    }

    [Serializable]
    /// <summary>
    /// Serilazable class of the Chart made from the user
    /// </summary>
    public class Chart
    {

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
        public Beats Track = new Beats();

        /// <value>
        /// List of the effects for the chart
        /// </value>
        public Effects Effects = new Effects();

        /// <value>
        /// List of the bookmars for the chart
        /// </value>
        public Bookmarks Bookmarks = new Bookmarks();

        /// <value>
        /// List of the Jumps for the chart
        /// </value>
        public Jumps Jumps = new Jumps();

        /// <value>
        /// List of the Crouchs for the chart
        /// </value>
        public Crouchs Crouchs = new Crouchs();

        /// <value>
        /// List of the Slides for the chart
        /// </value>
        public Slides Slides = new Slides();

        /// <value>
        /// List of the Lights for the chart
        /// </value>
        public Lights Lights = new Lights();

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

        public Chart()
        {
            if (Bookmarks == null)
            {
                Bookmarks = new Bookmarks();
            }
            if (Jumps == null)
            {
                Jumps = new Jumps();
            }
            if (Crouchs == null)
            {
                Crouchs = new Crouchs();
            }
            if (Slides == null)
            {
                Slides = new Slides();
            }
            if (Lights == null)
            {
                Lights = new Lights();
            }
            if (Tags == null)
            {
                Tags = new List<string>();
            }
        }
    }
}