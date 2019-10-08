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
using Shogoki.Utils;
using ThirdParty.Custom;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Diagnostics;

namespace MiKu.NET {

    public class FrequencyData {
        public FrequencyData() {
            peakTimes = new List<TimeWrapper>();
            barTimes = new List<TimeWrapper>();
        }
        private TimeWrapper NextPeak(TimeWrapper time) {
            TimeWrapper result = time;
            peakTimes.Sort();
            var temp = peakTimes.SkipWhile(t => t <= time);
            if(temp.ToList().Count != 0) {
                result = temp.First();
            }
            return result;
        }
        private TimeWrapper PreviousPeak(TimeWrapper time) {
            TimeWrapper result = time;
            peakTimes.Sort();
            peakTimes.Reverse();
            var temp = peakTimes.SkipWhile(t => t >= time);
            if(temp.ToList().Count != 0) {
                result = temp.First();
            }
            return result;
        }
        public static TimeWrapper SnapToBar(FrequencyData data, float StartOffset, TimeWrapper time, Track.PlacerClickSnapMode mode = Track.PlacerClickSnapMode.MinorBar) {
            TimeWrapper result = 0;
            if(mode == Track.PlacerClickSnapMode.MinorBar) {
                data.barTimes.Sort();
                data.barTimes.Reverse();
                var temp = data.barTimes.SkipWhile(t => t + StartOffset > time);
                result = temp.First() + StartOffset;
            } else {
                data.peakTimes.Sort();
                data.peakTimes.Reverse();
                var temp = data.peakTimes.SkipWhile(t => t + StartOffset > time);
                if(temp.Count() > 0)
                    result = temp.First() + StartOffset;
                else
                    result = 0;
            }
            return result;
        }
        public List<TimeWrapper> peakTimes;
        public List<TimeWrapper> barTimes;
    }
    public class Spectrum {
        public int spc_numChannels;
        public int spc_numTotalSamples;
        public int spc_sampleRate;
        public float spc_clipLength;
        public float[] spc_multiChannelSamples;
        public bool isSpectrumGenerated = false;
        public SpectralFluxAnalyzer preProcessedSpectralFluxAnalyzer;
        public Transform plotTempInstance;

        public bool threadFinished = false;
        public bool treadWithError = false;
        public Thread analyzerThread;
        public const string spc_cacheFolder = "/SpectrumData/";
        public const string spc_ext = ".spectrum";
        public Vector3 spectrumDefaultPos;

        public void BeginSpectralAnalyzer(string AudioName, AudioSource audioSource, FrequencyData frequencyData) {
            if(preProcessedSpectralFluxAnalyzer == null) {
                if(IsSpectrumCached(AudioName)) {
                    try {
                        using(FileStream file = File.OpenRead(SpectrumCachePath + Serializer.CleanInput(AudioName + spc_ext))) {
                            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                            preProcessedSpectralFluxAnalyzer = (SpectralFluxAnalyzer)bf.Deserialize(file);
                        }

                        EndSpectralAnalyzer(AudioName, frequencyData);
                        Track.LogMessage("Spectrum loaded from cached");
                    } catch(Exception ex) {
                        Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert,
                            "Error while dezerializing Spectrum " + ex.ToString()
                        );
                        Track.LogMessage(ex.ToString(), true);
                    }

                } else {
                    preProcessedSpectralFluxAnalyzer = new SpectralFluxAnalyzer();

                    // Need all audio samples.  If in stereo, samples will return with left and right channels interweaved
                    // [L,R,L,R,L,R]
                    spc_multiChannelSamples = new float[audioSource.clip.samples * audioSource.clip.channels];
                    spc_numChannels = audioSource.clip.channels;
                    spc_numTotalSamples = audioSource.clip.samples;
                    spc_clipLength = audioSource.clip.length;

                    // We are not evaluating the audio as it is being played by Unity, so we need the clip's sampling rate
                    spc_sampleRate = audioSource.clip.frequency;

                    audioSource.clip.GetData(spc_multiChannelSamples, 0);
                    Track.LogMessage("GetData done");

                    analyzerThread = new Thread(this.getFullSpectrumThreaded);

                    Track.LogMessage("Starting Background Thread");
                    analyzerThread.Start();

                    Track.s_instance.ToggleWorkingStateAlertOn(StringVault.Info_SpectrumLoading);
                }
            }
        }


