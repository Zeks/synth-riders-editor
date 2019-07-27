using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using DG.Tweening;
using DSPLib;
using MiKu.NET.Charting;
using MiKu.NET.Utils;
using Newtonsoft.Json;
using Shogoki.Utils;
using ThirdParty.Custom;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiKu.NET {

    /// <sumary>
    /// Small class for the representation of the Track info
    /// </sumary>
    [Serializable]
    public class TrackInfo {
        public string name;
        public string artist;
        public string duration;
        public string coverImage;
        public string audioFile;
        public string[] supportedDifficulties;
        public float bpm;
        public string mapper;

        public string SaveToJSON()
        {
            return JsonUtility.ToJson(this, true);
        }
    }

    /// <sumary>
    /// Lookup taable for all the beats on the track
    /// </sumary>
    [Serializable]
    public struct BeatsLookup {
        public float step;
        public List<float> beats;
    }

    [Serializable]
    public class BeatsLookupTable {
        public float BPM;

        public BeatsLookup full;

        public BeatsLookup half;

        public BeatsLookup quarter;

        public BeatsLookup eighth;

        public BeatsLookup sixteenth;

        public BeatsLookup thirtyTwo;

        public BeatsLookup sixtyFourth;

        public string SaveToJSON()
        {
            return JsonUtility.ToJson(this, true);
        }
    }


    public struct LongNote {
        public float startTime;
        public Note note;
        public Note mirroredNote;
        public GameObject gameObject;
        public GameObject mirroredObject;
        public float duration;
        public float lastSegment;
        public List<GameObject> segments;
        public List<int> segmentAxis;
    }

    public struct SelectionArea
    {
        public float startTime;
        public float endTime;		
    }

    public struct ClipBoardStruct
    {
        public float startTime;
        public float lenght;
        public Dictionary<float, List<Note>> notes;
        public List<float> effects;
        public List<float> jumps;
        public List<float> crouchs;
        public List<Slide> slides;	
        public List<float> lights;	
    }

    public struct TrackMetronome
    {
        public float bpm;
        public bool isPlaying;
        public List<float> beats;
    }

    [RequireComponent(typeof(AudioSource))]
    public class Track : MonoBehaviour {
        public enum TrackDifficulty {
            Easy,
            Normal,
            Hard,
            Expert,
            Master,
            Custom,
        }

        public enum PromtType {			
            // No action
            NoAction,
            // Delete GameObjects and Lists
            DeleteAll,
            // Only Delete GameObjects
            ClearAll,
            // Back to Menu
            BackToMenu,
            CopyAllNotes,
            PasteNotes,
            SaveAction,
            JumpActionTime,
            JumpActionBookmark,
            EditActionBPM,
            AddBookmarkAction,
            EditLatency,
            ClearBookmarks,
            EditOffset,
            MouseSentitivity,
            CustomDifficultyEdit,
            TagEdition
        }

#region Constanst
        // Time constants
        // A second is 1000 Milliseconds
        public const int MS = 1000;

        // A minute is 60 seconds
        public const int MINUTE = 60;

        // Music vars
        // Resolution
        private const int R = 192;

        // Unity Unit / Second ratio
        public const float UsC = 20f/1f;

        // Beat per Measure
        // BpM use to draw the lines
        private const float DBPM = 1f/1f;

        // Max Number of normal notes allowed
        private const int MAX_ALLOWED_NOTES = 2;

        // Max Number of spcecial notes allowed
        private const int MAX_SPECIAL_NOTES = 1;

        // Time on seconds added at the end of the song for relieve
        private const float END_OF_SONG_OFFSET = 0.5f;

        // The Left Boundary of the grid
        private const float LEFT_GRID_BOUNDARY = -0.9535f;

        // The Top Boundary of the grid
        private const float TOP_GRID_BOUNDARY = 0.7596f;

        // The Right Boundary of the grid
        private const float RIGHT_GRID_BOUNDARY = 0.9575001f;

        // The Bottom Boundary of the grid
        private const float BOTTOM_GRID_BOUNDARY = -0.6054f;

        // Min distance tow notes can get before they are considered as overlaping
        private const float MIN_OVERLAP_DISTANCE = 0.15f;

        // Min duration on milliseconds that the line can have
        private const float MIN_LINE_DURATION = 0.1f*MS;

        // Max duration on milliseconds that the line can have
        private const float MAX_LINE_DURATION = 10*MS;

        // Max size that the note can have
        private const float MAX_NOTE_RESIZE = 0.2f;

        // Min size that the note can have
        private const float MIN_NOTE_RESIZE = 0.1f;

        // Min interval on Milliseconds between each effect
        private const float MIN_FLASH_INTERVAL = 1000f;

        // Max number of effects allowed
        private const int MAX_FLASH_ALLOWED = 80;

        // Min time to ask for save, on seconds
        private const int SAVE_TIME_CHECK = 30;

        // Min time to ask for Auto Save, on seconds
        private const int AUTO_SAVE_TIME_CHECK = 300;

        // Tags for the movments sections
        private const string JUMP_TAG = "Jump";

        private const string CROUCH_TAG = "Crouch";

        private const string SLIDE_RIGHT_TAG = "SlideRight";

        private const string SLIDE_LEFT_TAG = "SlideLeft";

        private const string SLIDE_CENTER_TAG = "SlideCenter";

        private const string SLIDE_RIGHT_DIAG_TAG = "SlideRightDiag";

        private const string SLIDE_LEFT_DIAG_TAG = "SlideLeftDiag";

        private const float MIN_TIME_OVERLAY_CHECK = 5;

        private const float MIN_NOTE_START = 2;

        public const int MAX_TAG_ALLOWED = 10;
        
#endregion

        // For static access
        private static Track s_instance;

        [SerializeField]
        private string editorVersion = "1.1-alpha.3";

        // If is on Debug mode we print all the console messages
        [SerializeField]
        private bool debugMode = false;

        [Space(20)]
        [Header("Track Elements")]
        // Transform that had the Cameras and will be moved
        [SerializeField]
        private Transform m_CamerasHolder;

        [SerializeField]
        private Transform m_NotesHolder;

        [SerializeField]
        private Transform m_NoNotesElementHolder;

        [SerializeField]
        private Transform m_SpectrumHolder;

        [SerializeField]
        private GameObject m_MetaNotesColider;

        [SerializeField]
        private GameObject m_FlashMarker;

        [SerializeField]
        private GameObject m_LightMarker;

        [SerializeField]
        private GameObject m_BeatNumberLarge;

        [SerializeField]
        private GameObject m_BeatNumberSmall;

        [SerializeField]
        private GameObject m_BookmarkElement;

        [SerializeField]
        private GameObject m_JumpElement;

        [SerializeField]
        private GameObject m_CrouchElement;

        [SerializeField]
        private GameObject m_SlideRightElement;

        [SerializeField]
        private GameObject m_SlideLeftElement;

        [SerializeField]
        private GameObject m_SlideCenterElement;	

        [SerializeField]
        private GameObject m_SlideDiagRightElement;

        [SerializeField]
        private GameObject m_SlideDiagLeftElement;	

        [SerializeField]
        private Light m_flashLight;

        [SerializeField]
        private LineRenderer m_selectionMarker;

        // Lines to use to draw the stage
        [SerializeField]
        private GameObject m_SideLines;
        [SerializeField]
        private GameObject m_ThickLine;
        private GameObject generatedLeftLine;
        private GameObject generatedRightLine;

        [SerializeField]
        private GameObject m_ThinLine;

        [SerializeField]
        private GameObject m_ThinLineXS;

        /*[SerializeField]
        private Transform m_XSLinesParent;*/

        // Metronome class
        /* [SerializeField]
        private Metronome m_metronome; */
        
        [SerializeField]
        private GameObject m_LefthandNoteMarker;

        [SerializeField]
        private GameObject m_LefthandNoteMarkerSegment;

        [SerializeField]
        private GameObject m_RighthandNoteMarker;

        [SerializeField]
        private GameObject m_RighthandNoteMarkerSegment;

        [SerializeField]
        private GameObject m_SpecialOneHandNoteMarker;

        [SerializeField]
        private GameObject m_Special1NoteMarkerSegment;

        [SerializeField]
        private GameObject m_SpecialBothHandsNoteMarker;

        [SerializeField]
        private GameObject m_Special2NoteMarkerSegment;

        [SerializeField]
        private float m_NoteSegmentMarkerRedution = 0.5f;	

        [SerializeField]
        private GameObject m_NotesDropArea;   

        [SerializeField]
        private GameObject m_DirectionMarker;

        [SerializeField]
        private float m_DirectionNoteAngle = -45f;

        [SerializeField]
        private MoveCamera m_CameraMoverScript;

        [Header("Spectrum Settings")]
        [SerializeField]
        float heightMultiplier = 0.8f;	

        [SerializeField]
        private GameObject m_NormalPointMarker;

        [SerializeField]
        private GameObject m_PeakPointMarker;		     

        [Header("UI Elements")]
        [SerializeField]
        private CanvasGroup m_UIGroupLeft;
        [SerializeField]
        private CanvasGroup m_UIGroupRight;

        [SerializeField]
        private GameObject m_RightSideBar;

        [SerializeField]
        private GameObject m_LeftSideBar;

        [SerializeField]
        private ScrollRect m_SideBarScroll;

        [SerializeField]
        private TextMeshProUGUI m_diplaySongName;

        [SerializeField]
        private TextMeshProUGUI m_diplayTime;

        [SerializeField]
        private TextMeshProUGUI m_diplayTimeLeft;

        [SerializeField]
        private TextMeshProUGUI m_BPMDisplay;

        [SerializeField]
        private InputField m_BPMInput;

        [SerializeField]
        private InputField m_OffsetInput;

        [SerializeField]
        private InputField m_BookmarkInput;

        [SerializeField]
        private InputField m_PanningInput;

        [SerializeField]
        private InputField m_RotationInput;
        [SerializeField]
        private InputField m_TagInput;

        [SerializeField]
        private Slider m_BPMSlider;			

        [SerializeField]
        private Slider m_VolumeSlider;	

        [SerializeField]
        private Slider m_SFXVolumeSlider;	

        [SerializeField]
        private TextMeshProUGUI m_OffsetDisplay;

        [SerializeField]
        private TextMeshProUGUI m_PlaySpeedDisplay;

        [SerializeField]
        private TextMeshProUGUI m_StepMeasureDisplay;

        /* [SerializeField]
        private Text m_DifficultyDisplay; */

        [SerializeField]
        private TMP_Dropdown m_BookmarkJumpDrop;

        [SerializeField]
        private GameObject m_BookmarkNotFound;

        [SerializeField]
        private InputField m_LatencyInput;

        [SerializeField]
        private InputField m_CustomDiffNameInput;

        [SerializeField]
        private InputField m_CustomDiffSpeedInput;

        [Space(20)]
        [SerializeField]
        private Animator m_PromtWindowAnimator;

        [SerializeField]
        private Animator m_JumpWindowAnimator;

        [SerializeField]
        private Animator m_ManualBPMWindowAnimator;

        [SerializeField]
        private Animator m_ManualOffsetWindowAnimator;

        [SerializeField]
        private Animator m_BookmarkWindowAnimator;

        [SerializeField]
        private Animator m_BookmarkJumpWindowAnimator;

        [SerializeField]
        private Animator m_HelpWindowAnimator;

        [SerializeField]
        private Animator m_SaveLoaderAnimator;

        [SerializeField]
        private Animator m_LatencyWindowAnimator;

        [SerializeField]
        private Animator m_MouseSentitivityAnimator;

        [SerializeField]
        private Animator m_CustomDiffEditAnimator;

        [SerializeField]
        private Animator m_TagEditAnimator;

        [SerializeField]
        private TextMeshProUGUI m_PromtWindowText;	

        [Space(20)]
        [SerializeField]
        private GameObject m_StateInfoObject;

        [SerializeField]
        private TextMeshProUGUI m_StateInfoText;

        [Space(20)]
        [SerializeField]
        private GridGuideController m_GridGuideController;

        [SerializeField]
        private GameObject m_GridGuide;

        [Space(20)]
        [Header("Audio Elements")]		
        /*[SerializeField]
        private AudioSource m_metronome;*/

        [SerializeField]
        private AudioSource m_SFXAudioSource;

        [SerializeField]
        private AudioSource m_MetronomeAudioSource;

        [SerializeField]
        private AudioClip m_StepSound;

        [SerializeField]
        private AudioClip m_HitMetaSound;

        [SerializeField]
        private AudioClip m_MetronomeSound;

        [Space(20)]
        [Header("Stats Window")]	
        [SerializeField]
        private GameObject m_StatsContainer;
        [SerializeField]
        private TextMeshProUGUI m_statsArtistText;
        [SerializeField]
        private TextMeshProUGUI m_statsSongText;
        [SerializeField]
        private Text m_statsDurationText;
        [SerializeField]
        private Text m_statsDifficultyText;
        [SerializeField]
        private TextMeshProUGUI m_statsTotalNotesText;
        [SerializeField]
        private Image m_statsArtworkImage;		
        [SerializeField]
        private GameObject m_statsAdminOnlyWrap;
        [SerializeField]
        private TextMeshProUGUI m_statsAdminOnlyText;

        [Space(20)]
        [Header("Camera Elements")]
        [SerializeField]
        private GameObject m_FrontViewCamera;

        [SerializeField]
        private GameObject m_LeftViewCamera;

        [SerializeField]
        private GameObject m_RightViewCamera;

        [SerializeField]
        private GameObject m_FreeViewCamera;		

        [SerializeField]
        private float m_CameraNearReductionFactor = 0.5f;

        [Space(20)]
        [Header("Editor Settings")]
        public bool syncnhWithAudio = false;

        private GameObject SelectedCamera { get; set; }		

        [SerializeField]
        public bool DirectionalNotesEnabled = true;

        // distance/time that will be drawed
        private int TM;		

        // BPM
        private float _BPM = 120;

        // Milliseconds per Beat
        private float K;		

        // BpM use to for the track movement
        private float MBPM = 1f/1f;
        private float MBPMIncreaseFactor = 1f;

        // Current time advance the note selector
        private float _currentTime = 0; 

        // Current Play time
        private float _currentPlayTime = 0;      

        // Current multiplier for the number of lines drawed
        private int _currentMultiplier = 1;

        // Note horizontal padding
        private Vector2 _trackHorizontalBounds = new Vector2(-1.2f, 1.2f); 

        // To save currently drawed lines for ease of acces
        private List<GameObject> drawedLines;
        private List<GameObject> drawedXSLines;

        // Is the editor Current Playing the Track
        private bool isPlaying = false;		

        // Current chart meta data
        private Chart currentChart;

        // Current difficulty selected for edition
        private TrackDifficulty currentDifficulty = TrackDifficulty.Easy;

        // Flag to know when there is a heavy burden and not manipulate the data
        private bool isBusy = false;		

        // Track Duration for the lines drawing, default 60 seconds
        private float trackDuration = 60;

        // Offset before the song start playing
        private float startOffset = 0;

        private float playSpeed = 1f;

        // Seconds of Lattency offset
        private float latencyOffset = 0f;

        // Song to be played
        private AudioClip songClip;

        // Used to play the AudioClip
        private AudioSource audioSource;

        // The current selected type of note marker
        private Note.NoteType selectedNoteType = Note.NoteType.RightHanded;

        // Has the chart been Initiliazed
        private bool isInitilazed = false;

        // for keyboard interactions
        private float keyHoldDelta = 0.15f;
        private float nextKeyHold = 0.5f;
        private float keyHoldTime = 0;
        private bool keyIsHold = false;
        private bool isCTRLDown = false;
        private bool isALTDown = false;
        private bool isSHIFDown = false;
        //

        private float lastBPM = 120f;
        private float lastK = 0;
        private bool wasBPMUpdated = false;

        private PromtType currentPromt = PromtType.BackToMenu;
        private bool promtWindowOpen = false;
        private bool helpWindowOpen = false;

        // For the ease of disabling/enabling notes when arrive the base
        private List<GameObject> disabledNotes;

        // For the refresh of the selected marker when changed
        private NotesArea notesArea;
        private bool markerWasUpdated = false;
        private bool gridWasOn = false;
        private float currentXSLinesSection = -1;
        private float currentXSMPBM;
        private bool isMetronomeActive = false;
        private bool wasMetronomePlayed = false;

        public int TotalNotes { get; set; }

        // For the ease of resizing of notes when to close of the front camera
        private List<GameObject> resizedNotes;

        // For when a Long note is being added
        private bool isOnLongNoteMode = false;

        private LongNote CurrentLongNote { get; set; }

        private bool turnOffGridOnPlay = false;

        // For the specials
        private bool specialSectionStarted = false;

        private int currentSpecialSectionID = -1;

        // To Only Play one hit sound at the time
        private float lastHitNoteZ = -1;

        private Stack<float> effectsStacks;

        private List<float> hitSFXSource;
        private Queue<float> hitSFXQueue;

        private float lastSaveTime = 0;

        // For the Spectrum Analizer
        int spc_numChannels;
        int spc_numTotalSamples;
        int spc_sampleRate;
        float spc_clipLength;
        float[] spc_multiChannelSamples;
        SpectralFluxAnalyzer preProcessedSpectralFluxAnalyzer;		
        bool threadFinished = false;
        bool treadWithError = false;
        Thread analyzerThread;

        private const string spc_cacheFolder = "/SpectrumData/";
        private const string spc_ext = ".spectrum";

        // String Builders
        StringBuilder forwardTimeSB;
        StringBuilder backwardTimeSB;
        TimeSpan forwardTimeSpan;

        int CurrentVsync = 0;
        Transform plotTempInstance;		

        // Pref Settings
        private const string MUSIC_VOLUME_PREF_KEY = "com.synth.editor.MusicVolume";
        private const string SFX_VOLUME_PREF_KEY = "com.synth.editor.SFXVolume";
        private const string VSYNC_PREF_KEY = "com.synth.editor.VSync";
        private const string LATENCY_PREF_KEY = "com.synth.editor.Latency";
        private const string SONG_SYNC_PREF_KEY = "com.synth.editor.SongSync";
        private const string PANNING_PREF_KEY = "com.synth.editor.PanningSetting";
        private const string ROTATION_PREF_KEY = "com.synth.editor.RotationSetting";
        private const string MIDDLE_BUTTON_SEL_KEY = "com.synth.editor.MiddleButtonSel";
        private const string AUTOSAVE_KEY = "com.synth.editor.AutoSave"; 
        private const string SCROLLSOUND_KEY = "com.synth.editor.ScrollSound";

        // 
        private WaitForSeconds pointEightWait;

        private SelectionArea CurrentSelection;
        private Vector3 selectionStartPos;
        private Vector3 selectionEndPos;

        private ClipBoardStruct CurrentClipBoard;

        private uint SideBarsStatus = 0;
        private bool bookmarksLoaded = false;
        private float lastUsedCK;

        private TrackMetronome Metronome;
        private Queue<float> MetronomeBeatQueue;

        private int middleButtonNoteTarget = 0;

        private bool isSpectrumGenerated = false;
        private Vector3 spectrumDefaultPos;
        private int MiddleButtonSelectorType = 0;
        private bool canAutoSave = true;
        private bool doScrollSound = true;

        private bool isOnMirrorMode = false;
        private bool xAxisInverse = true;
        private bool yAxisInverse = false; 

        private GridGuideController.GridGuideType GridGuideShapeType = 0;

        private TrackInfo trackInfo;

        private BeatsLookupTable BeatsLookupTable;

        private const float MIN_HIGHLIGHT_CHECK = 0.2f; 
        private float currentHighlightCheck = 0;
        private bool highlightChecked = false;
        private CursorLockMode currentLockeMode;

        // Use this for initialization
        void Awake () {	
            // Initilization of the Game Object to use for the line drawing
            drawedLines = new List<GameObject>();
            drawedXSLines = new List<GameObject>();

            generatedLeftLine = GameObject.Instantiate(m_SideLines, Vector3.zero,
                 Quaternion.identity, gameObject.transform);
            generatedLeftLine.name = "[Generated Left Line]";

            generatedRightLine = GameObject.Instantiate(m_SideLines, Vector3.zero,
                 Quaternion.identity, gameObject.transform);
            generatedRightLine.name = "[Generated Right Line]";	

            // AudioSource initilization
            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;	

            notesArea = m_NotesDropArea.GetComponent<NotesArea>();

            disabledNotes = new List<GameObject>();
            resizedNotes = new List<GameObject>();
            hitSFXSource = new List<float>();

            pointEightWait = new WaitForSeconds(0.8f);

            if(!m_SpecialOneHandNoteMarker 
                || !m_LefthandNoteMarker 
                || !m_RighthandNoteMarker 
                || !m_SpecialBothHandsNoteMarker) {
                Debug.LogError("Note maker prefab missing");
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
            }	

            currentLockeMode = Cursor.lockState;

            s_instance = this;			
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if(hasFocus) {
                Cursor.lockState = currentLockeMode;

                /* Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                    Cursor.lockState.ToString()
                ); */

                isALTDown = false;
                isCTRLDown = false;
                isSHIFDown = false;
            } else {
                if(isPlaying) {
                    TogglePlay();
                }
            }
        }

        void OnEnable() {	
            try {
                UpdateDisplayTime(_currentTime);
                m_MetaNotesColider.SetActive(false);
                
                gridWasOn = m_GridGuide.activeSelf;
                // Toggle Grid on by default
                if(!gridWasOn) ToggleGridGuide();
                
                // After Enabled we proced to Init the Chart Data
                InitChart();
                SwitchRenderCamera(0);
                ToggleWorkingStateAlertOff();

                CurrentLongNote = new LongNote();			
                CurrentSelection = new SelectionArea();
                //
                CurrentClipBoard = new ClipBoardStruct();
                CurrentClipBoard.notes = new Dictionary<float, List<Note>>();
                CurrentClipBoard.effects = new List<float>();
                CurrentClipBoard.jumps = new List<float>();
                CurrentClipBoard.crouchs = new List<float>();
                CurrentClipBoard.slides = new List<Slide>();
                CurrentClipBoard.lights = new List<float>();

                if(m_selectionMarker != null) {
                    selectionStartPos = m_selectionMarker.GetPosition(0);
                    selectionEndPos = m_selectionMarker.GetPosition(1);
                }
                ClearSelectionMarker();
            } catch(Exception e) {
                Serializer.WriteToLogFile("There was a error loading the Chart");
                Serializer.WriteToLogFile(e.ToString());
            }			
        }
        
        // Update is called once per frame
        void Update () {
            if(isBusy || !IsInitilazed){ return; }

            lastSaveTime += Time.deltaTime;
            keyHoldTime = keyHoldTime + Time.deltaTime;

            // Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)
            if(Input.GetButtonDown("Input Modifier1"))
            {
                isCTRLDown = true;
            }

            // Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl)
            if(Input.GetButtonUp("Input Modifier1")) {
                isCTRLDown = false;
            }

            // Input.GetKeyDown(KeyCode.LeftAlt)
            if(Input.GetButtonDown("Input Modifier2")) {
                isALTDown = true;
            }

            // Input.GetKeyUp(KeyCode.LeftAlt)
            if(Input.GetButtonUp("Input Modifier2")) {
                isALTDown = false;
            }

            // Input.GetKeyDown(KeyCode.LeftAlt)
            if(Input.GetButtonDown("Input Modifier3")) {
                if(!isOnLongNoteMode && !PromtWindowOpen && !isPlaying) {
                    isSHIFDown = true;
                    RefreshCurrentTime();
                    ToggleSelectionArea();
                }				
            }

            // Input.GetKeyUp(KeyCode.LeftAlt)
            if(Input.GetButtonUp("Input Modifier3")) {
                if(!isOnLongNoteMode && !PromtWindowOpen && !isPlaying) {
                    isSHIFDown = false;
                    ToggleSelectionArea(true);
                }
            }

#region Keyboard Shorcuts
            // Change Step and BPM
            // Input.GetKey(KeyCode.RightArrow)
            if( Input.GetAxis("Horizontal") != 0 && !isBusy && keyHoldTime > nextKeyHold && !PromtWindowOpen) {
                nextKeyHold = keyHoldTime + keyHoldDelta;
                if(!IsPlaying) {
                    
                    /* if(isCTRLDown) { m_BPMSlider.value = BPM + 1; }
                    else { ChangeStepMeasure(true); }	 */		
                    if(!isCTRLDown) {
                        ChangeStepMeasure(Input.GetAxis("Horizontal") > 0);
                    }		
                } else {
                    ChangePlaySpeed(Input.GetAxis("Horizontal") > 0);
                }
                nextKeyHold = nextKeyHold - keyHoldTime;
                keyHoldTime = 0.0f;				
            }

            // Movement on the track
            // Input.GetKey(KeyCode.DownArrow)
            float vertAxis = 0;
            if(Input.GetAxis("Vertical") != 0) {
                vertAxis = Input.GetAxis("Vertical");
            }

            if(Input.GetAxis("Vertical Free Camera") != 0 && SelectedCamera != m_FreeViewCamera && !isSHIFDown) {
                vertAxis = Input.GetAxis("Vertical Free Camera");
            }

            if( vertAxis < 0 && keyHoldTime > nextKeyHold && !PromtWindowOpen && !isCTRLDown && !isALTDown) {
                nextKeyHold = keyHoldTime + keyHoldDelta;
                MoveCamera(true, GetPrevStepPoint());
                DrawTrackXSLines();
                nextKeyHold = nextKeyHold - keyHoldTime;
                keyHoldTime = 0.0f;
            }
            
            // Input.GetKey(KeyCode.UpArrow)
            if( vertAxis > 0 && keyHoldTime > nextKeyHold && !PromtWindowOpen && !isCTRLDown && !isALTDown) {				
                nextKeyHold = keyHoldTime + keyHoldDelta;
                MoveCamera(true, GetNextStepPoint());
                DrawTrackXSLines();
                nextKeyHold = nextKeyHold - keyHoldTime;
                keyHoldTime = 0.0f;				
            }

            // Delete all the notes of the current difficulty
            // Input.GetKeyDown(KeyCode.Delete) 
            if( Input.GetButtonDown("Delete") && !PromtWindowOpen) {
                if(isCTRLDown && !IsPlaying) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();
                    DoClearNotePositions();
                } else if(!IsPlaying) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();
                    DeleteNotesAtTheCurrentTime();
                }
            }

            // Return to start time
            // Input.GetKeyDown(KeyCode.Home)
            if(Input.GetButtonDown("Timeline Start") && !PromtWindowOpen) {
                if(isCTRLDown && !IsPlaying) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();
                    ReturnToStartTime();
                    DrawTrackXSLines();
                }
            }		

            // Input.GetKeyDown(KeyCode.End)
            if(Input.GetButtonDown("Timeline End") && !PromtWindowOpen) {
                if(isCTRLDown && !IsPlaying) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();
                    GoToEndTime();
                    DrawTrackXSLines();
                }
            }		

            // Play/Stop
            // Input.GetKeyDown(KeyCode.Space)
            if((Input.GetButtonDown("Play") || (Input.GetButtonDown("PlayReturn") && !isSHIFDown))  && !PromtWindowOpen) {
                CloseSpecialSection();
                FinalizeLongNoteMode();
                TogglePlay(Input.GetButton("PlayReturn"));
            }

            // Close the promt window or do Return to menu promt
            if(Input.GetButtonDown("Cancel")) {
            //if(Input.GetKeyDown(KeyCode.Escape)) {
                if(PromtWindowOpen) {
                    ClosePromtWindow();					
                } else if(helpWindowOpen) {
                    ToggleHelpWindow();
                } else {
                    if(CurrentSelection.endTime > CurrentSelection.startTime) {
                        ClearSelectionMarker();						
                    } else {
                        DoReturnToMainMenu();
                    }					
                }
            }
            
            if(Input.GetButtonDown("Submit")) {
            // Accept promt action
            //if(Input.GetKeyDown(KeyCode.Return)) {
                if(PromtWindowOpen) {
                    OnAcceptPromt();
                }
            }

            // Save Action
            if(Input.GetKeyDown(KeyCode.S)) {
                if(isCTRLDown && !IsPlaying) {
                    DoSaveAction();					
                }				
            }

            if(Input.GetMouseButtonDown(2)) {
                if(!PromtWindowOpen && !isPlaying) {
                    middleButtonNoteTarget = GetNoteMarkerTypeIndex(selectedNoteType) + 1;

                    if(MiddleButtonSelectorType == 0 && middleButtonNoteTarget > 1) {
                        middleButtonNoteTarget = 0;
                    }

                    if(MiddleButtonSelectorType == 1) {
                        if(middleButtonNoteTarget < 2 || middleButtonNoteTarget > 3) {
                            middleButtonNoteTarget = 2;
                        }						
                    }

                    if(MiddleButtonSelectorType == 2 && middleButtonNoteTarget > 3) {
                        middleButtonNoteTarget = 0;
                    }
                    
                    /* if(middleButtonNoteTarget > 3) {
                        middleButtonNoteTarget = 0;						
                    } */

                    SetNoteMarkerType(middleButtonNoteTarget);
                    markerWasUpdated = true;
                }
            }

            // Number keys actions
            // Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)
            if(Input.GetButtonDown("Left Hand Note")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(SLIDE_LEFT_TAG);
                    } else {
                        SetNoteMarkerType(GetNoteMarkerTypeIndex(Note.NoteType.LeftHanded));
                        markerWasUpdated = true;
                    }
                }				
            }

            // Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)
            if(Input.GetButtonDown("Right Hand Note")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(SLIDE_RIGHT_TAG);
                    } else {
                        SetNoteMarkerType(GetNoteMarkerTypeIndex(Note.NoteType.RightHanded));
                        markerWasUpdated = true;
                    }
                }
            }

            // Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)
            if(Input.GetButtonDown("One Hand Special")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(SLIDE_CENTER_TAG);
                    } else {
                        SetNoteMarkerType(GetNoteMarkerTypeIndex(Note.NoteType.OneHandSpecial));
                        markerWasUpdated = true;
                    }
                }
            }

            // Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)
            if(Input.GetButtonDown("Both Hands Special")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(SLIDE_LEFT_DIAG_TAG);
                    } else {
                        SetNoteMarkerType(GetNoteMarkerTypeIndex(Note.NoteType.BothHandsSpecial));
                        markerWasUpdated = true;
                    }	
                }			
            }

            // Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)
            if(Input.GetButtonDown("Center Camera")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(SLIDE_RIGHT_DIAG_TAG);
                    } else {
                        SwitchRenderCamera(0);
                    }
                }
            }

            // Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)
            if(Input.GetButtonDown("Left Camera")) {
                if(!PromtWindowOpen) {
                    if(isCTRLDown) {
                        ToggleMovementSectionToChart(CROUCH_TAG);
                    } else {
                        SwitchRenderCamera(1);
                    }
                }
            }

            // Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)
            if(Input.GetButtonDown("Right Camera")) {
                if(!PromtWindowOpen) {
                    SwitchRenderCamera(2);					
                }
            }

            // (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            if( Input.GetButtonDown("Free View Camera") && !isALTDown ) {
                if(!PromtWindowOpen) {
                    SwitchRenderCamera(3);
                }
            }

            // (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            if( Input.GetButtonDown("Bookmarks") ) {
                if(isCTRLDown){
                    if(isALTDown) {
                        ToggleLightsToChart();
                    } else {
                        ToggleEffectToChart();
                    }					
                } else {
                    if(currentPromt == PromtType.AddBookmarkAction) {
                        if(!m_BookmarkInput.isFocused) {
                            ClosePromtWindow();
                        }							
                    } else {
                        ToggleBookmarkToChart();
                    }					
                }				
            }

            // Input.GetKeyDown(KeyCode.G) 
            // Toggle Grid Guide
            if(Input.GetButtonDown("Guide Grid") && !PromtWindowOpen) {
                if(isCTRLDown) {
                    SwitchGruidGuideType();
                } else {
                    ToggleGridGuide();
                }                				
            }	

            if(Input.GetButtonDown("Bookmark Jump") && !PromtWindowOpen) {
                ToggleBookmarkJump();
            }		

            // Toggle Metronome
            // Input.GetKeyDown(KeyCode.M) Metronome
            if(Input.GetButtonDown("Metronome") && !PromtWindowOpen) {
                ToggleMetronome();
            }

            // Mouse Scroll
            if (Input.GetAxis("Mouse ScrollWheel") > 0f && !IsPlaying && !PromtWindowOpen) // forward
            {
                if(!isCTRLDown && !isALTDown) {
                    MoveCamera(true, GetNextStepPoint());
                    DrawTrackXSLines();
                } else if(isCTRLDown) {
                    ChangeStepMeasure(true);
                }				
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f && !IsPlaying && !PromtWindowOpen) // backwards
            {
                if(!isCTRLDown && !isALTDown) {
                    MoveCamera(true, GetPrevStepPoint());
                    DrawTrackXSLines();
                } else if(isCTRLDown){
                    ChangeStepMeasure(false);
                }
            }

            // Volume control
            // (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus)
            if( Input.GetAxis("Volume") > 0 
                && keyHoldTime > nextKeyHold && !PromtWindowOpen ){

                nextKeyHold = keyHoldTime + keyHoldDelta;

                if(isCTRLDown) {
                    m_SFXVolumeSlider.value += 0.1f;
                } else {
                    m_VolumeSlider.value += 0.1f;
                }
                
                nextKeyHold = nextKeyHold - keyHoldTime;
                keyHoldTime = 0.0f;				
            }

            // (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)
            if( Input.GetAxis("Volume") < 0 
                && keyHoldTime > nextKeyHold && !PromtWindowOpen ){
                
                nextKeyHold = keyHoldTime + keyHoldDelta;

                if(isCTRLDown) {
                    m_SFXVolumeSlider.value -= 0.1f;
                } else {
                    m_VolumeSlider.value -= 0.1f;
                }

                nextKeyHold = nextKeyHold - keyHoldTime;
                keyHoldTime = 0.0f;		
            }

            // Copy and Paste actions
            if(Input.GetKeyDown(KeyCode.C)) {
                if(isCTRLDown && !IsPlaying && !PromtWindowOpen) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();
                    CopyAction();

                    /* if(TotalNotes > 0) {
                        DoCopyCurrentDifficulty();
                    } else {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_NotNotesToCopy);
                    } */
                    
                } 
            }

            if(Input.GetKeyDown(KeyCode.V)) {
                if(isCTRLDown && !IsPlaying && !PromtWindowOpen) {
                    CloseSpecialSection();
                    FinalizeLongNoteMode();

                    PasteAction();
                    /* if(Miku_Clipboard.Initialized && 
                        Miku_Clipboard.CopiedDict != null && 
                            Miku_Clipboard.CopiedDict.Count > 0) {
                        DoPasteOnCurrentDifficulty();
                    } else {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_ClipBoardEmpty);
                    } */
                    
                } 
            }

            // Toggle Stats Window
            if((Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) && !PromtWindowOpen) {
                ToggleStatsWindow();
            }

            // Toggle Help Window
            if(Input.GetKeyDown(KeyCode.F1)) {
                ToggleHelpWindow();
            }

            // Toggle LongNote Mode
            // Input.GetKeyDown(KeyCode.L)
            if( Input.GetButtonDown("Long Line Mode") && !isPlaying && !PromtWindowOpen) {
                ToggleLineMode();
            }

            // Jumpt to Time
            // Input.GetKeyDown(KeyCode.F)
            if(Input.GetButtonDown("Jump to Time") && !IsPlaying && !PromtWindowOpen) {
                CloseSpecialSection();
                FinalizeLongNoteMode();
                DoJumpToTimeAction();
            }

            if(Input.GetKeyDown(KeyCode.F12) && !PromtWindowOpen && !IsPlaying) {
                ToggleAdminMode();			
            }

            if(Input.GetKeyDown(KeyCode.F11) && !PromtWindowOpen && !IsPlaying) {
                // ToggleSynthMode();
                DoCustomDiffEdit();			
            }	

            if(Input.GetKeyDown(KeyCode.F10) && !PromtWindowOpen && !isPlaying) {
                if(!isCTRLDown) {
                    TagController.InitContainer();
                    DoTagEdit();
                } else {
                    ToggleLatencyWindow();
                }				
            }	

            if(Input.GetKeyDown(KeyCode.F9)) {
                if(!isCTRLDown) {
                    ToggleScrollSound();
                } else {
                    ToggleVsycn();
                }							
            }

            if(Input.GetKeyDown(KeyCode.F8) && !PromtWindowOpen && !isPlaying) {
                DoClearBookmarks();				
            }	

            if(Input.GetKeyDown(KeyCode.F7)) {
                ToggleAudioSpectrum();				
            }

            if(Input.GetKeyDown(KeyCode.F6) && !PromtWindowOpen && !isPlaying) {
                DoMouseSentitivity();				
            }		

            if(Input.GetKeyDown(KeyCode.F5) && !PromtWindowOpen && !isPlaying) {
                UpdateMiddleButtonSelector();		
            }	

            if(Input.GetKeyDown(KeyCode.F4) && !PromtWindowOpen && !isPlaying) {
                UpdateAutoSaveAction();		
            }	

            if(Input.GetKeyDown(KeyCode.F3) && !PromtWindowOpen && !isPlaying) {
                if(!isCTRLDown) {
                    ToggleMirrorMode();	
                } else {
                    ToggleGripSnapping();
                }
                    
            }	

            if(Input.GetKeyDown(KeyCode.F2) && !PromtWindowOpen && !isPlaying) {
                ExportToJSON();	
            }		

            if( Input.GetButtonDown("Mirrored inverse Y") && isOnMirrorMode) { 
                YAxisInverse = !YAxisInverse;
            }

            // #endregionInput.GetKeyDown(KeyCode.PageUp)
            if( Input.GetButtonDown("Advance UP") && !PromtWindowOpen) {
                if(!IsPlaying) {
                    float ms = MeasureToTime((TimeToMeasure(CurrentTime) + 1));
                    if(ms <= TrackDuration * MS) {
                        CurrentTime = GetCloseStepMeasure(ms, false);
                        MoveCamera(true, MStoUnit(CurrentTime));
                        DrawTrackXSLines();
                    }					
                }
            }	

            // Input.GetKeyDown(KeyCode.PageDown)
            if( Input.GetButtonDown("Advance DOWN") && !PromtWindowOpen) {
                if(!IsPlaying) {
                    float ms = MeasureToTime((TimeToMeasure(CurrentTime) - 1));
                    if(ms < 0){
                        ms = 0;
                    }

                    CurrentTime = GetCloseStepMeasure(ms, false);
                    MoveCamera(true, MStoUnit(CurrentTime));
                    DrawTrackXSLines();					
                }
            }	

            if( Input.GetButtonDown("TAB")) {
                ToggleSideBars();
            }

            if(Input.GetButtonDown("Select All") && isCTRLDown) {
                if(!isPlaying && !isOnLongNoteMode && !PromtWindowOpen) {
                    SelectAll();
                }
            }

            if(Input.GetKeyDown(KeyCode.P)) {
                HighlightNotes();
            }

            if(isSHIFDown) {
                CurrentSelection.endTime = CurrentTime;
                UpdateSelectionMarker();
            }

            // Directional Notes

            if(isSHIFDown && !promtWindowOpen && !isPlaying && !isOnLongNoteMode) {
                if(Input.GetKeyDown(KeyCode.D)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.Right);
                } else if(Input.GetKeyDown(KeyCode.C)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.RightBottom);
                } else if(Input.GetKeyDown(KeyCode.X)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.Bottom);
                } else if(Input.GetKeyDown(KeyCode.Z)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.LeftBottom);
                } else if(Input.GetKeyDown(KeyCode.A)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.Left);
                } else if(Input.GetKeyDown(KeyCode.Q)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.LeftTop);
                } else if(Input.GetKeyDown(KeyCode.W)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.Top);
                } else if(Input.GetKeyDown(KeyCode.E)) {
                    ToggleNoteDirectionMarker(Note.NoteDirection.RightTop);
                }
            }
