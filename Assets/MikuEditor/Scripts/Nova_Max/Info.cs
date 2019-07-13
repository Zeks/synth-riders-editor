using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class Info
{
    public string _songName { get; set; }
    public string _songSubName { get; set; }
    public string _levelAuthorName { get; set; }
    public string _songAuthorName { get; set; }
    public float _beatsPerMinute { get; set; }
    public float _songTimeOffset { get; set; }
    public List<DifficultyBeatmapSet> _difficultyBeatmapSets { get; set; }
}

public class DifficultyBeatmapSet
{
    public List<DifficultyBeatmap> _difficultyBeatmaps { get; set; }
}

public class DifficultyBeatmap
{
    public string _difficulty { get; set; }
    public string _beatmapFilename { get; set; }
}

