using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shogoki.Utils {
	// Holder for all the Messages/Alert on the app
	public class StringVault : MonoBehaviour {
		
		public static StringVault s_instance;

#region Serliazed Fields
		[Header("English Text")]
		[Space(10)]
		[Header("Alerts")]
		[SerializeField]
		[TextArea(3, 8)]
		string alert_NoAudioData = "The loaded chart doesn't have audio data";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_AudioLoadError = "There was a problem reading the audio file, please try again";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_NoAudioSelected = "Please select an audio clip";	

		[SerializeField]
		[TextArea(3, 8)]
		string alert_CoverImageWrongSize = "Artwork max size allowed is 512x512";	
		
		[SerializeField]
		[TextArea(3, 8)]
		string alert_NoImageSelected = "Please select an image file";    

		[SerializeField]
		[TextArea(3, 8)]
		string alert_FileLoadError = "The selected file doesn't exist";  

		[SerializeField]
		[TextArea(3, 8)]
		string alert_FileLoadNotAdmin = "The selected file can't be loaded"; 

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteModeWrongNote = "A long note only can be of type LeftHanded or RightHanded"; 

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteLenghtBounds = "The long note duration must be between {0}s and {1}s";		

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteStartPoint = "A line segments must be after the line start time";	

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteStartSegment = "A line segments must be after the previous segments";	

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteNotFinalized = "Please finish or abort LongNote mode before adding a new note";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_MaxNumberOfNotes = "Max number of notes reached";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_MaxNumberOfSpecialNotes = "Max number of special notes reached";

        [SerializeField]
        [TextArea(3, 8)]
        string alert_CantPlaceRailOfDifferntSubtype = "You need to finish the current rail";

        [SerializeField]
		[TextArea(3, 8)]
		string alert_MaxNumberOfTypeNotes = "Max number of {0} notes reached";	

		[SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteNotFinalizedEffect = "Please finish or abort LongNote mode before toggling a effect";

        [SerializeField]
		[TextArea(3, 8)]
		string alert_LongNoteNotFinalizedBookmark = "Please finish or abort LongNote mode before toggling a bookmar";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_MaxNumberOfEffects = "There can be only a maximun of {0} active Flash effects";

		[SerializeField]
		[TextArea(3, 8)]
		string alert_EffectsInterval = "The Flash/Lights effects, must had a minimun of {0} seconds between each other";

		[Space(10)]
		[Header("Promts")]
		[SerializeField]
		[TextArea(3, 8)]
		private string promt_ClearNotes = "Delete all Notes of the current selected Difficulty?";

		[SerializeField]
		[TextArea(3, 8)]
		private string promt_BackToMenu = "Return to Main Menu?";

        [SerializeField]
        [TextArea(3, 8)]
		private string promt_NotSaveChanges = "All unsaved changes will be losed";

		[SerializeField]
		[TextArea(3, 8)]
		private string promt_CopyAllNotes = "Copy all the notes of the current selected Difficulty?";

		[SerializeField]
		[TextArea(3, 8)]
		private string promt_PasteNotes = "Replace the current notes with the content of the clipboard?";

		[SerializeField]
		[TextArea(3, 8)]
		private string promt_SaveFile = "Save the Beatmap?";

        [SerializeField]
		[TextArea(3, 8)]
		private string promt_ExitApp = "Are you sure that you want to exit?";        

        [Space(10)]
		[Header("Promts")]
		[SerializeField]
		[TextArea(3, 8)]
		private string promt_ClearBookmarks = "Delete all Bookmarks?";

		[Space(10)]
		[Header("Info")]
        [SerializeField]
		[TextArea(3, 8)]
		private string info_SaveAborted = "Save was aborted";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_UserOnSpecialSection = "Working on Special Section";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_UserOnLongNoteMode = "Working on Long Note Section";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_NotNotesToCopy = "There is no notes to be copied";

		[Header("Warning")]
		[SerializeField]
		[TextArea(3, 8)]
		private string info_ClipBoardEmpty = "The clipboard is empty";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_NotesCopied = "Notes copied succesfully";    

		[SerializeField]
		[TextArea(3, 8)]
		private string info_FileSaved = "Chart Saved!";    

		[SerializeField]
		[TextArea(3, 8)]
		private string info_Metronome = "The Metronome is "; 

		[SerializeField]
		[TextArea(3, 8)]
		private string info_LeftCameraLabel = "Left Camera";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_RightCameraLabel = "Right Camera";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_CenterCameraLabel = "Center Camera";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_FreeCameraLabel = "Free Camera";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_LongNoteModeEnabled = "LongNote mode enabled";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_LongNoteModeDisabled = "LongNote mode disabled";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_LongNoteModeFinalized = "LongNote mode disabled";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_LongNoteModeAborted = "LongNote mode finalized";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_NotePasteSuccess = "LongNote mode aborted";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_SpecialModeStarted = "Starting new Special section";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_SpecialModeFinalized = "Special section finalized";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_FlashOn = "Flash effect turned on for the current time";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_FlashOff = "Flash effect turned off for the current time";	 

        [SerializeField]
		[TextArea(3, 8)]
		private string info_JumpOn = "Jump section turned on for the current time";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_JumpOff = "Jump section turned off for the current time";	 

        [SerializeField]
		[TextArea(3, 8)]
		private string info_CrouchOn = "Crouch section turned on for the current time";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_CrouchOff = "Crouch section turned off for the current time";	 

        [SerializeField]
		[TextArea(3, 8)]
		private string info_SlideOn = "Slide section turned on for the current time";

		[SerializeField]
		[TextArea(3, 8)]
		private string info_SlideOff = "Slide section turned off for the current time";	 

        [SerializeField]
		[TextArea(3, 8)]
		private string info_BookmarkOn = "Bookmark add sucessfully";

        [SerializeField]
		[TextArea(3, 8)]
		private string info_BookmarkOff = "Bookmark turned off for the current time";	

        [SerializeField]
		[TextArea(3, 8)]
		string info_AdminMode = "The admin mode of the selected beatmpa is: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_SyncnMode = "The Synchronization of the playback and the track is: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_LatencyUpdated = "The Latency was updated";

        [SerializeField]
		[TextArea(3, 8)]
		private string info_BookmarksCleared = "Bookmarks Cleared";

        [SerializeField]
		[TextArea(3, 8)]
		string info_VSyncnMode = "The Vsync is: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_ScrollSound = "Scrolling sound is: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_SpectrumLoading = "Loading spectrum data...";

        [SerializeField]
		[TextArea(3, 8)]
		string info_UserOnSelectionMode = "Selection mode enabled";

        [SerializeField]
		[TextArea(3, 8)]
		string info_UserOffSelectionMode = "Selection mode disabled";

        [SerializeField]
		[TextArea(3, 8)]
		string info_MiddleButtonType = "Middle button note type: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_AutoSaveFunction = "Autosave is: {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_MirroredMode = "Mirrored mode {0}";

        [SerializeField]
		[TextArea(3, 8)]
		string info_NoteTooClose = "The note is to close to the starting point, the min time required is {0} seconds";

        [SerializeField]
		[TextArea(3, 8)]
		string info_PasteTooFar = "The area to paste exceed the track duration";

        [SerializeField]
		[TextArea(3, 8)]
		private string info_FileExported = "JSON file exported!";   

        [SerializeField]
		[TextArea(3, 8)]
		private string info_LightsEffect = "Flash lights effect is turned {0} for the current time";

        [SerializeField]
		[TextArea(3, 8)]
		private string info_GridSnapp = "Grid snapp {0}";
#endregion		

        void Start () {
			if(s_instance != null) {
				DestroyImmediate(this.gameObject);
				return;
			}

			this.transform.parent = null;
			s_instance = this;
			DontDestroyOnLoad(this.gameObject);
		}


#region Alerts
		public static string Alert_NoAudioData
        {
            get
            {
                return s_instance.alert_NoAudioData;
            }
        }

        public static string Alert_AudioLoadError
        {
            get
            {
                return s_instance.alert_AudioLoadError;
            }
        }

        public static string Alert_NoAudioSelected
        {
            get
            {
                return s_instance.alert_NoAudioSelected;
            }
        }

        public static string Alert_CoverImageWrongSize
        {
            get
            {
                return s_instance.alert_CoverImageWrongSize;
            }
        }

        public static string Alert_NoImageSelected
        {
            get
            {
                return s_instance.alert_NoImageSelected;
            }
        }

        public static string Alert_FileLoadError
        {
            get
            {
                return s_instance.alert_FileLoadError;
            }
        }

        public static string Alert_FileLoadNotAdmin
        {
            get
            {
                return s_instance.alert_FileLoadNotAdmin;
            }
        }		

        public static string Alert_LongNoteModeWrongNote
        {
            get
            {
                return s_instance.alert_LongNoteModeWrongNote;
            }
        }

        public static string Alert_LongNoteLenghtBounds
        {
            get
            {
                return s_instance.alert_LongNoteLenghtBounds;
            }
        }

        public static string Alert_LongNoteStartPoint
        {
            get
            {
                return s_instance.alert_LongNoteStartPoint;
            }
        }

        public static string Alert_LongNoteStartSegment
        {
            get
            {
                return s_instance.alert_LongNoteStartSegment;
            }
        }

        public static string Alert_LongNoteNotFinalized
        {
            get
            {
                return s_instance.alert_LongNoteNotFinalized;
            }
        }

        public static string Alert_MaxNumberOfNotes
        {
            get
            {
                return s_instance.alert_MaxNumberOfNotes;
            }
        }

        public static string Alert_MaxNumberOfSpecialNotes
        {
            get
            {
                return s_instance.alert_MaxNumberOfSpecialNotes;
            }
        }
        public static string Alert_CantPlaceRailOfDifferntSubtype
        {
            get
            {
                return s_instance.alert_CantPlaceRailOfDifferntSubtype;
            }
        }
        
        public static string Alert_MaxNumberOfTypeNotes
        {
            get
            {
                return s_instance.alert_MaxNumberOfTypeNotes;
            }
        }

        public static string Alert_LongNoteNotFinalizedEffect
        {
            get
            {
                return s_instance.alert_LongNoteNotFinalizedEffect;
            }
        }

        public static string Alert_LongNoteNotFinalizedBookmark
        {
            get
            {
                return s_instance.alert_LongNoteNotFinalizedBookmark;
            }
        }

        public static string Alert_MaxNumberOfEffects
        {
            get
            {
                return s_instance.alert_MaxNumberOfEffects;
            }
        }

        public static string Alert_EffectsInterval
        {
            get
            {
                return s_instance.alert_EffectsInterval;
            }
        }
#endregion

#region Promts
        public static string Promt_ClearNotes
        {
            get
            {
                return s_instance.promt_ClearNotes;
            }
        }

        public static string Promt_BackToMenu
        {
            get
            {
                return s_instance.promt_BackToMenu;
            }
        }

        public static string Promt_NotSaveChanges
        {
            get
            {
                return s_instance.promt_NotSaveChanges;
            }
        }        

        public static string Promt_CopyAllNotes
        {
            get
            {
                return s_instance.promt_CopyAllNotes;
            }
        }

        public static string Promt_PasteNotes
        {
            get
            {
                return s_instance.promt_PasteNotes;
            }
        }

        public static string Promt_SaveFile
        {
            get
            {
                return s_instance.promt_SaveFile;
            }
        }   

        public static string Promt_ExitApp
        {
            get
            {
                return s_instance.promt_ExitApp;
            }
        }      

        public static string Promt_ClearBookmarks
        {
            get
            {
                return s_instance.promt_ClearBookmarks;
            }
        }         
#endregion

#region Info
		public static string Info_UserOnSpecialSection
        {
            get
            {
                return s_instance.info_UserOnSpecialSection;
            }
        }

        public static string Info_UserOnLongNoteMode
        {
            get
            {
                return s_instance.info_UserOnLongNoteMode;
            }
        }

        public static string Info_NotNotesToCopy
        {
            get
            {
                return s_instance.info_NotNotesToCopy;
            }
        }

        public static string Info_ClipBoardEmpty
        {
            get
            {
                return s_instance.info_ClipBoardEmpty;
            }
        }

        public static string Info_NotesCopied
        {
            get
            {
                return s_instance.info_NotesCopied;
            }
        }

        public static string Info_FileSaved
        {
            get
            {
                return s_instance.info_FileSaved;
            }
        }

        public static string Info_Metronome
        {
            get
            {
                return s_instance.info_Metronome;
            }
        }

        public static string Info_LeftCameraLabel
        {
            get
            {
                return s_instance.info_LeftCameraLabel;
            }
        }

        public static string Info_RightCameraLabel
        {
            get
            {
                return s_instance.info_RightCameraLabel;
            }
        }

        public static string Info_CenterCameraLabel
        {
            get
            {
                return s_instance.info_CenterCameraLabel;
            }
        }

        public static string Info_FreeCameraLabel
        {
            get
            {
                return s_instance.info_FreeCameraLabel;
            }
        }

        public static string Info_LongNoteModeEnabled
        {
            get
            {
                return s_instance.info_LongNoteModeEnabled;
            }
        }

        public static string Info_LongNoteModeDisabled
        {
            get
            {
                return s_instance.info_LongNoteModeDisabled;
            }
        }

        public static string Info_LongNoteModeFinalized
        {
            get
            {
                return s_instance.info_LongNoteModeFinalized;
            }
        }

        public static string Info_LongNoteModeAborted
        {
            get
            {
                return s_instance.info_LongNoteModeAborted;
            }
        }

        public static string Info_NotePasteSuccess
        {
            get
            {
                return s_instance.info_NotePasteSuccess;
            }
        }

        public static string Info_SpecialModeStarted
        {
            get
            {
                return s_instance.info_SpecialModeStarted;
            }
        }

        public static string Info_SpecialModeFinalized
        {
            get
            {
                return s_instance.info_SpecialModeFinalized;
            }
        }

        public static string Info_FlashOn
        {
            get
            {
                return s_instance.info_FlashOn;
            }
        }

        public static string Info_FlashOff
        {
            get
            {
                return s_instance.info_FlashOff;
            }
        }

        public static string Info_BookmarkOn
        {
            get
            {
                return s_instance.info_BookmarkOn;
            }
        }

        public static string Info_BookmarkOff
        {
            get
            {
                return s_instance.info_BookmarkOff;
            }
        }

        public static string Info_JumpOn
        {
            get
            {
                return s_instance.info_JumpOn;
            }
        }

        public static string Info_JumpOff
        {
            get
            {
                return s_instance.info_JumpOff;
            }
        }

        public static string Info_CrouchOn
        {
            get
            {
                return s_instance.info_CrouchOn;
            }
        }

        public static string Info_CrouchOff
        {
            get
            {
                return s_instance.info_CrouchOff;
            }
        }

        public static string Info_SlideOn
        {
            get
            {
                return s_instance.info_SlideOn;
            }
        }

        public static string Info_SlideOff
        {
            get
            {
                return s_instance.info_SlideOff;
            }
        }

        public static string Info_AdminMode
        {
            get
            {
                return s_instance.info_AdminMode;
            }
        }

        public static string Info_SycnMode
        {
            get
            {
                return s_instance.info_SyncnMode;
            }
        }

        public static string Info_SaveAborted
        {
            get
            {
                return s_instance.info_SaveAborted;
            }
        }

        public static string Info_LatencyUpdated
        {
            get
            {
                return s_instance.info_LatencyUpdated;
            }
        }
        
        public static string Info_BookmarksCleared
        {
            get
            {
                return s_instance.info_BookmarksCleared;
            }
        }

        public static string Info_VSyncnMode
        {
            get
            {
                return s_instance.info_VSyncnMode;
            }
        }

        public static string Info_ScrollSound
        {
            get
            {
                return s_instance.info_ScrollSound;
            }
        }        

        public static string Info_SpectrumLoading
        {
            get
            {
                return s_instance.info_SpectrumLoading;
            }
        }

        public static string Info_UserOnSelectionMode
        {
            get
            {
                return s_instance.info_UserOnSelectionMode;
            }
        }

        public static string Info_UserOffSelectionMode
        {
            get
            {
                return s_instance.info_UserOffSelectionMode;
            }
        }  

        public static string Info_MiddleButtonType
        {
            get
            {
                return s_instance.info_MiddleButtonType;
            }
        }

        public static string Info_AutoSaveFunction
        {
            get
            {
                return s_instance.info_AutoSaveFunction;
            }
        }   

        public static string Info_MirroredMode
        {
            get
            {
                return s_instance.info_MirroredMode;
            }
        } 

        public static string Info_NoteTooClose
        {
            get
            {
                return s_instance.info_NoteTooClose;
            }
        } 

        public static string Info_PasteTooFar
        {
            get
            {
                return s_instance.info_PasteTooFar;
            }
        }             
             
        public static string Info_FileExported
        {
            get
            {
                return s_instance.info_FileExported;
            }
        }

        public static string Info_LightsEffect {
            get 
            {
                return s_instance.info_LightsEffect;
            }
        }

        public static string Info_GridSnapp {
            get 
            {
                return s_instance.info_GridSnapp;
            }
        }
        
#endregion

    }
}