#endregion

            if(markerWasUpdated) {
                markerWasUpdated = false;
                notesArea.RefreshSelectedObjec();
            }	

            if(lastSaveTime >= AUTO_SAVE_TIME_CHECK 
                && canAutoSave
                && !isOnLongNoteMode 
                && !PromtWindowOpen
                && !isPlaying) {
                SaveChartAction();
            }			
        }

        void FixedUpdate() {
            if(IsPlaying) {
                if(_currentPlayTime >= TrackDuration*MS){ Stop(); }
                else { MoveCamera(); }				
            }
        }

        void LateUpdate() {
            if(threadFinished) {
                threadFinished = false;
                EndSpectralAnalyzer();
            }

            if(IsPlaying) {
                CheckEffectsQueue();
                CheckSFXQueue();
                CheckMetronomeBeatQueue();
            } /* else {
                if(currentHighlightCheck <= MIN_HIGHLIGHT_CHECK) {
                    currentHighlightCheck += Time.deltaTime;
                    highlightChecked = false;
                } else {
                    if(!highlightChecked) {
                        highlightChecked = true;
                        RefreshCurrentTime();
                        HighlightNotes();
                        //LogMessage("Check for hightligh");
                    }					
                }
            } */
        }

        void OnDestroy() {
            CurrentChart = null;
            Serializer.ChartData = null;
            Serializer.CurrentAudioFileToCompress = null;
            SaveEditorUserPrefs();
            DoAbortThread();
        }

        void OnDrawGizmos() {
            if(s_instance == null) s_instance = this;

            float offset = transform.position.z;
            float ypos = transform.parent.position.y;
            CalculateConst();
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(_trackHorizontalBounds.x, ypos, offset), 
                new Vector3(_trackHorizontalBounds.x, ypos, GetLineEndPoint((TM-1)*K) + offset ));
            Gizmos.DrawLine(new Vector3(_trackHorizontalBounds.y, ypos, offset), 
                new Vector3(_trackHorizontalBounds.y, ypos,  GetLineEndPoint((TM-1)*K) + offset ));
            
            for(int i = 0; i < TM; i++) {
                Gizmos.DrawLine( new Vector3( _trackHorizontalBounds.x, ypos, i*GetLineEndPoint(K) ), 
                    new Vector3( _trackHorizontalBounds.y, ypos, i*GetLineEndPoint(K) ) );
                
            }

            float lastCiqo = 0;
            for(int j = 0; j < 4; ++j) {
                lastCiqo += K*( 1f/4f );
                Gizmos.DrawLine(new Vector3( _trackHorizontalBounds.x, ypos,  GetLineEndPoint(lastCiqo ) ), 
                    new Vector3( _trackHorizontalBounds.y, ypos,  GetLineEndPoint( lastCiqo ) ) );
            }
        }

        /// <summary>
        /// Init The chart metadata
        /// </summary>
        private void InitChart() {
            if(Serializer.Initialized) {
                CurrentChart = Serializer.ChartData;
                BPM = CurrentChart.BPM;	

                if(CurrentChart.Track.Master == null) {
                    CurrentChart.Track.Master = new Dictionary<float, List<Note>>();
                }

                if(CurrentChart.Effects.Master == null) {
                    CurrentChart.Effects.Master = new List<float>();				
                }

                if(CurrentChart.Jumps.Master == null) {
                    CurrentChart.Jumps.Master = new List<float>();					
                }

                if(CurrentChart.Crouchs.Master == null) {
                    CurrentChart.Crouchs.Master = new List<float>();					
                }

                if(CurrentChart.Slides.Master == null) {
                    CurrentChart.Slides.Master = new List<Slide>();				
                }

                if(CurrentChart.Track.Custom == null) {
                    CurrentChart.Track.Custom = new Dictionary<float, List<Note>>();
                }

                if(CurrentChart.Effects.Custom == null) {
                    CurrentChart.Effects.Custom = new List<float>();				
                }

                if(CurrentChart.Jumps.Custom == null) {
                    CurrentChart.Jumps.Custom = new List<float>();					
                }

                if(CurrentChart.Crouchs.Custom == null) {
                    CurrentChart.Crouchs.Custom = new List<float>();					
                }

                if(CurrentChart.Slides.Custom == null) {
                    CurrentChart.Slides.Custom = new List<Slide>();				
                }

                if(CurrentChart.CustomDifficultyName == null || CurrentChart.CustomDifficultyName == string.Empty) {
                    CurrentChart.CustomDifficultyName = "Custom";
                    CurrentChart.CustomDifficultySpeed = 1;
                }
                
                if(CurrentChart.Tags == null) {
                    CurrentChart.Tags = new List<string>();
                }

                /* songClip = AudioClip.Create(CurrentChart.AudioName,
                    CurrentChart.AudioData.Length,
                    CurrentChart.AudioChannels,
                    CurrentChart.AudioFrecuency,
                    false
                );

                songClip.SetData(Serializer.ChartData.AudioData, 0);
                StartOffset = CurrentChart.Offset;
                UpdateTrackDuration();				

                audioSource.clip = songClip; */
                Serializer.GetAudioClipFromZip(
                    (CurrentChart.FilePath != null && Serializer.CurrentAudioFileToCompress == null) ? CurrentChart.FilePath : string.Empty,
                    (Serializer.CurrentAudioFileToCompress == null) ? CurrentChart.AudioName : Serializer.CurrentAudioFileToCompress,
                    OnClipLoaded
                );

                LoadHitSFX();
            } else {
                
                CurrentChart = new Chart();
                Beats defaultBeats = new Beats();
                defaultBeats.Easy = new Dictionary<float, List<Note>>();
                defaultBeats.Normal = new Dictionary<float, List<Note>>();
                defaultBeats.Hard = new Dictionary<float, List<Note>>();
                defaultBeats.Expert = new Dictionary<float, List<Note>>();
                defaultBeats.Master = new Dictionary<float, List<Note>>();	
                defaultBeats.Custom = new Dictionary<float, List<Note>>();
                CurrentChart.Track = defaultBeats;

                Effects defaultEffects = new Effects();
                defaultEffects.Easy = new List<float>();
                defaultEffects.Normal = new List<float>();
                defaultEffects.Hard = new List<float>();
                defaultEffects.Expert = new List<float>();
                defaultEffects.Master = new List<float>();
                defaultEffects.Custom = new List<float>();
                CurrentChart.Effects = defaultEffects;

                Jumps defaultJumps = new Jumps();
                defaultJumps.Easy = new List<float>();
                defaultJumps.Normal = new List<float>();
                defaultJumps.Hard = new List<float>();
                defaultJumps.Expert = new List<float>();
                defaultJumps.Master = new List<float>();
                defaultJumps.Custom = new List<float>();
                CurrentChart.Jumps = defaultJumps;

                Crouchs defaultCrouchs = new Crouchs();
                defaultCrouchs.Easy = new List<float>();
                defaultCrouchs.Normal = new List<float>();
                defaultCrouchs.Hard = new List<float>();
                defaultCrouchs.Expert = new List<float>();
                defaultCrouchs.Master = new List<float>();
                defaultCrouchs.Custom = new List<float>();
                CurrentChart.Crouchs = defaultCrouchs;

                Slides defaultSlides = new Slides();
                defaultSlides.Easy = new List<Slide>();
                defaultSlides.Normal = new List<Slide>();
                defaultSlides.Hard = new List<Slide>();
                defaultSlides.Expert = new List<Slide>();
                defaultSlides.Master = new List<Slide>();
                defaultSlides.Custom = new List<Slide>();
                CurrentChart.Slides = defaultSlides;

                Lights defaultLights = new Lights();
                defaultLights.Easy = new List<float>();
                defaultLights.Normal = new List<float>();
                defaultLights.Hard = new List<float>();
                defaultLights.Expert = new List<float>();
                defaultLights.Master = new List<float>();
                defaultLights.Custom = new List<float>();
                CurrentChart.Lights = defaultLights;

                CurrentChart.BPM = BPM;
                CurrentChart.Bookmarks = new Bookmarks();
                CurrentChart.CustomDifficultyName = "Custom";
                CurrentChart.CustomDifficultySpeed = 1;

                CurrentChart.Tags = new List<string>();
                
                StartCoroutine(ResetApp());
            }	
        }

        void OnClipLoaded(AudioClip loadedClip) {
            if(!Serializer.IsExtratingClip && Serializer.ClipExtratedComplete) {
                songClip = loadedClip;
                StartOffset = CurrentChart.Offset;					
                audioSource.clip = songClip;

                UpdateTrackDuration();					
                // m_BPMSlider.value = BPM;
                m_BPMDisplay.SetText(BPM.ToString());
                UpdateDisplayStartOffset(StartOffset);			
                SetNoteMarkerType(); 
                DrawTrackLines();
                SetCurrentTrackDifficulty(TrackDifficulty.Easy);	
                SetStatWindowData();	
                IsInitilazed = true;

                BeginSpectralAnalyzer();
                LoadEditorUserPrefs();
                InitMetronome();

                // Setting the track info data
                trackInfo = new TrackInfo();
                trackInfo.name = CurrentChart.Name;
                trackInfo.artist = CurrentChart.Author;
                trackInfo.mapper = CurrentChart.Beatmapper;
                trackInfo.coverImage = CurrentChart.Artwork;
                trackInfo.audioFile = CurrentChart.AudioName;
                trackInfo.supportedDifficulties = new string[6] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
            }
        }

