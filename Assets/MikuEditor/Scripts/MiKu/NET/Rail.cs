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
        public GameObject thisNoteObject;
        public RailNoteWrapper previousNote;
        public RailNoteWrapper nextNote;

    }

    public class Rail  {
        public Rail() {
            railId = railCounter++;
            notesByID = new Dictionary<int, RailNoteWrapper>();
            notesByTime = new Dictionary<float, RailNoteWrapper>();
            noteObjects = new Dictionary<int, GameObject>();
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

            InstantiateNoteObject(notesByTime[times[0]]);
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

            // three distinct cases here, removing sole leader, breaker and removing simple note
            // first removes the rail altogether
            // second needs to set a new breaker as the previous note (merging two rails should be done by replacing the breaker with a note)
            // third just moves pointers around

            if(notesByID.Count == 1) {
                // we are removing the last note of the rail
                scheduleForDeletion = true;
                return;
            }

            RailNoteWrapper removedNote = notesByID[id];
            EditorNote note = removedNote.thisNote;

            if(IsBreakerNote(note.UsageType)) 
                RemoveBreakerNote(removedNote);
            else
                RemoveSimpleNote(removedNote);

            notesByID.Remove(id);
            notesByTime.Remove(removedNote.thisNote.TimePoint);

            RecalcDuration();
            
            DestroyNoteObjectAndRemoveItFromTheRail(id);
            ReinstantiateRail(this);
            return;
        }


        void RemoveSimpleNote(RailNoteWrapper wrapper) {
            EditorNote note = wrapper.thisNote;
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);

            // by this point we've established it's not the breaker so we only need 
            // to concern ourselves if we're removing the leader or not
            // need to check that this note isn't the leader and assign a new one
            if(leader.thisNote.noteId == note.noteId) {
                RailNoteWrapper oldNextNote = leader.nextNote;
                leader = oldNextNote;
            } else {
                RailNoteWrapper oldPreviousNote = wrapper.previousNote;
                RailNoteWrapper oldNextNote = wrapper.nextNote;

                oldPreviousNote.nextNote = oldNextNote;
                // we can be removing the last section of unterminated rail
                // in this case oldNextNote will be null
                if(oldNextNote != null)
                    oldNextNote.previousNote = oldPreviousNote;
            }
        }

        void RemoveBreakerNote(RailNoteWrapper wrapper) {
            EditorNote note = wrapper.thisNote;
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            // by this point we've established this note isn't a sole note of a rail
            // also at this point breaker can only be the last note in the segment
            // by default removing a breaker does _not_ merge the rail to the next one
            // neither does it automatically make the previous note a breaker
            previous.nextNote = null;
            breaker = null;
        }

        public void AddNote(EditorNote note) {
            // extending past the railbraker needs to make a new note railbreaker
            RailNoteWrapper wrapper = new RailNoteWrapper(note);
            if(IsBreakerNote(note.UsageType)) 
                AddBreakerNote(wrapper);
            else
                AddSimpleNote(wrapper);

            notesByID[note.noteId] = wrapper;
            notesByTime[note.TimePoint] = wrapper;

            RecalcDuration();

            InstantiateNoteObject(wrapper);
            ReinstantiateRail(this);
        }

        void AddSimpleNote(RailNoteWrapper wrapper) {

            EditorNote note = wrapper.thisNote;
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
            // adding new tail 
            else if(previous.nextNote == null ) {
                RailNoteWrapper previousLeftPoint = previous;

                previous.nextNote = wrapper;
                wrapper.nextNote = null;
                wrapper.previousNote = previousLeftPoint;
            }
            // adding new note in the middle
            else {
                RailNoteWrapper originalNextNote = previous.nextNote;

                previous.nextNote = wrapper;
                wrapper.nextNote = originalNextNote;
                wrapper.previousNote = previous;
            }
        }

        //need to handle special case of adding a breaker to an already broken rail
        void AddBreakerNote(RailNoteWrapper wrapper) {

            EditorNote note = wrapper.thisNote;
            Rail potentialNewRail = null;

            breaker = wrapper;

            

            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            // breaker becomes the first note 
            // two possibilities > first note of an existing rail and first note of a new rail
            // first shouldn't be happening because request like that won't pass the external function
            // still, better to handle this and just exit
            if(previous == null ) {

                // first note of an existing rail 
                // returning, this is bs
                if(notesByID.Count > 0)
                    return;
               
                // rail that starts with the breaker is theoretically possible
                leader = wrapper;
            }
            // adding non first note
            // since it's a breaker there are two possible cases
            // we're splitting an existing rail
            // or we're adding a breaker past an already existing breaker

            // if we're breaking an existing rail
            if(previous.nextNote != null ) {
                
                RailNoteWrapper previousLeftPoint = previous;
                RailNoteWrapper previousRightPoint = previous.nextNote;

                previous.nextNote = wrapper;
                wrapper.nextNote = null;
                wrapper.previousNote = previousLeftPoint;

                potentialNewRail = ConvertTheTailIntoNewRail(previousRightPoint);
            }
            // adding a breaker past the breaker
            // need to extend and set all params for a new breaker
            else {
                //RailNoteWrapper originalNextNote = previous.nextNote;
                previous.thisNote.UsageType = EditorNote.NoteUsageType.Line;
                previous.nextNote = wrapper;
                wrapper.nextNote = null;
            }

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
            if(rail == null)
                return;
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

            EditorNote leaderNote = rail.leader.thisNote;
            leaderNote.Segments = new float[rail.notesByID.Count, 3];
            
            //float[,] testedArray = new float[rail.notesByID.Count, 3];

            List<float> keys = rail.notesByTime.Keys.ToList();
            keys.Sort();
            try {
                for(int i = 0; i < keys.Count; i++) {
                    float key = keys[i];
                    RailNoteWrapper note = rail.notesByTime[key];

                    leaderNote.Segments[i, 0] = note.thisNote.Position[0];
                    leaderNote.Segments[i, 1] = note.thisNote.Position[1];
                    leaderNote.Segments[i, 2] = note.thisNote.Position[2];

                    note = rail.notesByTime[key];
                    //leaderNote.Segments[i, 1] = note.thisNote.Position[1];
                }
            } catch { }


            GameObject leaderObject = rail.leader.thisNoteObject;
            Game_LineWaveCustom waveCustom = leaderObject.GetComponentInChildren<Game_LineWaveCustom>();
            waveCustom.targetOptional = leaderNote.Segments;
            waveCustom.RenderLine(true);
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

        void InstantiateNoteObject(RailNoteWrapper wrapper) {
            EditorNote note = wrapper.thisNote;
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
            wrapper.thisNoteObject = noteGO;
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
