using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MiKu.NET.Charting;

public static class BeatSynthConverter
{
    private static float sqrttwo = (float)Math.Sqrt(2);

    public static Chart Convert(string path, int distancex = 50, int distancey = 35, int offsety = 10, int dynamic = 20, int repeat = 200)
    {
        path = path.Substring(0, path.LastIndexOf(@"\"));

        Chart synthSong = new Chart();

        if (File.Exists(path + @"\info.dat"))
        {
            //load song info
            string infoStr = "";
            using (StreamReader streamReader = new StreamReader(path + @"\info.dat"))
            {
                infoStr = streamReader.ReadToEnd();
            }
            //deserialize song info
            Info info = JsonConvert.DeserializeObject<Info>(infoStr);

            synthSong.Name = (info._songName != "") ? info._songName : "N/A";
            synthSong.Author = (info._songAuthorName != "") ? info._songAuthorName : "N/A";
            synthSong.BPM = info._beatsPerMinute;
            synthSong.Offset = (info._songTimeOffset < 0) ? info._songTimeOffset : synthSong.Offset = 0;
            synthSong.Beatmapper = (info._levelAuthorName != "") ? info._levelAuthorName : "N/A";

            synthSong.Artwork = "Default Artwork";
            synthSong.ArtworkBytes = null;
            synthSong.AudioName = "";
            synthSong.AudioData = null;
            synthSong.AudioFrecuency = 0;
            synthSong.AudioChannels = 2;
            synthSong.EditorVersion = "1.8";
            synthSong.IsAdminOnly = false;

            for (int i = 0; i < info._difficultyBeatmapSets[0]._difficultyBeatmaps.Count && i < 5; i++)
            {
                if (File.Exists(path + @"\" + info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._beatmapFilename))
                {
                    //load song difficulty
                    string beatSongStr = "";
                    using (StreamReader streamReader = new StreamReader(path + @"\" + info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._beatmapFilename))
                    {
                        beatSongStr = streamReader.ReadToEnd();
                    }

                    //deserialize song difficulty
                    BeatSong beatSong = JsonConvert.DeserializeObject<BeatSong>(beatSongStr);

                    //convert notes
                    Dictionary<float, List<Note>> track = new Dictionary<float, List<Note>>();
                    for (int j = 0; j < beatSong._notes.Count; j++)
                    {
                        int time = (int)Math.Round(beatSong._notes[j]._time * 60000f / info._beatsPerMinute);

                        float y_offset = 0;
                        float _offset = 0;
                        switch (beatSong._notes[j]._cutDirection)
                        {
                            case 0: y_offset += dynamic * 0.01f; break;//up
                            case 1: y_offset -= dynamic * 0.01f; break;//down
                            case 2: _offset += dynamic * 0.01f; break;//left
                            case 3: _offset -= dynamic * 0.01f; break;//right
                            case 4: y_offset += (dynamic / sqrttwo) * 0.01f; _offset += (dynamic / sqrttwo) * 0.01f; break;//up_left
                            case 5: y_offset += (dynamic / sqrttwo) * 0.01f; _offset -= (dynamic / sqrttwo) * 0.01f; break;//up_right
                            case 6: y_offset -= (dynamic / sqrttwo) * 0.01f; _offset += (dynamic / sqrttwo) * 0.01f; break;//down_left
                            case 7: y_offset -= (dynamic / sqrttwo) * 0.01f; _offset -= (dynamic / sqrttwo) * 0.01f; break;//down_right
                        }

                        Vector3 pos = new Vector3(_offset + (distancex / 150f) * (beatSong._notes[j]._lineIndex - 1.5f), y_offset + (distancey / 100f) * (beatSong._notes[j]._lineLayer - 1f) + offsety * 0.01f, (time) * 0.02f);

                        Note.NoteType type;
                        if (beatSong._notes[j]._type == 0)
                        {
                            type = Note.NoteType.LeftHanded;
                        }
                        else if (beatSong._notes[j]._type == 1)
                        {
                            type = Note.NoteType.RightHanded;
                        }
                        else//bombs
                        {
                            continue;
                        }

                        string id = i.ToString() + "," + j.ToString();

                        Note note = new Note(pos, id,-1, type);

                        if (!track.ContainsKey(time))
                        {
                            List<Note> notes = new List<Note>();
                            notes.Add(note);

                            track.Add(time, notes);
                        }
                        else
                        {
                            track[time].Add(note);
                        }
                    }

                    //convert obstacles
                    List<float> crouches = new List<float>();
                    List<Slide> slides = new List<Slide>();
                    for (int j = 0; j < beatSong._obstacles.Count; j++)
                    {
                        int time = (int)Math.Round(beatSong._obstacles[j]._time * 60000f / info._beatsPerMinute);
                        int duration = (int)Math.Round(beatSong._obstacles[j]._duration * 60000f / info._beatsPerMinute);

                        for (int n = time; n < time + duration; n += repeat)
                        {
                            if (beatSong._obstacles[j]._type == 1)
                            {
                                crouches.Add(n);
                            }
                            else
                            {
                                Slide slide = new Slide();
                                slide.time = n;
                                if (beatSong._obstacles[j]._width == 1)
                                {
                                    if (beatSong._obstacles[j]._lineIndex == 0)//one on the left
                                    {
                                        slide.slideType = (Note.NoteType)4;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 3)//one on the right
                                    {
                                        slide.slideType = (Note.NoteType)2;
                                    }
                                    else//one middle right or middle left
                                    {
                                        slide.slideType = (Note.NoteType)3;
                                    }
                                    slides.Add(slide);
                                }
                                else if (beatSong._obstacles[j]._width == 2)
                                {
                                    if (beatSong._obstacles[j]._lineIndex == 0)//two on the left
                                    {
                                        slide.slideType = (Note.NoteType)1;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 1)//two in the middle
                                    {
                                        slide.slideType = (Note.NoteType)3;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 2)//two on the right
                                    {
                                        slide.slideType = (Note.NoteType)0;
                                    }
                                    slides.Add(slide);
                                }
                                else
                                {
                                    crouches.Add(n);
                                }
                            }
                        }
                    }

                    if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Expert"))
                    {
                        synthSong.Track.Expert = track;
                        synthSong.Crouchs.Expert = crouches;
                        synthSong.Slides.Expert = slides;
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Hard"))
                    {
                        synthSong.Track.Hard = track;
                        synthSong.Crouchs.Hard = crouches;
                        synthSong.Slides.Hard = slides;
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Normal"))
                    {
                        synthSong.Track.Normal = track;
                        synthSong.Crouchs.Normal = crouches;
                        synthSong.Slides.Normal = slides;
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Easy"))
                    {
                        synthSong.Track.Easy = track;
                        synthSong.Crouchs.Easy = crouches;
                        synthSong.Slides.Easy = slides;
                    }
                    else
                    {
                        synthSong.Track.Master = track;
                        synthSong.Crouchs.Master = crouches;
                        synthSong.Slides.Master = slides;
                    }
                }
            }

            string[] filePaths = Directory.GetFiles(path);

            foreach (string filePath in filePaths)
            {
                if (filePath.Contains(".egg"))//replace the .egg file ending with .ogg
                {
                    string tmp = filePath.Substring(0, filePath.IndexOf(".egg")) + ".ogg";
                    if (!File.Exists(tmp))
                    {
                        File.Copy(filePath, tmp);
                    }
                }
            }
        }
        return synthSong;
    }
}