#region Spectrum Analyzer
        void BeginSpectralAnalyzer() {
            if(preProcessedSpectralFluxAnalyzer == null) {
                if(IsSpectrumCached()) {
                    try {
                        using(FileStream file = File.OpenRead(SpectrumCachePath+Serializer.CleanInput(CurrentChart.AudioName+spc_ext))) {
                            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            preProcessedSpectralFluxAnalyzer = (SpectralFluxAnalyzer) bf.Deserialize(file);
                        }

                        EndSpectralAnalyzer();
                        LogMessage("Spectrum loaded from cached");
                    } catch(Exception ex) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                            "Error while dezerializing Spectrum "+ex.ToString()
                        );
                        LogMessage(ex.ToString(), true);
                    }
                    
                } else {
                    preProcessedSpectralFluxAnalyzer = new SpectralFluxAnalyzer ();

                    // Need all audio samples.  If in stereo, samples will return with left and right channels interweaved
                    // [L,R,L,R,L,R]
                    spc_multiChannelSamples = new float[audioSource.clip.samples * audioSource.clip.channels];
                    spc_numChannels = audioSource.clip.channels;
                    spc_numTotalSamples = audioSource.clip.samples;
                    spc_clipLength = audioSource.clip.length;

                    // We are not evaluating the audio as it is being played by Unity, so we need the clip's sampling rate
                    spc_sampleRate = audioSource.clip.frequency;

                    audioSource.clip.GetData(spc_multiChannelSamples, 0);
                    LogMessage ("GetData done");

                    analyzerThread = new Thread (this.getFullSpectrumThreaded);

                    LogMessage ("Starting Background Thread");
                    analyzerThread.Start ();

                    ToggleWorkingStateAlertOn(StringVault.Info_SpectrumLoading);
                }				
            }			
        }

        void EndSpectralAnalyzer() {
            if(treadWithError) { 
                LogMessage ("Specturm could not be created", true);
                ToggleWorkingStateAlertOff();
                return; 
            }

            List<SpectralFluxInfo> flux = preProcessedSpectralFluxAnalyzer.spectralFluxSamples;
            Vector3 targetTransform = Vector3.zero;
            Vector3 targetScale = Vector3.one;
            
            Transform childTransform;
            for(int i = 0; i < flux.Count; ++i) {
                SpectralFluxInfo spcInfo = flux[i];
                if(spcInfo.spectralFlux > 0) {
                    plotTempInstance = Instantiate(
                        (spcInfo.isPeak) ? m_PeakPointMarker : m_NormalPointMarker
                    ).transform;
                    targetTransform.x = plotTempInstance.position.x;
                    targetTransform.y = plotTempInstance.position.y;
                    targetTransform.z = MStoUnit((spcInfo.time*MS)); //+StartOffset);
                    plotTempInstance.position = targetTransform;
                    plotTempInstance.parent = m_SpectrumHolder;

                    childTransform = plotTempInstance.Find("Point - Model");
                    if(childTransform != null) {
                        targetScale = childTransform.localScale;
                        targetScale.y = spcInfo.spectralFlux * heightMultiplier;
                        childTransform.localScale = targetScale;
                    }

                    childTransform = plotTempInstance.Find("Point - Model Top");
                    if(childTransform != null) {
                        targetScale = childTransform.localScale;
                        targetScale.x = spcInfo.spectralFlux * heightMultiplier;
                        childTransform.localScale = targetScale;
                    }
                    
                    //plotTempInstance.localScale = targetScale; 
                }	
                
                /* if(spcInfo.spectralFlux > 0) {
                    LogMessage ("Time is "+spcInfo.time+" at index "+i+" flux: "+(spcInfo.spectralFlux * 0.01f));
                    return;
                } */
            }	

            ToggleWorkingStateAlertOff();	
            isSpectrumGenerated = true;
            UpdateSpectrumOffset();

            if(!IsSpectrumCached()) {
                try {
                    using(FileStream file = File.Create(SpectrumCachePath+Serializer.CleanInput(CurrentChart.AudioName+spc_ext))) {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        bf.Serialize(file, preProcessedSpectralFluxAnalyzer);
                    }
                } catch {
                    LogMessage("There was a error while creating the specturm file", true);
                }				
            }
        }

        public int getIndexFromTime(float curTime) {
            float lengthPerSample = spc_clipLength / (float)spc_numTotalSamples;

            return Mathf.FloorToInt (curTime / lengthPerSample);
        }

        public float getTimeFromIndex(int index) {
            return ((1f / (float)spc_sampleRate) * index);
        }

        public void getFullSpectrumThreaded() {
            try {
                // We only need to retain the samples for combined channels over the time domain
                float[] preProcessedSamples = new float[spc_numTotalSamples];

                int numProcessed = 0;
                float combinedChannelAverage = 0f;
                for (int i = 0; i < spc_multiChannelSamples.Length; i++) {
                    combinedChannelAverage += spc_multiChannelSamples [i];

                    // Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
                    if ((i + 1) % spc_numChannels == 0) {
                        preProcessedSamples[numProcessed] = combinedChannelAverage / spc_numChannels;
                        numProcessed++;
                        combinedChannelAverage = 0f;
                    }
                }

                Debug.Log ("Combine Channels done");
                Debug.Log (preProcessedSamples.Length.ToString());

                // Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
                int spectrumSampleSize = 1024;
                int iterations = preProcessedSamples.Length / spectrumSampleSize;

                FFT fft = new FFT ();
                fft.Initialize ((UInt32)spectrumSampleSize);

                Debug.Log (string.Format("Processing {0} time domain samples for FFT", iterations));
                double[] sampleChunk = new double[spectrumSampleSize];
                for (int i = 0; i < iterations; i++) {
                    // Grab the current 1024 chunk of audio sample data
                    Array.Copy (preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

                    // Apply our chosen FFT Window
                    double[] windowCoefs = DSP.Window.Coefficients (DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
                    double[] scaledSpectrumChunk = DSP.Math.Multiply (sampleChunk, windowCoefs);
                    double scaleFactor = DSP.Window.ScaleFactor.Signal (windowCoefs);

                    // Perform the FFT and convert output (complex numbers) to Magnitude
                    Complex[] fftSpectrum = fft.Execute (scaledSpectrumChunk);
                    double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude (fftSpectrum);
                    scaledFFTSpectrum = DSP.Math.Multiply (scaledFFTSpectrum, scaleFactor);

                    // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
                    float curSongTime = getTimeFromIndex(i) * spectrumSampleSize;

                    // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
                    preProcessedSpectralFluxAnalyzer.analyzeSpectrum (Array.ConvertAll (scaledFFTSpectrum, x => (float)x), curSongTime);
                }

                Debug.Log ("Spectrum Analysis done");
                Debug.Log ("Background Thread Completed");
                
                threadFinished = true;
            } catch (Exception e) {
                threadFinished = true;
                treadWithError = true;
                // Catch exceptions here since the background thread won't always surface the exception to the main thread
                Debug.LogError (e.ToString ());
                Serializer.WriteToLogFile("getFullSpectrumThreaded Error");
                Serializer.WriteToLogFile(e.ToString());				
            }
        }

        private string SpectrumCachePath
        {
            get
            {
                /*if(Application.isEditor) {
                    return Application.dataPath+"/../"+save_path;
                } else {
                    return Application.persistentDataPath+save_path;
                }   */
                return Application.dataPath+"/../"+spc_cacheFolder;             
            }
        }

        private bool IsSpectrumCached() {
            if (!Directory.Exists(SpectrumCachePath)) {
                DirectoryInfo dir = Directory.CreateDirectory(SpectrumCachePath);
                dir.Attributes |= FileAttributes.Hidden;

                return false;
            }

            if(File.Exists(SpectrumCachePath+Serializer.CleanInput(CurrentChart.AudioName+spc_ext))){ 
                return true;				
            }

            return false;
        }

        private void UpdateSpectrumOffset() {
            if(isSpectrumGenerated) {
                if(spectrumDefaultPos == null) {
                    spectrumDefaultPos = new Vector3(
                        m_SpectrumHolder.transform.position.x, 
                        m_SpectrumHolder.transform.position.y, 
                        0
                    );
                }

                m_SpectrumHolder.transform.position = new Vector3(
                    spectrumDefaultPos.x, 
                    spectrumDefaultPos.y, 
                    spectrumDefaultPos.z + MStoUnit(StartOffset)
                );
            }
        }
#endregion
        

#region Public buttons actions
        /// <summary>
        /// Change Chart BPM and Redraw lines
        /// </summary>
        /// <param name="_bpm">the new bpm to set</param>
        public void ChangeChartBPM(float _bpm) {
            if(!IsInitilazed) return;

            // wasBPMUpdated = true;
            lastBPM = BPM;
            lastK = K;
            BPM = _bpm;
            m_BPMDisplay.SetText(BPM.ToString());			
            DrawTrackLines();
            DrawTrackXSLines(true);
            UpdateNotePositions();
            CurrentTime = 0;
            MoveCamera(true, CurrentTime);
            InitMetronome();
        }

        /// <summary>
        /// Change <see cname="StartOffset" /> by one Unit
        /// </summary>
        /// <param name="isIncrease">if true increase <see cname="StartOffset" /> otherwise decrease it</param>
        public void ChangeStartOffset(bool isIncrease) {
            int incrementFactor = (isCTRLDown) ? 1 : 100;
            int increment = (isIncrease) ? incrementFactor : -incrementFactor;
            StartOffset += increment;

            StartOffset = Mathf.Max(0, StartOffset);
            UpdateTrackDuration();
            UpdateDisplayStartOffset(StartOffset);			
        }

        /// <summary>
        /// Change the how large if the step that we are advancing on the measure
        /// </summary>
        /// <param name="isIncrease">if true increase <see cname="MBPM" /> otherwise decrease it</param>
        public void ChangeStepMeasure(bool isIncrease) {
            MBPMIncreaseFactor = (isIncrease) ? MBPMIncreaseFactor * 2 : MBPMIncreaseFactor / 2;
            MBPMIncreaseFactor = Mathf.Clamp(MBPMIncreaseFactor, 1, 64);
            MBPM = 1/MBPMIncreaseFactor;
            m_StepMeasureDisplay.SetText(string.Format("1/{0}", MBPMIncreaseFactor));
            DrawTrackXSLines();
        }

        /// <summary>
        /// Change the selected difficulty bein displayed
        /// </summary>
        /// <param name="isIncrease">if true get the next Difficulty otherwise the previous</param>
        public void ChangeDisplayDifficulty(bool isIncrease) {			
            int current = GetCurrentTrackDifficultyIndex();
            int increment = (isIncrease) ? 1 : -1;
            current += increment; 
            current = Mathf.Clamp(current, 0, 3);
            SetCurrentTrackDifficulty(current);						
        }

        /// <summary>
        /// Change <see cname="PlaySpeed" /> by one Unit
        /// </summary>
        /// <param name="isIncrease">if true increase <see cname="PlaySpeed" /> otherwise decrease it</param>
        public void ChangePlaySpeed(bool isIncrease) {
            float incrementFactor = 0.25f;
            float increment = (isIncrease) ? incrementFactor : -incrementFactor;
            PlaySpeed += increment;

            PlaySpeed = Mathf.Clamp(PlaySpeed, 0.5f, 2.5f);
            UpdatePlaybackSpeed();
            UpdateDisplayPlaybackSpeed();
        }

        /// <summary>
        /// Show Custom Difficulty windows />
        ///</summary>
        public void DoCustomDiffEdit() {
            if(currentPromt == PromtType.CustomDifficultyEdit) {
                ClosePromtWindow();
            } else {
                currentPromt = PromtType.CustomDifficultyEdit;
                m_CustomDiffNameInput.text = CurrentChart.CustomDifficultyName;
                m_CustomDiffSpeedInput.text = CurrentChart.CustomDifficultySpeed.ToString();
                ShowPromtWindow(String.Empty);
            }			
        }

        /// <summary>
        /// Show Tags windows />
        ///</summary>
        public void DoTagEdit() {
            if(currentPromt == PromtType.TagEdition) {
                ClosePromtWindow();
            } else {
                currentPromt = PromtType.TagEdition;
                m_TagInput.text = string.Empty;
                ShowPromtWindow(String.Empty);
            }			
        }

        /// <summary>
        /// Show MouseSentitivity windows />
        ///</summary>
        public void DoMouseSentitivity() {
            if(currentPromt == PromtType.MouseSentitivity) {
                ClosePromtWindow();
            } else {
                currentPromt = PromtType.MouseSentitivity;
                m_PanningInput.text = m_CameraMoverScript.panSpeed.ToString();
                m_RotationInput.text = m_CameraMoverScript.turnSpeed.ToString();
                ShowPromtWindow(String.Empty);
            }			
        }

        /// <summary>
        /// Show Promt to before the call to <see name="ClearNotePositions" />
        ///</summary>
        public void DoClearNotePositions() {
            currentPromt = PromtType.DeleteAll;
            ShowPromtWindow(StringVault.Promt_ClearNotes);
        }

        /// <summary>
        /// Show Promt to before returning to Main Menu />
        ///</summary>
        public void DoReturnToMainMenu() {
            currentPromt = PromtType.BackToMenu;
            ShowPromtWindow(
                StringVault.Promt_BackToMenu +(
                    NeedSaveAction() 
                    ?
                    "\n" +
                    StringVault.Promt_NotSaveChanges 
                    :
                    ""
                )
            );
        }

        /// <summary>
        /// Return view to start time
        ///</summary>
        public void ReturnToStartTime() {
            JumpToTime(0);
        }

        /// <summary>
        /// Send view to end time
        ///</summary>
        public void GoToEndTime() {
            JumpToTime(trackDuration * MS);
        }

        /// <summary>
        /// Show promt before saving the chart
        /// </summary>
        public void DoSaveAction(){
            currentPromt = PromtType.SaveAction;
            //ShowPromtWindow(string.Empty);
            OnAcceptPromt();
        }

        /// <summary>
        /// Show promt for manually edit BPM/Offset
        /// </summary>
        public void DoEditBPMManual(){
            currentPromt = PromtType.EditActionBPM;
            m_BPMInput.text = BPM.ToString();
            StartCoroutine(SetFieldFocus(m_BPMInput));
            ShowPromtWindow(string.Empty);
        }

        /// <summary>
        /// Show promt for manually edit BPM/Offset
        /// </summary>
        public void DoEditOffsetManual(){
            currentPromt = PromtType.EditOffset;
            m_OffsetInput.text = StartOffset.ToString();
            StartCoroutine(SetFieldFocus(m_OffsetInput));
            ShowPromtWindow(string.Empty);
        }

        /// <summary>
        /// Toggle the admin mode of the selected chart
        /// </summary>
        public void ToggleAdminMode(){
            CurrentChart.IsAdminOnly = !CurrentChart.IsAdminOnly;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, 
                string.Format(
                    StringVault.Info_AdminMode,
                    (currentChart.IsAdminOnly) ? "On" : "Off"
                )
            );

            SetStatWindowData();
        }

        /// <summary>
        /// Toggle the Synchronization of the movement with the playback
        /// </summary>
        public void ToggleSynthMode(){
            syncnhWithAudio = !syncnhWithAudio;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, 
                string.Format(
                    StringVault.Info_SycnMode,
                    (syncnhWithAudio) ? "On" : "Off"
                )
            );
        }

        /// <summary>
        /// Toggle the SpectralFlux Visualization
        /// </summary>
        public void ToggleAudioSpectrum(){
            m_SpectrumHolder.gameObject.SetActive(!m_SpectrumHolder.gameObject.activeSelf);
        }

        /// <summary>
        /// Action for the 'Yes' button of the promt windows.
        ///</summary>
        public void OnAcceptPromt() {
            switch(currentPromt) {
                case PromtType.DeleteAll:
                    ClearNotePositions();
                    resizedNotes.Clear();
                    break;
                case PromtType.BackToMenu:								
                    Miku_LoaderHelper.LauchPreloader();
                    break;
                case PromtType.CopyAllNotes:
                    if(Miku_Clipboard.Initialized) {
                        Miku_Clipboard.CopyTrackToClipboard(GetCurrentTrackDifficulty(), BPM);
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_NotesCopied);
                    }
                    break;
                case PromtType.PasteNotes:
                    if(Miku_Clipboard.Initialized) {
                        PasteChartNotes();
                    }
                    break;
                case PromtType.JumpActionTime:					
                    Miku_JumpToTime.GoToTime();
                    break;
                case PromtType.EditActionBPM:
                    if(m_BPMInput.text != string.Empty) {
                        float targetBPM = float.Parse(m_BPMInput.text);
                        if(targetBPM >= 40f && targetBPM <= 240f && targetBPM != BPM) {
                            //m_BPMSlider.value = targetBPM;
                            wasBPMUpdated = true;
                            ChangeChartBPM(targetBPM);
                        }
                    }
                    break;
                case PromtType.AddBookmarkAction:
                    List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
                    if(bookmarks != null){
                        Bookmark book = new Bookmark();
                        book.time = CurrentTime;
                        book.name = m_BookmarkInput.text;
                        bookmarks.Add(book);	
                        s_instance.AddBookmarkGameObjectToScene(book.time, book.name);

                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_BookmarkOn);
                    }
                    break;
                case PromtType.SaveAction:
                    SaveChartAction();
                    break;
                case PromtType.EditLatency:
                    float targetLatency = float.Parse(m_LatencyInput.text);
                    if(targetLatency <= 2f && targetLatency >= -2f) {
                        LatencyOffset = targetLatency;
                    }
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_LatencyUpdated);
                    break;
                case PromtType.ClearBookmarks:
                    ClearBookmarks();
                    break;
                case PromtType.EditOffset:
                    if(m_OffsetInput.text != string.Empty) {
                        float targetOffset = float.Parse(m_OffsetInput.text);
                        if(targetOffset >= 0 && targetOffset != StartOffset) {
                            StartOffset = targetOffset;
                            UpdateDisplayStartOffset(StartOffset);
                        }
                    }
                    break;
                case PromtType.MouseSentitivity:
                    if(m_PanningInput.text != string.Empty) {
                        float targetPan = float.Parse(m_PanningInput.text);
                        m_CameraMoverScript.panSpeed = targetPan;
                    }

                    if(m_RotationInput.text != string.Empty) {
                        float targetRot = float.Parse(m_RotationInput.text);
                        m_CameraMoverScript.turnSpeed = targetRot;
                    }
                    break;
                case PromtType.CustomDifficultyEdit:
                    if(m_CustomDiffNameInput.text != string.Empty) {
                        CurrentChart.CustomDifficultyName = m_CustomDiffNameInput.text;
                    }

                    if(m_CustomDiffSpeedInput.text != string.Empty) {
                        float targetSpeed = float.Parse(m_CustomDiffSpeedInput.text);
                        targetSpeed = Mathf.Clamp(targetSpeed, 1f, 3f);
                        CurrentChart.CustomDifficultySpeed = targetSpeed;
                    }
                    break;
                case PromtType.TagEdition:
                    if(m_TagInput.text != string.Empty) {
                        TagController.AddTag(m_TagInput.text);
                    }
                    break;
                default:
                    break;
            }

            if( currentPromt != PromtType.TagEdition) {
                ClosePromtWindow();
            }			
        }		

        /// <summary>
        /// Close the promt window
        /// </summary>
        public void ClosePromtWindow() {
            if(currentPromt != PromtType.JumpActionTime 
                && currentPromt != PromtType.EditActionBPM 
                && currentPromt != PromtType.JumpActionBookmark
                && currentPromt != PromtType.AddBookmarkAction
                && currentPromt != PromtType.SaveAction
                && currentPromt != PromtType.EditLatency
                && currentPromt != PromtType.EditOffset
                && currentPromt != PromtType.MouseSentitivity
                && currentPromt != PromtType.CustomDifficultyEdit
                && currentPromt != PromtType.TagEdition) {
                m_PromtWindowAnimator.Play("Panel Out");
            } else {
                if(currentPromt == PromtType.JumpActionTime) {
                    m_JumpWindowAnimator.Play("Panel Out");
                } else if(currentPromt == PromtType.AddBookmarkAction) {
                    m_BookmarkWindowAnimator.Play("Panel Out");
                    m_BookmarkInput.DeactivateInputField();
                } else if(currentPromt == PromtType.JumpActionBookmark) {
                    m_BookmarkJumpWindowAnimator.Play("Panel Out");
                } else if(currentPromt == PromtType.EditActionBPM) {
                    m_ManualBPMWindowAnimator.Play("Panel Out");
                    m_BPMInput.DeactivateInputField();
                } else if(currentPromt == PromtType.EditLatency) {
                    m_LatencyWindowAnimator.Play("Panel Out");
                    m_LatencyInput.DeactivateInputField();
                } else if(currentPromt == PromtType.EditOffset) {
                    m_ManualOffsetWindowAnimator.Play("Panel Out");
                    m_OffsetInput.DeactivateInputField();
                } else if(currentPromt == PromtType.MouseSentitivity) {
                    m_MouseSentitivityAnimator.Play("Panel Out");
                    m_PanningInput.DeactivateInputField();
                    m_RotationInput.DeactivateInputField();
                } else if(currentPromt == PromtType.CustomDifficultyEdit) {
                    m_CustomDiffEditAnimator.Play("Panel Out");
                    m_CustomDiffNameInput.DeactivateInputField();
                    m_CustomDiffSpeedInput.DeactivateInputField();
                } else if(currentPromt == PromtType.TagEdition) {
                    m_TagEditAnimator.Play("Panel Out");
                    m_TagInput.DeactivateInputField();
                }
            }
            currentPromt = PromtType.NoAction;			
            PromtWindowOpen = false;
        }

        /// <summary>
        /// Toggle play/stop actions
        ///</summary>
        public void TogglePlay(bool returnToStart = false) {
            System.GC.Collect();

            returnToStart = (returnToStart) ? returnToStart : isCTRLDown;
            if(IsPlaying) {
                lastHitNoteZ = -1;
                Stop(returnToStart);
            }
            else Play();
        }

        /// <summary>
        /// Save the chart to file
        /// </summary>
        public void SaveChartAction() {
            CurrentChart.BPM = BPM;
            CurrentChart.Offset = StartOffset;
            Serializer.ChartData = CurrentChart;
            Serializer.ChartData.EditorVersion = EditorVersion;

            TimeSpan t = TimeSpan.FromSeconds(TrackDuration);
            trackInfo.duration = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            if(CurrentChart.Track.Easy.Count > 0) {
                trackInfo.supportedDifficulties[0] = "Easy";
            }

            if(CurrentChart.Track.Normal.Count > 0) {
                trackInfo.supportedDifficulties[1] = "Normal";
            }

            if(CurrentChart.Track.Hard.Count > 0) {
                trackInfo.supportedDifficulties[2] = "Hard";
            }

            if(CurrentChart.Track.Expert.Count > 0) {
                trackInfo.supportedDifficulties[3] = "Expert";
            }

            if(CurrentChart.Track.Master.Count > 0) {
                trackInfo.supportedDifficulties[4] = "Master";
            }

            if(CurrentChart.Track.Custom.Count > 0) {
                trackInfo.supportedDifficulties[5] = "Custom";
            }

            trackInfo.bpm = CurrentChart.BPM;

            if(Serializer.SerializeToFile(Serializer.ChartData.FilePath)) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Info_FileSaved);
            }
            
            lastSaveTime = 0;
        }

        private void ExportToJSON() {
            CurrentChart.BPM = BPM;
            CurrentChart.Offset = StartOffset;
            Serializer.ChartData = CurrentChart;
            Serializer.ChartData.EditorVersion = EditorVersion;
            
            if(Serializer.SerializeToJSON()) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Info_FileExported);
            }
            
        }

        /// <summary>
        /// Turn On/Off the GridHelp
        /// </summary>
        /// <param name="forceOff">If true, the Guide will be allways turn off</param>
        public void ToggleGridGuide(bool forceOff = false) {
            if(isBusy) return;

            if(forceOff) m_GridGuide.SetActive(false);
            else {
                m_GridGuide.SetActive(!m_GridGuide.activeSelf);
                gridWasOn = m_GridGuide.activeSelf;
            }
        }

        /// <summary>
        /// Swith the grid guide between solid/outline
        /// </summary>
        public void SwitchGruidGuideType() {
            if(isBusy) return;

            m_GridGuideController.SwitchGridGuideType();
        }

        /// <summary>
        /// Toggle Metronome on/off
        ///</summary>
        public void ToggleMetronome() {
            isMetronomeActive = !isMetronomeActive;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Info_Metronome+(isMetronomeActive ? "On" : "Off"));			

            if(IsPlaying) {
                /* if(isMetronomeActive && !wasMetronomePlayed) {
                    m_metronome.Play( (_currentPlayTime % K) / MS );
                }

                if(!isMetronomeActive) {
                    m_metronome.Stop();
                }

                wasMetronomePlayed = isMetronomeActive; */

                if(isMetronomeActive && !wasMetronomePlayed) {
                    InitMetronomeQueue();
                    Metronome.isPlaying = true;
                } else {
                    Metronome.isPlaying = false;
                }

                wasMetronomePlayed = isMetronomeActive;
            }
        }

        /// <summary>
        /// Change Song audio volumen
        /// </summary>
        /// <param name="_volume">the volume to set</param>
        public void ChangeSongVolume(float _volume) {
            if(!IsInitilazed){ return; }

            audioSource.volume = _volume;
        }

        /// <summary>
        /// Change SFX audio volumen
        /// </summary>
        /// <param name="_volume">the volume to set</param>
        public void ChangeSFXVolume(float _volume) {
            if(!IsInitilazed){ return; }

            m_SFXAudioSource.volume = _volume;
            // m_MetronomeAudioSource.volume = _volume;
        }

        /// <summary>
        /// Show Promt to before the copy of the current difficulty notes />
        ///</summary>
        public void DoCopyCurrentDifficulty() {
            currentPromt = PromtType.CopyAllNotes;
            ShowPromtWindow(StringVault.Promt_CopyAllNotes);
        }

        /// <summary>
        /// Fill the clipboard with the data to be copied
        ///</summary>
        public void CopyAction() {
            isBusy = true;

            Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();
            List<float> effects = GetCurrentEffectDifficulty();
            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            List<Slide> slides = GetCurrentMovementListByDifficulty();
            List<float> lights = GetCurrentLightsByDifficulty();

            CurrentClipBoard.notes.Clear();
            CurrentClipBoard.effects.Clear();
            CurrentClipBoard.jumps.Clear();
            CurrentClipBoard.crouchs.Clear();
            CurrentClipBoard.slides.Clear();
            CurrentClipBoard.lights.Clear();

            List<float> keys_tofilter = workingTrack.Keys.ToList();
            if(CurrentSelection.endTime > CurrentSelection.startTime) {				
                keys_tofilter = keys_tofilter.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                CurrentClipBoard.effects = effects.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                CurrentClipBoard.jumps = jumps.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                CurrentClipBoard.crouchs = crouchs.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();
                
                CurrentClipBoard.slides = slides.Where(s => s.time >= CurrentSelection.startTime 
                    && s.time <= CurrentSelection.endTime).ToList();

                CurrentClipBoard.lights = lights.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();
                
                CurrentClipBoard.startTime = CurrentSelection.startTime;
                CurrentClipBoard.lenght = CurrentSelection.endTime - CurrentSelection.startTime;
            } else {
                RefreshCurrentTime();

                keys_tofilter = keys_tofilter.Where(time => time == CurrentTime).ToList();

                CurrentClipBoard.effects = effects.Where(time => time == CurrentTime).ToList();

                CurrentClipBoard.jumps = jumps.Where(time => time == CurrentTime).ToList();

                CurrentClipBoard.crouchs = crouchs.Where(time => time == CurrentTime).ToList();

                CurrentClipBoard.slides = slides.Where(s => s.time == CurrentTime).ToList();

                CurrentClipBoard.lights = lights.Where(time => time == CurrentTime).ToList();

                CurrentClipBoard.startTime = CurrentTime;
                CurrentClipBoard.lenght = 0;
            }

            for(int j = 0; j < keys_tofilter.Count; ++j) {
                float lookUpTime = keys_tofilter[j];

                if(workingTrack.ContainsKey(lookUpTime)) {
                    // If the time key exist, check how many notes are added
                    List<Note> copyNotes = workingTrack[lookUpTime];
                    List<Note> clipboardNotes = new List<Note>();
                    int totalNotes = copyNotes.Count;
                    
                    for(int i = 0; i < totalNotes; ++i) {
                        Note toCopy = copyNotes[i];
                        clipboardNotes.Add(toCopy);						
                    }

                    CurrentClipBoard.notes.Add(lookUpTime, clipboardNotes);
                }				
            }	

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_NotesCopied);
            ClearSelectionMarker();
            isBusy = false;	
        }

        /// <summary>
        /// Show Promt to before the paste of notes on the current difficulty />
        ///</summary>
        public void DoPasteOnCurrentDifficulty() {
            currentPromt = PromtType.PasteNotes;
            ShowPromtWindow(StringVault.Promt_PasteNotes);
        }

        public void PasteAction() {
            isBusy = true;
            float backUpTime = CurrentTime;

            CurrentSelection.startTime = backUpTime;
            CurrentSelection.endTime = backUpTime + CurrentClipBoard.lenght;

            // print(string.Format("Current {0} Lenght {1} Duration {2}", CurrentTime, CurrentClipBoard.lenght, TrackDuration * MS));
            if((CurrentTime + CurrentClipBoard.lenght) > (TrackDuration * MS) + MS) {
                // print(string.Format("{0} > {1} - {2}", (CurrentTime + CurrentClipBoard.lenght), TrackDuration * MS, (CurrentTime + CurrentClipBoard.lenght) > (TrackDuration * MS)));
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Info_PasteTooFar);
                isBusy = false;
                return;
            }

            DeleteNotesAtTheCurrentTime();

            List<float> note_keys = CurrentClipBoard.notes.Keys.ToList();
            if(note_keys.Count > 0) {
                Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();
                List<Note> copyList, currList;
                
                for(int i = 0; i < note_keys.Count; ++i) {
                    currList = CurrentClipBoard.notes[note_keys[i]];
                    copyList = new List<Note>();
                    float prevTime = note_keys[i];
                    float newTime = prevTime + (backUpTime - CurrentClipBoard.startTime);

                    for(int j = 0; j < currList.Count; ++j) {
                        Note currNote = currList[j];						
                        float newPos = MStoUnit(newTime);											

                        Note copyNote = new Note(
                            new Vector3(currNote.Position[0], currNote.Position[1], newPos),
                            FormatNoteName(newTime, j, currNote.Type),
                            currNote.ComboId,
                            currNote.Type,
                            currNote.Direction
                        );

                        if(currNote.Segments != null && currNote.Segments.GetLength(0) > 0) {	
                            float[,] copySegments = new float[currNote.Segments.GetLength(0), 3];
                            for(int x = 0; x < currNote.Segments.GetLength(0); ++x) {
                                Vector3 segmentPos = transform.InverseTransformPoint(
                                        currNote.Segments[x, 0],
                                        currNote.Segments[x, 1], 
                                        currNote.Segments[x, 2]
                                );

                                float tms = UnitToMS(segmentPos.z);
                                copySegments[x, 0] = currNote.Segments[x, 0];
                                copySegments[x, 1] = currNote.Segments[x, 1];
                                copySegments[x, 2] = MStoUnit(tms + (backUpTime - CurrentClipBoard.startTime));
                            }
                            copyNote.Segments = copySegments;
                        }

                        AddNoteGameObjectToScene(copyNote);
                        copyList.Add(copyNote);
                        UpdateTotalNotes();
                    }

                    workingTrack.Add(newTime, copyList);
                    AddTimeToSFXList(newTime);
                }				
            }			

            for(int i = 0; i < CurrentClipBoard.jumps.Count; ++i) {
                CurrentTime = CurrentClipBoard.jumps[i] + (backUpTime - CurrentClipBoard.startTime);
                ToggleMovementSectionToChart(JUMP_TAG, true);
            }

            for(int i = 0; i < CurrentClipBoard.crouchs.Count; ++i) {
                CurrentTime = CurrentClipBoard.crouchs[i] + (backUpTime - CurrentClipBoard.startTime);
                ToggleMovementSectionToChart(CROUCH_TAG, true);
            }

            for(int i = 0; i < CurrentClipBoard.slides.Count; ++i) {
                CurrentTime = CurrentClipBoard.slides[i].time + (backUpTime - CurrentClipBoard.startTime);
                ToggleMovementSectionToChart(GetSlideTagByType(CurrentClipBoard.slides[i].slideType), true);
            }

            for(int i = 0; i < CurrentClipBoard.effects.Count; ++i) {
                CurrentTime = CurrentClipBoard.effects[i] + (backUpTime - CurrentClipBoard.startTime);
                ToggleEffectToChart(true);
            }

            for(int i = 0; i < CurrentClipBoard.lights.Count; ++i) {
                CurrentTime = CurrentClipBoard.lights[i] + (backUpTime - CurrentClipBoard.startTime);
                ToggleLightsToChart(true);
            }

            CurrentTime = backUpTime;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_NotePasteSuccess);			
            isBusy = false;	
        }
        
        /// <summary>
        /// Turn on/off and update the data of the Stats Window
        /// </summary>
        public void ToggleStatsWindow() {
            m_StatsContainer.SetActive(!m_StatsContainer.activeSelf);
        }

        /// <summary>
        /// Change the currently use camera to render the scene
        /// </summary>
        /// <param name="cameraIndex">The index of the camera to use</param>
        public void SwitchRenderCamera(int cameraIndex) {
            if(PromtWindowOpen) { return; }
            if(SelectedCamera != null) { SelectedCamera.SetActive(false); }

            string cameraLabel;

            switch(cameraIndex) {
                case 1:
                    SelectedCamera = m_LeftViewCamera;
                    cameraLabel = StringVault.Info_LeftCameraLabel;
                    break;
                case 2:
                    SelectedCamera = m_RightViewCamera;
                    cameraLabel = StringVault.Info_RightCameraLabel;
                    break;
                case 3:
                    SelectedCamera = m_FreeViewCamera;
                    cameraLabel = StringVault.Info_FreeCameraLabel;
                    break;
                default:
                    SelectedCamera = m_FrontViewCamera;
                    cameraLabel = (StringVault.s_instance != null) ? StringVault.Info_CenterCameraLabel : "Center Camera";
                    break;
            }

            SelectedCamera.SetActive(true);
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, cameraLabel);
        }

        /// <summary>
        /// Toggle Help Window
        /// </summary>
        public void ToggleHelpWindow() {
            if(helpWindowOpen) {
                helpWindowOpen = false;
                m_HelpWindowAnimator.Play("Panel Out");
            } else {
                helpWindowOpen = true;
                m_HelpWindowAnimator.Play("Panel In");
            }
        }		

        /// <summary>
        /// Change the current Track Difficulty by index
        /// </summary>
        /// <param name="index">The index of the new difficulty from 0 - easy to 3 - Expert"</param>
        public void SetCurrentTrackDifficulty(int index = 0) {
            SetCurrentTrackDifficulty(GetTrackDifficultyByIndex(index));
        }

        /// <summary>
        /// Show the dialog to jump to a specifit time
        ///</summary>
        public void DoJumpToTimeAction() {
            Miku_JumpToTime.SetMinutePickerLenght(Mathf.RoundToInt(TrackDuration/60) + 1);
            currentPromt = PromtType.JumpActionTime;
            ShowPromtWindow(string.Empty);
        }

        /// <summary>
        /// Show the dialog to jump to a specifit bookmark
        ///</summary>
        public void ToggleBookmarkJump() {
            if(isBusy || IsPlaying) return;

            if(PromtWindowOpen) {
                if(currentPromt == PromtType.JumpActionBookmark){ ClosePromtWindow(); }
                return;
            }

            currentPromt = PromtType.JumpActionBookmark;
            m_BookmarkJumpDrop.ClearOptions();
            m_BookmarkJumpDrop.options.Add(new TMP_Dropdown.OptionData("Select a bookmark"));
            List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(bookmarks != null && bookmarks.Count > 0) {
                m_BookmarkJumpDrop.gameObject.SetActive(true);
                m_BookmarkNotFound.SetActive(false);
                bookmarks.Sort((x, y) => x.time.CompareTo(y.time));
                for(int i = 0; i < bookmarks.Count; ++i) {
                    m_BookmarkJumpDrop.options.Add(new TMP_Dropdown.OptionData(bookmarks[i].name));
                }
                m_BookmarkJumpDrop.RefreshShownValue();
                m_BookmarkJumpDrop.value = 0;

            } else {
                m_BookmarkJumpDrop.gameObject.SetActive(false);
                m_BookmarkNotFound.SetActive(true);
            }

            ShowPromtWindow(string.Empty);
        }

        /// <summary>
        /// Jump to the selected bookmark
        /// </summary>
        /// <param name="index">The index of the new difficulty from 0 - easy to 3 - Expert"</param>
        public void JumpToSelectedBookmark(int index = 0) {
            if(index <= 0) { return; }
            List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(bookmarks != null && bookmarks.Count > 0) {
                // print(bookmarks[index-1].time);
                JumpToTime(bookmarks[index-1].time);
                ClosePromtWindow();
            }			
        }

        /// <summary> 
        /// Toggle the LongLine Mode
        /// </summary>
        public void ToggleLineMode() {
            if(isOnLongNoteMode) {
                FinalizeLongNoteMode();
            } else {
                /* if(selectedNoteType == Note.NoteType.LeftHanded || selectedNoteType == Note.NoteType.RightHanded)  */{
                    isOnLongNoteMode = true;
                    CloseSpecialSection();
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_LongNoteModeEnabled);
                    ToggleWorkingStateAlertOn(StringVault.Info_UserOnLongNoteMode);
                }/*  else {
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteModeWrongNote);
                }	 */				
            }
        }

        /// <summary> 
        /// Public handler for the ToggleEffectToChart method
        /// </summary>
        public void ToggleFlash() {
            if(isCTRLDown) {
                ToggleLightsToChart();
            } else {
                ToggleEffectToChart();
            }			
        }

        /// <summary> 
        /// Public handler for the ToggleBookmarToChart method
        /// </summary>
        public void ToggleBookmark() {
            if(isCTRLDown) {
                ToggleBookmarkJump();
            } else {
                ToggleBookmarkToChart();
            }			
        }

        /// <summary>
        /// Show the dialog to edit playback latency
        ///</summary>
        public void ToggleLatencyWindow() {
            if(currentPromt == PromtType.EditLatency) {
                ClosePromtWindow();
            } else {
                currentPromt = PromtType.EditLatency;
                ShowPromtWindow(string.Empty);
            }			
        }

        /// <summary>
        /// Toggle the Vsync On/Off
        ///</summary>
        public void ToggleVsycn() {
            CurrentVsync++;
            if(CurrentVsync > 1) {
                CurrentVsync = 0;
            }

            QualitySettings.vSyncCount = CurrentVsync;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info,
                string.Format(
                    StringVault.Info_VSyncnMode,
                    (CurrentVsync == 1) ? "On" : "Off"
                )
            );
        }

        /// <summary>
        /// Toggle the Scroll sound On/Off
        ///</summary>
        public void ToggleScrollSound() {
            doScrollSound = !doScrollSound;

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info,
                string.Format(
                    StringVault.Info_ScrollSound,
                    (doScrollSound) ? "On" : "Off"
                )
            );
        }

        /// <summary>
        /// Show Promt to before the call to <see name="ClearBookmarks" />
        ///</summary>
        public void DoClearBookmarks() {
            currentPromt = PromtType.ClearBookmarks;
            ShowPromtWindow(StringVault.Promt_ClearBookmarks);
        }

        /// <summary>
        /// Clear The Beatmap Bookmarks
        ///</summary>
        public void ClearBookmarks() {
            List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(bookmarks != null && bookmarks.Count > 0) {
                for(int i = 0; i < bookmarks.Count; ++i) {
                    GameObject book = GameObject.Find(GetBookmarkIdFormated(bookmarks[i].time));
                    GameObject.DestroyImmediate(book);
                }

                bookmarks.Clear();
            }

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_BookmarksCleared);
        }

        /// <summary> 
        /// Control the visibility of the Sidebars
        /// </summary>
        public void ToggleSideBars() {
            SideBarsStatus++;
            if(SideBarsStatus > 3) {
                SideBarsStatus = 0;
            }

            switch(SideBarsStatus) {
                case 1: 
                    m_LeftSideBar.SetActive(false);
                    m_RightSideBar.SetActive(true);
                    break;
                case 2: 
                    m_LeftSideBar.SetActive(true);
                    m_RightSideBar.SetActive(false);
                    break;
                case 3: 
                    m_LeftSideBar.SetActive(false);
                    m_RightSideBar.SetActive(false);
                    break;
                default:
                    m_LeftSideBar.SetActive(true);
                    m_RightSideBar.SetActive(true);
                    break;
            }
        }

