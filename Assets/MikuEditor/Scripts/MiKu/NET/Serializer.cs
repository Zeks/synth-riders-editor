using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MiKu.NET.Charting;
using UnityEngine;
using Shogoki.Utils;
using System.Threading;
using Ionic.Zip;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using Newtonsoft.Json;

namespace MiKu.NET {
    /// <sumary>
    /// Operations of serailazation of the .synth files
    /// </sumary>
    public class Serializer : MonoBehaviour {

        private static Serializer s_instance;

        public bool IsAdminMode = true;

        // Chart created if new or loaded
        public static Chart ChartData { get; set; }

        private const string save_path = "/CustomSongs/";
        private const string temp_path = "/temp/";
        private const string file_ext = "synth";

        public const string meta_field_name = "beatmap.meta.bin";
        public const string data_field_name = "track.data.json";

        private bool initialized = false;

        public static bool IsBusy { get; set; }

        private bool threadFinished = false;

        private string PathToSave;
        public static bool ClipExtratedComplete { set; get; }
        public static bool IsExtratingClip { get; set; }

        public static string CurrentAudioFileToCompress { get; set;}
        public static string AudioCoverToCompress { get; set; }

        public static AudioClip ExtractedClip { get; set; }

        public static bool BachComplete { get; set; }

        // Use this for initialization
        void Start () {
            if(s_instance != null) {
                DestroyImmediate(this.gameObject);
                return;
            }

            this.transform.parent = null;
            s_instance = this;
            initialized = true;
            DontDestroyOnLoad(this.gameObject);
        }

        /// <sumary>
        /// To know if the class is initalized, Allways call this function before try to
        /// access another of the class methods
        /// </sumary>
        public static bool Initialized {
            get {
                return (s_instance != null ) ? s_instance.initialized : false;
            }
        }

        /// <sumary>
        /// Directory on where the custom chart will be saved/loaded 
        /// </sumary>
        public static string CHART_SAVE_PATH
        {
            get
            {
                /*if(Application.isEditor) {
                    return Application.dataPath+"/../"+save_path;
                } else {
                    return Application.persistentDataPath+save_path;
                }   */
                return Application.dataPath+"/../"+save_path;             
            }
        }

        /// <sumary>
        /// Directory on where the custom chart will be saved/loaded 
        /// </sumary>
        public static string CHART_TEMP_PATH
        {
            get
            {
                /*if(Application.isEditor) {
                    return Application.dataPath+"/../"+save_path;
                } else {
                    return Application.persistentDataPath+save_path;
                }   */
                return Application.dataPath+temp_path;             
            }
        }

        /// <sumary>
        /// Extension of the custom chart
        /// </sumary>
        public static string CHART_FILE_EXT
        {
            get
            {
                return file_ext;
            }
        }

        /// <sumary>
        /// To sanitaze the file name before the serialization proccess
        /// </sumary>
        public static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try {
                return Regex.Replace(strIn, @"[^\w\.@-]", "", RegexOptions.None); 
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (Exception) {
                return String.Empty;   
            }
        }

