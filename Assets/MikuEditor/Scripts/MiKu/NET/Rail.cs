using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiKu.NET.Charting;
using ThirdParty.Custom;
using UnityEngine;

namespace MiKu.NET {

    public class RailNoteWrapper {
        public RailNoteWrapper(EditorNote note) {
            thisNote = note;
        }

        public RailNoteWrapper() {
        }

        public EditorNote thisNote;
        public RailNoteWrapper previousNote;
        public RailNoteWrapper nextNote;
    }

    public class Rail : MonoBehaviour {
        public Rail() {
            railId = railCounter++;
        }

        ~Rail() {
            IdDictionaries.RemoveRail(railId);
        }

        public bool TimeInInterval(float time) {
            if(time > startTime && time < endTime)
                return true;
            return false;
        }

        public bool HasNoteAtTime(float time) {

            float timeRangeDuplicatesStart = time - Track.MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = time + Track.MIN_TIME_OVERLAY_CHECK;

            List<float> times = notesByTime.Keys.ToList();
            times = times.Where(testedTIme => testedTIme >= timeRangeDuplicatesStart
                    && testedTIme <= timeRangeDuplicatesEnd).ToList();
            return times.Count > 0;
        }

        public void MoveNoteAtTimeToPosition(float time, float x, float y) {
            // need to get the closest forward match
            float timeRangeDuplicatesStart = time - Track.MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = time + Track.MIN_TIME_OVERLAY_CHECK;

            List<float> times = notesByTime.Keys.ToList();
            times.Sort();

            times = times.Where(testedTIme => testedTIme >= timeRangeDuplicatesStart
                    && testedTIme <= timeRangeDuplicatesEnd).ToList();
            times.Sort();
            // we act on the first relevant note
            if(times.Count == 0)
                return;

            EditorNote foundNote = notesByTime[times[0]].thisNote;
            foundNote.Position[0] = x;
            foundNote.Position[1] = y;

            InstantiateNoteObject(foundNote);
            ReinstantiateRail(this);
        }


        public bool IsBreakerNote(EditorNote.NoteUsageType type) {
            if(type == EditorNote.NoteUsageType.Breaker) {
                return true;
            }
            return false;
        }

        public void RemoveNote(int id) {
            if(!notesByID.ContainsKey(id))
                return;

            Rail potentialRemovedRail = null;
            RailNoteWrapper removedNote = notesByID[id];

            // need to check that this note isn't the leader and assign a new one
            if(leader.thisNote.noteId == id) {
                RailNoteWrapper oldLeader = leader;
                RailNoteWrapper oldNextNote = leader.nextNote;
                if(oldNextNote == null) {
                    scheduleForDeletion = true;
                } else {
                    leader = oldNextNote;
                    oldNextNote.previousNote = null;
                }
            } else {
                // need to check that we're not removing the breaker
                // if we do, make the previous note a breaker
                // user can always change the type of it later
                if(IsBreakerNote(removedNote.thisNote.UsageType)) {
                    RailNoteWrapper previousNote = removedNote.previousNote;
                    previousNote.thisNote.UsageType = removedNote.thisNote.UsageType;
                    previousNote.nextNote = null;
                } else {
                    RailNoteWrapper previousNote = removedNote.previousNote;
                    previousNote.nextNote = removedNote.nextNote; // it's fine if nextNote is null
                }
            }

            notesByID.Remove(id);
            notesByTime.Remove(removedNote.thisNote.TimePoint);

            // by removing the breaker we are opening merge potential
            // this means the next following rails might need to be deinstantiated
            // as its notes will move to this one
            // ^^^ disregard this comment for now, I will move merges to node type change
            //potentialRemovedRail = GetNextRail();
            //if (!CanMerge(this, potentialRemovedRail))
            //{
            //    potentialRemovedRail = null;
            //}
            //else {
            //    Merge(this, potentialRemovedRail);
            //}

            RecalcDuration();
            
            DestroyNoteObjectAndRemoveItFromTheRail(id);
            ReinstantiateRail(this);
            Destroy(potentialRemovedRail);
            return;
        }

        public void AddNote(EditorNote note) {
            // extending past the railbraker needs to make a new note railbreaker

            Rail potentialNewRail = null;

            RailNoteWrapper wrapper = new RailNoteWrapper(note);
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            // adding new leader
            if(previous == null) {
                RailNoteWrapper previousLeader = leader;
                leader = wrapper;
                if(previousLeader != null) {
                    wrapper.nextNote = previousLeader;
                    previousLeader.previousNote = wrapper;
                }
            }
            // adding new tail or non breaker
            else if(previous.nextNote == null || !IsBreakerNote(note.UsageType)) {
                RailNoteWrapper previousLeftPoint = previous;
                RailNoteWrapper previousRightPoint = previous.nextNote;
                bool previousIsBreaker = IsBreakerNote(previousLeftPoint.thisNote.UsageType);

                previous.nextNote = wrapper;
                wrapper.nextNote = previousRightPoint;
                wrapper.previousNote = previousLeftPoint;
                if(previousIsBreaker) {
                    previousLeftPoint.thisNote.UsageType = EditorNote.NoteUsageType.Line;
                    wrapper.thisNote.UsageType = EditorNote.NoteUsageType.Breaker;
                }
            }
            // adding new note in the middle, need to handle breaker case
            else {
                RailNoteWrapper originalNextNote = previous.nextNote;

                previous.nextNote = wrapper;
                wrapper.nextNote = null;

                potentialNewRail = ConvertTheTailIntoNewRail(originalNextNote);

            }
            notesByID[note.noteId] = wrapper;
            notesByTime[note.TimePoint] = wrapper;

            RecalcDuration();


            InstantiateNoteObject(note);
            ReinstantiateRail(this);
            ReinstantiateRail(potentialNewRail);
        }

