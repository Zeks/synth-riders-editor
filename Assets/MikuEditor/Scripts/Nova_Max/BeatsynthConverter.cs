using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class BeatsynthConverter
{
    private static float sqrttwo = (float)Math.Sqrt(2);

    public static string Convert(string path, int distancex = 50, int distancey = 35, int offsety = 10, int dynamic = 20, int repeat = 200)
    {
        string result="";
        path = path.Substring(0, path.LastIndexOf(@"\"));

        if (File.Exists(path + "\\info.dat"))
        {
            //load song info
            string infoStr = "";
            using (StreamReader streamReader = new StreamReader(path + "\\info.dat"))
            {
                infoStr = streamReader.ReadToEnd();
            }
            //deserialize song info
            Info info = JsonConvert.DeserializeObject<Info>(infoStr);

            //initialize SynthSong with parameters
            SynthSong synthSong = new SynthSong();


            synthSong.Name = (info._songName != "") ? info._songName : "N/A";
            synthSong.Author = (info._songAuthorName != "") ? info._songAuthorName : "N/A";
            synthSong.BPM = info._beatsPerMinute;
            synthSong.Offset = (info._songTimeOffset < 0) ? info._songTimeOffset : synthSong.Offset = 0;
            synthSong.Beatmapper = (info._levelAuthorName != "") ? info._levelAuthorName : "N/A";

            for (int i = 0; i < info._difficultyBeatmapSets[0]._difficultyBeatmaps.Count && i < 5; i++)
            {
                if (File.Exists(path + "\\" + info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._beatmapFilename))
                {
                    //load song difficulty
                    string beatSongStr = "";
                    using (StreamReader streamReader = new StreamReader(path + "\\" + info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._beatmapFilename))
                    {
                        beatSongStr = streamReader.ReadToEnd();
                    }

                    //deserialize song difficulty
                    BeatSong beatSong = JsonConvert.DeserializeObject<BeatSong>(beatSongStr);

                    string difficulty = "Master";

                    if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Expert"))
                    {
                        difficulty = "Expert";
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Hard"))
                    {
                        difficulty = "Hard";
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Normal"))
                    {
                        difficulty = "Normal";
                    }
                    else if (info._difficultyBeatmapSets[0]._difficultyBeatmaps[i]._difficulty.Equals("Easy"))
                    {
                        difficulty = "Easy";
                    }

                    //convert notes
                    for (int j = 0; j < beatSong._notes.Count; j++)
                    {
                        SynthNote note = new SynthNote();
                        note.Id = i.ToString() + "," + j.ToString();

                        int time = (int)Math.Round(beatSong._notes[j]._time * 60000f / info._beatsPerMinute);

                        float x = (distancex / 150f) * (beatSong._notes[j]._lineIndex - 1.5f);
                        float y = (distancey / 100f) * (beatSong._notes[j]._lineLayer - 1f) + offsety * 0.01f;
                        float z = (time) * 0.02f;

                        switch (beatSong._notes[j]._cutDirection)
                        {
                            case 0: y += dynamic * 0.01f; break;//up
                            case 1: y -= dynamic * 0.01f; break;//down
                            case 2: x += dynamic * 0.01f; break;//left
                            case 3: x -= dynamic * 0.01f; break;//right
                            case 4: y += (dynamic / sqrttwo) * 0.01f; x += (dynamic / sqrttwo) * 0.01f; break;//up_left
                            case 5: y += (dynamic / sqrttwo) * 0.01f; x -= (dynamic / sqrttwo) * 0.01f; break;//up_right
                            case 6: y -= (dynamic / sqrttwo) * 0.01f; x += (dynamic / sqrttwo) * 0.01f; break;//down_left
                            case 7: y -= (dynamic / sqrttwo) * 0.01f; x -= (dynamic / sqrttwo) * 0.01f; break;//down_right
                        }

                        note.Position = new float[3] { x, y, z };

                        if (beatSong._notes[j]._type == 0)
                        {
                            note.Type = 1;
                        }
                        else if (beatSong._notes[j]._type == 1)
                        {
                            note.Type = 0;
                        }
                        else//bombs
                        {
                            continue;
                        }

                        string key = time.ToString();

                        if (!synthSong.Track[difficulty].ContainsKey(key))
                        {
                            List<SynthNote> notes = new List<SynthNote>();
                            notes.Add(note);

                            synthSong.Track[difficulty].Add(key, notes);
                        }
                        else
                        {
                            synthSong.Track[difficulty][key].Add(note);
                        }
                    }

                    //convert obstacles
                    for (int j = 0; j < beatSong._obstacles.Count; j++)
                    {
                        int time = (int)Math.Round(beatSong._obstacles[j]._time * 60000f / info._beatsPerMinute);
                        int duration = (int)Math.Round(beatSong._obstacles[j]._duration * 60000f / info._beatsPerMinute);

                        for (int n = time; n < time + duration; n += repeat)
                        {
                            if (beatSong._obstacles[j]._type == 1)
                            {
                                synthSong.Crouchs[difficulty].Add(n);
                            }
                            else
                            {
                                CSlide slide = new CSlide();
                                slide.time = n;
                                if (beatSong._obstacles[j]._width == 1)
                                {
                                    if (beatSong._obstacles[j]._lineIndex == 0)//one on the left
                                    {
                                        slide.SlideType = 4;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 3)//one on the right
                                    {
                                        slide.SlideType = 2;
                                    }
                                    else//one middle right or middle left
                                    {
                                        slide.SlideType = 3;
                                    }
                                    synthSong.Slides[difficulty].Add(slide);
                                }
                                else if (beatSong._obstacles[j]._width == 2)
                                {
                                    if (beatSong._obstacles[j]._lineIndex == 0)//two on the left
                                    {
                                        slide.SlideType = 1;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 1)//two in the middle
                                    {
                                        slide.SlideType = 3;
                                    }
                                    else if (beatSong._obstacles[j]._lineIndex == 2)//two on the right
                                    {
                                        slide.SlideType = 0;
                                    }
                                    synthSong.Slides[difficulty].Add(slide);
                                }
                                else
                                {
                                    synthSong.Crouchs[difficulty].Add(n);
                                }
                            }
                        }
                    }
                }
            }


            string[] filePaths = Directory.GetFiles(path);

            foreach (string filePath in filePaths)
            {
                if (filePath.Contains(".egg"))//replace the .egg file ending with .ogg
                {
                    string tmp = filePath.Substring(0, filePath.IndexOf(".egg")) + ".ogg";
                    File.Copy(filePath, tmp);
                }
            }
            //serialize SynthSong
           result = JsonConvert.SerializeObject(synthSong);  
        }
        return result;
    }
}