#endregion

        /// <summary>
        /// Try to find the Hit's SFX and begin its load
        /// </summary>
        void LoadHitSFX() {
            // Load of the Hit Sound
            string targetPath = string.Format("{0}/SFX/", Application.dataPath);
            string HitclipName = "HitSound.*";
            string StepclipName = "StepSound.*";
            string MetronomeclipName = "MetronomeSound.*";
            string[] targetFiles;
            if(Directory.Exists(targetPath)) {
                targetFiles = Directory.GetFiles(targetPath, HitclipName);
                if(targetFiles.Length > 0) {
                    StartCoroutine(GetHitAudioClip(@targetFiles[0]));
                }

                targetFiles = Directory.GetFiles(targetPath, StepclipName);
                if(targetFiles.Length > 0) {
                    StartCoroutine(GetHitAudioClip(@targetFiles[0], 1));
                }

                targetFiles = Directory.GetFiles(targetPath, MetronomeclipName);
                if(targetFiles.Length > 0) {
                    StartCoroutine(GetHitAudioClip(@targetFiles[0], 2));
                }
            } 
        }

        /// <summary>
        /// IEnumerator for the load of the hit sound
        /// </summary>
        IEnumerator GetHitAudioClip(string url, int type = 0)
        {
            using (WWW www = new WWW(url))
            {
                yield return www;	

                try {
                    if(type == 0) {
                        m_HitMetaSound = www.GetAudioClip(false, true);
                    } else if(type == 1) {
                        m_StepSound = www.GetAudioClip(false, true);
                    } else if(type == 2) {
                        m_MetronomeSound = www.GetAudioClip(false, true);
                    }
                                        
                }			
                catch (Exception ex)
                {
                    LogMessage("Problem opening the hit audio, please check extension" + ex.Message, true);
                } 			
            }
        }	

        private void SetStatWindowData() {
            m_statsArtistText.SetText(CurrentChart.Author);
            m_statsSongText.SetText(CurrentChart.Name);

            TimeSpan t = TimeSpan.FromSeconds(TrackDuration);
            m_statsDurationText.text = string.Format("{0:D2}:{1:D2}",
                t.Minutes, 
                t.Seconds);

            if(CurrentChart.ArtworkBytes != null) {
                Texture2D text = new Texture2D(1, 1);
                text.LoadImage(Convert.FromBase64String(CurrentChart.ArtworkBytes));
                Sprite artWorkSprite = Sprite.Create(text, new Rect(0,0, text.width, text.height), new Vector2(0.5f, 0.5f));	
                m_statsArtworkImage.sprite = artWorkSprite;
            }			

            if(Serializer.IsAdmin()) {
                m_statsAdminOnlyText.text = (CurrentChart.IsAdminOnly) ? "Admin Only Edit" : "Public Edit";
            } else {
                m_statsAdminOnlyWrap.SetActive(false);
            }

            m_diplaySongName.SetText(CurrentChart.Name);
        }

        /// <summary>
        /// Opens the promt window, and display the passed message
        /// </summary>
        /// <param name="message">The mensaje to show to the user</param>
        void ShowPromtWindow(string message) {
            if(currentPromt != PromtType.JumpActionTime 
                && currentPromt != PromtType.EditActionBPM 
                && currentPromt != PromtType.JumpActionBookmark
                && currentPromt != PromtType.AddBookmarkAction
                && currentPromt != PromtType.SaveAction
                && currentPromt != PromtType.EditLatency
                && currentPromt != PromtType.EditOffset
                && currentPromt != PromtType.MouseSentitivity
                && currentPromt != PromtType.CustomDifficultyEdit 
                && currentPromt != PromtType.TagEdition) {
                m_PromtWindowText.SetText(message);
                m_PromtWindowAnimator.Play("Panel In");
            } else {
                if(currentPromt == PromtType.JumpActionTime) {
                    m_JumpWindowAnimator.Play("Panel In");
                    Miku_JumpToTime.SetPickersValue(_currentTime);
                } else if(currentPromt == PromtType.AddBookmarkAction) {
                    m_BookmarkWindowAnimator.Play("Panel In");
                    m_BookmarkInput.text = string.Format("Bookmark-{0}", CurrentTime);
                    StartCoroutine(SetFieldFocus(m_BookmarkInput));
                } else if(currentPromt == PromtType.JumpActionBookmark) {
                    m_BookmarkJumpWindowAnimator.Play("Panel In");
                } else if(currentPromt == PromtType.EditActionBPM) { 
                    m_ManualBPMWindowAnimator.Play("Panel In");
                } else if(currentPromt == PromtType.EditLatency) {
                    m_LatencyWindowAnimator.Play("Panel In");
                    m_LatencyInput.text = string.Format("{0:.##}", LatencyOffset);
                    StartCoroutine(SetFieldFocus(m_LatencyInput));
                } else if(currentPromt == PromtType.EditOffset) { 
                    m_ManualOffsetWindowAnimator.Play("Panel In");
                } else if(currentPromt == PromtType.MouseSentitivity) {
                    m_MouseSentitivityAnimator.Play("Panel In");
                    StartCoroutine(SetFieldFocus(m_PanningInput));
                } else if(currentPromt == PromtType.CustomDifficultyEdit) {
                    m_CustomDiffEditAnimator.Play("Panel In");
                    StartCoroutine(SetFieldFocus(m_CustomDiffNameInput));
                } else if(currentPromt == PromtType.TagEdition) {
                    m_TagEditAnimator.Play("Panel In");
                    StartCoroutine(SetFieldFocus(m_TagInput));
                }
            }
            
            PromtWindowOpen = true;
        }

        /// </summary>
        /// Set the focus on the passed input field
        /// <summary>
        IEnumerator SetFieldFocus(InputField field) {
            yield return pointEightWait;

            field.ActivateInputField();
        }

        void FillLookupTable() {
            if(BeatsLookupTable == null) {
                BeatsLookupTable = new BeatsLookupTable();
            }

            if(BeatsLookupTable.BPM != BPM) {

                BeatsLookupTable.BPM = BPM;
                BeatsLookupTable.full = new BeatsLookup();
                BeatsLookupTable.full.step = K;
                BeatsLookupTable.half.step = K/2;
                BeatsLookupTable.quarter.step = K/4;
                BeatsLookupTable.eighth.step = K/8;
                BeatsLookupTable.sixteenth.step = K/16;
                BeatsLookupTable.thirtyTwo.step = K/32;
                BeatsLookupTable.sixtyFourth.step = K/64;
                
                if(BeatsLookupTable.full.beats == null) {
                    BeatsLookupTable.full.beats = new List<float>();
                    BeatsLookupTable.half.beats = new List<float>();
                    BeatsLookupTable.quarter.beats = new List<float>();
                    BeatsLookupTable.eighth.beats = new List<float>();
                    BeatsLookupTable.sixteenth.beats = new List<float>();
                    BeatsLookupTable.thirtyTwo.beats = new List<float>();
                    BeatsLookupTable.sixtyFourth.beats = new List<float>();
                } else {
                    BeatsLookupTable.full.beats.Clear();
                    BeatsLookupTable.half.beats.Clear();
                    BeatsLookupTable.quarter.beats.Clear();
                    BeatsLookupTable.eighth.beats.Clear();
                    BeatsLookupTable.sixteenth.beats.Clear();
                    BeatsLookupTable.thirtyTwo.beats.Clear();
                    BeatsLookupTable.sixtyFourth.beats.Clear();
                }				

                /* for(int i = 0; i < TM; i++) {
                    float lineEndPosition = (i*K);
                    BeatsLookupTable.full.beats.Add(lineEndPosition);
                } */
                float currentBeat = 0;
                while(currentBeat < TrackDuration * MS) {
                    BeatsLookupTable.full.beats.Add(currentBeat);
                    currentBeat += BeatsLookupTable.full.step;
                }

                // print(BeatsLookupTable.SaveToJSON());
            }
        }

        /// </summary>
        /// Draw the track lines
        /// <summary>
        void DrawTrackLines() {	
            // Make sure that all the const are calculated before Drawing the lines
            CalculateConst();
            // FillLookupTable();
            ClearLines();
            // DrawTrackXSLines();

            float offset = transform.position.z;
            float ypos = transform.parent.position.y;

            LineRenderer lr = GetLineRenderer(generatedLeftLine);
            lr.SetPosition(0, new Vector3(_trackHorizontalBounds.x, ypos, offset));
            lr.SetPosition(1, new Vector3(_trackHorizontalBounds.x, ypos, GetLineEndPoint((TM-1)*K) + offset ));
            
            LineRenderer rl = GetLineRenderer(generatedRightLine);
            rl.SetPosition(0, new Vector3(_trackHorizontalBounds.y, ypos, offset));
            rl.SetPosition(1, new Vector3(_trackHorizontalBounds.y, ypos, GetLineEndPoint((TM-1)*K) + offset ));

            uint currentBEAT = 0;
            uint beatNumberReal = 0;
            GameObject trackLine;
            LineRenderer trackRender;
            for(int i = 0; i < TM * _currentMultiplier; i++) {
                float lineEndPosition = (i*GetLineEndPoint(K)) + offset;
                if(currentBEAT % 4 == 0) {
                    trackLine = GameObject.Instantiate(m_ThickLine, Vector3.zero,
                        Quaternion.identity, gameObject.transform);	

                    DrawBeatNumber(beatNumberReal, lineEndPosition, trackLine.transform, true);
                    //beatNumberReal++;				
                } else {
                    trackLine = GameObject.Instantiate(m_ThinLine, Vector3.zero,
                        Quaternion.identity, gameObject.transform);	
                    DrawBeatNumber(beatNumberReal, lineEndPosition, trackLine.transform, false);
                }
                trackLine.name = "[Generated Beat Line]";
                trackRender = GetLineRenderer(trackLine);
                drawedLines.Add(trackLine);
                
                trackRender.SetPosition(0, new Vector3(_trackHorizontalBounds.x, ypos, lineEndPosition));
                trackRender.SetPosition(1, new Vector3(_trackHorizontalBounds.y, ypos, lineEndPosition));
                currentBEAT++;
                beatNumberReal++;
            }
        }

        /// </summary>
        /// Draw the track extra thin lines when the <see cref="MBPM"/> is increase
        /// <summary>
        /// <param name="forceClear">If true, the lines will be forcefull redrawed</param>
        void DrawTrackXSLines(bool forceClear = false) {	
            if(MBPM < 1) {
                float newXSSection = 0;
                float _CK = ( K*MBPM );
                // double xsKConst = (MS*MINUTE)/(double)BPM;
                if( (_currentTime%K) > 0 ) {
                    newXSSection = _currentTime - ( _currentTime%K );
                } else {
                    newXSSection = _currentTime; //+ ( K - (_currentTime%K ) );			
                }

                //print(string.Format("{2} : {0} - {1}", currentXSLinesSection, newXSSection, _currentTime));

                if(currentXSLinesSection != newXSSection || currentXSMPBM != MBPM || forceClear) {
                    ClearXSLines();

                    currentXSLinesSection = newXSSection;
                    currentXSMPBM = MBPM;
                    float startTime = currentXSLinesSection;
                    //float offset = transform.position.z;
                    float ypos = 0;

                    for(int j = 0; j < MBPMIncreaseFactor * 2; ++j) {
                        startTime += K*MBPM;
                        GameObject trackLineXS = GameObject.Instantiate(m_ThinLineXS, 
                            Vector3.zero, Quaternion.identity, gameObject.transform);	
                            trackLineXS.name = "[Generated Beat Line XS]";

                        trackLineXS.transform.localPosition = new Vector3(0, 0, trackLineXS.transform.localPosition.z);

                        LineRenderer trackRenderXS = GetLineRenderer(trackLineXS);
                        drawedXSLines.Add(trackLineXS);

                        trackRenderXS.SetPosition(0, new Vector3( _trackHorizontalBounds.x, ypos, GetLineEndPoint(startTime)) );
                        trackRenderXS.SetPosition(1, new Vector3( _trackHorizontalBounds.y, ypos, GetLineEndPoint(startTime)) );
                    }
                }				
            } else {
                ClearXSLines();
            }
        }

        /// <summary>
        /// Clear the already drawed lines
        /// </summary>
        void ClearLines() {
            if(drawedLines.Count <= 0) return;

            for(int i=0; i < drawedLines.Count; i++) {
                Destroy(drawedLines[i]);
            }

            drawedLines.Clear();
        }

        /// <summary>
        /// Clear the already drawed extra thin lines
        /// </summary>
        void ClearXSLines() {
            if(drawedXSLines.Count <= 0) return;

            for(int i=0; i < drawedXSLines.Count; i++) {
                DestroyImmediate(drawedXSLines[i]);
            }

            drawedXSLines.Clear();
        }

        /// <summary>
        /// Instance the number game object for the beat
        /// </summary>
        void DrawBeatNumber(uint number, float zPos, Transform parent = null, bool large = true) {
            if(number == 0) { return; }

            GameObject numberGO = GameObject.Instantiate( large ? m_BeatNumberLarge : m_BeatNumberSmall);
            numberGO.transform.localPosition = new Vector3(
                                                0,
                                                0, 
                                                zPos
                                            );
            
            numberGO.transform.rotation = Quaternion.identity;
            numberGO.transform.parent = (parent == null) ? m_NoNotesElementHolder : parent;
            string numberFormated = string.Format("{0:00}", number);

            BeatNumberHelper textField = numberGO.GetComponentInChildren<BeatNumberHelper>();
            textField.SetText(numberFormated);
            numberGO.name = "beat-"+numberFormated;
        }

        /// <summary>
        /// Calculate the constans needed to draw the track
        /// </summary>
        void CalculateConst() {
            K = (MS*MINUTE)/BPM;
            TM = Mathf.RoundToInt( BPM * ( TrackDuration/60 ) ) + 1;
        }

        /// <summary>
        /// Transform Milliseconds to Unity Unit
        /// </summary>
        /// <param name="_ms">Milliseconds to convert</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float MStoUnit(float _ms) {
            return (_ms/MS) * UsC;
        }

        /// <summary>
        /// Transform Unity Unit to Milliseconds
        /// </summary>
        /// <param name="_unit">Unity Units to convert</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float UnitToMS(float _unit) {
            return (_unit/UsC) * MS;
        }

        /// <summary>
        /// Given the Milliseconds return the position on Unity Unit
        /// </summary>
        /// <param name="_ms">Milliseconds to convert</param>
        /// <param name="offset">Offest at where the line will be drawed</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float GetLineEndPoint(float _ms, float offset = 0) {
            return MStoUnit((_ms + offset)*DBPM);
        }
        
        /// <summary>
        /// Return the next point to displace the stage
        /// </summary>
        /// <remarks>
        /// Based on the values of <see cref="K"/> and <see cref="MBPM"/>
        /// </remarks>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float GetNextStepPoint() {
            float _CK = ( K*MBPM );

            //_currentTime+= K*MBPM;
            //Debug.Log("Current "+_currentTime);
            if(_currentTime%_CK == 0) { 
                _currentTime += _CK; 
            } else { 
                float nextPoint = _currentTime + ( _CK - (_currentTime%_CK ) );
                //Debug.Log("Next "+nextPoint);
                //print(_CK);

                if(nextPoint == _currentTime) {
                    nextPoint = _currentTime + _CK;
                }

                if((nextPoint - _currentTime) <= _CK && lastUsedCK == _CK) {
                    nextPoint = _currentTime + _CK;
                }

                //nextPoint = _currentTime + _CK;
                _currentTime = nextPoint; //_currentTime + ( _CK - (_currentTime%_CK ) );
            }

            _currentTime = Mathf.Min(_currentTime, (TM-1)*K);
            lastUsedCK = _CK;
            return MStoUnit(_currentTime);
        }

        /// <summary>
        /// Return the prev point to displace the stage
        /// </summary>
        /// <remarks>
        /// Based on the values of <see cref="K"/> and <see cref="MBPM"/>
        /// </remarks>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float GetPrevStepPoint() {
            float _CK = ( K*MBPM );
            //_currentTime-= K*MBPM;
            if(_currentTime%_CK == 0) { 
                _currentTime -= _CK; 
            } else { 
                float nextPoint = _currentTime - ( _currentTime%_CK );

                if(nextPoint == _currentTime ) {
                    nextPoint = _currentTime - _CK;
                    //print("Now Here");
                    // || (_currentTime - nextPoint) <= _CK
                }

                if((_currentTime - nextPoint) <= _CK && lastUsedCK == _CK) {
                    nextPoint = _currentTime - _CK;
                }

                _currentTime = nextPoint; //_currentTime - ( _currentTime%_CK ); 
            }
            _currentTime = Mathf.Max(_currentTime, 0);	
            lastUsedCK = _CK;		
            return MStoUnit(_currentTime);
        }

        /// <summary>
        /// Get the LineRender to use to draw the line given the Prefab
        /// </summary>
        /// <returns>Returns <typeparamref name="LineRenderer"/></returns>
        LineRenderer GetLineRenderer(GameObject lineObj) {
            return lineObj.GetComponent<LineRenderer>();
        }

        void InitMetronome() {
            // Init metronme if not initialized
            if(Metronome.bpm == 0 || Metronome.bpm != BPM) {
                if(Metronome.beats == null) {
                    Metronome.beats = new List<float>(); 
                }

                Metronome.beats.Clear();
                Metronome.bpm = BPM;

                // Init the beats to a max of 10min
                //print("A beat every "+K.ToString());
                float metroDuration = Math.Max(5000, TrackDuration*MS);
                for(int i = 1; i <= metroDuration; ++i) {
                    //print(i+"-"+(i*K).ToString());
                    Metronome.beats.Add(i*K);
                }
                Metronome.beats.Sort();
            }
        }

        /// <summary>
        /// Play the track from the start or from <see cref="StartOffset"/>
        /// </summary>
        void Play() {
            float seekTime = (StartOffset > 0) ? Mathf.Max(0, (_currentTime / MS) - (StartOffset / MS) ) : (_currentTime / MS);
            // if(seekTime >= audioSource.clip.length) { seekTime = audioSource.clip.length; }
            audioSource.time = seekTime;
            /*float targetSample = (StartOffset > 0) ? Mathf.Max(0, (_currentTime / MS) - (StartOffset / MS) ) : (_currentTime / MS);
            targetSample = (CurrentChart.AudioFrecuency * CurrentChart.AudioChannels) * (_currentTime + targetSample);
            audioSource.timeSamples = (int)targetSample;*/
            _currentPlayTime = _currentTime;
            
            m_NotesDropArea.SetActive(false);
            m_MetaNotesColider.SetActive(true);

            if(turnOffGridOnPlay) { ToggleGridGuide(true); }

            m_UIGroupLeft.blocksRaycasts = false;
            m_UIGroupLeft.interactable = false;
            // m_UIGroupLeft.alpha = 0.3f;

            m_UIGroupRight.blocksRaycasts = false;
            m_UIGroupRight.interactable = false;
            // m_UIGroupRight.alpha = 0.3f;

            if(m_SideBarScroll) { 
                m_SideBarScroll.verticalNormalizedPosition = 1; 
            }		

            // Deprecated, Old Metronome Code
            /* if (m_metronome != null) {
                m_metronome.BPM = BPM;

                if(isMetronomeActive) {
                    if(m_MetronomeSound != null) {
                        m_metronome.TickClip = m_MetronomeSound;
                    }
                    
                    m_metronome.Play( (_currentPlayTime % K) / (float)MS);
                    wasMetronomePlayed = true;
                }
            } */

            if(isMetronomeActive) {
                InitMetronomeQueue();
                Metronome.isPlaying = true;
                wasMetronomePlayed = true;
            }

            EventSystem.current.SetSelectedGameObject(null);

            // Fill the effect stack for the controll
            if(effectsStacks == null) {
                effectsStacks = new Stack<float>();
            }

            List<float> workingEffects = GetCurrentEffectDifficulty();
            workingEffects.Sort();
            if(workingEffects != null && workingEffects.Count > 0) {
                for(int i = workingEffects.Count - 1; i >= 0; --i) {
                //for(int i = 0; i < workingEffects.Count; ++i) {
                    effectsStacks.Push(workingEffects[i]);
                }
                
                //Track.LogMessage(effectsStacks.Peek().ToString());
            }	
                

            if(hitSFXQueue == null) {
                hitSFXQueue = new Queue<float>();
            } else {
                hitSFXQueue.Clear();
            }

            hitSFXSource.Sort();
            for(int i = 0; i < hitSFXSource.Count; ++i) {
                if(hitSFXSource[i] >= _currentPlayTime){
                    hitSFXQueue.Enqueue(hitSFXSource[i]);
                }				
            }

            ResetResizedList();

            ClearSelectionMarker();

            if(seekTime < audioSource.clip.length){
                if(StartOffset == 0) { audioSource.Play(); }
                else { StartCoroutine(StartAudioSourceDelay()); }
            }

            // MoveCamera(true , MStoUnit(_currentPlayTime));	

            IsPlaying = true;
        }

        /// <summary>
        /// Init the metronome queue with the beats to play
        /// </summary>
        void InitMetronomeQueue() {
            if(MetronomeBeatQueue == null) {
                MetronomeBeatQueue = new Queue<float>();
            }

            MetronomeBeatQueue.Clear();
            if(Metronome.beats != null) {
                for(int i = 0; i < Metronome.beats.Count; ++i) {
                    if(Metronome.beats[i] >= _currentPlayTime){
                        MetronomeBeatQueue.Enqueue(Metronome.beats[i]);
                    }				
                }
            }			
        }

        /// <summary>
        /// Coorutine that start the AudioSource after the <see cref="StartOffset"/> millisecons has passed
        /// </summary>
        IEnumerator StartAudioSourceDelay() {
            yield return new WaitForSecondsRealtime(Mathf.Max(0, ( (StartOffset / MS)  - (_currentTime / MS)) / PlaySpeed  ));

            if(IsPlaying){ audioSource.Play(); }
        }

        /// <summary>
        /// Stop the play
        /// </summary>
        void Stop(bool backToPreviousPoint = false) {
            audioSource.time = 0;
            //audioSource.timeSamples = 0;
            

            if(StartOffset > 0) StopCoroutine(StartAudioSourceDelay());
            audioSource.Stop();
            
            /* if(m_metronome != null) {
                if(isMetronomeActive) m_metronome.Stop();
                wasMetronomePlayed = false;
            } */

            wasMetronomePlayed = false;
            IsPlaying = false;

            if(!backToPreviousPoint) {
                float _CK = ( K*MBPM );
                if( (_currentPlayTime%_CK) / _CK >= 0.5f ) {
                    _currentTime = GetCloseStepMeasure(_currentPlayTime);
                } else {
                    _currentTime = GetCloseStepMeasure(_currentPlayTime, false);
                }
            }

            _currentPlayTime = 0;

            MoveCamera(true , MStoUnit(_currentTime));

            m_NotesDropArea.SetActive(true);
            m_MetaNotesColider.SetActive(false);

            m_UIGroupLeft.blocksRaycasts = true;
            m_UIGroupLeft.interactable = true;
            // m_UIGroupLeft.alpha = 1f;

            m_UIGroupRight.blocksRaycasts = true;
            m_UIGroupRight.interactable = true;
            // m_UIGroupRight.alpha = 1f;

            if(gridWasOn && turnOffGridOnPlay) ToggleGridGuide();

            ResetDisabledList();
            // ResetResizedList();
            DrawTrackXSLines();

            // Clear the effect stack
            effectsStacks.Clear();
        }

        /// <summary>
        /// Play the track from the start
        /// </summary>
        /// <param name="manual">If "true" <paramref name="moveTo"/> will be used to translate <see cref="m_CamerasHolder"/> otherwise <see cref="_currentPlayTime"/> will be use</param>
        /// <param name="moveTo">Position to be translate</param>
        void MoveCamera(bool manual = false, float moveTo = 0) {
            float zDest = 0f;

            if(manual) {
                zDest = moveTo;
                UpdateDisplayTime(_currentTime);

                if(_currentTime > 0 && doScrollSound) {
                    PlaySFX(m_StepSound);
                }
                currentHighlightCheck = 0;
            } else {
                //_currentPlayTime += Time.unscaledDeltaTime * MS;
                if(audioSource.isPlaying && syncnhWithAudio)
                    _currentPlayTime = ( (audioSource.timeSamples / (float)audioSource.clip.frequency ) * MS) + StartOffset;
                else {
                    _currentPlayTime += (Time.smoothDeltaTime * MS) * PlaySpeed;
                }

                //_currentPlayTime -= (LatencyOffset * MS);
                //GetTrackTime();
                UpdateDisplayTime(_currentPlayTime);
                //m_CamerasHolder.Translate((Vector3.forward * Time.unscaledDeltaTime) * UsC);
                zDest = MStoUnit(_currentPlayTime - (LatencyOffset * MS));							
            }			
            
            m_CamerasHolder.position = new Vector3(
                    m_CamerasHolder.position.x,
                    m_CamerasHolder.position.y,
                    zDest);
            
        }

        /// <summary>
        /// Update the current time on with the user is
        /// </summary>
        /// <param name="_ms">Milliseconds to format</param>
        void UpdateDisplayTime(float _ms) {
            if(forwardTimeSB == null) {
                forwardTimeSB = new StringBuilder(16);
                backwardTimeSB = new StringBuilder(16);
            }
            forwardTimeSpan = TimeSpan.FromMilliseconds(_ms);

            forwardTimeSB.Length = 0;
            forwardTimeSB.AppendFormat("{0:D2}m:{1:D2}s.{2:D3}ms",
                forwardTimeSpan.Minutes.ToString("D2"), 
                forwardTimeSpan.Seconds.ToString("D2"), 
                forwardTimeSpan.Milliseconds.ToString("D3")
            );

            m_diplayTime.SetText(forwardTimeSB);

            forwardTimeSpan = TimeSpan.FromMilliseconds((TrackDuration*MS) - _ms);

            backwardTimeSB.Length = 0;
            backwardTimeSB.AppendFormat("{0:D2}m:{1:D2}s.{2:D3}ms",
                forwardTimeSpan.Minutes.ToString("D2"), 
                forwardTimeSpan.Seconds.ToString("D2"), 
                forwardTimeSpan.Milliseconds.ToString("D3")
            );

            m_diplayTimeLeft.SetText(backwardTimeSB);
        }

        /// <summary>
        /// Update the display of the Start Offset to a user friendly form
        /// </summary>
        /// <param name="_ms">Milliseconds to format</param>
        void UpdateDisplayStartOffset(float _ms) {
            TimeSpan t = TimeSpan.FromMilliseconds(_ms);

            m_OffsetDisplay.SetText(string.Format("{0:D2}s.{1:D3}ms",
                t.Seconds.ToString(), 
                t.Milliseconds.ToString()));
            
            UpdateSpectrumOffset();
            SetStatWindowData();
        }	

        /// <summary>
        /// Update the display of the PlayBack speed
        /// </summary>
        void UpdateDisplayPlaybackSpeed() {
            m_PlaySpeedDisplay.SetText(string.Format("{0:D1}x",
                PlaySpeed.ToString()
            ));
        }		

        /// <summary>
        /// Load the menu scene when the Serializer had not been initialized
        /// </summary>
        IEnumerator ResetApp() {			
            
            while(Miku_LoaderHelper.s_instance == null) {
                yield return null;
            }
            currentPromt = PromtType.BackToMenu;
            OnAcceptPromt();
        }

        /// <summary>
        /// Update the <see cref="TrackDuration" />
        /// </summary>
        private void UpdateTrackDuration() {
            if(Serializer.Initialized) {
                // TrackDuration = (StartOffset / MS) + ( CurrentChart.AudioData.Length / (CurrentChart.AudioFrecuency * CurrentChart.AudioChannels) ) + END_OF_SONG_OFFSET;
                TrackDuration = (StartOffset / MS) + ( songClip.length ) + END_OF_SONG_OFFSET;
            } else {
                TrackDuration = (StartOffset / MS) + ( MINUTE ) + END_OF_SONG_OFFSET;
            }
            
        }

        /// <summary>
        /// Update the audiosource picth />
        /// </summary>
        private void UpdatePlaybackSpeed() {
            audioSource.pitch = PlaySpeed;
        }

        /// <summary>
        /// Load the notes on the chart file, using the selected difficulty
        /// </summary>
        private void LoadChartNotes() {
            isBusy = true;
            UpdateTotalNotes(true);
            Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();
            Dictionary<float, List<Note>>.ValueCollection valueColl = workingTrack.Values;

            List<float> keys_sorted = workingTrack.Keys.ToList();
            keys_sorted.Sort();

            if(workingTrack != null && workingTrack.Count > 0) {
                // Iterate each entry on the Dictionary and get the note to update
                //foreach( List<Note> _notes in valueColl ) {
                foreach( float key in keys_sorted ) {
                    if(key > (TrackDuration * MS) ) {
                        // If the note to add is pass the current song duration, we delete it
                        workingTrack.Remove(key);
                    } else {
                        List<Note> _notes = workingTrack[key];
                        // Iterate each note and update its info
                        for(int i = 0; i < _notes.Count; i++) {
                            Note n = _notes[i];

                            // If the version of the Beatmap is not the same that the
                            // editor then move the note to the GridBoundaries to prevent
                            // breaking if the Grid had change sizes between update
                            // also apply the combo ids
                            if(CurrentChart.EditorVersion == null ||
                                CurrentChart.EditorVersion.Equals(string.Empty) ||
                                !CurrentChart.EditorVersion.Equals(EditorVersion)) {
                                // Clamp the notes to the Grid Boundaries
                                MoveToGridBoundaries(n);

                                // Update Combo ID
                                AddComboIdToNote(n);
                            } else {
                                // Update currentSpecialSectionID info
                                if(IsOfSpecialType(n)) {
                                    s_instance.currentSpecialSectionID = n.ComboId;
                                }
                            }
                            

                            // And add the note game object to the screen
                            AddNoteGameObjectToScene(n);
                            UpdateTotalNotes();

                            // Uncoment to enable sound on line end
                            /* if(n.Segments != null && n.Segments.GetLength(0) > 0) {
                                int last = n.Segments.GetLength(0) - 1;
                                Vector3 endPointPosition = transform.InverseTransformPoint(
                                        n.Segments[last, 0],
                                        n.Segments[last, 1], 
                                        n.Segments[last, 2]
                                );

                                float tms = UnitToMS(endPointPosition.z);
                                AddTimeToSFXList(tms);
                            } */
                        }

                        AddTimeToSFXList(key);						
                    }								
                }
            }

            Track.LogMessage("Current Special ID: "+s_instance.currentSpecialSectionID);

            if(CurrentChart.Effects == null) {
                CurrentChart.Effects = new Effects();
            }

            List<float> workingEffects = GetCurrentEffectDifficulty();
            if(workingEffects == null) {
                workingEffects = new List<float>();
            } else {
                if(workingEffects.Count > 0) {
                    for(int i = 0; i < workingEffects.Count; ++i) {
                        AddEffectGameObjectToScene(workingEffects[i]);
                    }
                }
            }

            List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(bookmarks != null && bookmarks.Count > 0) {
                for(int i = 0; i < bookmarks.Count; ++i) {
                    AddBookmarkGameObjectToScene(bookmarks[i].time, bookmarks[i].name);
                }
            }
            
            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            if(jumps != null && jumps.Count > 0) {
                for(int i = 0; i < jumps.Count; ++i) {
                    AddMovementGameObjectToScene(jumps[i], JUMP_TAG);
                }
            }

            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            if(crouchs != null && crouchs.Count > 0) {
                for(int i = 0; i < crouchs.Count; ++i) {
                    AddMovementGameObjectToScene(crouchs[i], CROUCH_TAG);
                }
            }

            List<Slide> slides = GetCurrentMovementListByDifficulty();
            if(slides != null && slides.Count > 0) {
                for(int i = 0; i < slides.Count; ++i) {
                    AddMovementGameObjectToScene(slides[i].time, GetSlideTagByType(slides[i].slideType));
                }
            }

            if(CurrentChart.Lights == null) {
                CurrentChart.Lights = new Lights();
            }

            List<float> lights = GetCurrentLightsByDifficulty();
            if(lights == null) {
                lights = new List<float>();
            } else {
                if(lights.Count > 0) {
                    for(int i = 0; i < lights.Count; ++i) {
                        AddLightGameObjectToScene(lights[i]);
                    }
                }
            }

            // If the Chart BPM was Changed we updated
            if(wasBPMUpdated) {
                wasBPMUpdated = false;
                float newBPM = BPM;
                BPM = CurrentChart.BPM;
                CurrentChart.BPM = newBPM;
                ChangeChartBPM(newBPM);
            }

            specialSectionStarted = false;
            isBusy = false;
        }

        /// <summary>
        /// Load the notes from the clipboard, using the selected difficulty
        /// </summary>
        [Obsolete("Method deprecated use PasteAction")]
        private void PasteChartNotes() {
            isBusy = true;
            UpdateTotalNotes(true);
            // First Clear the current chart data
            ClearNotePositions();						

            // Now get the track to start the paste operation

            // Track on where the notes will be paste
            Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();

            // Track from where the notes will be copied
            Dictionary<float, List<Note>> copiedTrack = Miku_Clipboard.CopiedDict;

            if(copiedTrack != null && copiedTrack.Count > 0) {

                // Iterate each entry on the Dictionary and get the note to copy
                foreach( KeyValuePair<float, List<Note>> kvp in copiedTrack )
                {
                    List<Note> _notes = kvp.Value;
                    List<Note> copiedList = new List<Note>();

                    // Iterate each note and update its info
                    for(int i = 0; i < _notes.Count; i++) {
                        Note n = _notes[i];
                        Note newNote = new Note(Vector3.zero);
                        newNote.Position = n.Position;
                        newNote.Id = Track.FormatNoteName(kvp.Key, i, n.Type);
                        newNote.Type = n.Type;
                        newNote.ComboId = n.ComboId;
                        newNote.Segments = n.Segments;

                        // And add the note game object to the screen
                        AddNoteGameObjectToScene(newNote);
                        UpdateTotalNotes();


                        copiedList.Add(newNote);
                    }
                    
                    // Add copied note to the list
                    workingTrack.Add(kvp.Key, copiedList);
                }
            }

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_NotePasteSuccess);

            if(Miku_Clipboard.ClipboardBPM != BPM) {
                UpdateNotePositions(Miku_Clipboard.ClipboardBPM);
            }
            isBusy = false;
        }

        /// <summary>
        /// Instantiate the Note GameObject on the scene
        /// </summary>
        /// <param name="noteData">The Note from where the data for the instantiation will be read</param>
        GameObject AddNoteGameObjectToScene(Note noteData) {
            // And add the note game object to the screen
            GameObject noteGO = GameObject.Instantiate(GetNoteMarkerByType(noteData.Type));
            noteGO.transform.localPosition = new Vector3(
                                                noteData.Position[0], 
                                                noteData.Position[1], 
                                                noteData.Position[2]
                                            );
            noteGO.transform.rotation =	Quaternion.identity;
            noteGO.transform.parent = m_NotesHolder;
            noteGO.name = noteData.Id;

            // if note has segments we added it
            if(noteData.Segments != null && noteData.Segments.Length > 0) {
                AddNoteSegmentsObject(noteData, noteGO.transform.Find("LineArea"));
            }

            if(noteData.Direction != Note.NoteDirection.None) {
                AddNoteDirectionObject(noteData);
            }

            return noteGO;
        }

        /// <summary>
        /// Instantiate the Segment GameObject on the scene
        /// </summary>
        /// <param name="noteData">The Note from where the data for the instantiation will be read</param>
        void AddNoteSegmentsObject(Note noteData, Transform segmentsParent, bool isRefresh = false) {
            //if(Track.IsOnDebugMode) {
            if(isRefresh) {
                int childsNum = segmentsParent.childCount;
                for(int j = 0; j < childsNum; j++) {
                    GameObject target = segmentsParent.GetChild(j).gameObject;
                    if(!target.name.Equals("Segments")) {
                        DestroyImmediate(target);
                    }						
                }
            }

            for(int i = 0; i < noteData.Segments.GetLength(0); ++i) {
                GameObject noteGO = GameObject.Instantiate(GetNoteMarkerByType(noteData.Type, true));
                noteGO.transform.localPosition = new Vector3(
                    noteData.Segments[i, 0],
                    noteData.Segments[i, 1],
                    noteData.Segments[i, 2]
                );
                noteGO.transform.rotation =	Quaternion.identity;
                noteGO.transform.localScale *= m_NoteSegmentMarkerRedution;
                noteGO.transform.parent = segmentsParent;
                noteGO.name = noteData.Id+"_Segment";
            }
            //}
            
            RenderLine(segmentsParent.gameObject, noteData.Segments, isRefresh);		
        }

        /// <summary>
        /// Instantiate the Direction GameObject on the scene
        /// </summary>
        /// <param name="noteData">The Note from where the data for the instantiation will be read</param>
        void AddNoteDirectionObject(Note noteData) {
            if(noteData.Direction != Note.NoteDirection.None) {					
                GameObject parentGO =  GameObject.Find(noteData.Id);
                GameObject dirGO;
                Transform dirTrans = parentGO.transform.Find("DirectionWrap/DirectionMarker");

                if(dirTrans == null) {
                    dirGO = GameObject.Instantiate(m_DirectionMarker);				
                    Transform parent = parentGO.transform.Find("DirectionWrap");
                    dirGO.transform.parent = parent;
                    dirGO.transform.localPosition = Vector3.zero;
                    dirGO.transform.rotation =	Quaternion.identity;				
                    dirGO.name = "DirectionMarker";
                } else {
                    dirGO = dirTrans.gameObject;
                    dirGO.SetActive(true);
                }

                Quaternion localRot = dirGO.transform.localRotation;
                localRot.eulerAngles = new Vector3(0,0, (int)(noteData.Direction - 1) * m_DirectionNoteAngle);
                dirGO.transform.localRotation = localRot;
            }		
        }

        /// <summary>
        /// Instantiate the Flash GameObject on the scene
        /// </summary>
        /// <param name="ms">The time in with the GameObect will be positioned</param>
        GameObject AddEffectGameObjectToScene(float ms) {
            GameObject effectGO = GameObject.Instantiate(s_instance.m_FlashMarker);
            effectGO.transform.localPosition = new Vector3(
                                                0,
                                                0, 
                                                s_instance.MStoUnit(ms)
                                            );
            effectGO.transform.rotation =	Quaternion.identity;
            effectGO.transform.parent = s_instance.m_NoNotesElementHolder;
            effectGO.name = s_instance.GetEffectIdFormated(ms);

            return effectGO;
        }	

        /// <summary>
        /// Instantiate the Flash GameObject on the scene
        /// </summary>
        /// <param name="ms">The time in with the GameObect will be positioned</param>
        /// <param name="name">The name of the bookmark</param>
        GameObject AddBookmarkGameObjectToScene(float ms, string name) {
            GameObject bookmarkGO = GameObject.Instantiate(s_instance.m_BookmarkElement);
            bookmarkGO.transform.localPosition = new Vector3(
                                                0,
                                                0, 
                                                s_instance.MStoUnit(ms)
                                            );
            bookmarkGO.transform.rotation =	Quaternion.identity;
            bookmarkGO.transform.parent = s_instance.m_NoNotesElementHolder;
            bookmarkGO.name = s_instance.GetBookmarkIdFormated(ms);

            TextMeshPro bookmarkText = bookmarkGO.GetComponentInChildren<TextMeshPro>();
            bookmarkText.SetText(name);
            return bookmarkGO;
        }	

        /// <summary>
        /// Instantiate the Flash GameObject on the scene
        /// </summary>
        /// <param name="ms">The time in with the GameObect will be positioned</param>
        GameObject AddMovementGameObjectToScene(float ms, string MovementTag) {
            GameObject movementToInst;
            switch(MovementTag) {
                case JUMP_TAG:
                    movementToInst = s_instance.m_JumpElement;
                    break;
                case CROUCH_TAG:
                    movementToInst = s_instance.m_CrouchElement;
                    break;
                case SLIDE_CENTER_TAG:
                    movementToInst = s_instance.m_SlideCenterElement;
                    break;
                case SLIDE_LEFT_TAG:
                    movementToInst = s_instance.m_SlideLeftElement;
                    break;
                case SLIDE_RIGHT_TAG:
                    movementToInst = s_instance.m_SlideRightElement;
                    break;
                case SLIDE_LEFT_DIAG_TAG:
                    movementToInst = s_instance.m_SlideDiagLeftElement;
                    break;
                case SLIDE_RIGHT_DIAG_TAG:
                    movementToInst = s_instance.m_SlideDiagRightElement;
                    break;					
                default:
                    movementToInst = s_instance.m_JumpElement;
                    break;
            }

            GameObject moveSectGO = GameObject.Instantiate(movementToInst);
            moveSectGO.transform.localPosition = new Vector3(
                                                0,
                                                0, 
                                                s_instance.MStoUnit(ms)
                                            );
            moveSectGO.transform.rotation =	Quaternion.identity;
            moveSectGO.transform.parent = s_instance.m_NoNotesElementHolder;
            moveSectGO.name = s_instance.GetMovementIdFormated(ms, MovementTag);

            return moveSectGO;
        }	

        /// <summary>
        /// Instantiate the Light GameObject on the scene
        /// </summary>
        /// <param name="ms">The time in with the GameObect will be positioned</param>
        GameObject AddLightGameObjectToScene(float ms) {
            GameObject lightGO = GameObject.Instantiate(s_instance.m_LightMarker);
            lightGO.transform.localPosition = new Vector3(
                                                0,
                                                0, 
                                                s_instance.MStoUnit(ms)
                                            );
            lightGO.transform.rotation =	Quaternion.identity;
            lightGO.transform.parent = s_instance.m_NoNotesElementHolder;
            lightGO.name = s_instance.GetLightIdFormated(ms);

            return lightGO;
        }			

        /// <summary>
        /// Update the <see cref="TotalNotes" /> stat
        /// </summary>
        /// <param name="clear">If true, will reset count to 0</param>
        /// <param name="deleted">If true, the count will be decreased</param>
        void UpdateTotalNotes(bool clear = false, bool deleted = false) {
            if(clear) {
                TotalNotes = 0;
            } else {
                if(deleted) TotalNotes--;
                else TotalNotes++;
            }
            
            m_statsTotalNotesText.SetText(TotalNotes.ToString() + " Notes");
        }

        /// <summary>
        /// Start the functionality to add a longnote
        /// </summary>
        void StartLongNoteMode() {
            /// TODO, show message with mode instructions
            Track.LogMessage("TODO Show help long note");
        }

        /// <summary>
        /// Finalize the LongNote mode functionality and remove any incomplete elements
        /// </summary>
        void FinalizeLongNoteMode() {
            if(isOnLongNoteMode) {
                isOnLongNoteMode = false;
                bool abortLongNote = false;
                
                if(CurrentLongNote.duration <= 0) {
                    // if the line has no segement we just disable the mode
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_LongNoteModeDisabled);
                    abortLongNote = true;
                } else if(CurrentLongNote.duration < MIN_LINE_DURATION || CurrentLongNote.duration > MAX_LINE_DURATION) {
                    // if the line duration is not between the min/max duration				
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                        string.Format(StringVault.Alert_LongNoteLenghtBounds,
                            MIN_LINE_DURATION/MS, MAX_LINE_DURATION/MS
                        ));
                    abortLongNote = true;					
                } else {
                    // Add the long segment to the working track;
                    // first we check if theres is any note in that time period
                    // We need to check the track difficulty selected
                    Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();
                    if(workingTrack != null) {
                        if(!workingTrack.ContainsKey(CurrentLongNote.startTime)) {
                            workingTrack.Add(CurrentLongNote.startTime, new List<Note>());
                        } 

                        if(CurrentLongNote.note.Segments == null) {
                            CurrentLongNote.note.Segments = new float[CurrentLongNote.segments.Count, 3];

                            if(CurrentLongNote.mirroredNote != null) { 
                                CurrentLongNote.mirroredNote.Segments = new float[CurrentLongNote.segments.Count, 3];
                            }
                        }

                        for(int i = 0; i < CurrentLongNote.segments.Count; ++i) {
                            Transform segmentTransform = CurrentLongNote.segments[i].transform;
                            int segmentAxis = CurrentLongNote.segmentAxis[i];
                            CurrentLongNote.note.Segments[i, 0] = segmentTransform.position.x; 
                            CurrentLongNote.note.Segments[i, 1] = segmentTransform.position.y;
                            CurrentLongNote.note.Segments[i, 2] = segmentTransform.position.z;	

                            if(CurrentLongNote.mirroredNote != null) {
                                CurrentLongNote.mirroredNote.Segments[i, 0] = segmentTransform.position.x * -1; 
                                CurrentLongNote.mirroredNote.Segments[i, 1] = segmentTransform.position.y * segmentAxis;
                                CurrentLongNote.mirroredNote.Segments[i, 2] = segmentTransform.position.z;
                            }						
                        }

                        workingTrack[CurrentLongNote.startTime].Add(CurrentLongNote.note);
                        abortLongNote = false;
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_LongNoteModeFinalized);
                        
                        UpdateTotalNotes();
                        RenderLine(CurrentLongNote.gameObject, CurrentLongNote.note.Segments);
                        AddTimeToSFXList(CurrentLongNote.startTime);

                        if(CurrentLongNote.mirroredNote != null) {
                            workingTrack[CurrentLongNote.startTime].Add(CurrentLongNote.mirroredNote);
                            UpdateTotalNotes();
                            RenderLine(CurrentLongNote.mirroredObject, CurrentLongNote.mirroredNote.Segments);
                        }
                        // Uncoment to enable sound on line end
                        // AddTimeToSFXList(CurrentLongNote.lastSegment);
                    } else {
                        abortLongNote = true;
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_LongNoteModeAborted);
                    }					
                }

                if(abortLongNote) {
                    // If aborted, remove the GameObject
                    GameObject.DestroyImmediate(CurrentLongNote.gameObject);
                    if(CurrentLongNote.mirroredObject != null) {
                        GameObject.DestroyImmediate(CurrentLongNote.mirroredObject);
                    }

                    /* if(CurrentLongNote.startTime > 0){
                        _currentTime = CurrentLongNote.startTime;
                        MoveCamera(true, _currentTime);
                    }	*/				
                    
                } /* else {
                    // Otherwise, just remove the segments markers
                    // If debug mode, we let it on for testin purpose
                    if(!Track.IsOnDebugMode) {
                        for(int segm = 0; segm < CurrentLongNote.segments.Count; ++segm) {
                            GameObject.Destroy(CurrentLongNote.segments[segm]);							
                        }
                    }				
                } */

                Track.LogMessage("Note Duration "+CurrentLongNote.duration);
                CurrentLongNote = new LongNote();
                ToggleWorkingStateAlertOff();			
            }
        }

        /// <summary>
        /// Add a Segment to the current longnote
        /// </summary>
        void AddLongNoteSegment(GameObject note) {
            // check if the insert time if less that the start time
            if(_currentTime <= CurrentLongNote.startTime) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteStartPoint);
                return;
            }

            LongNote workingLongNote = CurrentLongNote;
            // if there is not segments initialize the List
            if(workingLongNote.segments == null) {
                workingLongNote.segments = new List<GameObject>();
                workingLongNote.segmentAxis = new List<int>();
            }

            // check if there was a previos segment
            if(workingLongNote.lastSegment > 0) {
                // check if new segment insert larger that the previous segments
                if(_currentTime <= workingLongNote.lastSegment) {
                    if(!IsOnMirrorMode) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteStartSegment);
                    }					
                    return;
                }				
            }

            // starting insert proccess
            // updating duration
            workingLongNote.duration = _currentTime - workingLongNote.startTime;
            // updating the time of the lastSegment
            workingLongNote.lastSegment = _currentTime;
            // add segment object to the scene
            // add the note game object to the screen
            GameObject noteGO = GameObject.Instantiate(GetNoteMarkerByType(workingLongNote.note.Type, true));
            noteGO.transform.localPosition = note.transform.position;
            noteGO.transform.rotation =	Quaternion.identity;
            noteGO.transform.localScale *= m_NoteSegmentMarkerRedution;
            noteGO.transform.parent = workingLongNote.gameObject.transform.Find("LineArea");
            noteGO.name = workingLongNote.note.Id+"_Segment";
            // and finally add the gameObject to the segment list
            workingLongNote.segments.Add(noteGO);
            workingLongNote.segmentAxis.Add(YAxisInverse ? -1 : 1);

            if(isOnMirrorMode) {
                GameObject mirroredNoteGO = GameObject.Instantiate(GetNoteMarkerByType(GetMirroreNoteMarkerType(workingLongNote.note.Type), true));
                mirroredNoteGO.transform.localPosition = GetMirrorePosition(note.transform.position);
                mirroredNoteGO.transform.rotation =	Quaternion.identity;
                mirroredNoteGO.transform.localScale *= m_NoteSegmentMarkerRedution;
                mirroredNoteGO.transform.parent = workingLongNote.mirroredObject.transform.Find("LineArea");
                mirroredNoteGO.name = workingLongNote.mirroredNote.Id+"_Segment";
            }

            CurrentLongNote = workingLongNote;
        }

        /// <summary>
        /// Close the special section if active
        /// </summary>
        void CloseSpecialSection() {
            if(specialSectionStarted) {
                specialSectionStarted = false;

                // If not on LongNote mode whe pront the user of the section end
                if(!isOnLongNoteMode) {
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_SpecialModeFinalized);
                    ToggleWorkingStateAlertOff();
                }
            }
        }

        /// <summary>
        /// Passing the time returns the next close step measure
        /// </summary>
        /// <param name="time">Time in Millesconds</param>
        /// <param name="forward">If true the close measure to return will be on the forward direction, otherwise it will be the close passed meassure</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float GetCloseStepMeasure(float time, bool forward = true) {
            float _CK = ( K*MBPM );
            float closeMeasure = 0;
            if( forward) {
                closeMeasure = time + ( _CK - (time%_CK ) );

                /* if(closeMeasure == time) {
                    closeMeasure = time + _CK;
                }

                if((closeMeasure - time) <= _CK) {
                    closeMeasure = _currentTime + _CK;
                } */
                return closeMeasure;
                // time + ( _CK - (time%_CK ) );
            } else {
                closeMeasure = time - ( time%_CK );

                /* if(closeMeasure == time ) {
                    closeMeasure = time - _CK;
                }

                if((time - closeMeasure) <= _CK ) {
                    closeMeasure = time - _CK;
                } */

                return closeMeasure;
                //time - ( time%_CK );
            }		
        }

        float TimeToMeasure(float time) {
            return ((time*BPM)/(MINUTE*MS)) / 4;
        }

        float MeasureToTime(float measure){
            return ((measure*(MINUTE*MS))/BPM) * 4;
        }

        void RefreshCurrentTime() {
            float timeRangeDuplicatesStart = CurrentTime - MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = CurrentTime + MIN_TIME_OVERLAY_CHECK;
            Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();

            if(workingTrack.Count > 0) {
                List<float> keys_tofilter = workingTrack.Keys.ToList();
                keys_tofilter = keys_tofilter.Where(time => time >= timeRangeDuplicatesStart 
                    && time <= timeRangeDuplicatesEnd).ToList();
            
                if(keys_tofilter.Count > 0) {
                    CurrentTime = keys_tofilter[0];
                    return;
                }
            }
            

            List<float> workingEffects = GetCurrentEffectDifficulty();
            if(workingEffects.Count > 0) {
                List<float> effects_tofilter;
                effects_tofilter = workingEffects.Where(time => time >= timeRangeDuplicatesStart 
                        && time <= timeRangeDuplicatesEnd).ToList();
                
                if(effects_tofilter.Count > 0) {
                    CurrentTime = effects_tofilter[0];
                    return;
                }
            }
            

            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            if(jumps.Count > 0) {
                List<float> jumps_tofilter;
                jumps_tofilter = jumps.Where(time => time >= timeRangeDuplicatesStart 
                        && time <= timeRangeDuplicatesEnd).ToList();

                if(jumps_tofilter.Count > 0) {
                    CurrentTime = jumps_tofilter[0];
                    return;
                }
            }

            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            if(crouchs.Count > 0) {
                List<float> crouchs_tofilter;
                crouchs_tofilter = crouchs.Where(time => time >= (timeRangeDuplicatesStart + 3) 
                        && time <= (timeRangeDuplicatesEnd + 3)).ToList();
                
                if(crouchs_tofilter.Count > 0) {
                    CurrentTime = crouchs_tofilter[0];
                    return;
                }
            }
            
            
            List<Slide> slides = GetCurrentMovementListByDifficulty();
            if(slides.Count > 0) {
                List<Slide> slides_tofilter;
                slides_tofilter = slides.Where(s => s.time >= (timeRangeDuplicatesStart + 3) 
                        && s.time <= (timeRangeDuplicatesEnd + 3)).ToList();

                if(slides_tofilter.Count > 0) {
                    CurrentTime = slides_tofilter[0].time;
                    return;
                }
            }
        }

        void HighlightNotes() {
            RefreshCurrentTime();

            Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();
            if(workingTrack.ContainsKey(CurrentTime)) {
                List<Note> notes = workingTrack[CurrentTime];
                int totalNotes = notes.Count;
                
                for(int i = 0; i < totalNotes; ++i) {
                    Note toHighlight= notes[i];

                    GameObject highlighter = GetHighlighter(toHighlight.Id);
                    if(highlighter) {
                        highlighter.SetActive(true);
                    }
                }
            }
        }

        GameObject GetHighlighter(string parendId) {
            try {
                GameObject highlighter = GameObject.Find(parendId);
                if(highlighter) {
                    return highlighter.transform.GetChild(highlighter.transform.childCount - 1).gameObject;
                }
            } catch {
                return null;
            }			

            return null;
        }

        void ToggleNoteDirectionMarker(Note.NoteDirection direction) {
            if(DirectionalNotesEnabled) {
                isBusy = true;

                Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();

                float timeRangeDuplicatesStart = CurrentTime - MIN_TIME_OVERLAY_CHECK;
                float timeRangeDuplicatesEnd = CurrentTime + MIN_TIME_OVERLAY_CHECK;
                List<float> keys_tofilter = workingTrack.Keys.ToList();
                keys_tofilter = keys_tofilter.Where(time => time >= timeRangeDuplicatesStart 
                            && time <= timeRangeDuplicatesEnd).ToList();

                if(keys_tofilter.Count > 0) {
                    int totalFilteredTime = keys_tofilter.Count;

                    for(int filterList = 0; filterList < totalFilteredTime; ++filterList) {
                        // If the time key exist, check how many notes are added
                        float targetTime = keys_tofilter[filterList];
                        //print(targetTime+" "+CurrentTime);
                        List<Note> notes = workingTrack[targetTime];
                        int totalNotes = notes.Count;

                        for(int i = 0; i < totalNotes; ++i) {
                            Note n = notes[i];
                            if(isALTDown && n.Type != Note.NoteType.LeftHanded) { 
                                continue; 
                            }

                            if(!isALTDown && n.Type == Note.NoteType.LeftHanded) {
                                continue;
                            }
                            
                            n.Direction = direction;
                            AddNoteDirectionObject(notes[i]);
                        }
                    }

                }

                isBusy = false;
            }			
        }

        /// <summary>
        /// Remove the notes on the current time
        /// </summary>
        void DeleteNotesAtTheCurrentTime() {
            isBusy = true;

            Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();
            List<float> workingEffects = GetCurrentEffectDifficulty();
            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            List<Slide> slides = GetCurrentMovementListByDifficulty();
            List<float> lights = GetCurrentLightsByDifficulty();
            GameObject targetToDelete;
            float lookUpTime;

            List<float> keys_tofilter = workingTrack.Keys.ToList();
            List<float> effects_tofilter, jumps_tofilter, crouchs_tofilter, lights_tofilter;
            List<Slide> slides_tofilter;		


            if(CurrentSelection.endTime > CurrentSelection.startTime) {				
                keys_tofilter = keys_tofilter.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                effects_tofilter = workingEffects.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                jumps_tofilter = jumps.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();

                crouchs_tofilter = crouchs.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();
                
                slides_tofilter = slides.Where(s => s.time >= CurrentSelection.startTime 
                    && s.time <= CurrentSelection.endTime).ToList();

                lights_tofilter = lights.Where(time => time >= CurrentSelection.startTime 
                    && time <= CurrentSelection.endTime).ToList();
                
            } else {
                RefreshCurrentTime();

                keys_tofilter = keys_tofilter.Where(time => time == CurrentTime).ToList();

                effects_tofilter = workingEffects.Where(time => time == CurrentTime).ToList();

                jumps_tofilter = jumps.Where(time => time == CurrentTime).ToList();

                crouchs_tofilter = crouchs.Where(time => time == CurrentTime).ToList();

                slides_tofilter = slides.Where(s => s.time == CurrentTime).ToList();

                lights_tofilter = lights.Where(time => time == CurrentTime).ToList();
            }

            for(int j = 0; j < keys_tofilter.Count; ++j) {
                lookUpTime = keys_tofilter[j];

                if(workingTrack.ContainsKey(lookUpTime)) {
                    // If the time key exist, check how many notes are added
                    List<Note> notes = workingTrack[lookUpTime];
                    int totalNotes = notes.Count;
                    
                    for(int i = 0; i < totalNotes; ++i) {
                        Note toRemove = notes[i];

                        targetToDelete = GameObject.Find(toRemove.Id);
                        // print(targetToDelete);
                        if(targetToDelete) {
                            DestroyImmediate(targetToDelete);
                        }
                        
                        s_instance.UpdateTotalNotes(false, true);
                    }

                    notes.Clear();
                    workingTrack.Remove(lookUpTime);
                    hitSFXSource.Remove(lookUpTime);
                }				
            }	

            for(int j = 0; j < effects_tofilter.Count; ++j) {
                lookUpTime = effects_tofilter[j];

                if(workingEffects.Contains(lookUpTime)) {					
                    workingEffects.Remove(lookUpTime);
                    targetToDelete = GameObject.Find(GetEffectIdFormated(lookUpTime));
                    if(targetToDelete) {
                        DestroyImmediate(targetToDelete);
                    }
                }
            }

            for(int j = 0; j < jumps_tofilter.Count; ++j) {
                lookUpTime = jumps_tofilter[j];

                if(jumps.Contains(lookUpTime)) {	
                    RemoveMovementFromList(jumps, lookUpTime, JUMP_TAG);
                    /* jumps.Remove(lookUpTime);
                    targetToDelete = GameObject.Find(GetMovementIdFormated(lookUpTime, JUMP_TAG));
                    if(targetToDelete) {
                        Destroy(targetToDelete);
                    } */
                }
            }

            for(int j = 0; j < crouchs_tofilter.Count; ++j) {
                lookUpTime = crouchs_tofilter[j];

                if(crouchs.Contains(lookUpTime)) {	
                    RemoveMovementFromList(crouchs, lookUpTime, CROUCH_TAG);
                    /* crouchs.Remove(lookUpTime);
                    targetToDelete = GameObject.Find(GetMovementIdFormated(lookUpTime, CROUCH_TAG));
                    if(targetToDelete) {
                        Destroy(targetToDelete);
                    } */
                }
            }

            for(int j = 0; j < slides_tofilter.Count; ++j) {
                Slide currSlide = slides_tofilter[j];
                
                if(slides.Contains(currSlide)) {
                    lookUpTime = currSlide.time;
                    RemoveMovementFromList(slides, lookUpTime, GetSlideTagByType(currSlide.slideType));
                    /* slides.Remove(currSlide);
                    targetToDelete = GameObject.Find(GetMovementIdFormated(lookUpTime, GetSlideTagByType(currSlide.slideType)));
                    if(targetToDelete) {
                        Destroy(targetToDelete);
                    } */
                }
            }

            for(int j = 0; j < lights_tofilter.Count; ++j) {
                lookUpTime = lights_tofilter[j];

                if(lights.Contains(lookUpTime)) {					
                    lights.Remove(lookUpTime);
                    targetToDelete = GameObject.Find(GetLightIdFormated(lookUpTime));
                    if(targetToDelete) {
                        DestroyImmediate(targetToDelete);
                    }
                }
            }

            // LogMessage(keys_tofilter.Count+" Keys deleted");
            keys_tofilter.Clear();
            effects_tofilter.Clear();
            jumps_tofilter.Clear();
            crouchs_tofilter.Clear();
            slides_tofilter.Clear();
            lights_tofilter.Clear();
            ClearSelectionMarker();
            isBusy = false;	
        }

        /// <summary>
        /// Render the line passing the segments
        /// </summary>
        /// <param name="segments">The segements for where the line will pass</param>
        void RenderLine(GameObject noteGameObject, float[,] segments, bool refresh = false) {
            Game_LineWaveCustom waveCustom = noteGameObject.GetComponentInChildren<Game_LineWaveCustom>();
            waveCustom.targetOptional = segments;
            waveCustom.RenderLine(refresh);
        }

        /// <summary>
        /// Update the type of note the middle button can select
        /// </summary>
        void UpdateMiddleButtonSelector() {
            MiddleButtonSelectorType += 1;
            
            if(MiddleButtonSelectorType >2) {
                MiddleButtonSelectorType = 0;
            }

            string _noteType = "Normal Type";
            if(MiddleButtonSelectorType == 0) {
                _noteType = "Normal Type";
            } else if(MiddleButtonSelectorType == 1) { 
                _noteType = "Special Type";
            } else { 
                _noteType = "All";
            }

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                string.Format(StringVault.Info_MiddleButtonType, _noteType)
            );
        }

        /// <summary>
        /// Toggle the autosave function
        /// </summary>
        void UpdateAutoSaveAction() {
            canAutoSave = !canAutoSave;
            lastSaveTime = 0;

            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                string.Format(
                    StringVault.Info_AutoSaveFunction,
                    (canAutoSave) ? "On" : "Off"
                )
            );
        }

        /// <summary>
        /// Toggle the autosave function
        /// </summary>
        void ToggleMirrorMode() {
            isOnMirrorMode = !isOnMirrorMode;
            if(isOnMirrorMode) {
                ToggleWorkingStateAlertOn(string.Format(
                    StringVault.Info_MirroredMode,
                    ""
                ));

                if(selectedNoteType == Note.NoteType.OneHandSpecial || selectedNoteType == Note.NoteType.BothHandsSpecial){
                    SetNoteMarkerType(0);
                }
            } else {
                ToggleWorkingStateAlertOff();
                FinalizeLongNoteMode();
            }

            notesArea.RefreshSelectedObjec();
        }

        void ToggleGripSnapping() {
            notesArea.SnapToGrip = !notesArea.SnapToGrip;
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                string.Format(
                    StringVault.Info_GridSnapp,
                    (notesArea.SnapToGrip) ? "On" : "Off"
                )
            );
        }		

