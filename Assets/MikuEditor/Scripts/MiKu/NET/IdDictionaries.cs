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
        public static void AddRail(Rail rail) {
            if(rails == null)
                rails = new Dictionary<int, Rail>();
            rails.Add(rail.railId, rail);
        }
        public static void AddNote(int noteId, EditorNote note) {
            if(notes == null)
                notes = new Dictionary<int, EditorNote>();
            notes.Add(noteId, note);
        }
        public static void RemoveRail(int railId) {
            if(rails == null)
                return;
            rails.Remove(railId);
        }
        public static void RemoveNote(int noteId)
        {
            if(notes == null)
                return;
            notes.Remove(noteId);
        }
        public static EditorNote GetNote(int id) {
            if(notes == null) {
                return null;
            }
            if (notes.ContainsKey(id))
                return notes[id];
            return null;

        }
        public static Rail GetRail(int id){
            if(notes == null) {
                return null;
            }
            if (rails.ContainsKey(id))
                return rails[id];
            return null;
        }
        public static Dictionary<int, EditorNote> notes;
        public static Dictionary<int, Rail> rails;
    }

    class TimeDictionaries {
        public static bool Contains(float time) {
            if(notes == null) {
                return false;
            }
            return notes.ContainsKey(time);
        }
        public static void RemoveNote(EditorNote note) {
            // will need to cycle to determine which one
            if(notes == null) {
                return;
            }
            if(!notes.ContainsKey(note.TimePoint))
                return;
            List<EditorNote> notesAtTime = notes[note.TimePoint];
            notesAtTime.RemoveAll(item => item.noteId == note.noteId);
        }
        public static List<EditorNote> GetNotes(float time) {
            if(notes == null) {
                return null;
            }
            if(notes.ContainsKey(time))
                return notes[time];
            return null;

        }
        public static void AddNote(float noteTime, EditorNote note) {
            if(notes == null)
                notes = new Dictionary<float, List<EditorNote>>();
            if(!notes.ContainsKey(noteTime)) {
                notes.Add(noteTime, new List<EditorNote>());
            }
            notes[noteTime].Add(note);
        }
        public static Dictionary<float, List<EditorNote>> notes;
    }
}