        public RailNoteWrapper GetPreviousNote(float time) {
            List<float> keys = notesByTime.Keys.ToList();
            keys.Reverse();
            float first = keys.FirstOrDefault(x => x < time);
            if(EqualityComparer<float>.Default.Equals(first, default(float)))
                return null;
            return notesByTime[first];
        }

        EditorNote.NoteHandType GetNoteHandType() {
            return noteType;
        }

        Rail ConvertTheTailIntoNewRail(RailNoteWrapper note) {
            return null;
        }

        public static void ReinstantiateRail(Rail rail) {
            // need to somehow indicate that rail is too shor or too long, maybe with a color
            if(rail.duration < Track.MIN_LINE_DURATION ||
                rail.duration > Track.MAX_LINE_DURATION) {
                return;
            }

            if(!Track.Instance.AddTimeToCurrentTrack(rail.startTime))
                return;

            // longNote.segments is basically a temprorary prototype GameObjects that need to be used
            // to initialize the actual Note object with Note.segments
            // since I am doing my own housekeeping in the Rail, I can skip LongNote.segments altogether
            // and go straight to rendered note setup
            if(rail.waveCustom != null) {
                GameObject.DestroyImmediate(rail.waveCustom);
                rail.waveCustom = null;
            }


            // will render it myself?
            //if(rail.linkedObject == null) {
            //    rail.linkedObject = Track.Instance.AddNoteGameObjectToScene(rail.leader.thisNote);
            //}

            var leaderNote = rail.leader.thisNote;
            leaderNote.Segments = new float[rail.notesByID.Count, 3];

            List<float> keys = rail.notesByTime.Keys.ToList();
            keys.Sort();

            for(int i = 0; i < keys.Count; ++i) {
                leaderNote.Segments[i, 0] = rail.notesByTime[keys[i]].thisNote.Position[0];
                leaderNote.Segments[i, 1] = rail.notesByTime[keys[i]].thisNote.Position[1];
                leaderNote.Segments[i, 2] = rail.notesByTime[keys[i]].thisNote.Position[2];
            }


            rail.waveCustom = new Game_LineWaveCustom();
            rail.waveCustom.targetOptional = leaderNote.Segments;
            rail.waveCustom.RenderLine(true);
        }

        public static void Destroy(Rail rail) {
            GameObject.DestroyImmediate(rail.waveCustom);
        }

        public void RecreateRail() { }

        Rail GetNextRail() {
            return null;
        }
        bool CanMerge(Rail firstRail, Rail secondRail) {
            // maybe not needed
            return false;
        }

        bool Merge(Rail firstRail, Rail secondRail) {
            // need to check that second is past the first and that rail types are the same
            if(firstRail.GetNoteHandType() != secondRail.GetNoteHandType())
                return false;

            if(firstRail.startTime > secondRail.startTime)
                return false;

            List<int> keys = secondRail.notesByID.Keys.ToList();
            // temporarily using just note addition, ideally would need shortcut
            for(int i = 0; i < keys.Count; i++) {
                firstRail.AddNote(notesByID[i].thisNote);
            }
            return true;
        }

        void RecalcDuration() {
            List<float> keys = notesByTime.Keys.ToList();
            if(keys.Count == 0)
                return;

            keys.Sort();
            var lastTime = keys[keys.Count - 1];
            var firstTime = keys[0];
            if(keys.Count == 0)
                duration = 0;
            else
                duration = lastTime - firstTime;
            startTime = keys[0];
            endTime = keys[keys.Count - 1];
        }

        void InstantiateNoteObject(EditorNote note) {
            DestroyNoteObjectAndRemoveItFromTheRail(note.noteId);

            GameObject noteGO = GameObject.Instantiate(Track.s_instance.GetNoteMarkerByType(note.HandType));
            noteGO.transform.localPosition = new Vector3(
                                                note.Position[0],
                                                note.Position[1],
                                                note.Position[2]
                                            );
            noteGO.transform.rotation = Quaternion.identity;
            noteGO.transform.parent = Track.s_instance.m_NotesHolder;
            noteGO.name = note.Id;
        }

        void DestroyNoteObjectAndRemoveItFromTheRail(int id) {
            if(!noteObjects.ContainsKey(id))
                return;
            GameObject.DestroyImmediate(noteObjects[id]);
            noteObjects.Remove(id);
        }

        //float CanReplaceNextNote()
        public static int railCounter = 0;

        public int railId;
        public float startTime;
        public float endTime;
        public float duration;

        public EditorNote.NoteHandType noteType;

        bool scheduleForDeletion = false;

        public Dictionary<int, RailNoteWrapper> notesByID;
        public Dictionary<float, RailNoteWrapper> notesByTime;
        // rail does its own object housekeeping
        // and its own note object instantiation / destruction
        public Dictionary<int, GameObject> noteObjects; 

        public RailNoteWrapper leader;
        public RailNoteWrapper breaker;
        //public LongNote railInstance;
        Game_LineWaveCustom waveCustom;
        
    }
}