#region Statics Methods
        /// <summary>
        /// Display the passed <paramref  name="message" /> on the console
        /// </summary>
        /// <param name="message">The message to show</param>
        /// <param name="logError">If true the message will be showed as a LogError</param>
        public static void LogMessage(string message, bool logError = false) {
            if(!s_instance || !IsOnDebugMode) return;

            if(Application.isEditor) {
                if(logError) {
                    Debug.LogError(message);
                    return;
                }

                Debug.Log(message);
            }	

            if(logError) {
                Serializer.WriteToLogFile("there was a error...");
                Serializer.WriteToLogFile(message);
            }		
        }

        /// <summary>
        /// Get the <typeparamref name="GameObject"/> instance for the normal note to place
        /// </summary>
        /// <returns>Returns <typeparamref name="GameObject"/></returns>
        public static GameObject GetSelectedNoteMarker() {
            return GameObject.Instantiate(s_instance.GetNoteMarkerByType(s_instance.selectedNoteType), Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Get the <typeparamref name="GameObject"/> instance for the mirrored normal note to place
        /// </summary>
        /// <returns>Returns <typeparamref name="GameObject"/></returns>
        public static GameObject GetMirroredNoteMarker() {
            Note.NoteType targedMirrored =  s_instance.selectedNoteType == Note.NoteType.LeftHanded ? Note.NoteType.RightHanded : Note.NoteType.LeftHanded;
            return GameObject.Instantiate(s_instance.GetNoteMarkerByType(targedMirrored), Vector3.zero, Quaternion.identity);
        }

        public static Note.NoteType GetMirroreNoteMarkerType(Note.NoteType tocheck) {
            return tocheck == Note.NoteType.LeftHanded ? Note.NoteType.RightHanded : Note.NoteType.LeftHanded;
        }

        public static Vector3 GetMirrorePosition(Vector3 targetpPos) {
            if(Track.IsOnMirrorMode) {
                Vector3 mirroredPosition = targetpPos;

                if(Track.XAxisInverse) {
                    mirroredPosition.x *= -1;
                }

                if(Track.YAxisInverse) {
                    mirroredPosition.y *= -1;
                }
                
                return mirroredPosition;
            }

            return targetpPos;
        }

        /// <summary>
        /// Add note to chart
        /// </summary>
        public static void AddNoteToChart(GameObject note) {
            if(PromtWindowOpen || s_instance.isBusy) return;

            if(CurrentTime < MIN_NOTE_START * MS) {
                Miku_DialogManager.ShowDialog(
                    Miku_DialogManager.DialogType.Alert, 
                    string.Format(
                        StringVault.Info_NoteTooClose,
                        MIN_NOTE_START
                    )
                );

                return;
            }

            // first we check if theres is any note in that time period
            // We need to check the track difficulty selected
            Dictionary<float, List<Note>> workingTrack = s_instance.GetCurrentTrackDifficulty();
            if(workingTrack != null) {
                if(s_instance.isOnLongNoteMode && s_instance.CurrentLongNote.gameObject != null) {
                    if(CurrentTime == s_instance.CurrentLongNote.startTime) {
                        if(!IsOnMirrorMode) {
                            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteNotFinalized);
                        }						
                        return;
                    } else {
                        s_instance.AddLongNoteSegment(note);
                        return;
                    }
                }

                float timeRangeDuplicatesStart = CurrentTime - MIN_TIME_OVERLAY_CHECK;
                float timeRangeDuplicatesEnd = CurrentTime + MIN_TIME_OVERLAY_CHECK;
                List<float> keys_tofilter = workingTrack.Keys.ToList();
                keys_tofilter = keys_tofilter.Where(time => time >= timeRangeDuplicatesStart 
                        && time <= timeRangeDuplicatesEnd).ToList();
                
                //if(workingTrack.ContainsKey(CurrentTime)) {
                if(keys_tofilter.Count > 0) {

                    int totalFilteredTime = keys_tofilter.Count;
                    for(int filterList = 0; filterList < totalFilteredTime; ++filterList) {
                        // If the time key exist, check how many notes are added
                        float targetTime = keys_tofilter[filterList];
                        //print(targetTime+" "+CurrentTime);
                        List<Note> notes = workingTrack[targetTime];
                        int totalNotes = notes.Count;

                        // Check for overlaping notes and delete if close
                        for(int i = 0; i < totalNotes; ++i) {
                            Note overlap = notes[i];

                            if(ArePositionsOverlaping(note.transform.position, 
                                new Vector3(overlap.Position[0],
                                    overlap.Position[1],
                                    overlap.Position[2]
                                ))) 
                            {
                                GameObject nToDelete = GameObject.Find(overlap.Id);
                                if(nToDelete) {
                                    DestroyImmediate(nToDelete);
                                }

                                notes.Remove(overlap);
                                totalNotes--;
                                s_instance.UpdateTotalNotes(false, true);

                                if(totalNotes <= 0) {
                                    workingTrack.Remove(targetTime);
                                    s_instance.hitSFXSource.Remove(targetTime);
                                } else {
                                    overlap = notes[0];
                                    if(overlap.Type == Note.NoteType.OneHandSpecial) {
                                        nToDelete = GameObject.Find(overlap.Id);
                                        overlap.Id = FormatNoteName(targetTime, 0, overlap.Type);
                                        nToDelete.name = overlap.Id;
                                    }								
                                }
                                return;
                            }
                        }

                        if(totalNotes > 0) {
                            CurrentTime = targetTime;
                        }

                        // if count is MAX_ALLOWED_NOTES then return because not more notes are allowed
                        if(totalNotes >= MAX_ALLOWED_NOTES) {
                            //Track.LogMessage("Max number of notes reached");
                            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_MaxNumberOfNotes);
                            return;
                        } else {
                            // Both hand notes only allowed 1 total
                            // RightHanded/Left Handed notes only allowed 1 of their types
                            Note specialsNotes = notes.Find(x => x.Type == Note.NoteType.BothHandsSpecial || x.Type == Note.NoteType.OneHandSpecial);
                            if(specialsNotes != null || ((s_instance.selectedNoteType == Note.NoteType.BothHandsSpecial || s_instance.selectedNoteType == Note.NoteType.OneHandSpecial )
                                                                && totalNotes >= MAX_SPECIAL_NOTES)) {
                                //Track.LogMessage("Max number of both hands notes reached");
                                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_MaxNumberOfSpecialNotes);
                                return;
                            } else {
                                //if(s_instance.selectedNoteType != Note.NoteType.OneHandSpecial) {
                                    specialsNotes = notes.Find(x => x.Type == s_instance.selectedNoteType);
                                    if(specialsNotes != null) {
                                        //Track.LogMessage("Max number of "+s_instance.selectedNoteType.ToString()+" notes reached");
                                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, string.Format(StringVault.Alert_MaxNumberOfTypeNotes, s_instance.selectedNoteType.ToString()));
                                        return;
                                    }
                                //}							
                            }									
                        }
                    }					
                } else {
                    if(!s_instance.isOnLongNoteMode) {
                        // If the entry time does not exist we just added
                        workingTrack.Add(CurrentTime, new List<Note>());

                        s_instance.AddTimeToSFXList(CurrentTime);
                    }					
                }

                // workingTrack[CurrentTime].Count

                Note n = new Note(note.transform.position, FormatNoteName(CurrentTime, s_instance.TotalNotes + 1, s_instance.selectedNoteType));
                n.Type = s_instance.selectedNoteType;

                // If is not on long note mode we add the note as usual
                if(!s_instance.isOnLongNoteMode) {					

                    // Check if the note placed if of special type 
                    if(IsOfSpecialType(n)) {
                        // If whe are no creating a special, Then we init the new special section
                        if(!s_instance.specialSectionStarted) {
                            s_instance.specialSectionStarted = true;
                            s_instance.currentSpecialSectionID++;
                            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_SpecialModeStarted);
                            s_instance.ToggleWorkingStateAlertOn(StringVault.Info_UserOnSpecialSection);
                        }

                        // Assing the Special ID to the note
                        n.ComboId = s_instance.currentSpecialSectionID;		
                        
                        Track.LogMessage("Current Special ID: "+s_instance.currentSpecialSectionID);
                    }
                    
                    // Finally we added the note to the dictonary
                    // ref of the note for easy of access to properties						
                    if(workingTrack.ContainsKey(CurrentTime)) {
                        // print("Trying currentTime "+CurrentTime);
                        workingTrack[CurrentTime].Add(n);
                        s_instance.AddNoteGameObjectToScene(n);
                        s_instance.UpdateTotalNotes();	
                    } else {
                        Track.LogMessage("Time does not exist");
                    }								
                } else {
                    if(s_instance.CurrentLongNote.note == null) {
                        // Other wise, init the strut and beign the inserting of the LongNote mode
                        LongNote longNote = s_instance.CurrentLongNote;
                        longNote.startTime = CurrentTime;
                        longNote.note = n;
                        longNote.gameObject = s_instance.AddNoteGameObjectToScene(n);
                        if(IsOnMirrorMode) {
                            Note mirroredN = new Note(GetMirrorePosition(note.transform.position), FormatNoteName(CurrentTime, s_instance.TotalNotes + 2, GetMirroreNoteMarkerType(n.Type)));
                            mirroredN.Type = GetMirroreNoteMarkerType(n.Type);
                            longNote.mirroredNote = mirroredN;
                            longNote.mirroredObject = s_instance.AddNoteGameObjectToScene(mirroredN);
                        }
                        longNote.lastSegment = 0;
                        longNote.duration = 0;
                        s_instance.CurrentLongNote = longNote;
                        s_instance.StartLongNoteMode();
                    }					
                }
            }
        }

        
        public static void AddNoteToChart(GameObject[] notes) { 
            Note.NoteType defaultType = s_instance.selectedNoteType;

            int totalNotes = notes.Length;
            for(int i = 0; i < totalNotes; ++i) {
                GameObject nextNote = notes[i];

                if(i > 0) {
                    s_instance.selectedNoteType = GetMirroreNoteMarkerType(s_instance.selectedNoteType);
                }

                AddNoteToChart(nextNote);
            }

            s_instance.selectedNoteType = defaultType;
        }
        /// <summary>
        /// Add to hitEffectsSource list to manage the play of sfx
        /// </summary>
        /// <param name="_ms">Millesconds of the current position to use on the formating</param>
        private void AddTimeToSFXList(float _ms)
        {           
            if(!s_instance.hitSFXSource.Contains(_ms)) {
                s_instance.hitSFXSource.Add(_ms);
            }
        }

        /// <summary>
        /// Return a string formated to be use as the note id
        /// </summary>
        /// <param name="_ms">Millesconds of the current position to use on the formating</param>
        /// <param name="index">Index of the note to use on the formating</param>	
        /// <param name="noteType">The type of note to look for, default is <see cref="Note.NoteType.RightHanded" /></param>
        public static string FormatNoteName(float _ms, int index, Note.NoteType noteType = Note.NoteType.RightHanded) {
            return (_ms.ToString("R")+noteType.ToString()+index).ToString();
        }

        /// <summary>
        /// Add the passed Note GameObject to the <see cref="disabledNotes" /> list of disabled objects
        /// also disable the GameObject after added
        /// </summary>
        /// <param name="note">The GameObject to add to the list</summary>
        /// <param name="playBeatSound">If false not sound effect will be played</summary>
        public static void AddNoteToDisabledList(GameObject note, bool playBeatSound = true) {
            s_instance.disabledNotes.Add(note);
            note.SetActive(false);	
            Transform directionWrap = note.transform.parent.Find("DirectionWrap");
            if(directionWrap != null) {
                directionWrap.gameObject.SetActive(false);
            }		
            /* if(s_instance.lastHitNoteZ != note.transform.position.z && playBeatSound) {
                s_instance.lastHitNoteZ = note.transform.position.z;
                s_instance.PlaySFX(s_instance.m_HitMetaSound);
            } */
            
        }		

        /// <summary>
        /// Add the passed Note GameObject to the <see cref="resizedNotes" /> list of resized objects
        /// also resize the GameObject after added
        /// </summary>
        /// <param name="note">The GameObject to add to the list</summary>
        public static void AddNoteToReduceList(GameObject note, bool turnOff = false) {
            // || s_instance.MBPMIncreaseFactor == 1
            // || s_instance.isOnLongNoteMode 
            if(Track.IsPlaying || note == null) return;
            //string searchName = note.name.Equals("Lefthand Single Note") || note.name.Equals("Righthand Single Note") || turnOff ? note.transform.parent.name : note.name;
            string searchName = note.transform.parent.name;
            
            int index = s_instance.resizedNotes.FindIndex(x => x != null && (x.name.Equals(searchName) || x.transform.parent.name.Equals(searchName)));
            if(index < 0) {				
                s_instance.resizedNotes.Add(note);
                Transform directionWrap = note.transform.parent.Find("DirectionWrap");
                if(directionWrap != null) {
                    directionWrap.gameObject.SetActive(false);
                }
                
                if(turnOff) {
                    note.GetComponent<MeshRenderer>().enabled = false;
                    /* GameObject highlighter = s_instance.GetHighlighter(searchName);
                    if(highlighter) {
                        highlighter.SetActive(false);
                    } */
                } else {
                    if(note.transform.localScale.x > MIN_NOTE_RESIZE) {
                        note.transform.localScale = note.transform.localScale * s_instance.m_CameraNearReductionFactor;
                    }	
                }							
            }			
        }

        /// <summary>
        /// Remove the passed Note GameObject from the <see cref="resizedNotes" /> list of resized objects
        /// also resize the GameObject after added
        /// </summary>
        /// <param name="note">The GameObject to add to the list</summary>
        public static void RemoveNoteToReduceList(GameObject note,  bool turnOn = false) {
            if(Track.IsPlaying || note == null) return;
            // string searchName = note.name.Equals("Lefthand Single Note") || note.name.Equals("Righthand Single Note") || turnOn ? note.transform.parent.name : note.name;
            string searchName = note.transform.parent.name;
            
            int index = s_instance.resizedNotes.FindIndex(x => x != null && (x.name.Equals(searchName) || x.transform.parent.name.Equals(searchName)));
            if(index >= 0) {
                s_instance.resizedNotes.RemoveAt(index);
                Transform directionWrap = note.transform.parent.Find("DirectionWrap");
                if(directionWrap != null) {
                    directionWrap.gameObject.SetActive(true);
                }
                if(turnOn) {
                    note.GetComponent<MeshRenderer>().enabled = true;
                } else {
                    if(note.transform.localScale.x < MAX_NOTE_RESIZE) {
                        note.transform.localScale = note.transform.localScale / s_instance.m_CameraNearReductionFactor;	
                    }
                }							
            }
        }

        /// <summary>
        /// Check if the note if out of the Grid Boundaries and Update its position
        /// </summary>
        /// <param name="note">The note object to check</param>
        public static void MoveToGridBoundaries(Note note) {
            // Clamp between Horizontal Boundaries
            note.Position[0] = Mathf.Clamp(note.Position[0], LEFT_GRID_BOUNDARY, RIGHT_GRID_BOUNDARY);

            // Camp between Veritcal Boundaries
            note.Position[1] = Mathf.Clamp(note.Position[1], BOTTOM_GRID_BOUNDARY, TOP_GRID_BOUNDARY);
        }

        /// <summary>
        /// Check if the note if of special type, and update the combo id info
        /// </summary>
        /// <param name="note">The note object to check</param>
        public static void AddComboIdToNote(Note note) {
            // Check if the note placed if of special type 
            if(IsOfSpecialType(note)) {
                // If whe are no creating a special, Then we init the new special section
                if(!s_instance.specialSectionStarted) {
                    s_instance.specialSectionStarted = true;
                    s_instance.currentSpecialSectionID++;
                }

                // Assing the Special ID to the note
                note.ComboId = s_instance.currentSpecialSectionID;						
            } else {
                s_instance.specialSectionStarted = false;
            }
        }

        /// <summary>
        /// Check if the note if out of the Grid Boundaries and Update its position
        /// </summary>
        /// <param name="note">The note object to check</param>
        /// <param name="boundaries">The boudanries to clamp the note position to. x = left, y = right; z = top, w = bottom</param>
        public static void MoveToGridBoundaries(Note note, Vector4 boundaries) {
            // Clamp between Horizontal Boundaries
            note.Position[0] = Mathf.Clamp(note.Position[0], boundaries.x, boundaries.y);

            // Camp between Veritcal Boundaries
            note.Position[1] = Mathf.Clamp(note.Position[1], boundaries.z, boundaries.w);
        }

        /// <summary>
        /// Check if two position are overlapin
        /// </summary>
        /// <param name="pos1"><see cref="Vector3"/> to check</param>
        /// <param name="pos2"><see cref="Vector3"/> to check</param>
        /// <param name="minDistance">Overwrite the <see cref="MIN_OVERLAP_DISTANCE"/> constant</param>
        /// <returns>Returns <typeparamref name="bool"/></returns>
        public static bool ArePositionsOverlaping(Vector3 pos1, Vector3 pos2, float minDistance = 0) {
            float dist = Vector3.Distance(pos1, pos2);
            minDistance = (minDistance == 0) ? MIN_OVERLAP_DISTANCE : minDistance;

            if(Mathf.Abs(dist) < minDistance) {				
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the passed note if of Special Type: <see cref="Note.NoteType.BothHandsSpecial" /> or <see cref="Note.NoteType.OneHandSpecial" />
        /// </summary>
        /// <param name="n"><see cref="Note"/> to check</param>
        /// <returns>Returns <typeparamref name="bool"/></returns>
        public static bool IsOfSpecialType(Note n) {
            if(n.Type == Note.NoteType.OneHandSpecial || n.Type == Note.NoteType.BothHandsSpecial) { 
                return true;
            }

            return false;
        }

        /// <summary>
        /// Move the camera to the closed measure of the passed time value
        /// </summary>
        public static void JumpToTime(float time) {
            time = Mathf.Min(time, s_instance.TrackDuration * MS);
            s_instance._currentTime = s_instance.GetCloseStepMeasure(time, false);
            s_instance.MoveCamera(true, s_instance.MStoUnit(s_instance._currentTime));
            if(PromtWindowOpen) {
                s_instance.ClosePromtWindow();
            }			
            s_instance.DrawTrackXSLines();
            s_instance.ResetResizedList();
            s_instance.ResetDisabledList();
        }

        /// <summary>
        /// Toggle Effects for the current time
        /// </summary>
        public static void ToggleEffectToChart(bool isOverwrite = false){
            if(PromtWindowOpen || IsPlaying) return;

            if(s_instance.isOnLongNoteMode && s_instance.CurrentLongNote.gameObject != null) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteNotFinalizedEffect);
                return;
            }

            // first we check if theres is any effect in that time period
            // We need to check the effect difficulty selected
            List<float> workingEffects = s_instance.GetCurrentEffectDifficulty();
            if(workingEffects != null) {
                if(workingEffects.Contains(CurrentTime)) {
                    workingEffects.Remove(CurrentTime);
                    GameObject effectGO = GameObject.Find(s_instance.GetEffectIdFormated(CurrentTime));
                    if(effectGO != null) {
                        DestroyImmediate(effectGO);
                    }

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_FlashOff);
                    }
                } else {
                    if(workingEffects.Count >= MAX_FLASH_ALLOWED) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert,
                            string.Format(StringVault.Alert_MaxNumberOfEffects, MAX_FLASH_ALLOWED));
                        return;
                    }
                    
                    for(int i = 0; i < workingEffects.Count; ++i) {

                        if(IsWithin(workingEffects[i], CurrentTime - MIN_FLASH_INTERVAL, CurrentTime + MIN_FLASH_INTERVAL)) {
                            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert,
                                string.Format(StringVault.Alert_EffectsInterval, (MIN_FLASH_INTERVAL/MS)));
                            return;
                        }
                    }
                    workingEffects.Add(CurrentTime);	
                    s_instance.AddEffectGameObjectToScene(CurrentTime);

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_FlashOn);
                    }			
                }				
            }
        }

        /// <summary>
        /// Toggle Bookmark for the current time
        /// </summary>
        public static void ToggleBookmarkToChart(){
            if(PromtWindowOpen || s_instance.isBusy || IsPlaying) return;

            if(s_instance.isOnLongNoteMode && s_instance.CurrentLongNote.gameObject != null) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteNotFinalizedBookmark);
                return;
            }			

            // first we check if theres is any effect in that time period
            // We need to check the effect difficulty selected
            List<Bookmark> workingBookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(workingBookmarks != null) {
                Bookmark currentBookmark = workingBookmarks.Find(x => x.time >= CurrentTime - MIN_TIME_OVERLAY_CHECK
                    && x.time <= CurrentTime + MIN_TIME_OVERLAY_CHECK);
                if(currentBookmark.time >= 0 && currentBookmark.name != null) {
                    workingBookmarks.Remove(currentBookmark);
                    GameObject bookmarkGO = GameObject.Find(s_instance.GetBookmarkIdFormated(CurrentTime));
                    if(bookmarkGO != null) {
                        DestroyImmediate(bookmarkGO);
                    }

                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, StringVault.Info_BookmarkOff);
                } else {
                    s_instance.currentPromt = PromtType.AddBookmarkAction;
                    s_instance.ShowPromtWindow(string.Empty);
                }				
            }
        }

        /// <summary>
        /// Toggle Jump for the current time
        /// </summary>
        public static void ToggleMovementSectionToChart(string MoveTAG, bool isOverwrite = false) {
            if(PromtWindowOpen || IsPlaying) return;

            if(CurrentTime < MIN_NOTE_START * MS) {
                Miku_DialogManager.ShowDialog(
                    Miku_DialogManager.DialogType.Alert, 
                    string.Format(
                        StringVault.Info_NoteTooClose,
                        MIN_NOTE_START
                    )
                );

                return;
            }

            if(s_instance.isOnLongNoteMode && s_instance.CurrentLongNote.gameObject != null) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteNotFinalizedEffect);
                return;
            }

            s_instance.RefreshCurrentTime();


            GameObject moveGO = null;
            string offText;
            string onText;
            List<float> workingElementVert = null;
            List<Slide> workingElementHorz = null;
            switch(MoveTAG) {
                case JUMP_TAG:
                    offText = StringVault.Info_JumpOff;
                    onText = StringVault.Info_JumpOn;
                    workingElementVert = s_instance.GetCurrentMovementListByDifficulty(true);
                    break;
                case CROUCH_TAG:
                    offText = StringVault.Info_CrouchOff;
                    onText = StringVault.Info_CrouchOn;
                    workingElementVert = s_instance.GetCurrentMovementListByDifficulty(false);
                    break;
                case SLIDE_CENTER_TAG:
                case SLIDE_LEFT_TAG:
                case SLIDE_RIGHT_TAG:
                case SLIDE_RIGHT_DIAG_TAG:
                case SLIDE_LEFT_DIAG_TAG:
                    offText = StringVault.Info_SlideOff;
                    onText = StringVault.Info_SlideOn;
                    workingElementHorz = s_instance.GetCurrentMovementListByDifficulty();
                    break;
                default:
                    offText = StringVault.Info_JumpOff;
                    onText = StringVault.Info_JumpOn;
                    workingElementVert = s_instance.GetCurrentMovementListByDifficulty(true);
                    break;
            }

            // first we check if theres is any effect in that time period
            // We need to check the effect difficulty selected
            if(workingElementVert != null) {
                if(workingElementVert.Contains(CurrentTime)) {					
                    workingElementVert.Remove(CurrentTime);
                    moveGO = GameObject.Find(s_instance.GetMovementIdFormated(CurrentTime, MoveTAG));
                    if(moveGO != null) {
                        DestroyImmediate(moveGO);
                    }

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, offText);
                    }
                } else {

                    s_instance.RemoveMovementSectionFromChart(MoveTAG, CurrentTime);

                    workingElementVert.Add(CurrentTime);	
                    s_instance.AddMovementGameObjectToScene(CurrentTime, MoveTAG);

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, onText);			
                    }	
                }				
            }

            if(workingElementHorz != null) {
                Slide currentSlide = workingElementHorz.Find(x => x.time == CurrentTime);
                string CurrentTag = String.Empty;
                if(currentSlide.initialized) {
                    CurrentTag = s_instance.GetSlideTagByType(currentSlide.slideType);
                    //if(!isOverwrite) {									
                    workingElementHorz.Remove(currentSlide);
                    moveGO = GameObject.Find(s_instance.GetMovementIdFormated(CurrentTime, CurrentTag));
                    if(moveGO != null) {
                        DestroyImmediate(moveGO);
                    }			
                    //}		
                } 

                if(CurrentTag.Equals(MoveTAG)) {
                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, offText);
                    } 
                } else {
                    s_instance.RemoveMovementSectionFromChart(MoveTAG, CurrentTime);

                    Slide slide = new Slide();
                    slide.time = CurrentTime;
                    slide.initialized = true;

                    switch(MoveTAG) {
                        case SLIDE_LEFT_TAG:
                            slide.slideType = Note.NoteType.LeftHanded;
                            break;
                        case SLIDE_RIGHT_TAG:
                            slide.slideType = Note.NoteType.RightHanded;
                            break;
                        case SLIDE_LEFT_DIAG_TAG:
                            slide.slideType = Note.NoteType.SeparateHandSpecial;
                            break;
                        case SLIDE_RIGHT_DIAG_TAG:
                            slide.slideType = Note.NoteType.OneHandSpecial;
                            break;
                        default:
                            slide.slideType = Note.NoteType.BothHandsSpecial;
                            break;
                    }

                    workingElementHorz.Add(slide);	
                    s_instance.AddMovementGameObjectToScene(CurrentTime, MoveTAG);
                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Info, onText);	
                    }
                }
            } 		
        }		

        /// <summary>
        /// Toggle Lights for the current time
        /// </summary>
        public static void ToggleLightsToChart(bool isOverwrite = false){
            if(PromtWindowOpen || IsPlaying) return;

            if(s_instance.isOnLongNoteMode && s_instance.CurrentLongNote.gameObject != null) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_LongNoteNotFinalizedEffect);
                return;
            }

            // first we check if theres is any effect in that time period
            // We need to check the effect difficulty selected
            List<float> lights = s_instance.GetCurrentLightsByDifficulty();
            if(lights != null) {
                if(lights.Contains(CurrentTime)) {
                    lights.Remove(CurrentTime);
                    GameObject lightGO = GameObject.Find(s_instance.GetLightIdFormated(CurrentTime));
                    if(lightGO != null) {
                        DestroyImmediate(lightGO);
                    }

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(
                            Miku_DialogManager.DialogType.Info, 
                            string.Format(StringVault.Info_LightsEffect, "OFF")
                        );
                    }
                } else {					
                    for(int i = 0; i < lights.Count; ++i) {

                        if(IsWithin(lights[i], CurrentTime - MIN_FLASH_INTERVAL, CurrentTime + MIN_FLASH_INTERVAL)) {
                            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert,
                                string.Format(StringVault.Alert_EffectsInterval, (MIN_FLASH_INTERVAL/MS)));
                            return;
                        }
                    }
                    lights.Add(CurrentTime);	
                    s_instance.AddLightGameObjectToScene(CurrentTime);

                    if(!isOverwrite) {
                        Miku_DialogManager.ShowDialog(
                            Miku_DialogManager.DialogType.Info, 
                            string.Format(StringVault.Info_LightsEffect, "ON")
                        );
                    }			
                }				
            }
        }

        public static bool IsWithin(float value, float minimum, float maximum)
        {
            return value > minimum && value < maximum;
        }

        public static bool NeedSaveAction() {
            return (s_instance.lastSaveTime > SAVE_TIME_CHECK);
        }
