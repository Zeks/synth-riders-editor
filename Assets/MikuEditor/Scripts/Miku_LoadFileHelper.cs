using System;
using System.Collections;
using System.Collections.Generic;
using B83.Win32;
using MiKu.NET;
using MiKu.NET.Charting;
using SFB;
using Shogoki.Utils;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Miku_LoadFileHelper : MonoBehaviour {

	[Header("Audio Selection")]
	public Button selectButton;
	public Button selectButtonEdit;
	public GameObject loopPreloader;
	public GameObject loopPreloaderEdit;

	[Header("New Chart Elements")]
	public InputField nameField;
	public InputField authorField;
	public InputField trackField;
	public InputField mapperField;

	private string artWorkField = "Default Image";
	public Image newArtworkField;
	public Button newChartButton;

	[Header("Edit Chart Elements")]
	public Animator editModePanelAnimator;
	public Animator editPanelAnimator;	
	public InputField editNameField;
	public InputField editAuthorField;
	public InputField editTrackField;
	public Image editArtworkField;
	public InputField editMapperField;

	public Button editChartButton;

	[Space(20)]
	public Texture2D defaultArtwork;
	
	// AudioSource for testing only
	//private AudioSource _audioSource;
	private bool newAudioSelected = false;
	private bool artworkEdited = false;
	private bool audioEdited = false;
	private float[] audioData;
	AudioClip loadedClip;
	string loadedArtwork;
	string defaultArtworkData;

	public GameObject batchLoader;

	InputField currentField;

	// important to keep the instance alive while the hook is active.
    UnityDragAndDropHook hook;

	void Start() {
		if(defaultArtwork != null){
			defaultArtworkData = Convert.ToBase64String(defaultArtwork.EncodeToPNG());
			SetSpriteToImage(newArtworkField, defaultArtworkData);
		} 

		// must be created on the main thread to get the right thread id.
        hook = new UnityDragAndDropHook();
        hook.InstallHook();
        hook.OnDroppedFiles += OnFiles;
	}

	void OnDisable()
    {
        hook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, POINT aPos)
    {
        // do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files has been dropped.
        /* Debug.Log("Dropped "+aFiles.Count+" files at: " + aPos + "\n"+
            aFiles.Aggregate((a, b) => a + "\n" + b)); */
		if(aFiles.Count > 0) {
			// use only the first file of the drop
			string file = aFiles[0];
			if(file.Contains(".synth")){
				LoadAudioChart(new System.Uri(file).LocalPath);
			} else if(file.Contains(".json") || file.Contains(".dat")) {
				LoadAudioChart(new System.Uri(file).LocalPath, true);
			}
        }
    }

	public void OpenBrowseDialogAudio(bool isEdit = false)
	{
		try {	
			// Open file with filter
			var extensions = new [] {
				new ExtensionFilter("Audio Files", "ogg"),
				new ExtensionFilter("All Files", "*" ),
			};

			StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, 
				false,
				(string[] paths) => {  
					if(paths.Length > 0) {
						ShowPreloader(isEdit);
						StartCoroutine(LoadAudioTrack(new System.Uri(paths[0]).AbsoluteUri, isEdit));
					} else {
						HidePreloader();
					}	
				});					
		}		
		catch (Exception ex)
        {
			HidePreloader();
            Debug.LogError("Error: Could not read file from disk. Original error: " + ex.Message);
        }
	}

	public void OpenBrowseDialogChart()
	{
		try {	
			// Open file with filter
			var extensions = new [] {
				new ExtensionFilter("Synth Files", Serializer.CHART_FILE_EXT),
			};

			StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, 
				false,
				(string[] paths) => {  
					if(paths.Length > 0) {
						LoadAudioChart(new System.Uri(paths[0]).LocalPath);
					}	
				});					
		}		
		catch (Exception ex)
        {
            Debug.LogError("Error: Could not read file from disk. Original error: " + ex.Message);
        }
	}

	public void OpenBrowseDialogImage(bool isEdit = false)
	{
		try {	
			// Open file with filter
			var extensions = new [] {
				new ExtensionFilter("Image Files", "jpg", "png"),
				new ExtensionFilter("All Files", "*" ),
			};

			StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, 
				false,
				(string[] paths) => {  
					if(paths.Length > 0) {
						// ShowPreloader();
						StartCoroutine(LoadTrackArtwork(new System.Uri(paths[0]).AbsoluteUri, isEdit));
					} else {
						// HidePreloader();
					}	
				});					
		}		
		catch (Exception ex)
        {
			// HidePreloader();
            Debug.LogError("Error: Could not read file from disk. Original error: " + ex.Message);
        }
	}

	public void OpenBrowseDialogJSON()
	{
		try {	
			// Open file with filter
			var extensions = new [] {
				new ExtensionFilter("JSON Files", "json", "dat"),
			};

			StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, 
				false,
				(string[] paths) => {  
					if(paths.Length > 0) {
						LoadAudioChart(new System.Uri(paths[0]).LocalPath, true);
					}	
				});					
		}		
		catch (Exception ex)
        {
            Debug.LogError("Error: Could not read file from disk. Original error: " + ex.Message);
        }
	}

	public void OpenBatchProccess()
	{
		try {
			string[] SynthFilesFolder = StandaloneFileBrowser.OpenFolderPanel("Select Folder", Application.dataPath+"/../", false);	
			if(SynthFilesFolder.Length > 0) {
				batchLoader.SetActive(true);
				Serializer.BachProcess(SynthFilesFolder[0]);
			}			
		}		
		catch (Exception ex)
        {
            Debug.LogError("Error: Could not read file from disk. Original error: " + ex.Message);
        }
	}


    private void LoadAudioChart(string absoluteUri, bool isJSON = false)
    {
        if(Serializer.Initialized) {
			bool fileLoadSuccess = Serializer.LoadFronFile(absoluteUri, isJSON, absoluteUri.Contains(".dat"));
			if(fileLoadSuccess) {

				editPanelAnimator.Play("Panel In");
				editModePanelAnimator.Play("Panel Out");

				editNameField.text = Serializer.ChartData.Name;
				editAuthorField.text = Serializer.ChartData.Author;
				editTrackField.text = (Serializer.ChartData.AudioData != null) ? string.Empty : Serializer.ChartData.AudioName;
				editMapperField.text = Serializer.ChartData.Beatmapper;
				if(!isJSON) {
					Serializer.ChartData.FilePath = absoluteUri;
				}				

				// For the artwork texture
				if(Serializer.ChartData.ArtworkBytes == null) {
					Serializer.ChartData.Artwork = "Default Artwork";
					Serializer.ChartData.ArtworkBytes = defaultArtworkData;								
				} 

				SetSpriteToImage(editArtworkField, Serializer.ChartData.ArtworkBytes);

				// If not has effect data
				if(Serializer.ChartData.Effects == null) {
					Effects defaultEffects = new Effects();
					defaultEffects.Easy = new List<float>();
					defaultEffects.Normal = new List<float>();
					defaultEffects.Hard = new List<float>();
					defaultEffects.Expert = new List<float>();
					defaultEffects.Master = new List<float>();
					defaultEffects.Custom = new List<float>();
					
					Serializer.ChartData.Effects = defaultEffects;
				}

				if(Serializer.ChartData.Jumps == null) {
					Jumps defaultJumps = new Jumps();
					defaultJumps.Easy = new List<float>();
					defaultJumps.Normal = new List<float>();
					defaultJumps.Hard = new List<float>();
					defaultJumps.Expert = new List<float>();
					defaultJumps.Master = new List<float>();
					defaultJumps.Custom = new List<float>();

					Serializer.ChartData.Jumps = defaultJumps;
				}

				if(Serializer.ChartData.Crouchs == null) {
					Crouchs defaultCrouchs = new Crouchs();
					defaultCrouchs.Easy = new List<float>();
					defaultCrouchs.Normal = new List<float>();
					defaultCrouchs.Hard = new List<float>();
					defaultCrouchs.Expert = new List<float>();
					defaultCrouchs.Master = new List<float>();
					defaultCrouchs.Custom = new List<float>();
					
					Serializer.ChartData.Crouchs = defaultCrouchs;
				}

				if(Serializer.ChartData.Slides == null) {
					Slides defaultSlides = new Slides();
					defaultSlides.Easy = new List<Slide>();
					defaultSlides.Normal = new List<Slide>();
					defaultSlides.Hard = new List<Slide>();
					defaultSlides.Expert = new List<Slide>();
					defaultSlides.Master = new List<Slide>();
					defaultSlides.Custom = new List<Slide>();

					Serializer.ChartData.Slides = defaultSlides;
				}

				if(Serializer.ChartData.Lights == null) {
					Lights defaultLights= new Lights();
					defaultLights.Easy = new List<float>();
					defaultLights.Normal = new List<float>();
					defaultLights.Hard = new List<float>();
					defaultLights.Expert = new List<float>();
					defaultLights.Master = new List<float>();
					defaultLights.Custom = new List<float>();
					
					Serializer.ChartData.Lights = defaultLights;
				}

				if(Serializer.ChartData.Bookmarks == null) { 
					Serializer.ChartData.Bookmarks = new Bookmarks();
				}

				InitFormsSelection(true);	
			}			
		}
    }

    IEnumerator LoadAudioTrack(string url, bool isEdit = false) {
		/* WWW m_get = new WWW(url);
			
		yield return m_get; */
		
		
		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
		{
			// ((DownloadHandlerAudioClip)www.downloadHandler).audioClip.
			yield return www.SendWebRequest();

			try {
				if (www.isNetworkError)
				{
					Debug.LogError("Problem opening audio, please check extension" + www.error);
				}
				else
				{
					Serializer.CurrentAudioFileToCompress = new System.Uri(url).LocalPath;
					// loadedClip = m_get.GetAudioClip();
					loadedClip = DownloadHandlerAudioClip.GetContent(www);
					if(loadedClip != null) {
						//_audioSource.clip = clip;
					
						//_audioSource.Play();
						audioData = new float[loadedClip.samples * loadedClip.channels];
						if(loadedClip.GetData(audioData, 0)) {
							if(isEdit) {
								editTrackField.text = System.IO.Path.GetFileName(new System.Uri(url).LocalPath);
							} else {
								trackField.text = System.IO.Path.GetFileName(new System.Uri(url).LocalPath);
							}					
							newAudioSelected = true;
							audioEdited = isEdit;
						} else {
							Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_AudioLoadError);
						}
						
						HidePreloader();
					} else {				
						Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_NoAudioSelected);
						HidePreloader();
					}
				}
			}			
			catch (Exception ex)
			{
				Debug.LogError("Problem opening audio, please check extension" + ex.Message);
				HidePreloader();
			} 
		}		
			
			         
	}

	IEnumerator LoadTrackArtwork(string url, bool isEdit = false) {
		/* WWW m_get = new WWW(url);
			
		yield return m_get; */

		using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.LogError("Problem opening image, please check extension" + uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                // var texture = DownloadHandlerTexture.GetContent(uwr);
				try {
					Serializer.AudioCoverToCompress = new System.Uri(url).LocalPath;
					// Texture2D selectedTexture = m_get.texture;
					Texture2D selectedTexture = DownloadHandlerTexture.GetContent(uwr);
					if(selectedTexture != null) {
						if(selectedTexture.width > 512 || selectedTexture.height > 512) {
							Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_CoverImageWrongSize);
						} else {
							loadedArtwork = Convert.ToBase64String(selectedTexture.EncodeToPNG());
							if(loadedArtwork != null) {
								artWorkField = System.IO.Path.GetFileName(new System.Uri(url).LocalPath);
								if(isEdit){ SetSpriteToImage(editArtworkField, loadedArtwork); }
								else { SetSpriteToImage(newArtworkField, loadedArtwork); }
								artworkEdited = isEdit;
							} 
						}
						
					} else {				
						Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_NoImageSelected);
					}
				}			
				catch (Exception ex)
				{
					Debug.LogError("Problem opening image, please check extension" + ex.Message);
				} 
            }
        }
	}

	void ShowPreloader(bool isEdit = false) {
		newAudioSelected = false;
		trackField.text = "";
		if(isEdit){
			selectButtonEdit.interactable = false;
			loopPreloaderEdit.SetActive(true);
		} else {
			selectButton.interactable = false;
			loopPreloader.SetActive(true);
		}
	}

	 void HidePreloader() {
		selectButton.interactable = true;
		loopPreloader.SetActive(false);
		selectButtonEdit.interactable = true;
		loopPreloaderEdit.SetActive(false);
	 }

	 void Update() {
		if(!newChartButton.interactable && NewChartFormIsValid()) {
			newChartButton.interactable = true;
		}

		if(newChartButton.interactable && !NewChartFormIsValid()) {
			newChartButton.interactable = false;
		}

		if(!editChartButton.interactable && EditChartFormIsValid()) {
			editChartButton.interactable = true;
		}

		if(editChartButton.interactable && !EditChartFormIsValid()) {
			editChartButton.interactable = false;
		}

		if(Input.GetKeyDown(KeyCode.Tab)) {
			if(currentField == authorField) {
				SetFieldFocus(nameField);
			} else if(currentField == nameField) {
				SetFieldFocus(mapperField);
			} else if(currentField == mapperField) {
				SetFieldFocus(authorField);
			} else if(currentField == editAuthorField) {
				SetFieldFocus(editNameField);			
			} else if(currentField == editNameField) {
				SetFieldFocus(editMapperField);
			} else if(currentField == editMapperField) {
				SetFieldFocus(editAuthorField);
			}
		}

		if(Input.GetKeyDown(KeyCode.F12)) {
			OpenBatchProccess();
		}

		if(Serializer.BachComplete && batchLoader.activeInHierarchy) {
			batchLoader.SetActive(false);
		}
	}

    private bool NewChartFormIsValid()
    {
		return (newAudioSelected && nameField.text.Length > 0 && authorField.text.Length > 0 && trackField.text.Length >0);
    }

	private bool EditChartFormIsValid()
    {
		return (editNameField.text.Length > 0 && editAuthorField.text.Length > 0 && editTrackField.text.Length >0);
    }

	public void InitFormsSelection(bool isEdit) {
		if(isEdit) {
			currentField = editAuthorField;
		} else {
			currentField = authorField;
		}

		StartCoroutine(DelayFocus(currentField));
	}

	IEnumerator DelayFocus(InputField field) {
		yield return new WaitForSeconds(0.5f);

		SetFieldFocus(field);
	}

	void SetFieldFocus(InputField field) {
		currentField = field;
		currentField.ActivateInputField();
	}

	/// <sumary>
	/// Instantiate the new Chart and load the editor scene
	/// </sumary>
	public void StartEditor(bool isEdit = false) {
		if(Serializer.Initialized) {
			if(Serializer.ChartData == null) {
				Chart chart = new Chart();
				Beats defaultBeats = new Beats();
				defaultBeats.Easy = new Dictionary<float, List<Note>>();
				defaultBeats.Normal = new Dictionary<float, List<Note>>();
				defaultBeats.Hard = new Dictionary<float, List<Note>>();
				defaultBeats.Expert = new Dictionary<float, List<Note>>();
				defaultBeats.Master = new Dictionary<float, List<Note>>();
				defaultBeats.Custom = new Dictionary<float, List<Note>>();

				Effects defaultEffects = new Effects();
				defaultEffects.Easy = new List<float>();
				defaultEffects.Normal = new List<float>();
				defaultEffects.Hard = new List<float>();
				defaultEffects.Expert = new List<float>();
				defaultEffects.Master = new List<float>();
				defaultEffects.Custom = new List<float>();

				Jumps defaultJumps = new Jumps();
				defaultJumps.Easy = new List<float>();
				defaultJumps.Normal = new List<float>();
				defaultJumps.Hard = new List<float>();
				defaultJumps.Expert = new List<float>();
				defaultJumps.Master = new List<float>();
				defaultJumps.Custom = new List<float>();

				Crouchs defaultCrouchs = new Crouchs();
				defaultCrouchs.Easy = new List<float>();
				defaultCrouchs.Normal = new List<float>();
				defaultCrouchs.Hard = new List<float>();
				defaultCrouchs.Expert = new List<float>();
				defaultCrouchs.Master = new List<float>();
				defaultCrouchs.Custom = new List<float>();

				Slides defaultSlides = new Slides();
				defaultSlides.Easy = new List<Slide>();
				defaultSlides.Normal = new List<Slide>();
				defaultSlides.Hard = new List<Slide>();
				defaultSlides.Expert = new List<Slide>();
				defaultSlides.Master = new List<Slide>();
				defaultSlides.Custom = new List<Slide>();

				Lights defaultLights= new Lights();
				defaultLights.Easy = new List<float>();
				defaultLights.Normal = new List<float>();
				defaultLights.Hard = new List<float>();
				defaultLights.Expert = new List<float>();
				defaultLights.Master = new List<float>();
				defaultLights.Custom = new List<float>();
				
				/// For testing				
				/*var list = new List<Note>();
				list.Add(new Note(new Vector3(-0.5756355f, 0.2400601f, 0), 1));
				defaultBeats.Easy.Add(0, list);
				list = new List<Note>();
				list.Add(new Note(new Vector3(-0.7826607f, 0.3006552f, 20f), 2002));
				defaultBeats.Easy.Add(2000, list);
				list = new List<Note>();
				list.Add(new Note(new Vector3(0.1514833f, 0.3359979f, 40f), 4001));
				defaultBeats.Easy.Add(4000, list);	*/			

				chart.Track = defaultBeats; 
				chart.Effects = defaultEffects;
				chart.Jumps = defaultJumps;
				chart.Crouchs = defaultCrouchs;
				chart.Slides = defaultSlides;
				chart.Bookmarks = new Bookmarks();
				chart.Name = nameField.text;
				chart.Author = authorField.text;
				chart.AudioName = trackField.text;
				chart.AudioData = null; //audioData;
				chart.AudioFrecuency = loadedClip.frequency;
				chart.AudioChannels = loadedClip.channels;
				chart.BPM = 120;
				chart.FilePath = null;
				chart.Artwork = artWorkField;
				chart.ArtworkBytes = loadedArtwork;
				chart.IsAdminOnly = Serializer.IsAdmin();
				chart.Beatmapper = mapperField.text;
				chart.CustomDifficultyName = "Custom";
				chart.CustomDifficultySpeed = 1;
				chart.Tags = new List<string>();
				chart.Lights = defaultLights;
				
				Serializer.ChartData = chart;
			}

			if(artworkEdited) {
				Serializer.ChartData.Artwork = artWorkField;
				Serializer.ChartData.ArtworkBytes = loadedArtwork;
			}

			if(audioEdited) {
				// Serializer.ChartData.AudioData = audioData;
				Serializer.ChartData.AudioFrecuency = loadedClip.frequency;
				Serializer.ChartData.AudioChannels = loadedClip.channels;
			}

			if(isEdit) {
				Serializer.ChartData.Name = editNameField.text;
				Serializer.ChartData.Author = editAuthorField.text;
				Serializer.ChartData.AudioName = editTrackField.text;
				Serializer.ChartData.Beatmapper = editMapperField.text;
			}
			// Complete editor process
			// Serializer.SerializeToFile();			
			Miku_LoaderHelper.LauchPreloader();
		}
	}

	/// <sumary>
	/// Clear the text fields
	/// </sumary>
	public void ClearFields() {
		// New chart files
		nameField.text = string.Empty;
		authorField.text = string.Empty;
		trackField.text = string.Empty;
		mapperField.text = string.Empty;

		// Edit chart files
		editNameField.text = string.Empty;
		editAuthorField.text = string.Empty;
		editTrackField.text = string.Empty;
		editMapperField.text = string.Empty;
		
		newAudioSelected = false;
		Serializer.ChartData = null;

		//
		loadedArtwork = null;
		artworkEdited = false;
		audioEdited = false;
		audioData = null;
		loadedClip = null;

		//
		currentField = null;
	}

	private void SetSpriteToImage(Image imageField, string SrpiteBase64) {
		Texture2D text = new Texture2D(1, 1);
		text.LoadImage(Convert.FromBase64String(SrpiteBase64));
		Sprite artWorkSprite = Sprite.Create(text, new Rect(0,0, text.width, text.height), new Vector2(0.5f, 0.5f));	
		imageField.sprite = artWorkSprite;
	}
}