        public void EndSpectralAnalyzer(string AudioName, FrequencyData frequencyData) {
            if(treadWithError) {
                Track.LogMessage("Specturm could not be created", true);
                Track.s_instance.ToggleWorkingStateAlertOff();
                return;
            }

            List<SpectralFluxInfo> flux = preProcessedSpectralFluxAnalyzer.spectralFluxSamples;
            Vector3 targetTransform = Vector3.zero;
            Vector3 targetScale = Vector3.one;

            Transform childTransform;
            float msInSecond = Track.msInSecond;
            for(int i = 0; i < flux.Count; ++i) {
                SpectralFluxInfo spcInfo = flux[i];
                if(spcInfo.spectralFlux > 0) {
                    plotTempInstance = GameObject.Instantiate(
                        (spcInfo.isPeak) ? Track.s_instance.m_PeakPointMarker : Track.s_instance.m_NormalPointMarker
                    ).transform;
                    targetTransform.x = plotTempInstance.position.x;
                    targetTransform.y = plotTempInstance.position.y;
                    targetTransform.z = Track.MStoUnit((spcInfo.time * msInSecond)); //+StartOffset);
                    if(spcInfo.isPeak)
                        frequencyData.peakTimes.Add(spcInfo.time * msInSecond);
                    else
                        frequencyData.barTimes.Add(spcInfo.time * msInSecond);
                    plotTempInstance.position = targetTransform;
                    plotTempInstance.parent = Track.s_instance.m_SpectrumHolder;

                    childTransform = plotTempInstance.Find("Point - Model");
                    if(childTransform != null) {
                        targetScale = childTransform.localScale;
                        targetScale.y = spcInfo.spectralFlux * Track.s_instance.heightMultiplier;
                        childTransform.localScale = targetScale;
                    }

                    childTransform = plotTempInstance.Find("Point - Model Top");
                    if(childTransform != null) {
                        targetScale = childTransform.localScale;
                        targetScale.x = spcInfo.spectralFlux * Track.s_instance.heightMultiplier;
                        childTransform.localScale = targetScale;
                    }

                    //plotTempInstance.localScale = targetScale; 
                }

                /* if(spcInfo.spectralFlux > 0) {
                    LogMessage ("Time is "+spcInfo.time+" at index "+i+" flux: "+(spcInfo.spectralFlux * 0.01f));
                    return;
                } */
            }

            Track.s_instance.ToggleWorkingStateAlertOff();
            isSpectrumGenerated = true;
            UpdateSpectrumOffset();

            if(!IsSpectrumCached(AudioName)) {
                try {
                    using(FileStream file = File.Create(SpectrumCachePath + Serializer.CleanInput(AudioName + spc_ext))) {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        bf.Serialize(file, preProcessedSpectralFluxAnalyzer);
                    }
                } catch {
                    Track.LogMessage("There was a error while creating the specturm file", true);
                }
            }
        }

        public int getIndexFromTime(float curTime) {
            float lengthPerSample = spc_clipLength / (float)spc_numTotalSamples;

            return Mathf.FloorToInt(curTime / lengthPerSample);
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
                for(int i = 0; i < spc_multiChannelSamples.Length; i++) {
                    combinedChannelAverage += spc_multiChannelSamples[i];

                    // Each time we have processed all channels samples for a point in time, we will store the average of the channels combined
                    if((i + 1) % spc_numChannels == 0) {
                        preProcessedSamples[numProcessed] = combinedChannelAverage / spc_numChannels;
                        numProcessed++;
                        combinedChannelAverage = 0f;
                    }
                }

                UnityEngine.Debug.Log("Combine Channels done");
                UnityEngine.Debug.Log(preProcessedSamples.Length.ToString());

                // Once we have our audio sample data prepared, we can execute an FFT to return the spectrum data over the time domain
                int spectrumSampleSize = 1024;
                int iterations = preProcessedSamples.Length / spectrumSampleSize;

                FFT fft = new FFT();
                fft.Initialize((UInt32)spectrumSampleSize);

                UnityEngine.Debug.Log(string.Format("Processing {0} time domain samples for FFT", iterations));
                double[] sampleChunk = new double[spectrumSampleSize];
                for(int i = 0; i < iterations; i++) {
                    // Grab the current 1024 chunk of audio sample data
                    Array.Copy(preProcessedSamples, i * spectrumSampleSize, sampleChunk, 0, spectrumSampleSize);

                    // Apply our chosen FFT Window
                    double[] windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.Hanning, (uint)spectrumSampleSize);
                    double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);
                    double scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

                    // Perform the FFT and convert output (complex numbers) to Magnitude
                    Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
                    double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
                    scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

                    // These 1024 magnitude values correspond (roughly) to a single point in the audio timeline
                    float curSongTime = getTimeFromIndex(i) * spectrumSampleSize;

                    // Send our magnitude data off to our Spectral Flux Analyzer to be analyzed for peaks
                    preProcessedSpectralFluxAnalyzer.analyzeSpectrum(Array.ConvertAll(scaledFFTSpectrum, x => (float)x), curSongTime);
                }

                UnityEngine.Debug.Log("Spectrum Analysis done");
                UnityEngine.Debug.Log("Background Thread Completed");

                threadFinished = true;
            } catch(Exception e) {
                threadFinished = true;
                treadWithError = true;
                // Catch exceptions here since the background thread won't always surface the exception to the main thread
                UnityEngine.Debug.LogError(e.ToString());
                Serializer.WriteToLogFile("getFullSpectrumThreaded Error");
                Serializer.WriteToLogFile(e.ToString());
            }
        }

        public string SpectrumCachePath
        {
            get
            {
                /*if(Application.isEditor) {
                    return Application.dataPath+"/../"+save_path;
                } else {
                    return Application.persistentDataPath+save_path;
                }   */
                return Application.dataPath + "/../" + spc_cacheFolder;
            }
        }

        public bool IsSpectrumCached(string AudioName) {
            if(!Directory.Exists(SpectrumCachePath)) {
                DirectoryInfo dir = Directory.CreateDirectory(SpectrumCachePath);
                dir.Attributes |= FileAttributes.Hidden;

                return false;
            }

            if(File.Exists(SpectrumCachePath + Serializer.CleanInput(AudioName + spc_ext))) {
                return true;
            }

            return false;
        }

        public void UpdateSpectrumOffset() {
            if(isSpectrumGenerated) {
                if(spectrumDefaultPos == null) {
                    spectrumDefaultPos = new Vector3(
                        Track.s_instance.m_SpectrumHolder.transform.position.x,
                        Track.s_instance.m_SpectrumHolder.transform.position.y,
                        0
                    );
                }

                Track.s_instance.m_SpectrumHolder.transform.position = new Vector3(
                    spectrumDefaultPos.x,
                    spectrumDefaultPos.y,
                    spectrumDefaultPos.z + Track.MStoUnit(Track.s_instance.StartOffset)
                );
            }
        }
    }
}