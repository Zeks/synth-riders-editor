using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace MiKu.NET.Charting {

    // this is here just so there's no need to do if x == null for each foreach
    public static class LinqHelper {
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T> source) {
            return source ?? Enumerable.Empty<T>();
        }
    }

    // this class manages the conversion of Editor's structures into game's structures and vice versa
    // the decoupling will allow the editor's code to evolve unrestricted by deserialization concerns on both ends
    // i.e. Editors structures are fully independent and are passed to game's structures (in ExportedClasses) on load/save
    // whenever a chart is getting deserialized from game's structures, this class needs to be used as intermediary 
    // between improrted data and working set in the actual editor
    // same for the file save operation.
    class ChartConverter {

        // these need to be nulled each time a context switch is performed 
        // aka: getting back to main menu, or closing the song open window without procceeding further
        public static EditorChart editorChart; // holder for editor's chart
        public static Chart gameChart; // holder for game's chart


        // Converts NoteType enums between game and the editor
        private EditorNote.NoteHandType ConvertGameNoteTypeToEditorNoteType(Note.NoteType type) {
            switch(type) {
                case Note.NoteType.BothHandsSpecial:
                    return EditorNote.NoteHandType.BothHandsSpecial;
                case Note.NoteType.LeftHanded:
                    return EditorNote.NoteHandType.LeftHanded;
                case Note.NoteType.NoHand:
                    return EditorNote.NoteHandType.NoHand;
                case Note.NoteType.OneHandSpecial:
                    return EditorNote.NoteHandType.OneHandSpecial;
                case Note.NoteType.RightHanded:
                    return EditorNote.NoteHandType.RightHanded;
                case Note.NoteType.SeparateHandSpecial:
                    return EditorNote.NoteHandType.SeparateHandSpecial;
                default:
                    return EditorNote.NoteHandType.BothHandsSpecial;
            }

        }

        // Converts NoteType enums between game and the editor
        private Note.NoteType ConvertEditorNoteTypeToGameNoteType(EditorNote.NoteHandType type) {
            switch(type) {
                case EditorNote.NoteHandType.BothHandsSpecial:
                    return Note.NoteType.BothHandsSpecial;
                case EditorNote.NoteHandType.LeftHanded:
                    return Note.NoteType.LeftHanded;
                case EditorNote.NoteHandType.NoHand:
                    return Note.NoteType.NoHand;
                case EditorNote.NoteHandType.OneHandSpecial:
                    return Note.NoteType.OneHandSpecial;
                case EditorNote.NoteHandType.RightHanded:
                    return Note.NoteType.RightHanded;
                case EditorNote.NoteHandType.SeparateHandSpecial:
                    return Note.NoteType.SeparateHandSpecial;
                default:
                    return Note.NoteType.BothHandsSpecial;
            }

        }

        // Adds EditorDrum instance to a list of game's drums
        void PassEditorDrumDataToGame(EditorDrum editorDrum, List<Drum> drums) {
            if(drums == null)
                return;
            Drum drum = new Drum()
            {
                time = editorDrum.time.FloatValue,
                audio = editorDrum.audio,
                playType= editorDrum.playType
            };
            drums.Add(drum);
        }

        // Adds EditorSlide instance to a list of game's slides
        void PassEditorSlideDataToGame(EditorSlide editorSlide, List<Slide> slides) {
            if(slides == null)
                return;
            Slide slide = new Slide()
            {
                initialized = editorSlide.initialized,
                slideType = (Note.NoteType)editorSlide.slideType,
                time = editorSlide.initialTime.FloatValue
            };
            slides.Add(slide);
        }

        // Fully passes the Editor's single difficulty note data to game's note data
        void PassEditorNoteDataToGame(Dictionary<TimeWrapper, List<EditorNote>> editorDictionary, Dictionary<float, List<Note>> exportValue) {
            if(editorDictionary == null)
                return;
            if(exportValue == null) {
                exportValue = new Dictionary<float, List<Note>>();
            }

            List<TimeWrapper> keys = editorDictionary.Keys.ToList();
            if(keys == null)
                return;
            keys.Sort();

            foreach(TimeWrapper key in keys.OrEmptyIfNull()) {
                List<EditorNote> listAtCurrentTime = editorDictionary[key];
                // will need to create note's name in the format that game understands
                foreach(var editorNote in listAtCurrentTime.OrEmptyIfNull()) {
                    if(!exportValue.ContainsKey(editorNote.InitialTimePoint.FloatValue))
                        exportValue.Add(editorNote.InitialTimePoint.FloatValue, new List<Note>());

                    Note exportNote = new Note(new UnityEngine.Vector3 { x = editorNote.Position[0], y = editorNote.Position[1], z = Track.MStoUnit(editorNote.InitialTimePoint) },
                        editorNote.name, editorNote.ComboId, ConvertEditorNoteTypeToGameNoteType(editorNote.HandType));
                    exportNote.Segments = editorNote.Segments;
                    exportValue[editorNote.InitialTimePoint.FloatValue].Add(exportNote);
                }
            }
        }

        void PassEditorRailDataToGame(List<Rail> rails, Dictionary<float, List<Note>> exportValue) {
            if(rails == null)
                return;

            if(exportValue == null) {
                exportValue = new Dictionary<float, List<Note>>();
            }
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                EditorNote leaderNote = rail.Leader.thisNote; // !!!
                if(!exportValue.ContainsKey(leaderNote.InitialTimePoint.FloatValue)) {
                    exportValue.Add(leaderNote.InitialTimePoint.FloatValue, new List<Note>());
                }
                // first we create a note to pass to the game, theb go over the rail assigning the segments
                Vector3 pos = new Vector3(leaderNote.Position[0], leaderNote.Position[1], leaderNote.Position[2]);
                Note gameNote = new Note(pos, "Rail_" + leaderNote.noteId, -1 /* will be assigned later */, ConvertEditorNoteTypeToGameNoteType(leaderNote.HandType));
                gameNote.Segments = new float[rail.notesByTime.Count-1, 3];
                int i = 0;
                foreach(TimeWrapper time in rail.notesByTime.Keys.OrEmptyIfNull()) {
                    if(leaderNote.TimePoint == time)
                        continue;
                    EditorNote  segmentNote = rail.notesByTime[time].thisNote;
                    gameNote.Segments[i, 0] = segmentNote.Position[0];
                    gameNote.Segments[i, 1] = segmentNote.Position[1];
                    gameNote.Segments[i, 2] = segmentNote.Position[2];
                    i++;
                }
                exportValue[leaderNote.TimePoint.FloatValue].Add(gameNote);
            }
        }


        string CreateNoteNameForGame(EditorNote note) {
            return null;

        }

        public TimeWrapper GetNextStepPoint(float kbpm, TimeWrapper time) {
            float realStepsFloat = time.FloatValue/kbpm;
            int realStepsInt = (int)realStepsFloat;
            float fraction = Math.Abs((realStepsInt+1)*kbpm - time.FloatValue)/kbpm;
            if(fraction > 0.1)
                time=(realStepsInt+1)*kbpm;
            else
                time=(realStepsInt+2)*kbpm;
            return time;
        }

        /// <summary>
        /// Return the prev point to displace the stage
        /// </summary>
        /// <remarks>
        /// Based on the values of <see cref="K"/> and <see cref="MBPM"/>
        /// </remarks>
        /// <returns>Returns <typeparamref name="float"/></returns>
        public TimeWrapper GetPrevStepPoint(float kbpm, TimeWrapper time) {
            float realStepsFloat = time.FloatValue/kbpm;
            int realStepsInt = (int)realStepsFloat;
            float fraction = Math.Abs(realStepsInt*kbpm-time.FloatValue)/(kbpm);
            bool diffLessThan0 = (realStepsInt*kbpm-time.FloatValue) < 0;
            if(diffLessThan0 && fraction > 0.1)
                time=(realStepsInt)*kbpm;
            else
                time=(realStepsInt-1)*kbpm;
            return time;
        }

        // Fully passes the Game's single difficulty note data to Editor's note data
        void PassGameNoteDataToEditor(float bpm, Dictionary<float, List<Note>> gameDictionary, Dictionary<TimeWrapper, List<EditorNote>> editorDictionary, List<Rail> listOfRails) {
            if(gameDictionary == null)
                return;
            if(editorDictionary == null) {
                editorDictionary = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
            }
            foreach(KeyValuePair<float, List<Note>> entry in gameDictionary.OrEmptyIfNull()) {
                

                foreach(var gameNote in entry.Value.OrEmptyIfNull()) {
                    TimeWrapper key = entry.Key;
                    float msPerBeat = (1000 * 60)/bpm;
                    float step = 1/64f;

                    // this makes sure the editor will not skip the hash note is supposed to be positioned at on step
                    // if it does - it's bound to the next step it WILL visit
                    TimeWrapper nextPoint = GetNextStepPoint(msPerBeat*step, key);
                    TimeWrapper prevPoint = GetPrevStepPoint(msPerBeat*step, key);
                    TimeWrapper repeat = GetNextStepPoint(msPerBeat*step, prevPoint);
                    if(repeat != key && key.Hash != nextPoint.Hash && key.Hash != prevPoint.Hash)
                        key = key - prevPoint >= nextPoint - key ? nextPoint : prevPoint;
                        //key = nextPoint;

                    //var divisor = (bpm/60f)/64f;
                    //float divided = key/divisor;
                    //TimeWrapper testedTime = new TimeWrapper(key);

                    EditorNote exportNote = new EditorNote(bpm,
                        new UnityEngine.Vector3 { x = gameNote.Position[0], y = gameNote.Position[1], z = Track.MStoUnit(key) }, key.FloatValue,
                         gameNote.ComboId, ConvertGameNoteTypeToEditorNoteType(gameNote.Type));
                    // if we have segments this means we need to create a rail from them
                    // preferably NOT note by note and WITHOUT instantiation

                    //exportNote.Segments = gameNote.Segments;
                    if(gameNote.Segments != null) {
                        exportNote.Segments = new float[gameNote.Segments.GetLength(0), 3];

                        for(int i = 0; i < gameNote.Segments.GetLength(0); i++) {
                            exportNote.Segments[i, 0] = gameNote.Segments[i, 0];
                            exportNote.Segments[i, 1] = gameNote.Segments[i, 1];
                            exportNote.Segments[i, 2] = gameNote.Segments[i, 2];
                        }
                    }

                    if(exportNote.Segments == null || exportNote.Segments.Length == 0) {
                        if(!editorDictionary.ContainsKey(new TimeWrapper(key.FloatValue)))
                            editorDictionary.Add(key, new List<EditorNote>());
                        editorDictionary[key].Add(exportNote);
                        bool contains = editorDictionary.ContainsKey(key);
                        contains = editorDictionary.ContainsKey(key);
                    } else {
                        // if there are segments, note belongs to a rail
                        Rail rail = RailHelper.CreateRailFromSegments(bpm, key, exportNote);
                        listOfRails.Add(rail);
                    }
                }
            }
        }

        // Pre-Instantiates necessary structs for exported data
        public void InstantiateExportedChartDataStructures() {
            gameChart = new Chart();

            if(gameChart.Effects == null) {
                Beats defaultBeats = new Beats();
                defaultBeats.Easy = new Dictionary<float, List<Note>>();
                defaultBeats.Normal = new Dictionary<float, List<Note>>();
                defaultBeats.Hard = new Dictionary<float, List<Note>>();
                defaultBeats.Expert = new Dictionary<float, List<Note>>();
                defaultBeats.Master = new Dictionary<float, List<Note>>();
                defaultBeats.Custom = new Dictionary<float, List<Note>>();
                gameChart.Track = defaultBeats;
            }

            if(gameChart.Effects == null) {
                Effects defaultEffects = new Effects();
                defaultEffects.Easy = new List<float>();
                defaultEffects.Normal = new List<float>();
                defaultEffects.Hard = new List<float>();
                defaultEffects.Expert = new List<float>();
                defaultEffects.Master = new List<float>();
                defaultEffects.Custom = new List<float>();
                gameChart.Effects = defaultEffects;
            }

            if(gameChart.Jumps == null) {
                Jumps defaultJumps = new Jumps();
                defaultJumps.Easy = new List<float>();
                defaultJumps.Normal = new List<float>();
                defaultJumps.Hard = new List<float>();
                defaultJumps.Expert = new List<float>();
                defaultJumps.Master = new List<float>();
                defaultJumps.Custom = new List<float>();

                gameChart.Jumps = defaultJumps;
            }

            if(gameChart.Crouchs == null) {
                Crouchs defaultCrouchs = new Crouchs();
                defaultCrouchs.Easy = new List<float>();
                defaultCrouchs.Normal = new List<float>();
                defaultCrouchs.Hard = new List<float>();
                defaultCrouchs.Expert = new List<float>();
                defaultCrouchs.Master = new List<float>();
                defaultCrouchs.Custom = new List<float>();

                gameChart.Crouchs = defaultCrouchs;
            }

            if(gameChart.Slides == null) {
                Slides defaultSlides = new Slides();
                defaultSlides.Easy = new List<Slide>();
                defaultSlides.Normal = new List<Slide>();
                defaultSlides.Hard = new List<Slide>();
                defaultSlides.Expert = new List<Slide>();
                defaultSlides.Master = new List<Slide>();
                defaultSlides.Custom = new List<Slide>();

                gameChart.Slides = defaultSlides;
            }

            if(gameChart.Lights == null) {
                Lights defaultLights = new Lights();
                defaultLights.Easy = new List<float>();
                defaultLights.Normal = new List<float>();
                defaultLights.Hard = new List<float>();
                defaultLights.Expert = new List<float>();
                defaultLights.Master = new List<float>();
                defaultLights.Custom = new List<float>();

                gameChart.Lights = defaultLights;
            }

            if(gameChart.Bookmarks == null) {
                gameChart.Bookmarks = new Bookmarks();
            }
        }

        void ReinstantiateComboIDs(int idBase, Dictionary<float, List<Note>> difficulty) {
            List<float> times = difficulty.Keys.ToList();
            times.Sort();
            Note.NoteType previousType = Note.NoteType.NoHand;
            foreach(float time in times.OrEmptyIfNull()) {
                List<Note> notes = difficulty[time];
                if(notes == null)
                    continue;
                //technically end of the rail can have the ball coincide with the beginning of special section
                // so we really need to check for this usecase 
                if(notes.Count == 1) {
                    bool thisIsOfSpecialType = Track.IsOfSpecialType(ConvertGameNoteTypeToEditorNoteType(notes[0].Type));
                    bool previousIsOfSpecialType = Track.IsOfSpecialType(ConvertGameNoteTypeToEditorNoteType(previousType));
                    if(thisIsOfSpecialType && !previousIsOfSpecialType) {
                        //need to create a new combo id
                        idBase+=1;
                    }
                    if(thisIsOfSpecialType)
                        notes[0].ComboId = idBase;
                }

            }

        }

        // Converts the Editor's chart into Game's chart and stores this new chart into a static instance
        public bool ConvertEditorChartToGameChart(EditorChart chart) {

            editorChart = chart;

            // instantiating the Game's chart data
            InstantiateExportedChartDataStructures();

            // properties that can possible be null
            if(editorChart.Artwork != null)
                gameChart.Artwork = editorChart.Artwork;
            if(editorChart.ArtworkBytes != null)
                gameChart.ArtworkBytes = editorChart.ArtworkBytes;
            if(editorChart.AudioData != null)
                gameChart.AudioData = editorChart.AudioData;
            if(editorChart.AudioName != null)
                gameChart.AudioName = editorChart.AudioName;
            if(editorChart.Author != null)
                gameChart.Author = editorChart.Author;
            if(editorChart.Beatmapper != null)
                gameChart.Beatmapper = editorChart.Beatmapper;
            if(editorChart.Tags != null)
                gameChart.Tags = editorChart.Tags;
            if(editorChart.CustomDifficultyName != null)
                gameChart.CustomDifficultyName = editorChart.CustomDifficultyName;
            if(editorChart.EditorVersion != null)
                gameChart.EditorVersion = editorChart.EditorVersion;
            if(editorChart.FilePath != null)
                gameChart.FilePath = editorChart.FilePath;
            if(editorChart.Name != null)
                gameChart.Name = editorChart.Name;

            // can't be null
            gameChart.Offset = editorChart.Offset.FloatValue;
            gameChart.IsAdminOnly = editorChart.IsAdminOnly;
            gameChart.AudioChannels = editorChart.AudioChannels;
            gameChart.AudioFrecuency = editorChart.AudioFrecuency;
            gameChart.CustomDifficultySpeed = editorChart.CustomDifficultySpeed;
            gameChart.BPM = editorChart.BPM;


            // exporting bookmarks
            if(editorChart.Bookmarks != null && editorChart.Bookmarks.BookmarksList != null) {
                int size = editorChart.Bookmarks.BookmarksList.Count;
                foreach(var editorBookmark in editorChart.Bookmarks.BookmarksList.OrEmptyIfNull()) {
                    Bookmark exportBookmark = new Bookmark
                    {
                        name = editorBookmark.name,
                        time= editorBookmark.time.FloatValue
                    };
                    gameChart.Bookmarks.BookmarksList.Add(exportBookmark);
                }
            }


            // strictly speaking _exported_ difficulties shouldn't be nulls 
            // but it's better to have mirror code to the importer and still check
            if(editorChart.Crouchs.Easy != null)
                 gameChart.Crouchs.Easy = TimeWrapper.Convert(editorChart.Crouchs.Easy);
            if(editorChart.Crouchs.Expert != null)
                gameChart.Crouchs.Expert = TimeWrapper.Convert(editorChart.Crouchs.Expert);
            if(editorChart.Crouchs.Hard != null)
                gameChart.Crouchs.Hard = TimeWrapper.Convert(editorChart.Crouchs.Hard);
            if(editorChart.Crouchs.Master != null)
                gameChart.Crouchs.Master = TimeWrapper.Convert(editorChart.Crouchs.Master);
            if(editorChart.Crouchs.Normal != null)
                gameChart.Crouchs.Normal = TimeWrapper.Convert(editorChart.Crouchs.Normal);
            if(editorChart.Crouchs.Custom != null)
                gameChart.Crouchs.Custom = TimeWrapper.Convert(editorChart.Crouchs.Custom);


            if(editorChart.Effects.Easy != null)
                gameChart.Effects.Easy = TimeWrapper.Convert(editorChart.Effects.Easy);
            if(editorChart.Effects.Expert != null)
                gameChart.Effects.Expert = TimeWrapper.Convert(editorChart.Effects.Expert);
            if(editorChart.Effects.Hard != null)
                gameChart.Effects.Hard = TimeWrapper.Convert(editorChart.Effects.Hard);
            if(editorChart.Effects.Master != null)
                gameChart.Effects.Master = TimeWrapper.Convert(editorChart.Effects.Master);
            if(editorChart.Effects.Normal != null)
                gameChart.Effects.Normal = TimeWrapper.Convert(editorChart.Effects.Normal);
            if(editorChart.Effects.Custom != null)
                gameChart.Effects.Custom = TimeWrapper.Convert(editorChart.Effects.Custom);

            if(editorChart.Jumps.Easy != null)
                gameChart.Jumps.Easy = TimeWrapper.Convert(editorChart.Jumps.Easy);
            if(editorChart.Jumps.Expert != null)
                gameChart.Jumps.Expert = TimeWrapper.Convert(editorChart.Jumps.Expert);
            if(editorChart.Jumps.Hard != null)
                gameChart.Jumps.Hard = TimeWrapper.Convert(editorChart.Jumps.Hard);
            if(editorChart.Jumps.Master != null)
                gameChart.Jumps.Master = TimeWrapper.Convert(editorChart.Jumps.Master);
            if(editorChart.Jumps.Normal != null)
                gameChart.Jumps.Custom = TimeWrapper.Convert(editorChart.Jumps.Normal);
            if(editorChart.Jumps.Easy != null)
                gameChart.Jumps.Custom = TimeWrapper.Convert(editorChart.Jumps.Custom);

            // Lights holder may itself be null, needs a check
            if(editorChart.Lights != null) {
                if(editorChart.Lights.Easy != null)
                    gameChart.Lights.Easy = TimeWrapper.Convert(editorChart.Lights.Easy);
                if(editorChart.Lights.Expert != null)
                    gameChart.Lights.Expert = TimeWrapper.Convert(editorChart.Lights.Expert);
                if(editorChart.Lights.Hard != null)
                    gameChart.Lights.Hard = TimeWrapper.Convert(editorChart.Lights.Hard);
                if(editorChart.Lights.Master != null)
                    gameChart.Lights.Master = TimeWrapper.Convert(editorChart.Lights.Master);
                if(editorChart.Lights.Normal != null)
                    gameChart.Lights.Normal = TimeWrapper.Convert(editorChart.Lights.Normal);
                if(editorChart.Lights.Custom != null)
                    gameChart.Lights.Custom = TimeWrapper.Convert(editorChart.Lights.Custom);
            }

            // slides holder may itself be null, checking
            var slides = editorChart.Slides;
            if(slides != null) {
                foreach(var editorValue in slides.Custom.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Custom);
                }
                foreach(var editorValue in slides.Easy.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Easy);
                }
                foreach(var editorValue in slides.Normal.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Normal);
                }
                foreach(var editorValue in slides.Hard.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Hard);
                }
                foreach(var editorValue in slides.Expert.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Expert);
                }
                foreach(var editorValue in slides.Master.OrEmptyIfNull()) {
                    PassEditorSlideDataToGame(editorValue, gameChart.Slides.Master);
                }
            }

            // passing one dictionary of notes into another 
            if(editorChart.Track.Custom != null) {
                PassEditorNoteDataToGame(editorChart.Track.Custom, gameChart.Track.Custom);
                PassEditorRailDataToGame(editorChart.Rails.Custom, gameChart.Track.Custom);
                // when everything is passed we need to create special combo ids
                // for that we fish within the notes for uninterrupted special color segments (green and gold)

            }

            int idBase = 0;
            if(editorChart.Track.Easy != null) {
                PassEditorNoteDataToGame(editorChart.Track.Easy, gameChart.Track.Easy);
                PassEditorRailDataToGame(editorChart.Rails.Easy, gameChart.Track.Easy);
                ReinstantiateComboIDs(idBase, gameChart.Track.Easy);
            }
            if(editorChart.Track.Normal != null) {
                PassEditorNoteDataToGame(editorChart.Track.Normal, gameChart.Track.Normal);
                PassEditorRailDataToGame(editorChart.Rails.Normal, gameChart.Track.Normal);
                ReinstantiateComboIDs(idBase, gameChart.Track.Normal);
            }
            if(editorChart.Track.Hard != null) {
                PassEditorNoteDataToGame(editorChart.Track.Hard, gameChart.Track.Hard);
                PassEditorRailDataToGame(editorChart.Rails.Hard, gameChart.Track.Hard);
                ReinstantiateComboIDs(idBase, gameChart.Track.Hard);
            }
            if(editorChart.Track.Expert != null) {
                PassEditorNoteDataToGame(editorChart.Track.Expert, gameChart.Track.Expert);
                PassEditorRailDataToGame(editorChart.Rails.Expert, gameChart.Track.Expert);
                ReinstantiateComboIDs(idBase, gameChart.Track.Expert);
            }
            if(editorChart.Track.Master != null) {
                PassEditorNoteDataToGame(editorChart.Track.Master, gameChart.Track.Master);
                PassEditorRailDataToGame(editorChart.Rails.Master, gameChart.Track.Master);
                ReinstantiateComboIDs(idBase, gameChart.Track.Master);
            }

            return true;
        }

        // Pre-Instantiates necessary structs for imported data
        public void InstantiateEditorChartDataStructures() {
            editorChart = new EditorChart();

            if(editorChart.Track == null) {
                EditorBeats defaultBeats = new EditorBeats();
                defaultBeats.Easy = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                defaultBeats.Normal = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                defaultBeats.Hard = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                defaultBeats.Expert = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                defaultBeats.Master = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                defaultBeats.Custom = new Dictionary<TimeWrapper, List<EditorNote>>(new TimeWrapper());
                editorChart.Track = defaultBeats;
            }

            if(editorChart.Effects == null) {
                EditorEffects defaultEffects = new EditorEffects();
                defaultEffects.Easy = new List<TimeWrapper>();
                defaultEffects.Normal = new List<TimeWrapper>();
                defaultEffects.Hard = new List<TimeWrapper>();
                defaultEffects.Expert = new List<TimeWrapper>();
                defaultEffects.Master = new List<TimeWrapper>();
                defaultEffects.Custom = new List<TimeWrapper>();
                editorChart.Effects = defaultEffects;
            }

            if(editorChart.Jumps == null) {
                EditorJumps defaultJumps = new EditorJumps();
                defaultJumps.Easy = new List<TimeWrapper>();
                defaultJumps.Normal = new List<TimeWrapper>();
                defaultJumps.Hard = new List<TimeWrapper>();
                defaultJumps.Expert = new List<TimeWrapper>();
                defaultJumps.Master = new List<TimeWrapper>();
                defaultJumps.Custom = new List<TimeWrapper>();

                editorChart.Jumps = defaultJumps;
            }

            if(editorChart.Crouchs == null) {
                EditorCrouchs defaultCrouchs = new EditorCrouchs();
                defaultCrouchs.Easy = new List<TimeWrapper>();
                defaultCrouchs.Normal = new List<TimeWrapper>();
                defaultCrouchs.Hard = new List<TimeWrapper>();
                defaultCrouchs.Expert = new List<TimeWrapper>();
                defaultCrouchs.Master = new List<TimeWrapper>();
                defaultCrouchs.Custom = new List<TimeWrapper>();

                editorChart.Crouchs = defaultCrouchs;
            }

            if(editorChart.Slides == null) {
                EditorSlides defaultSlides = new EditorSlides();
                defaultSlides.Easy = new List<EditorSlide>();
                defaultSlides.Normal = new List<EditorSlide>();
                defaultSlides.Hard = new List<EditorSlide>();
                defaultSlides.Expert = new List<EditorSlide>();
                defaultSlides.Master = new List<EditorSlide>();
                defaultSlides.Custom = new List<EditorSlide>();

                editorChart.Slides = defaultSlides;
            }

            if(editorChart.Rails == null) {
                EditorRails defaultRails = new EditorRails();
                defaultRails.Easy = new List<Rail>();
                defaultRails.Normal = new List<Rail>();
                defaultRails.Hard = new List<Rail>();
                defaultRails.Expert = new List<Rail>();
                defaultRails.Master = new List<Rail>();
                defaultRails.Custom = new List<Rail>();

                editorChart.Rails = defaultRails;
            }

            if(editorChart.Lights == null) {
                EditorLights defaultLights = new EditorLights();
                defaultLights.Easy = new List<TimeWrapper>();
                defaultLights.Normal = new List<TimeWrapper>();
                defaultLights.Hard = new List<TimeWrapper>();
                defaultLights.Expert = new List<TimeWrapper>();
                defaultLights.Master = new List<TimeWrapper>();
                defaultLights.Custom = new List<TimeWrapper>();

                editorChart.Lights = defaultLights;
            }

            if(editorChart.Bookmarks == null) {
                editorChart.Bookmarks = new EditorBookmarks();
            }
        }

        // Adds Drum instance to a list of editor's drums
        void PassGameDrumDataToEditor(Drum editorDrum, List<EditorDrum> drums) {
            if(drums == null)
                return;
            EditorDrum drum = new EditorDrum()
            {
                time = editorDrum.time,
                audio = editorDrum.audio,
                playType= editorDrum.playType
            };
            drums.Add(drum);
        }

        // Adds Slide instance to a list of editor's slides
        void PassGameSlideDataToEditor(Slide editorSlide, List<EditorSlide> slides) {
            if(slides == null)
                return;
            EditorSlide slide = new EditorSlide()
            {
                initialized = editorSlide.initialized,
                slideType = (EditorNote.NoteHandType)editorSlide.slideType,
                time = editorSlide.time,
                initialTime = editorSlide.time
            };
            slides.Add(slide);
        }

        public static TimeWrapper UpdateTimeToBPM(TimeWrapper ms, float bpm) {
         
            float K = (Track.msInSecond * Track.secondsInMinute) / bpm;
            //return ms - ( ( (MS*MINUTE)/CurrentChart.BPM ) - K );
            if(ms > 0) {
                return (K * ms.FloatValue) / ((Track.msInSecond * Track.secondsInMinute) / bpm);
            } else {
                return ms;
            }
        }

        // Converts the game's chart into editor's chart and stores this new chart into a static instance
        public bool ConvertGameChartToEditorChart(Chart chart) {

            gameChart = chart;

            // pre-instantiating editor's structures
            InstantiateEditorChartDataStructures();

            // possible nulls
            if(gameChart.Artwork != null)
                editorChart.Artwork = gameChart.Artwork;
            if(gameChart.ArtworkBytes != null)
                editorChart.ArtworkBytes = gameChart.ArtworkBytes;
            if(gameChart.AudioData != null)
                editorChart.AudioData = gameChart.AudioData;
            if(gameChart.AudioName != null)
                editorChart.AudioName = gameChart.AudioName;
            if(gameChart.Author != null)
                editorChart.Author = gameChart.Author;
            if(gameChart.Beatmapper != null)
                editorChart.Beatmapper = gameChart.Beatmapper;
            if(gameChart.Tags != null)
                editorChart.Tags = gameChart.Tags;
            if(gameChart.CustomDifficultyName != null)
                editorChart.CustomDifficultyName = gameChart.CustomDifficultyName;
            if(gameChart.EditorVersion != null)
                editorChart.EditorVersion = gameChart.EditorVersion;
            if(gameChart.FilePath != null)
                editorChart.FilePath = gameChart.FilePath;
            if(gameChart.Name != null)
                editorChart.Name = gameChart.Name;
            if(gameChart.Artwork != null)
                editorChart.Offset = gameChart.Offset;

            // can't be null
            editorChart.AudioChannels = gameChart.AudioChannels;
            editorChart.AudioFrecuency = gameChart.AudioFrecuency;
            editorChart.BPM = gameChart.BPM;
            editorChart.CustomDifficultySpeed = gameChart.CustomDifficultySpeed;
            editorChart.IsAdminOnly = gameChart.IsAdminOnly;

            // exporting bookmarks
            if(gameChart.Bookmarks != null && gameChart.Bookmarks.BookmarksList != null) {
                int size = gameChart.Bookmarks.BookmarksList.Count;
                foreach(var gameBookmark in gameChart.Bookmarks.BookmarksList.OrEmptyIfNull()) {
                    EditorBookmark exportBookmark = new EditorBookmark
                    {
                        name = gameBookmark.name,
                        time = gameBookmark.time,
                        initialTime = gameBookmark.time
                    };
                    editorChart.Bookmarks.BookmarksList.Add(exportBookmark);
                }
            }


            // since the editor can load legacy files, some instances of difficulties tend to be nulls
            // this needs a separate check each time 

            if(gameChart.Crouchs.Easy != null)
                editorChart.Crouchs.Easy = TimeWrapper.Convert(gameChart.Crouchs.Easy);
            if(gameChart.Crouchs.Expert != null)
                editorChart.Crouchs.Expert = TimeWrapper.Convert(gameChart.Crouchs.Expert);
            if(gameChart.Crouchs.Hard != null)
                editorChart.Crouchs.Hard = TimeWrapper.Convert(gameChart.Crouchs.Hard);
            if(gameChart.Crouchs.Master != null)
                editorChart.Crouchs.Master = TimeWrapper.Convert(gameChart.Crouchs.Master);
            if(gameChart.Crouchs.Normal != null)
                editorChart.Crouchs.Normal = TimeWrapper.Convert(gameChart.Crouchs.Normal);
            if(gameChart.Crouchs.Custom != null)
                editorChart.Crouchs.Custom = TimeWrapper.Convert(gameChart.Crouchs.Custom);

            if(gameChart.Effects.Easy != null)
                editorChart.Effects.Easy = TimeWrapper.Convert(gameChart.Effects.Easy);
            if(gameChart.Effects.Expert != null)
                editorChart.Effects.Expert = TimeWrapper.Convert(gameChart.Effects.Expert);
            if(gameChart.Effects.Hard != null)
                editorChart.Effects.Hard = TimeWrapper.Convert(gameChart.Effects.Hard);
            if(gameChart.Effects.Master != null)
                editorChart.Effects.Master = TimeWrapper.Convert(gameChart.Effects.Master);
            if(gameChart.Effects.Normal != null)
                editorChart.Effects.Normal = TimeWrapper.Convert(gameChart.Effects.Normal);
            if(gameChart.Effects.Custom != null)
                editorChart.Effects.Custom = TimeWrapper.Convert(gameChart.Effects.Custom);

            if(gameChart.Jumps.Easy != null)
                editorChart.Jumps.Easy = TimeWrapper.Convert(gameChart.Jumps.Easy);
            if(gameChart.Jumps.Expert != null)
                editorChart.Jumps.Expert = TimeWrapper.Convert(gameChart.Jumps.Expert);
            if(gameChart.Jumps.Hard != null)
                editorChart.Jumps.Hard = TimeWrapper.Convert(gameChart.Jumps.Hard);
            if(gameChart.Jumps.Master != null)
                editorChart.Jumps.Master = TimeWrapper.Convert(gameChart.Jumps.Master);
            if(gameChart.Jumps.Normal != null)
                editorChart.Jumps.Custom = TimeWrapper.Convert(gameChart.Jumps.Normal);
            if(gameChart.Jumps.Easy != null)
                editorChart.Jumps.Custom = TimeWrapper.Convert(gameChart.Jumps.Custom);

            // Lights holder may itself be null, needs a check
            if(gameChart.Lights != null) {
                if(gameChart.Lights.Easy != null)
                    editorChart.Lights.Easy = TimeWrapper.Convert(gameChart.Lights.Easy);
                if(gameChart.Lights.Expert != null)
                    editorChart.Lights.Expert = TimeWrapper.Convert(gameChart.Lights.Expert);
                if(gameChart.Lights.Hard != null)
                    editorChart.Lights.Hard = TimeWrapper.Convert(gameChart.Lights.Hard);
                if(gameChart.Lights.Master != null)
                    editorChart.Lights.Master = TimeWrapper.Convert(gameChart.Lights.Master);
                if(gameChart.Lights.Normal != null)
                    editorChart.Lights.Normal = TimeWrapper.Convert(gameChart.Lights.Normal);
                if(gameChart.Lights.Custom != null)
                    editorChart.Lights.Custom = TimeWrapper.Convert(gameChart.Lights.Custom);
            }

            // Slides holder may itself be null, needs a check
            if(gameChart.Slides == null) {
                editorChart.Slides = null;
            } else {
                var slides = gameChart.Slides;
                foreach(var editorValue in slides.Custom.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Custom);
                }
                foreach(var editorValue in slides.Easy.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Easy);
                }
                foreach(var editorValue in slides.Normal.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Normal);
                }
                foreach(var editorValue in slides.Hard.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Hard);
                }
                foreach(var editorValue in slides.Expert.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Expert);
                }
                foreach(var editorValue in slides.Master.OrEmptyIfNull()) {
                    PassGameSlideDataToEditor(editorValue, editorChart.Slides.Master);
                }

            }

            // passing one dictionary of notes into another 
            if(gameChart.Track.Custom != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Custom, editorChart.Track.Custom, editorChart.Rails.Custom);
            if(gameChart.Track.Easy != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Easy, editorChart.Track.Easy, editorChart.Rails.Easy);
            if(gameChart.Track.Normal != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Normal, editorChart.Track.Normal, editorChart.Rails.Normal);
            if(gameChart.Track.Hard != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Hard, editorChart.Track.Hard, editorChart.Rails.Hard);
            if(gameChart.Track.Expert != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Expert, editorChart.Track.Expert, editorChart.Rails.Expert);
            if(gameChart.Track.Master != null)
                PassGameNoteDataToEditor(editorChart.BPM, gameChart.Track.Master, editorChart.Track.Master, editorChart.Rails.Master);
            return true;
        }
    };




};