#endregion

        public void ToggleMovementSectionToChart(int MoveTAGIndex){
             ToggleMovementSectionToChart(GetMoveTagTypeByIndex(MoveTAGIndex));
        }	

        /// <summary>
        /// Set the note marker type to be used
        /// </summary>
        /// <param name="noteType">The type of note to use. Default is 0 that is equal to <see cref="Note.NoteType.LeftHanded" /></param>
        public void SetNoteMarkerType(int noteType = 0) {
            if(GetNoteMarkerTypeIndex(selectedNoteType) != noteType) {
                CloseSpecialSection();
                FinalizeLongNoteMode();
            }			

            switch(noteType) {
                case 0:
                    selectedNoteType = Note.NoteType.LeftHanded;
                    break;
                case 1:
                    selectedNoteType = Note.NoteType.RightHanded;					
                    break;
                case 2:
                    selectedNoteType = Note.NoteType.OneHandSpecial;
                    if(isOnMirrorMode) {
                        isOnMirrorMode = false;
                    }
                    break;
                default: 
                    selectedNoteType = Note.NoteType.BothHandsSpecial;
                    if(isOnMirrorMode) {
                        isOnMirrorMode = false;
                    }
                    break;
            }			
        }

        /// <summary>
        /// Returns note marker game object, based on the type selected
        /// </summary>
        /// <param name="noteType">The type of note to look for, default is <see cref="Note.NoteType.LeftHanded" /></param>
        /// <returns>Returns <typeparamref name="GameObject"/></returns>
        GameObject GetNoteMarkerByType(Note.NoteType noteType = Note.NoteType.LeftHanded, bool isSegment = false) {
            switch(noteType) {
                case Note.NoteType.LeftHanded:
                    return isSegment ? m_LefthandNoteMarkerSegment : m_LefthandNoteMarker;
                case Note.NoteType.RightHanded:
                    return isSegment ? m_RighthandNoteMarkerSegment : m_RighthandNoteMarker;
                case Note.NoteType.BothHandsSpecial:
                    return isSegment ? m_Special2NoteMarkerSegment : m_SpecialBothHandsNoteMarker;
            }

            return isSegment ? m_Special1NoteMarkerSegment : m_SpecialOneHandNoteMarker;
        }

        /// <summary>
        /// Returns index of the NoteType
        /// </summary>
        /// <param name="noteType">The type of note to look for, default is <see cref="Note.NoteType.LeftHanded" /></param>
        /// <returns>Returns <typeparamref name="int"/></returns>
        int GetNoteMarkerTypeIndex(Note.NoteType noteType = Note.NoteType.LeftHanded) {
            switch(noteType) {
                case Note.NoteType.LeftHanded:
                    return 0;
                case Note.NoteType.RightHanded:
                    return 1;
                case Note.NoteType.OneHandSpecial:
                    return 2;
            }

            return 3;
        }

        /// <summary>
        /// Update the position on the Current Place notes when any of the const changes		
        /// </summary>
        /// <remarks>
        /// some constants include BMP, Speed, etc.
        /// </remarks>
        /// <param name="fromBPM">Overwrite the BPM use for the update</param>
        void UpdateNotePositions(float fromBPM = 0, bool kWasChange = false) {
            isBusy = true;

            try {
                // Get the current working track
                Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();	
                float newTime, newPos;		

                if(workingTrack != null && workingTrack.Count > 0) {
                    // New Dictionary on where the new data will be update
                    Dictionary<float, List<Note>> updateData = new Dictionary<float, List<Note>>();

                    // Iterate each entry on the Dictionary and get the note to update
                    foreach( KeyValuePair<float, List<Note>> kvp in workingTrack )
                    {
                        List<Note> _notes = kvp.Value;
                        List<Note> updateList = new List<Note>();

                        // The new update time
                        newTime = UpdateTimeToBPM(kvp.Key, fromBPM);		

                        // Iterate each note and update its info
                        for(int i = 0; i < _notes.Count; i++) {
                            Note n = _notes[i];

                            // Get the new position using the new constants
                            newPos = MStoUnit(newTime);
                            // And update the value on the Dictionary
                            n.Position = new float[3] { n.Position[0], n.Position[1], newPos };
                            // And update the position of the GameObject
                            GameObject noteGO = GameObject.Find(n.Id);
                            noteGO.transform.position = new Vector3(
                                noteGO.transform.position.x,
                                noteGO.transform.position.y,
                                newPos );							

                            // Update data
                            n.Id = FormatNoteName(newTime, i, n.Type);
                            updateList.Add(n);
                            noteGO.name = n.Id;		

                            if(n.Segments != null && n.Segments.GetLength(0) > 0) {		
                                for(int j = 0; j < n.Segments.GetLength(0); ++j) {
                                    Vector3 segmentPos = transform.InverseTransformPoint(
                                            n.Segments[j, 0],
                                            n.Segments[j, 1], 
                                            n.Segments[j, 2]
                                    );

                                    float tms = UnitToMS(segmentPos.z);
                                    n.Segments[j, 2] = MStoUnit(UpdateTimeToBPM(tms, fromBPM));
                                }

                                AddNoteSegmentsObject(n, noteGO.transform.Find("LineArea"), true);
                            }

                            /* if(n.Segments != null && n.Segments.GetLength(0) > 0) {
                                for(int j = 0; j < n.Segments.GetLength(0); ++j) {
                                    Vector3 segmentPos = transform.InverseTransformPoint(
                                            n.Segments[j, 0],
                                            n.Segments[j, 1], 
                                            n.Segments[j, 2]
                                    );

                                    float tms = UnitToMS(segmentPos.z);
                                    n.Segments[j, 2] = MStoUnit(UpdateTimeToBPM(tms, fromBPM));
                                }
                                
                                RenderLine(noteGO, n.Segments, true);
                            } */
                        }
                        
                        // Add update note to new list
                        updateData.Add(newTime, updateList);
                    }

                    // Finally Update the note data
                    workingTrack.Clear();
                    UpdateCurrentTrackDifficulty(updateData);
                }

                List<float> workingEffects = GetCurrentEffectDifficulty();
                if(workingEffects != null && workingEffects.Count > 0) {
                    List<float> updatedEffects = new List<float>();
                    for(int i = 0; i < workingEffects.Count; ++i) {
                        // The new update time
                        newTime = UpdateTimeToBPM(workingEffects[i], fromBPM);
                        newPos = MStoUnit(newTime);

                        GameObject effectGO = GameObject.Find(GetEffectIdFormated(workingEffects[i]));
                        if(effectGO != null) {
                            effectGO.transform.position = new Vector3(
                                effectGO.transform.position.x,
                                effectGO.transform.position.y,
                                newPos );
                            effectGO.name = GetEffectIdFormated(newTime);
                            
                            updatedEffects.Add(newTime);
                        } 
                        
                    }

                    workingEffects.Clear();
                    UpdateCurrentEffectDifficulty(updatedEffects);
                }

                List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
                if(bookmarks != null && bookmarks.Count > 0) {
                    List<Bookmark> updateBookmarks = new List<Bookmark>();
                    Bookmark currBookmark;
                    for(int i = 0; i < bookmarks.Count; ++i) {
                        currBookmark = bookmarks[i];
                        newTime = UpdateTimeToBPM(currBookmark.time, fromBPM);
                        newPos = MStoUnit(newTime);						

                        GameObject bookmarkGO = GameObject.Find(GetBookmarkIdFormated(currBookmark.time));
                        if(bookmarkGO != null) {
                            bookmarkGO.transform.position = new Vector3(
                                bookmarkGO.transform.position.x,
                                bookmarkGO.transform.position.y,
                                newPos );
                            bookmarkGO.name = GetBookmarkIdFormated(newTime);
                            //currBookmark.time = newTime;
                            Bookmark copyBookmark = new Bookmark();
                            copyBookmark.name = currBookmark.name;
                            copyBookmark.time = newTime;
                            updateBookmarks.Add(copyBookmark);
                        } 					
                    }

                    bookmarks.Clear();
                    UpdateCurrentEffectDifficulty(updateBookmarks, true);
                }

                List<float> jumps = GetCurrentMovementListByDifficulty(true);
                if(jumps != null && jumps.Count > 0) {
                    List<float> updatedJumps = new List<float>();
                    for(int i = 0; i < jumps.Count; ++i) {
                        newTime = UpdateTimeToBPM(jumps[i], fromBPM);
                        newPos = MStoUnit(newTime);

                        GameObject moveSectGO = GameObject.Find(GetMovementIdFormated(jumps[i], JUMP_TAG));
                        if(moveSectGO != null) {
                            moveSectGO.transform.position = new Vector3(
                                moveSectGO.transform.position.x,
                                moveSectGO.transform.position.y,
                                newPos );
                            moveSectGO.name = GetMovementIdFormated(newTime, JUMP_TAG);

                            updatedJumps.Add(newTime);
                        }
                    }

                    jumps.Clear();
                    UpdateCurrentMovementDifficulty(updatedJumps, JUMP_TAG);
                }

                List<float> crouchs = GetCurrentMovementListByDifficulty(false);
                if(crouchs != null && crouchs.Count > 0) {
                    List<float> updatedCrouchs = new List<float>();
                    for(int i = 0; i < crouchs.Count; ++i) {
                        newTime = UpdateTimeToBPM(crouchs[i], fromBPM);
                        newPos = MStoUnit(newTime);

                        GameObject moveSectGO = GameObject.Find(GetMovementIdFormated(crouchs[i], CROUCH_TAG));
                        if(moveSectGO != null) {
                            moveSectGO.transform.position = new Vector3(
                                moveSectGO.transform.position.x,
                                moveSectGO.transform.position.y,
                                newPos );
                            moveSectGO.name = GetMovementIdFormated(newTime, CROUCH_TAG);

                            updatedCrouchs.Add(newTime);
                        }
                    }

                    crouchs.Clear();
                    UpdateCurrentMovementDifficulty(updatedCrouchs, CROUCH_TAG);
                }

                List<Slide> slides = GetCurrentMovementListByDifficulty();
                if(slides != null && slides.Count > 0) {
                    List<Slide> updateSlides = new List<Slide>();
                    for(int i = 0; i < slides.Count; ++i) {
                        Slide currSlide = slides[i];
                        newTime = UpdateTimeToBPM(slides[i].time, fromBPM);
                        newPos = MStoUnit(newTime);
                        
                        GameObject moveSectGO = GameObject.Find(GetMovementIdFormated(currSlide.time, GetSlideTagByType(currSlide.slideType)));
                        if(moveSectGO != null) {
                            moveSectGO.transform.position = new Vector3(
                                moveSectGO.transform.position.x,
                                moveSectGO.transform.position.y,
                                newPos );
                            moveSectGO.name = GetMovementIdFormated(newTime, GetSlideTagByType(currSlide.slideType));

                            currSlide.time = newTime;
                            updateSlides.Add(currSlide);
                        } 
                    }

                    slides.Clear();
                    UpdateCurrentMovementDifficulty(updateSlides, SLIDE_CENTER_TAG);
                }

                List<float> lights = GetCurrentLightsByDifficulty();
                if(lights != null && lights.Count > 0) {
                    List<float> updatedLights = new List<float>();
                    for(int i = 0; i < lights.Count; ++i) {
                        // The new update time
                        newTime = UpdateTimeToBPM(lights[i], fromBPM);
                        newPos = MStoUnit(newTime);

                        GameObject effectGO = GameObject.Find(GetLightIdFormated(lights[i]));
                        if(effectGO != null) {
                            effectGO.transform.position = new Vector3(
                                effectGO.transform.position.x,
                                effectGO.transform.position.y,
                                newPos );
                            effectGO.name = GetLightIdFormated(newTime);
                            
                            updatedLights.Add(newTime);
                        } 
                        
                    }

                    lights.Clear();
                    UpdateCurrentLightsDifficulty(updatedLights);
                }

            } catch(Exception ex) {
                LogMessage("BPM update error");
                LogMessage(ex.ToString());
                Serializer.WriteToLogFile("BPM update error");
                Serializer.WriteToLogFile(ex.ToString());
            }
            

            ///_currentTime = UpdateTimeToBPM(_currentTime);
            /*_currentTime = _currentTime - (K);
            MoveCamera(true , MStoUnit(_currentTime));*/

            //CurrentChart.BPM = BPM;
            isBusy = false;
        }

        /// <summary>
        /// Return the given time update to the new BPM
        /// </summary>
        /// <param name="fromBPM">Overwrite the BPM use for the update</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float UpdateTimeToBPM(float ms, float fromBPM = 0) {
            //return ms - ( ( (MS*MINUTE)/CurrentChart.BPM ) - K );
            if(ms > 0) {
                fromBPM = ( fromBPM > 0) ? fromBPM : lastBPM;
                return (K * ms) / ((MS*MINUTE)/fromBPM);
            } else {
                return ms;
            }
            
            //return (BPM * ms) / lastBPM;
        }

        /// <summary>
        /// Return the given time update to the new K const
        /// </summary>
        /// <param name="fromK">Overwrite the K use for the update</param>
        /// <returns>Returns <typeparamref name="float"/></returns>
        float UpdateTimeToK(float ms, float fromK = 0) {
            if(ms > 0) {
                return (K * ms) / fromK;
            } else {
                return ms;
            }
        }

        /// <summary>
        /// Delete all the GameObject notes of the Current Difficulty.
        /// Also clear its corresponding List and Dictonary Entry		
        /// </summary>
        void ClearNotePositions() {
            isBusy = true;

            // Get the current working track
            Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();		

            if(workingTrack != null && workingTrack.Count > 0) {
                // New Empty Dictionary on where the new data will be update
                Dictionary<float, List<Note>> updateData = new Dictionary<float, List<Note>>();

                // Iterate each entry on the Dictionary and get the note to update
                foreach( KeyValuePair<float, List<Note>> kvp in workingTrack )
                {
                    List<Note> _notes = kvp.Value;

                    // Iterate each note and update its info
                    for(int i = 0; i < _notes.Count; i++) {
                        Note n = _notes[i];

                        // And after find its related GameObject we delete it
                        GameObject noteGO = GameObject.Find(n.Id);
                        GameObject.DestroyImmediate(noteGO);
                    }
                    
                    // And Finally clear the list
                    _notes.Clear();
                }

                // Finally Update the note data
                workingTrack.Clear();
                UpdateCurrentTrackDifficulty(updateData);
            }

            // Get the current effects track
            List<float> workingEffects = GetCurrentEffectDifficulty();
            if(workingEffects != null && workingEffects.Count > 0) {
                for(int i = 0; i < workingEffects.Count; i++) {
                    float t = workingEffects[i];

                    // And after find its related GameObject we delete it
                    GameObject effectGo = GameObject.Find(GetEffectIdFormated(t));
                    GameObject.DestroyImmediate(effectGo);
                }
            }
            workingEffects.Clear();

            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            if(jumps != null && jumps.Count > 0) {
                for(int i = 0; i < jumps.Count; ++i) {
                    float t = jumps[i];
                    GameObject jumpGO = GameObject.Find(GetMovementIdFormated(t, JUMP_TAG));
                    GameObject.DestroyImmediate(jumpGO);
                }
            }
            jumps.Clear();

            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            if(crouchs != null && crouchs.Count > 0) {
                for(int i = 0; i < crouchs.Count; ++i) {
                    float t = crouchs[i];
                    GameObject crouchGO = GameObject.Find(GetMovementIdFormated(t, CROUCH_TAG));
                    GameObject.DestroyImmediate(crouchGO);
                }
            }
            crouchs.Clear();

            List<Slide> slides = GetCurrentMovementListByDifficulty();
            if(slides != null && slides.Count > 0) {
                for(int i = 0; i < slides.Count; ++i) {
                    float t = slides[i].time;
                    GameObject slideGO = GameObject.Find(GetMovementIdFormated(t, GetSlideTagByType(slides[i].slideType)));
                    GameObject.DestroyImmediate(slideGO);
                }
            }
            slides.Clear();

            // Get the current effects track
            List<float> lights = GetCurrentLightsByDifficulty();
            if(lights != null && lights.Count > 0) {
                for(int i = 0; i < lights.Count; i++) {
                    float t = lights[i];

                    // And after find its related GameObject we delete it
                    GameObject lightGO = GameObject.Find(GetLightIdFormated(t));
                    GameObject.DestroyImmediate(lightGO);
                }
            }
            lights.Clear();

            // Reset the current time
            _currentTime = 0;
            MoveCamera(true, _currentTime);

            UpdateTotalNotes(true);

            hitSFXSource.Clear();

            isBusy = false;
        }

        /// <summary>
        /// Only delete all the GameObject notes of the Current Difficulty.
        /// Its corresponding List and Dictonary Entry remains unchanged	
        /// </summary>
        void DeleteNotesGameObjects() {
            isBusy = true;

            // Get the current working track
            Dictionary<float, List<Note>> workingTrack = GetCurrentTrackDifficulty();

            if(workingTrack != null && workingTrack.Count > 0) {
                // Iterate each entry on the Dictionary and get the note to update
                foreach( KeyValuePair<float, List<Note>> kvp in workingTrack )
                {
                    List<Note> _notes = kvp.Value;

                    // Iterate each note and update its info
                    for(int i = 0; i < _notes.Count; i++) {
                        Note n = _notes[i];

                        // And after find its related GameObject we delete it
                        GameObject noteGO = GameObject.Find(n.Id);
                        GameObject.DestroyImmediate(noteGO);
                    }
                }
            }

            // Get the current effects track
            List<float> workingEffects = GetCurrentEffectDifficulty();
            if(workingEffects != null && workingEffects.Count > 0) {
                for(int i = 0; i < workingEffects.Count; i++) {
                    float t = workingEffects[i];

                    // And after find its related GameObject we delete it
                    GameObject effectGo = GameObject.Find(GetEffectIdFormated(t));
                    GameObject.DestroyImmediate(effectGo);
                }
            }

            List<Bookmark> bookmarks = CurrentChart.Bookmarks.BookmarksList;
            if(bookmarks != null && bookmarks.Count > 0) {
                for(int i = 0; i < bookmarks.Count; ++i) {
                    float t = bookmarks[i].time;
                    GameObject bookGO = GameObject.Find(GetBookmarkIdFormated(t));
                    GameObject.DestroyImmediate(bookGO);
                }
            }

            List<float> jumps = GetCurrentMovementListByDifficulty(true);
            if(jumps != null && jumps.Count > 0) {
                for(int i = 0; i < jumps.Count; ++i) {
                    float t = jumps[i];
                    GameObject jumpGO = GameObject.Find(GetMovementIdFormated(t, JUMP_TAG));
                    GameObject.DestroyImmediate(jumpGO);
                }
            }

            List<float> crouchs = GetCurrentMovementListByDifficulty(false);
            if(crouchs != null && crouchs.Count > 0) {
                for(int i = 0; i < crouchs.Count; ++i) {
                    float t = crouchs[i];
                    GameObject crouchGO = GameObject.Find(GetMovementIdFormated(t, CROUCH_TAG));
                    GameObject.DestroyImmediate(crouchGO);
                }
            }

            List<Slide> slides = GetCurrentMovementListByDifficulty();
            if(slides != null && slides.Count > 0) {
                for(int i = 0; i < slides.Count; ++i) {
                    float t = slides[i].time;
                    GameObject slideGO = GameObject.Find(GetMovementIdFormated(t, GetSlideTagByType(slides[i].slideType)));
                    GameObject.DestroyImmediate(slideGO);
                }
            }

            // Get the current effects track
            List<float> lights = GetCurrentLightsByDifficulty();
            if(lights != null && lights.Count > 0) {
                for(int i = 0; i < lights.Count; i++) {
                    float t = lights[i];

                    // And after find its related GameObject we delete it
                    GameObject lightGO = GameObject.Find(GetLightIdFormated(t));
                    GameObject.DestroyImmediate(lightGO);
                }
            }
            
            hitSFXSource.Clear();
            isBusy = false;
        }

        /// <summary>
        /// Change the current Track Difficulty by TrackDifficulty
        /// </summary>
        /// <param name="difficulty">The new difficulty"</param>
        void SetCurrentTrackDifficulty(TrackDifficulty difficulty) {
            currentSpecialSectionID = -1;
            CloseSpecialSection();
            FinalizeLongNoteMode();

            DeleteNotesGameObjects();
            CurrentDifficulty = difficulty;
            LoadChartNotes();
            // m_DifficultyDisplay.text = CurrentDifficulty.ToString();
            m_statsDifficultyText.text = CurrentDifficulty.ToString();

            resizedNotes.Clear();			

            // Reset the current time
            _currentTime = 0;
            MoveCamera(true, _currentTime);
        }

        /// <summary>
        /// Get The current track difficulty based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="Dictionary<float, List<Note>>"/></returns>
        Dictionary<float, List<Note>> GetCurrentTrackDifficulty() {
            if(CurrentChart == null) return null;

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return CurrentChart.Track.Normal;
                case TrackDifficulty.Hard:
                    return CurrentChart.Track.Hard;
                case TrackDifficulty.Expert:
                    return CurrentChart.Track.Expert;
                case TrackDifficulty.Master:
                    return CurrentChart.Track.Master;
                case TrackDifficulty.Custom:
                    return CurrentChart.Track.Custom;
            }

            return CurrentChart.Track.Easy;
        }

        /// <summary>
        /// Get The current track difficulty index, based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="int"/></returns>
        int GetCurrentTrackDifficultyIndex() {
            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return 1;
                case TrackDifficulty.Hard:
                    return 2;
                case TrackDifficulty.Expert:
                    return 3;
                case TrackDifficulty.Master:
                    return 4;
                case TrackDifficulty.Custom:
                    return 5;
            }

            return 0;
        }

        /// <summary>
        /// Get The track difficulty based on the given index
        /// </summary>
        /// <param name="index">The index of difficulty from 0 - easy to 3 - Expert"</param>
        /// <returns>Returns <typeparamref name="TrackDifficulty"/></returns>
        TrackDifficulty GetTrackDifficultyByIndex(int index = 0) {
            switch(index) {
                case 1:
                    return TrackDifficulty.Normal;
                case 2:
                    return TrackDifficulty.Hard;
                case 3:
                    return TrackDifficulty.Expert;
                case 4:
                    return TrackDifficulty.Master;
                case 5:
                    return TrackDifficulty.Custom;
            }

            return TrackDifficulty.Easy;
        }

        /// <summary>
        /// Update the current track difficulty data based on the selected difficulty
        /// </summary>
        void UpdateCurrentTrackDifficulty( Dictionary<float, List<Note>> newData ) {

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    CurrentChart.Track.Normal.Clear();
                    CurrentChart.Track.Normal = newData;
                    break;
                case TrackDifficulty.Hard:
                    CurrentChart.Track.Hard.Clear();
                    CurrentChart.Track.Hard = newData;
                    break;
                case TrackDifficulty.Expert:
                    CurrentChart.Track.Expert.Clear();
                    CurrentChart.Track.Expert = newData;
                    break;
                case TrackDifficulty.Master:
                    CurrentChart.Track.Master.Clear();
                    CurrentChart.Track.Master = newData;
                    break;
                case TrackDifficulty.Custom:
                    CurrentChart.Track.Custom.Clear();
                    CurrentChart.Track.Custom = newData;
                    break;
                default:
                    CurrentChart.Track.Easy.Clear();
                    CurrentChart.Track.Easy = newData;
                    break;
            }

            disabledNotes.Clear();
            resizedNotes.Clear();
        }

        /// <summary>
        /// Update the current effects difficulty data based on the selected difficulty
        /// </summary>
        void UpdateCurrentEffectDifficulty<T>( List<T> newData, bool IsBookmark = false ) {

            if(!IsBookmark) {
                switch(CurrentDifficulty) {
                    case TrackDifficulty.Normal:
                        CurrentChart.Effects.Normal.Clear();
                        CurrentChart.Effects.Normal = newData as List<float>;
                        break;
                    case TrackDifficulty.Hard:
                        CurrentChart.Effects.Hard.Clear();
                        CurrentChart.Effects.Hard = newData as List<float>;
                        break;
                    case TrackDifficulty.Expert:
                        CurrentChart.Effects.Expert.Clear();
                        CurrentChart.Effects.Expert = newData as List<float>;
                        break;
                    case TrackDifficulty.Master:
                        CurrentChart.Effects.Master.Clear();
                        CurrentChart.Effects.Master = newData as List<float>;
                        break;
                    case TrackDifficulty.Custom:
                        CurrentChart.Effects.Custom.Clear();
                        CurrentChart.Effects.Custom = newData as List<float>;
                        break;
                    default:
                        CurrentChart.Effects.Easy.Clear();
                        CurrentChart.Effects.Easy = newData as List<float>;
                        break;
                }
            } else {
                CurrentChart.Bookmarks.BookmarksList.Clear();
                CurrentChart.Bookmarks.BookmarksList = newData as List<Bookmark>;
            }
        }

        /// <summary>
        /// Update the current effects difficulty data based on the selected difficulty
        /// </summary>
        void UpdateCurrentMovementDifficulty<T>( List<T> newData, string MOV_TAG ) {

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Normal.Clear();
                        CurrentChart.Jumps.Normal = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Normal.Clear();
                        CurrentChart.Crouchs.Normal = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Normal.Clear();
                        CurrentChart.Slides.Normal = newData as List<Slide>;
                    }					
                    break;
                case TrackDifficulty.Hard:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Hard.Clear();
                        CurrentChart.Jumps.Hard = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Hard.Clear();
                        CurrentChart.Crouchs.Hard = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Hard.Clear();
                        CurrentChart.Slides.Hard = newData as List<Slide>;
                    }	
                    break;
                case TrackDifficulty.Expert:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Expert.Clear();
                        CurrentChart.Jumps.Expert = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Expert.Clear();
                        CurrentChart.Crouchs.Expert = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Expert.Clear();
                        CurrentChart.Slides.Expert = newData as List<Slide>;
                    }
                    break;
                case TrackDifficulty.Master:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Master.Clear();
                        CurrentChart.Jumps.Master = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Master.Clear();
                        CurrentChart.Crouchs.Master = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Master.Clear();
                        CurrentChart.Slides.Master = newData as List<Slide>;
                    }
                    break;
                case TrackDifficulty.Custom:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Custom.Clear();
                        CurrentChart.Jumps.Custom = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Custom.Clear();
                        CurrentChart.Crouchs.Custom = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Custom.Clear();
                        CurrentChart.Slides.Custom = newData as List<Slide>;
                    }
                    break;
                default:
                    if(MOV_TAG.Equals(JUMP_TAG)) {
                        CurrentChart.Jumps.Easy.Clear();
                        CurrentChart.Jumps.Easy = newData as List<float>;
                    } else if(MOV_TAG.Equals(CROUCH_TAG)) {
                        CurrentChart.Crouchs.Easy.Clear();
                        CurrentChart.Crouchs.Easy = newData as List<float>;
                    } else {
                        CurrentChart.Slides.Easy.Clear();
                        CurrentChart.Slides.Easy = newData as List<Slide>;
                    }
                    break;
            }
        }

        /// <summary>
        /// Update the current lights difficulty data based on the selected difficulty
        /// </summary>
        void UpdateCurrentLightsDifficulty( List<float> newData ) {
            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    CurrentChart.Lights.Normal.Clear();
                    CurrentChart.Lights.Normal = newData;
                    break;
                case TrackDifficulty.Hard:
                    CurrentChart.Lights.Hard.Clear();
                    CurrentChart.Lights.Hard = newData;
                    break;
                case TrackDifficulty.Expert:
                    CurrentChart.Lights.Expert.Clear();
                    CurrentChart.Lights.Expert = newData;
                    break;
                case TrackDifficulty.Master:
                    CurrentChart.Lights.Master.Clear();
                    CurrentChart.Lights.Master = newData;
                    break;
                case TrackDifficulty.Custom:
                    CurrentChart.Lights.Custom.Clear();
                    CurrentChart.Lights.Custom = newData;
                    break;
                default:
                    CurrentChart.Lights.Easy.Clear();
                    CurrentChart.Lights.Easy = newData;
                    break;
            }
        }

        /// <summary>
        /// Reactivate the GameObjects in <see cref="disabledNotes" /> and clear the list
        /// </summary>
        void ResetDisabledList() {
            for(int i = 0; i < disabledNotes.Count; ++i) {
                disabledNotes[i].SetActive(true);
                Transform directionWrap = disabledNotes[i].transform.parent.Find("DirectionWrap");
                if(directionWrap != null) {
                    directionWrap.gameObject.SetActive(true);
                }
            }

            disabledNotes.Clear();
        }

        /// <summary>
        /// Resize the GameObjects in <see cref="disabledNotes" /> and clear the list
        /// </summary>
        void ResetResizedList() {
            for(int i = 0; i < resizedNotes.Count; ++i) {
                if(resizedNotes[i] != null) {
                    resizedNotes[i].GetComponent<MeshRenderer>().enabled = true;
                    // resizedNotes[i].transform.localScale = resizedNotes[i].transform.localScale / m_CameraNearReductionFactor;
                    Transform directionWrap = resizedNotes[i].transform.parent.Find("DirectionWrap");
                    if(directionWrap != null) {
                        directionWrap.gameObject.SetActive(true);
                    }
                }				
            }

            resizedNotes.Clear();
        }

        /// <summary>
        /// Play the passed audioclip
        /// </summary>
        void PlaySFX(AudioClip soundToPlay, bool isMetronome = false) {
            if(isMetronome) {
                PlayMetronomeBeat();
            } else {
                if(soundToPlay != null) {
                    m_SFXAudioSource.clip = soundToPlay;
                    m_SFXAudioSource.PlayOneShot(m_SFXAudioSource.clip);
                }
            }						
        }

        /// <summary>
        /// Play the Metronome audioclip
        /// </summary>
        void PlayMetronomeBeat() {
            m_MetronomeAudioSource.clip = m_MetronomeSound;
            m_MetronomeAudioSource.PlayOneShot(m_MetronomeAudioSource.clip);
        }

        /// <summary>
        /// Show the info window that notifie the user of the current working section
        /// </summary>
        /// <param name="message">The message to show</param>
        void ToggleWorkingStateAlertOn(string message){
            if(!m_StateInfoObject.activeSelf) {
                m_StateInfoObject.SetActive(false);
                m_StateInfoText.SetText(message);

                StartCoroutine(EnableStateAlert());
            } else {
                m_StateInfoText.SetText(message);
            }
        }

        // To give enoungh time for the animation to run correctly
        IEnumerator EnableStateAlert() {
            yield return null;

            m_StateInfoObject.SetActive(true);
        }

        /// <summary>
        /// Hide the info window that notifie the user of the current working section
        /// </summary>
        void ToggleWorkingStateAlertOff(){
            if(m_StateInfoObject.activeSelf) {
                m_StateInfoObject.SetActive(false);
            }
        }

        /// <summary>
        /// Get The current effect list based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="List"/></returns>
        List<float> GetCurrentEffectDifficulty() {
            if(CurrentChart == null) return null;

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return CurrentChart.Effects.Normal;
                case TrackDifficulty.Hard:
                    return CurrentChart.Effects.Hard;
                case TrackDifficulty.Expert:
                    return CurrentChart.Effects.Expert;
                case TrackDifficulty.Master:
                    return CurrentChart.Effects.Master;
                case TrackDifficulty.Custom:
                    return CurrentChart.Effects.Custom;
            }

            return CurrentChart.Effects.Easy;
        }

        /// <summary>
        /// Get The current movement section list based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="List"/></returns>
        List<float> GetCurrentMovementListByDifficulty(bool fromJumpList) {
            if(CurrentChart == null) return null;

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return fromJumpList ? CurrentChart.Jumps.Normal : CurrentChart.Crouchs.Normal;
                case TrackDifficulty.Hard:
                    return fromJumpList ? CurrentChart.Jumps.Hard : CurrentChart.Crouchs.Hard;
                case TrackDifficulty.Expert:
                    return fromJumpList ? CurrentChart.Jumps.Expert : CurrentChart.Crouchs.Expert;
                case TrackDifficulty.Master:
                    return fromJumpList ? CurrentChart.Jumps.Master : CurrentChart.Crouchs.Master;
                case TrackDifficulty.Custom:
                    return fromJumpList ? CurrentChart.Jumps.Custom : CurrentChart.Crouchs.Custom;
            }

            return fromJumpList ? CurrentChart.Jumps.Easy : CurrentChart.Crouchs.Easy;
        }

        /// <summary>
        /// Get The current movement section list based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="List"/></returns>
        List<Slide> GetCurrentMovementListByDifficulty() {
            if(CurrentChart == null) return null;

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return CurrentChart.Slides.Normal;
                case TrackDifficulty.Hard:
                    return CurrentChart.Slides.Hard;
                case TrackDifficulty.Expert:
                    return CurrentChart.Slides.Expert;
                case TrackDifficulty.Master:
                    return CurrentChart.Slides.Master;
                case TrackDifficulty.Custom:
                    return CurrentChart.Slides.Custom;
            }

            return CurrentChart.Slides.Easy;
        }

        /// <summary>
        /// Get The current lights list based on the selected difficulty
        /// </summary>
        /// <returns>Returns <typeparamref name="List"/></returns>
        List<float> GetCurrentLightsByDifficulty() {
            if(CurrentChart == null) return null;

            switch(CurrentDifficulty) {
                case TrackDifficulty.Normal:
                    return CurrentChart.Lights.Normal;
                case TrackDifficulty.Hard:
                    return CurrentChart.Lights.Hard;
                case TrackDifficulty.Expert:
                    return CurrentChart.Lights.Expert;
                case TrackDifficulty.Master:
                    return CurrentChart.Lights.Master;
                case TrackDifficulty.Custom:
                    return CurrentChart.Lights.Custom;
            }

            return CurrentChart.Lights.Easy;
        }

        /// <summary>
        /// handler to get the effect name passing the time
        /// </summary>
        /// <param name="ms">The time on with the effect is</param>
        string GetEffectIdFormated(float ms) {
            return string.Format("Flash_{0}", ms.ToString("R"));
        }

        /// <summary>
        /// handler to get the bookmark name passing the time
        /// </summary>
        /// <param name="ms">The time on with the bookmark is</param>
        string GetBookmarkIdFormated(float ms) {
            return string.Format("Bookmark_{0}", ms.ToString("R"));
        }

        /// <summary>
        /// handler to get the Movement Section name passing the time
        /// </summary>
        /// <param name="ms">The time on with the bookmark is</param>
        string GetMovementIdFormated(float ms, string section = "Jump") {
            return string.Format("{0}_{1}", section, ms.ToString("R"));
        }

        /// <summary>
        /// handler to get the light name passing the time
        /// </summary>
        /// <param name="ms">The time on with the effect is</param>
        string GetLightIdFormated(float ms) {
            return string.Format("Light_{0}", ms.ToString("R"));
        }

        /// <summary>
        /// handler to get the Slide Tag relative to its type
        /// </summary>
        /// <param name="SlideType">The type of the slide</param>
        string GetSlideTagByType(Note.NoteType SlideType) {
            switch(SlideType) {
                case Note.NoteType.LeftHanded:
                    return SLIDE_LEFT_TAG;
                case Note.NoteType.RightHanded:
                    return SLIDE_RIGHT_TAG;
                case Note.NoteType.SeparateHandSpecial:
                    return SLIDE_LEFT_DIAG_TAG;
                case Note.NoteType.OneHandSpecial:
                    return SLIDE_RIGHT_DIAG_TAG;
                default:
                    return SLIDE_CENTER_TAG;
            }
        }

        /// <summary>
        /// handler to get the Slide Type relative to its tag
        /// </summary>
        /// <param name="TagName">The Tag of the slide</param>
        Note.NoteType GetSlideTypeByTag(string TagName) {
            switch(TagName) {
                case SLIDE_LEFT_TAG:
                    return Note.NoteType.LeftHanded;
                case SLIDE_RIGHT_TAG:
                    return Note.NoteType.RightHanded;
                case SLIDE_LEFT_DIAG_TAG:
                    return Note.NoteType.SeparateHandSpecial;
                case SLIDE_RIGHT_DIAG_TAG:
                    return Note.NoteType.OneHandSpecial;
                default:
                    return Note.NoteType.BothHandsSpecial;
            }
        }

        /// <summary>
        /// handler to get the move tag relative to its index
        /// </summary>
        /// <param name="TagIndex">The index of the Tag</param>
        string GetMoveTagTypeByIndex(int TagIndex) {
            switch(TagIndex) {
                case 0:
                    return SLIDE_LEFT_TAG;
                case 1:
                    return SLIDE_RIGHT_TAG;
                case 2:
                    return SLIDE_CENTER_TAG;
                case 3:
                    return SLIDE_LEFT_DIAG_TAG;
                case 4:
                    return SLIDE_RIGHT_DIAG_TAG;
                default:
                    return CROUCH_TAG;
            }
        }

        /// <summary>
        /// Check if an effects need to be played
        /// </summary>
        void CheckEffectsQueue() {
            if(effectsStacks == null || effectsStacks.Count == 0) return;

            // If the playing time is in the range of the next effect
            // we play the effect and remove the item from the stack
            if(_currentPlayTime >= effectsStacks.Peek()) {
                float effectMS = effectsStacks.Pop();

                if(_currentPlayTime - effectMS <= 3000) {
                    m_flashLight
                        .DOIntensity(3, 0.3f)
                        .SetLoops(2, LoopType.Yoyo); 
                }			 
                
                Track.LogMessage("Effect left in stack: "+effectsStacks.Count);
            }			
        }

        /// <summary>
        /// Check if an hit sfx need to be played
        /// </summary>
        void CheckSFXQueue() {
            if(hitSFXQueue == null || hitSFXQueue.Count == 0) return;

            // If the playing time is in the range of the next sfx
            // we play the sound and remove the item from the queue
            if(_currentPlayTime >= hitSFXQueue.Peek()) {
                float SFX_MS = hitSFXQueue.Dequeue();

                if(_currentPlayTime - SFX_MS <= 100) {
                    PlaySFX(m_HitMetaSound);
                }							 
            }			
        }

        /// <summary>
        /// Check if an metronome beat sfx need to be played
        /// </summary>
        void CheckMetronomeBeatQueue() {
            if(MetronomeBeatQueue == null || MetronomeBeatQueue.Count == 0) return;

            // If the playing time is in the range of the next beat
            // we play the sound and remove the item from the queue
            if(_currentPlayTime >= MetronomeBeatQueue.Peek()) {
                float SFX_MS = MetronomeBeatQueue.Dequeue();

                // Offset to only play beats close to the time
                if(_currentPlayTime - SFX_MS <= 100 && Metronome.isPlaying) {
                    PlaySFX(m_MetronomeSound, true);
                }							 
            }			
        }

        /// <summary>
        /// Delete the movement GameObjects at the passed time, filtering the passed Tag
        /// </summary>
        public void RemoveMovementSectionFromChart(string MoveTAG, float ms){
            List <Slide> slideList;
            switch(MoveTAG) {
                case JUMP_TAG:
                    slideList = GetCurrentMovementListByDifficulty();
                    RemoveMovementFromList(GetCurrentMovementListByDifficulty(false), ms, CROUCH_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_CENTER_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_LEFT_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_RIGHT_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_LEFT_DIAG_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_RIGHT_DIAG_TAG);
                    break;
                case CROUCH_TAG:
                    slideList = GetCurrentMovementListByDifficulty();
                    RemoveMovementFromList(GetCurrentMovementListByDifficulty(true), ms, JUMP_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_CENTER_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_LEFT_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_RIGHT_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_LEFT_DIAG_TAG);
                    RemoveMovementFromList(slideList, ms, SLIDE_RIGHT_DIAG_TAG);
                    break;
                default:
                    RemoveMovementFromList(GetCurrentMovementListByDifficulty(true), ms, JUMP_TAG);
                    RemoveMovementFromList(GetCurrentMovementListByDifficulty(false), ms, CROUCH_TAG);
                    break;
            }

            
        }

        private void RemoveMovementFromList<T>(List<T> workingList, float ms, string MoveTAG) {
            if(workingList is List<float>) {
                List<float> endList = workingList as List<float>;
                if(!endList.Contains(ms)) {
                    return;
                }

                endList.Remove(ms);
                
            } else if(workingList is List<Slide>) {
                List<Slide> endList = workingList as List<Slide>;
                Slide index = endList.Find(x => x.time == ms && x.slideType == GetSlideTypeByTag(MoveTAG));
                if(!index.initialized) {
                    return;
                }
                endList.Remove(index);
            }
            
            GameObject effectGO = GameObject.Find(GetMovementIdFormated(ms, MoveTAG));
            if(effectGO != null) {
                DestroyImmediate(effectGO);
            }
        }

        private void LoadEditorUserPrefs() {
            m_VolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF_KEY, 1f);
            m_SFXVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_PREF_KEY, 1f);
            LatencyOffset = PlayerPrefs.GetFloat(LATENCY_PREF_KEY, 0);
            syncnhWithAudio = ( PlayerPrefs.GetInt(SONG_SYNC_PREF_KEY, 0) > 0) ? true : false;
            if(PlayerPrefs.GetInt(VSYNC_PREF_KEY, 1) > 0) {
                ToggleVsycn();
            }
            m_CameraMoverScript.panSpeed = PlayerPrefs.GetFloat(PANNING_PREF_KEY, 0.15f);
            m_CameraMoverScript.turnSpeed = PlayerPrefs.GetFloat(ROTATION_PREF_KEY, 1.5f);
            MiddleButtonSelectorType = PlayerPrefs.GetInt(MIDDLE_BUTTON_SEL_KEY, 0);
            canAutoSave = ( PlayerPrefs.GetInt(AUTOSAVE_KEY, 1) > 0) ? true : false;
            doScrollSound = ( PlayerPrefs.GetInt(SCROLLSOUND_KEY, 1) > 0) ? true : false;
        }

        private void SaveEditorUserPrefs() {
            PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF_KEY, m_VolumeSlider.value);
            PlayerPrefs.SetFloat(SFX_VOLUME_PREF_KEY, m_SFXVolumeSlider.value);
            PlayerPrefs.SetFloat(LATENCY_PREF_KEY, LatencyOffset);
            PlayerPrefs.SetInt(SONG_SYNC_PREF_KEY, (syncnhWithAudio) ? 1 : 0);
            PlayerPrefs.SetInt(VSYNC_PREF_KEY, CurrentVsync);
            PlayerPrefs.SetFloat(PANNING_PREF_KEY, m_CameraMoverScript.panSpeed);
            PlayerPrefs.SetFloat(ROTATION_PREF_KEY, m_CameraMoverScript.turnSpeed);
            PlayerPrefs.SetInt(MIDDLE_BUTTON_SEL_KEY, MiddleButtonSelectorType);
            PlayerPrefs.SetInt(AUTOSAVE_KEY, (canAutoSave) ? 1 : 0);
            PlayerPrefs.SetInt(SCROLLSOUND_KEY, (doScrollSound) ? 1 : 0);
        }

        /// <summary>
        /// Abort the spectrum tread is it has not finished
        /// </summary>
        private void DoAbortThread()
        {
            try {
                if(analyzerThread != null && analyzerThread.ThreadState == ThreadState.Running) {
                    analyzerThread.Abort();
                }
            } catch(Exception ex) {
                LogMessage(ex.ToString(), true);
                Serializer.WriteToLogFile("DoAbortThread");
                Serializer.WriteToLogFile(ex.ToString());
            }
        }

        private void ToggleSelectionArea(bool isOFF = false) {
            if(isOFF) {
                ToggleWorkingStateAlertOff();		
            } else {
                CurrentSelection.startTime = CurrentTime;
                ToggleWorkingStateAlertOn(StringVault.Info_UserOnSelectionMode);
            }
            
        }

        private void SelectAll() {
            CurrentSelection.startTime = 0;
            CurrentSelection.endTime = TrackDuration * 1000;
            UpdateSelectionMarker();
        }

        private void ClearSelectionMarker() {
            CurrentSelection.startTime = 0;
            CurrentSelection.endTime = 0;
            UpdateSelectionMarker();
        }

        /// <summary>
        /// Update the selecion marker position and scale
        /// </summary>
        private void UpdateSelectionMarker() {
            if(m_selectionMarker != null) {
                selectionStartPos.z = MStoUnit(CurrentSelection.startTime);

                if(CurrentSelection.endTime >= CurrentSelection.startTime) {
                    selectionEndPos.z = MStoUnit(CurrentSelection.endTime);
                }				

                m_selectionMarker.SetPosition(0, selectionStartPos);
                m_selectionMarker.SetPosition(1, selectionEndPos);;
            }
        }

