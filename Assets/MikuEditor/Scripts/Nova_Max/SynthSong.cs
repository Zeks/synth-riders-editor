using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class SynthSong
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Artwork = "Default Artwork";
    public string ArtworkBytes = null;
    public string AudioName = "";
    public string AudioData = null;
    public int AudioFrequency = 0;
    public int AudioChannels = 2;
    public float BPM { get; set; }
    public float Offset { get; set; }
    public Dictionary<string, Dictionary<string, List<SynthNote>>> Track = new Dictionary<string, Dictionary<string, List<SynthNote>>>
        {
            {"Easy",new Dictionary<string,List<SynthNote>>()},
            {"Normal",new Dictionary<string,List<SynthNote>>()},
            {"Hard",new Dictionary<string,List<SynthNote>>()},
            {"Expert",new Dictionary<string,List<SynthNote>>()},
            {"Master",new Dictionary<string,List<SynthNote>>()}
        };
    public Dictionary<string, List<string>> Effects = new Dictionary<string, List<string>>
        {
            {"Easy",new List<string>()},
            {"Normal",new List<string>()},
            {"Hard",new List<string>()},
            {"Expert",new List<string>()},
            {"Master",new List<string>()}
        };
    public Dictionary<string, List<string>> Bookmarks = new Dictionary<string, List<string>>
        {
            {"Easy",new List<string>()},
            {"Normal",new List<string>()},
            {"Hard",new List<string>()},
            {"Expert",new List<string>()},
            {"Master",new List<string>()}
        };
    public Dictionary<string, List<string>> Jumps = new Dictionary<string, List<string>>
        {
            {"Easy",new List<string>()},
            {"Normal",new List<string>()},
            {"Hard",new List<string>()},
            {"Expert",new List<string>()},
            {"Master",new List<string>()}
        };
    public Dictionary<string, List<int>> Crouchs = new Dictionary<string, List<int>>
        {
            {"Easy",new List<int>()},
            {"Normal",new List<int>()},
            {"Hard",new List<int>()},
            {"Expert",new List<int>()},
            {"Master",new List<int>()}
        };
    public Dictionary<string, List<CSlide>> Slides = new Dictionary<string, List<CSlide>>
        {
            {"Easy",new List<CSlide>()},
            {"Normal",new List<CSlide>()},
            {"Hard",new List<CSlide>()},
            {"Expert",new List<CSlide>()},
            {"Master",new List<CSlide>()}
        };
    public string FilePath = "";
    public bool IsAdminOnly = false;
    public string EditorVersion = "1.1.1.1";
    public string Beatmapper { get; set; }
}

public class SynthNote
{
    public string Id { get; set; }
    public int Combold = -1;
    public float[] Position { get; set; }
    public string Segments = null;
    public int Type { get; set; }
}

public class CSlide
{
    public int time { get; set; }
    public int SlideType { get; set; }
    public bool initialized = true;
}

