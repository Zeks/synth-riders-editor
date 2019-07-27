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

namespace MiKu.NET
{
    
    class IdDictionaries {
        public static void RemoveRail(int railId) {
            rails.Remove(railId);
        }
        public static void RemoveNote(int noteId)
        {
            notes.Remove(noteId);
        }
        public static Note GetNote(int id) {
            if (notes.ContainsKey(id))
                return notes[id];
            return null;

        }
        public static Rail GetRail(int id){
            if (rails.ContainsKey(id))
                return rails[id];
            return null;
        }
        public static Dictionary<int, Note> notes;
        public static Dictionary<int, Rail> rails;
    }
}