#region Setters & Getters

        /// <value>
        /// The BPM that the track will have
        /// </value>
        public static float BPM
        {
            get
            {
                return (s_instance != null) ? s_instance._BPM : 0;
            }

            set
            {
                s_instance._BPM = value;
            }
        }

        /// <value>
        /// The Current time in with the track is
        /// </value>
        public static float CurrentTime
        {
            get
            {
                return (s_instance != null) ? s_instance._currentTime : 0;
            }

            set
            {
                s_instance._currentTime = value;
            }
        }

        /// <value>
        /// The Current Unity unit relative to _currentTime in with the track is
        /// </value>
        public static float CurrentUnityUnit
        {
            get
            {
                return (s_instance != null) ? s_instance.MStoUnit(s_instance._currentTime) : 0;
            }
        }

        /// <value>
        /// The current Chart object being used
        /// </value>
        public static Chart CurrentChart
        {
            get
            {
                return (s_instance != null) ? s_instance.currentChart : null;
            }

            set
            {
                s_instance.currentChart = value;
            }
        }

        /// <value>
        /// The current Difficulty being used
        /// </value>
        public static TrackDifficulty CurrentDifficulty
        {
            get
            {
                return (s_instance != null) ? s_instance.currentDifficulty : TrackDifficulty.Easy;
            }

            set
            {
                s_instance.currentDifficulty = value;
            }
        }

        /// <value>
        /// Offset on milliseconds befor the Song start playing
        /// </value>
        public float StartOffset
        {
            get
            {
                return startOffset;
            }

            set
            {
                startOffset = value;
            }
        }

        /// <value>
        /// Playback Speed
        /// </value>
        public float PlaySpeed
        {
            get
            {
                return playSpeed;
            }

            set
            {
                playSpeed = value;
            }
        }

        /// <value>
        /// Track Duration for the lines drawing, default 60 seconds
        /// </value>
        public float TrackDuration
        {
            get
            {
                return trackDuration;
            }

            set
            {
                trackDuration = value;
            }
        }

        public static bool IsPlaying
        {
            get
            {
                return s_instance.isPlaying;
            }

            set
            {
                s_instance.isPlaying = value;
            }
        }

        public static bool IsInitilazed
        {
            get
            {
                return ( s_instance != null ) ? s_instance.isInitilazed : false;
            }

            set
            {
                s_instance.isInitilazed = value;
            }
        }

        public static string EditorVersion
        {
            get
            {
                return s_instance.editorVersion;
            }
        }

        public static bool IsOnDebugMode
        {
            get
            {
                return s_instance.debugMode;
            }
        }

        public float LatencyOffset
        {
            get
            {
                return latencyOffset;
            }

            set
            {
                latencyOffset = value;
            }
        }

        public static bool PromtWindowOpen
        {
            get
            {
                return s_instance.promtWindowOpen;
            }

            set
            {
                s_instance.promtWindowOpen = value;
            }
        }

        public static bool IsOnMirrorMode
        {
            get
            {
                return s_instance.isOnMirrorMode;
            }

            set
            {
                s_instance.isOnMirrorMode = value;
            }
        }

        public static bool XAxisInverse
        {
            get
            {
                return s_instance.xAxisInverse;
            }

            set
            {
                s_instance.xAxisInverse = value;
            }
        }

        public static bool YAxisInverse
        {
            get
            {
                return s_instance.yAxisInverse;
            }

            set
            {
                s_instance.yAxisInverse = value;
            }
        }

        public static TrackInfo TrackInfo {
            get {
                return s_instance.trackInfo;
            }
        }        

        #endregion
    }
}