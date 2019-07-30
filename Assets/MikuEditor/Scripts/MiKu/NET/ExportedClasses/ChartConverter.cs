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
                time = editorDrum.time,
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
                time = editorSlide.time
            };
            slides.Add(slide);
        }

        // Fully passes the Editor's single difficulty note data to game's note data
        void PassEditorNoteDataToGame(Dictionary<float, List<EditorNote>> editorValue, Dictionary<float, List<Note>> exportValue) {
            if(editorValue == null)
                return;
            if(exportValue == null) {
                exportValue = new Dictionary<float, List<Note>>();
            }
            foreach(KeyValuePair<float, List<EditorNote>> entry in editorValue) {
                if(!exportValue.ContainsKey(entry.Key))
                    exportValue.Add(entry.Key, new List<Note>());

                foreach(var editorNote in entry.Value) {
                    Note exportNote = new Note(new UnityEngine.Vector3 { x = editorNote.Position[0], y = editorNote.Position[1], z = editorNote.Position[2] },
                        editorNote.Id, editorNote.ComboId, ConvertEditorNoteTypeToGameNoteType(editorNote.HandType));
                    exportNote.Segments = editorNote.Segments;
                    exportValue[entry.Key].Add(exportNote);
                }
            }
        }

        // Fully passes the Game's single difficulty note data to Editor's note data
        void PassGameNoteDataToEditor(Dictionary<float, List<Note>> gameDictionary, Dictionary<float, List<EditorNote>> editorDictionary) {
            if(gameDictionary == null)
                return;
            if(editorDictionary == null) {
                editorDictionary = new Dictionary<float, List<EditorNote>>();
            }
            foreach(KeyValuePair<float, List<Note>> entry in gameDictionary) {
                if(!editorDictionary.ContainsKey(entry.Key))
                    editorDictionary.Add(entry.Key, new List<EditorNote>());

                foreach(var gameNote in entry.Value) {
                    EditorNote exportNote = new EditorNote(new UnityEngine.Vector3 { x = gameNote.Position[0], y = gameNote.Position[1], z = gameNote.Position[2] }, entry.Key,
                        gameNote.Id, gameNote.ComboId, ConvertGameNoteTypeToEditorNoteType(gameNote.Type));
                    exportNote.Segments = gameNote.Segments;
                    editorDictionary[entry.Key].Add(exportNote);
                }
            }
        }

        // Pre-Instantiates necessary structs for exported data
        void InstantiateExportedChartDataStructures() {
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
            gameChart.Offset = editorChart.Offset;
            gameChart.IsAdminOnly = editorChart.IsAdminOnly;
            gameChart.AudioChannels = editorChart.AudioChannels;
            gameChart.AudioFrecuency = editorChart.AudioFrecuency;
            gameChart.CustomDifficultySpeed = editorChart.CustomDifficultySpeed;
            gameChart.BPM = editorChart.BPM;


            // exporting bookmarks
            if(editorChart.Bookmarks != null && editorChart.Bookmarks.BookmarksList != null) {
                int size = editorChart.Bookmarks.BookmarksList.Count;
                foreach(var editorBookmark in editorChart.Bookmarks.BookmarksList) {
                    Bookmark exportBookmark = new Bookmark
                    {
                        name = editorBookmark.name,
                        time= editorBookmark.time
                    };
                    gameChart.Bookmarks.BookmarksList.Add(exportBookmark);
                }
            }


            // strictly speaking _exported_ difficulties shouldn't be nulls 
            // but it's better to have mirror code to the importer and still check
            if(editorChart.Crouchs.Easy != null)
                gameChart.Crouchs.Easy = editorChart.Crouchs.Easy;
            if(editorChart.Crouchs.Expert != null)
                gameChart.Crouchs.Expert = editorChart.Crouchs.Expert;
            if(editorChart.Crouchs.Hard != null)
                gameChart.Crouchs.Hard = editorChart.Crouchs.Hard;
            if(editorChart.Crouchs.Master != null)
                gameChart.Crouchs.Master = editorChart.Crouchs.Master;
            if(editorChart.Crouchs.Normal != null)
                gameChart.Crouchs.Normal = editorChart.Crouchs.Normal;
            if(editorChart.Crouchs.Custom != null)
                gameChart.Crouchs.Custom = editorChart.Crouchs.Custom;


            if(editorChart.Effects.Easy != null)
                gameChart.Effects.Easy = editorChart.Effects.Easy;
            if(editorChart.Effects.Expert != null)
                gameChart.Effects.Expert = editorChart.Effects.Expert;
            if(editorChart.Effects.Hard != null)
                gameChart.Effects.Hard = editorChart.Effects.Hard;
            if(editorChart.Effects.Master != null)
                gameChart.Effects.Master = editorChart.Effects.Master;
            if(editorChart.Effects.Normal != null)
                gameChart.Effects.Normal = editorChart.Effects.Normal;
            if(editorChart.Effects.Custom != null)
                gameChart.Effects.Custom = editorChart.Effects.Custom;

            if(editorChart.Jumps.Easy != null)
                gameChart.Jumps.Easy = editorChart.Jumps.Easy;
            if(editorChart.Jumps.Expert != null)
                gameChart.Jumps.Expert = editorChart.Jumps.Expert;
            if(editorChart.Jumps.Hard != null)
                gameChart.Jumps.Hard = editorChart.Jumps.Hard;
            if(editorChart.Jumps.Master != null)
                gameChart.Jumps.Master = editorChart.Jumps.Master;
            if(editorChart.Jumps.Normal != null)
                gameChart.Jumps.Custom = editorChart.Jumps.Normal;
            if(editorChart.Jumps.Easy != null)
                gameChart.Jumps.Custom = editorChart.Jumps.Custom;

            // Lights holder may itself be null, needs a check
            if(editorChart.Lights != null) {
                if(editorChart.Lights.Easy != null)
                    gameChart.Lights.Easy = editorChart.Lights.Easy;
                if(editorChart.Lights.Expert != null)
                    gameChart.Lights.Expert = editorChart.Lights.Expert;
                if(editorChart.Lights.Hard != null)
                    gameChart.Lights.Hard = editorChart.Lights.Hard;
                if(editorChart.Lights.Master != null)
                    gameChart.Lights.Master = editorChart.Lights.Master;
                if(editorChart.Lights.Normal != null)
                    gameChart.Lights.Normal = editorChart.Lights.Normal;
                if(editorChart.Lights.Custom != null)
                    gameChart.Lights.Custom = editorChart.Lights.Custom;
            }

            // passing values of drums into new lists 1 by 1
            //var drumSamples = editorChart.DrumSamples;
            //foreach(var editorValue in drumSamples.Custom) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Custom);
            //}
            //foreach(var editorValue in drumSamples.Easy) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Easy);
            //}
            //foreach(var editorValue in drumSamples.Normal) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Normal);
            //}
            //foreach(var editorValue in drumSamples.Hard) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Hard);
            //}
            //foreach(var editorValue in drumSamples.Expert) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Expert);
            //}
            //foreach(var editorValue in drumSamples.Master) {
            //    PassEditorDrumDataToGame(editorValue, gameChart.DrumSamples.Master);
            //}

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
            if(editorChart.Track.Custom != null)
                PassEditorNoteDataToGame(editorChart.Track.Custom, gameChart.Track.Custom);
            if(editorChart.Track.Easy != null)
                PassEditorNoteDataToGame(editorChart.Track.Easy,   gameChart.Track.Easy);
            if(editorChart.Track.Normal != null)
                PassEditorNoteDataToGame(editorChart.Track.Normal, gameChart.Track.Normal);
            if(editorChart.Track.Hard != null)
                PassEditorNoteDataToGame(editorChart.Track.Hard,   gameChart.Track.Hard);
            if(editorChart.Track.Expert != null)
                PassEditorNoteDataToGame(editorChart.Track.Expert, gameChart.Track.Expert);
            if(editorChart.Track.Master != null)
                PassEditorNoteDataToGame(editorChart.Track.Master, gameChart.Track.Master);
            return true;
        }

        // Pre-Instantiates necessary structs for imported data
        void InstantiateEditorChartDataStructures() {
            editorChart = new EditorChart();

            if(editorChart.Effects == null) {
                EditorBeats defaultBeats = new EditorBeats();
                defaultBeats.Easy = new Dictionary<float, List<EditorNote>>();
                defaultBeats.Normal = new Dictionary<float, List<EditorNote>>();
                defaultBeats.Hard = new Dictionary<float, List<EditorNote>>();
                defaultBeats.Expert = new Dictionary<float, List<EditorNote>>();
                defaultBeats.Master = new Dictionary<float, List<EditorNote>>();
                defaultBeats.Custom = new Dictionary<float, List<EditorNote>>();
                editorChart.Track = defaultBeats;
            }

            if(editorChart.Effects == null) {
                EditorEffects defaultEffects = new EditorEffects();
                defaultEffects.Easy = new List<float>();
                defaultEffects.Normal = new List<float>();
                defaultEffects.Hard = new List<float>();
                defaultEffects.Expert = new List<float>();
                defaultEffects.Master = new List<float>();
                defaultEffects.Custom = new List<float>();
                editorChart.Effects = defaultEffects;
            }

            if(editorChart.Jumps == null) {
                EditorJumps defaultJumps = new EditorJumps();
                defaultJumps.Easy = new List<float>();
                defaultJumps.Normal = new List<float>();
                defaultJumps.Hard = new List<float>();
                defaultJumps.Expert = new List<float>();
                defaultJumps.Master = new List<float>();
                defaultJumps.Custom = new List<float>();

                editorChart.Jumps = defaultJumps;
            }

            if(editorChart.Crouchs == null) {
                EditorCrouchs defaultCrouchs = new EditorCrouchs();
                defaultCrouchs.Easy = new List<float>();
                defaultCrouchs.Normal = new List<float>();
                defaultCrouchs.Hard = new List<float>();
                defaultCrouchs.Expert = new List<float>();
                defaultCrouchs.Master = new List<float>();
                defaultCrouchs.Custom = new List<float>();

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

            if(editorChart.Lights == null) {
                EditorLights defaultLights = new EditorLights();
                defaultLights.Easy = new List<float>();
                defaultLights.Normal = new List<float>();
                defaultLights.Hard = new List<float>();
                defaultLights.Expert = new List<float>();
                defaultLights.Master = new List<float>();
                defaultLights.Custom = new List<float>();

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
                time = editorSlide.time
            };
            slides.Add(slide);
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
                foreach(var gameBookmark in gameChart.Bookmarks.BookmarksList) {
                    EditorBookmark exportBookmark = new EditorBookmark
                    {
                        name = gameBookmark.name,
                        time= gameBookmark.time
                    };
                    editorChart.Bookmarks.BookmarksList.Add(exportBookmark);
                }
            }


            // since the editor can load legacy files, some instances of difficulties tend to be nulls
            // this needs a separate check each time 

            if(gameChart.Crouchs.Easy != null)
                editorChart.Crouchs.Easy = gameChart.Crouchs.Easy;
            if(gameChart.Crouchs.Expert != null)
                editorChart.Crouchs.Expert = gameChart.Crouchs.Expert;
            if(gameChart.Crouchs.Hard != null)
                editorChart.Crouchs.Hard = gameChart.Crouchs.Hard;
            if(gameChart.Crouchs.Master != null)
                editorChart.Crouchs.Master = gameChart.Crouchs.Master;
            if(gameChart.Crouchs.Normal != null)
                editorChart.Crouchs.Normal = gameChart.Crouchs.Normal;
            if(gameChart.Crouchs.Custom != null)
                editorChart.Crouchs.Custom = gameChart.Crouchs.Custom;

            if(gameChart.Effects.Easy != null)
                editorChart.Effects.Easy = gameChart.Effects.Easy;
            if(gameChart.Effects.Expert != null)
                editorChart.Effects.Expert = gameChart.Effects.Expert;
            if(gameChart.Effects.Hard != null)
                editorChart.Effects.Hard = gameChart.Effects.Hard;
            if(gameChart.Effects.Master != null)
                editorChart.Effects.Master = gameChart.Effects.Master;
            if(gameChart.Effects.Normal != null)
                editorChart.Effects.Normal = gameChart.Effects.Normal;
            if(gameChart.Effects.Custom != null)
                editorChart.Effects.Custom = gameChart.Effects.Custom;

            if(gameChart.Jumps.Easy != null)
                editorChart.Jumps.Easy = gameChart.Jumps.Easy;
            if(gameChart.Jumps.Expert != null)
                editorChart.Jumps.Expert = gameChart.Jumps.Expert;
            if(gameChart.Jumps.Hard != null)
                editorChart.Jumps.Hard = gameChart.Jumps.Hard;
            if(gameChart.Jumps.Master != null)
                editorChart.Jumps.Master = gameChart.Jumps.Master;
            if(gameChart.Jumps.Normal != null)
                editorChart.Jumps.Custom = gameChart.Jumps.Normal;
            if(gameChart.Jumps.Easy != null)
                editorChart.Jumps.Custom = gameChart.Jumps.Custom;

            // Lights holder may itself be null, needs a check
            if(gameChart.Lights != null) {
                if(gameChart.Lights.Easy != null)
                    editorChart.Lights.Easy = gameChart.Lights.Easy;
                if(gameChart.Lights.Expert != null)
                    editorChart.Lights.Expert = gameChart.Lights.Expert;
                if(gameChart.Lights.Hard != null)
                    editorChart.Lights.Hard = gameChart.Lights.Hard;
                if(gameChart.Lights.Master != null)
                    editorChart.Lights.Master = gameChart.Lights.Master;
                if(gameChart.Lights.Normal != null)
                    editorChart.Lights.Normal = gameChart.Lights.Normal;
                if(gameChart.Lights.Custom != null)
                    editorChart.Lights.Custom = gameChart.Lights.Custom;
            }

            // passing values of drums into new lists 1 by 1

            //foreach(var editorValue in gameChart.DrumSamples.Custom) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Custom);
            //}
            //foreach(var editorValue in gameChart.DrumSamples.Easy) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Easy);
            //}
            //foreach(var editorValue in gameChart.DrumSamples.Normal) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Normal);
            //}
            //foreach(var editorValue in gameChart.DrumSamples.Hard) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Hard);
            //}
            //foreach(var editorValue in gameChart.DrumSamples.Expert) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Expert);
            //}
            //foreach(var editorValue in gameChart.DrumSamples.Master) {
            //    PassGameDrumDataToEditor(editorValue, editorChart.DrumSamples.Master);
            //}

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
                PassGameNoteDataToEditor(gameChart.Track.Custom, editorChart.Track.Custom);
            if(gameChart.Track.Easy != null)
                PassGameNoteDataToEditor(gameChart.Track.Easy, editorChart.Track.Easy);
            if(gameChart.Track.Normal != null)
                PassGameNoteDataToEditor(gameChart.Track.Normal, editorChart.Track.Normal);
            if(gameChart.Track.Hard != null)
                PassGameNoteDataToEditor(gameChart.Track.Hard, editorChart.Track.Hard);
            if(gameChart.Track.Expert != null)
                PassGameNoteDataToEditor(gameChart.Track.Expert, editorChart.Track.Expert);
            if(gameChart.Track.Master != null)
                PassGameNoteDataToEditor(gameChart.Track.Master, editorChart.Track.Master);
            return true;
        }
    };




};