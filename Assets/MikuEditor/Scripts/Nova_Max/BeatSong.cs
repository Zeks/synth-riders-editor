using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BeatSong
{
    public List<BeatNote> _notes { get; set; }
    public List<BeatObstacle> _obstacles { get; set; }
}

public class BeatNote
{
    public float _time { get; set; }
    public int _lineIndex { get; set; }
    public int _lineLayer { get; set; }
    public int _type { get; set; }
    public int _cutDirection { get; set; }
}

public class BeatObstacle
{
    public float _time { get; set; }
    public int _lineIndex { get; set; }
    public int _type { get; set; }
    public float _duration { get; set; }
    public int _width { get; set; }
}