        /// <sumary>
        /// Serialize Chart Data to disk
        /// </sumary>
        public static bool SerializeToFile(string pathToSave = null)
        {
            if(s_instance == null) {
                Debug.LogError("Serializer class not initialized");
                return false;
            }

            if(IsBusy) return false;

            IsBusy = true;

            try {

                s_instance.PathToSave = pathToSave;
                if(pathToSave == null || pathToSave.Equals(string.Empty)) {
                    // If the [CHART_SAVE_PATH] directory does not exist we created it
                    // TODO Selecte Save folder
                    if (!Directory.Exists(CHART_SAVE_PATH)) {
                        Directory.CreateDirectory(CHART_SAVE_PATH);
                    }
                    
                    s_instance.PathToSave = string.Format("{0}{1}.{2}",
                        CHART_SAVE_PATH,
                        CleanInput(ChartData.Name),
                        CHART_FILE_EXT
                    );

                    ChartData.FilePath = s_instance.PathToSave;				
                } 

                
                string destination = s_instance.PathToSave;
                // Debug.Log("Destination "+destination);
                bool isUpdate = false;
                if(File.Exists(destination)){ 
                    if(ChartData.AudioData == null) {
                        isUpdate = true;
                    } else {
                        Debug.Log("deleting "+destination);
                        File.Delete(destination);
                    }				
                }

                if(CurrentAudioFileToCompress != null && !CurrentAudioFileToCompress.Equals(string.Empty)) {
                    var audioEXT = CurrentAudioFileToCompress != null && !CurrentAudioFileToCompress.Equals(string.Empty) ? Path.GetExtension(@CurrentAudioFileToCompress) : Path.GetExtension(ChartData.AudioName);
                    ChartData.AudioData = null;
                    ChartData.AudioName = string.Format(
                        "{0}{1}",
                        CleanInput(ChartData.Name),
                        audioEXT
                    );
                }


                // Deprecated, now using JSON
                /* MemoryStream memStream = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(memStream, ChartData);
                memStream.Seek(0, SeekOrigin.Begin); */
                
                // using (memStream)
                {			
                    if(isUpdate) {
                        using (ZipFile zip = ZipFile.Read(destination))
                        {	
                            // zip.UpdateEntry(meta_field_name, memStream);
                            zip.UpdateEntry(meta_field_name, JsonConvert.SerializeObject(ChartData, Formatting.Indented));
                            if(CurrentAudioFileToCompress != null && !CurrentAudioFileToCompress.Equals(string.Empty)) {
                                ZipEntry toDelete = null;
                                //foreach (ZipEntry e in zip.Where(x => !x.FileName.EndsWith(".bin")))
                                foreach (ZipEntry e in zip.Where(x => x.FileName.EndsWith(".ogg") || x.FileName.EndsWith(".wav")))
                                {
                                    toDelete = e;								
                                }
                                if(toDelete != null){	
                                    zip.RemoveEntry(toDelete);
                                }						
                                // zip.AddFile(@CurrentAudioFileToCompress, "");
                                zip.AddEntry(
                                    ChartData.AudioName,
                                    File.ReadAllBytes(@CurrentAudioFileToCompress)
                                );
                            }		

                            if(AudioCoverToCompress != null && !AudioCoverToCompress.Equals(string.Empty)) {
                                ZipEntry toDelete = null;
                                //foreach (ZipEntry e in zip.Where(x => !x.FileName.EndsWith(".bin")))
                                foreach (ZipEntry e in zip.Where(x => x.FileName.EndsWith(".jpg") || x.FileName.EndsWith(".png")))
                                {
                                    toDelete = e;								
                                }
                                if(toDelete != null){	
                                    zip.RemoveEntry(toDelete);
                                }						
                                zip.AddFile(@AudioCoverToCompress, "");							
                            }	

                            ZipEntry jsonToDelete = null;
                            foreach (ZipEntry e in zip.Where(x => x.FileName.EndsWith(".json")))
                            {
                                jsonToDelete = e;								
                            }

                            if(jsonToDelete != null){	
                                zip.RemoveEntry(jsonToDelete);
                            }	
                            
                            zip.AddEntry(data_field_name, Track.TrackInfo.SaveToJSON(), ASCIIEncoding.Unicode);			
                            zip.Save();
                        }
                    } else {
                        using (ZipFile zip = new ZipFile(Encoding.UTF8))
                        {	
                            // zip.AddEntry(meta_field_name, memStream);
                            zip.AddEntry(meta_field_name, JsonConvert.SerializeObject(ChartData, Formatting.Indented));
                            // zip.AddFile(@CurrentAudioFileToCompress, "");
                            zip.AddEntry(
                                ChartData.AudioName,
                                File.ReadAllBytes(@CurrentAudioFileToCompress)
                            );
                            
                            if(AudioCoverToCompress != null && !AudioCoverToCompress.Equals(string.Empty)) {
                                zip.AddFile(@AudioCoverToCompress, "");
                            }
                            zip.AddEntry(data_field_name, Track.TrackInfo.SaveToJSON(), ASCIIEncoding.Unicode);	
                            zip.Save(destination);
                        }					
                    }
                    
                }

                CurrentAudioFileToCompress = null;
            } catch(Exception e) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                    "There was error saving the song file, please check if the target folder is not write protected\n"+
                    "or the file is not being use by other program\n"+
                    e.ToString(),
                    true
                );

                WriteToLogFile(e.ToString());
                IsBusy = false;
                return false;
            }			

            IsBusy = false;
            return true;
        }

        /// <sumary>
        /// Deserialize the Chart Data from disk
        /// </sumary>
        public static bool LoadFronFile(string filePath) {
            if(s_instance == null) {
                Debug.LogError("Serializer class not initialized");
                return false;
            }

            if(IsBusy) return false;

            IsBusy = true;			
    
            if(!File.Exists(filePath)) 
            {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadError);
                IsBusy = false;
                return false;
            }

            try {
                MemoryStream memStream = new MemoryStream();

                try {
                    using (ZipFile zip = ZipFile.Read(filePath))
                    {
                        ZipEntry e = zip[meta_field_name];					
                        e.Extract(memStream);
                    }	
                    memStream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader( memStream );
                    string jsonDATA = reader.ReadToEnd();
                    ChartData = JsonConvert.DeserializeObject<Chart>(jsonDATA);
                } catch(Exception) {
                    Debug.Log("File made in version previous to 1.8, trying BinaryFormatter");
                    // Section for load of files previos to version 1.8					
                    using (ZipFile zip = ZipFile.Read(filePath))
                    {
                        ZipEntry e = zip[meta_field_name];					
                        e.Extract(memStream);
                    }		
                    memStream.Seek(0, SeekOrigin.Begin);
                    
                    BinaryFormatter bf = new BinaryFormatter();
                    ChartData = (Chart) bf.Deserialize(memStream);	
                }

            } catch(Exception) {
                // Section for very old Synth Files
                try {
                    FileStream file = File.OpenRead(filePath);
                    BinaryFormatter bf = new BinaryFormatter();
                    ChartData = (Chart) bf.Deserialize(file);
                    file.Close();
                } catch(Exception e) {
                    Debug.LogError("Deserialization Error");
                    Debug.LogError(e);
                    Serializer.WriteToLogFile("Deserialization Error");
                    Serializer.WriteToLogFile(e.ToString());

                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadNotAdmin);
                    ChartData = null;
                    IsBusy = false;
                    return false;
                }
                
            }
            
            if(ChartData.IsAdminOnly && !s_instance.IsAdminMode) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadNotAdmin);
                    ChartData = null;
                IsBusy = false;
                return false;
            }

            IsBusy = false;
            return true;
        }	

        /// <sumary>
        /// Deserialize the Chart Data from JSON file
        /// </sumary>
        public static bool LoadFronFile(string filePath, bool isJSON, bool isBeatSong = false) {
            if(s_instance == null) {
                Debug.LogError("Serializer class not initialized");
                return false;
            }

            if(IsBusy) return false;

            IsBusy = true;			
    
            if(!File.Exists(filePath)) 
            {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadError);
                IsBusy = false;
                return false;
            }

            try {
                Chart tmp= isBeatSong ? BeatSynthConverter.Convert(filePath) : JsonConvert.DeserializeObject<Chart>(File.ReadAllText(filePath));
                   ChartData = tmp;
                ChartData.AudioName = null;
                ChartData.FilePath = string.Empty;
            } catch(Exception e) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadError);
                Debug.Log(e);
                IsBusy = false;
                return false;				
            }
            
            if(ChartData.IsAdminOnly && !s_instance.IsAdminMode) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadNotAdmin);
                ChartData = null;
                IsBusy = false;
                return false;
            }

            IsBusy = false;
            return true;
        }

        public static bool SerializeToJSON()
        {
            if(s_instance == null) {
                Debug.LogError("Serializer class not initialized");
                return false;
            }

            if(IsBusy) return false;

            IsBusy = true;

            try {
                if (!Directory.Exists(CHART_SAVE_PATH)) {
                    Directory.CreateDirectory(CHART_SAVE_PATH);
                }
                
                s_instance.PathToSave = string.Format("{0}{1}.{2}",
                    CHART_SAVE_PATH,
                    CleanInput(ChartData.Name),
                    "json"
                ); 
                
                string destination = s_instance.PathToSave;
                File.WriteAllText(s_instance.PathToSave, JsonConvert.SerializeObject(ChartData, Formatting.Indented));	
            } catch(Exception e) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                    "There was error exporting the song data, please check if the target folder is not write protected\n"+
                    "or the file is not being use by other program\n"+
                    e.ToString(),
                    true
                );

                WriteToLogFile(e.ToString());
                IsBusy = false;
                return false;
            }

            IsBusy = false;
            return true;
        }	

        /// <sumary>
        /// Method for the bach conver of SynthFiles of Binaryformater to JSON
        /// </sumary>
        public static void BachProcess(string dirPath) {
            if(s_instance == null) {
                Debug.LogError("Serializer class not initialized");
                return;
            }

            if(!Directory.Exists(dirPath)) 
            {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_FileLoadError);
                return;
            }
            
            BachComplete = false;
            s_instance.StartCoroutine(s_instance.BatchProccesser(dirPath));
        }	

        // TODO change this to background thread
        WaitForSeconds batchWait = new WaitForSeconds(0.3f);
        private IEnumerator BatchProccesser(string dirPath) {
            // Get all the SynthFiles of the direrctory
            string[] synthFiles = Directory.GetFiles(@dirPath, "*.synth");

            WriteToLogFile("Starting batch converter at "+dirPath);		

            int cont = 0;
            bool complete = false;
            while(!complete) {
                foreach (string synthFile in synthFiles) 
                {
                    cont += 1;
                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                        string.Format("Converting {0} of {1}", cont, synthFiles.Length)
                    );

                    try {
                        MemoryStream memStream = new MemoryStream();
                        // Section for load of files previos to version 1.8					
                        using (ZipFile zip = ZipFile.Read(synthFile))
                        {
                            ZipEntry e = zip[meta_field_name];					
                            e.Extract(memStream);
                        }		
                        memStream.Seek(0, SeekOrigin.Begin);
                        
                        BinaryFormatter bf = new BinaryFormatter();
                        Chart data = (Chart) bf.Deserialize(memStream);	

                        using (ZipFile zip = ZipFile.Read(synthFile))
                        {	
                            zip.UpdateEntry(meta_field_name, JsonConvert.SerializeObject(data, Formatting.Indented));								
                            zip.Save();
                        }
                    } catch(Exception e) {
                        Debug.Log("File not in compatible BinaryFormtter or already converted");		
                        WriteToLogFile("Bach error "+e.ToString());		
                    }
                    yield return batchWait;
                }
                complete = true;
            }				
            
            Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, "Batch converter complete!");
            WriteToLogFile("batch converter complete");
            BachComplete = true;
        }

        public static void WriteToLogFile(string msg) {
            try {
                string LogFile = Application.dataPath+"/../ErrorLog.txt";
                if(File.Exists(LogFile)) {
                    File.AppendAllText(LogFile, string.Format("{0}: {1}\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), msg));
                } else {
                    File.WriteAllText(LogFile, string.Format("{0}: {1}\n", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"), msg));
                }			
                File.AppendAllText(LogFile, Environment.NewLine);
            } catch(Exception e) {
                Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, 
                    "There was error saving the log file\n"+
                    e.ToString()
                );
            }
        }

        public static bool IsAdmin() {
            return (s_instance == null) ? false : s_instance.IsAdminMode;
        }

        /// <sumary>
        /// Get Audio Clip from ZIP
        /// </sumary>
        public static void GetAudioClipFromZip(string zipPath, string fileName,  Action<AudioClip> callback = null) {
            IsExtratingClip = true;
            ClipExtratedComplete = false;

            string extractedClip = fileName;
            bool deleteAfter = false;
            if(!zipPath.Equals(string.Empty) && File.Exists(zipPath)) {
                if (!Directory.Exists(CHART_TEMP_PATH))
                    Directory.CreateDirectory(CHART_TEMP_PATH);

                using (ZipFile zip = ZipFile.Read(zipPath))
                {
                    foreach (ZipEntry e in zip.Where(x => x.FileName.EndsWith(".ogg") || x.FileName.EndsWith(".wav")))
                    {
                        e.Extract(CHART_TEMP_PATH, ExtractExistingFileAction.OverwriteSilently);
                        extractedClip = CHART_TEMP_PATH+e.FileName;
                    }					
                }

                deleteAfter = true;
            }
            s_instance.StartCoroutine(s_instance.GetAudioClip(@extractedClip, deleteAfter, callback));
        }

        IEnumerator GetAudioClip(string url, bool deleteAfterExtract = false, Action<AudioClip> callback = null)
        {
            using (WWW www = new WWW(url))
            {
                yield return www;	

                try {
                    ExtractedClip = www.GetAudioClip(false);					
                }			
                catch (Exception ex)
                {
                    Debug.LogError("Problem opening audio, please check extension" + ex.Message);
                    Serializer.WriteToLogFile("GetAudioClip Serializer");
                    Serializer.WriteToLogFile(ex.ToString());
                } 			
            }

            // Delete the extrated audio after instatiating the clip
            if(deleteAfterExtract && File.Exists(url)){ 
                File.Delete(url);
            }

            IsExtratingClip = false;
            ClipExtratedComplete = true;
            if(callback != null) {
                callback(ExtractedClip);
            }
        }		
    }
}
