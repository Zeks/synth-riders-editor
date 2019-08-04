using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiKu.NET.Charting;
using ThirdParty.Custom;
using UnityEngine;
using System.Diagnostics;

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
            Trace.WriteLine("Created new rail with ID: " + railId);
            notesByID = new Dictionary<int, RailNoteWrapper>();
            notesByTime = new Dictionary<float, RailNoteWrapper>();
            noteObjects = new Dictionary<int, GameObject>();
    }

        ~Rail() {
            Trace.WriteLine("Removing rail with ID: " + railId);
            IdDictionaries.RemoveRail(railId);
        }

        public void Log() {
            Trace.WriteLine("Rail ID: " + railId + "start:"  + startTime + " end:" + endTime +  " duration:" + duration +  "type: " + noteType);
        }

        public bool TimeInInterval(float time) {
            Trace.WriteLine("Detecting if time: " + time + " is within " + startTime + " and " + endTime);
            if(time > startTime && time < endTime) {
                Trace.WriteLine("returning true");
                return true;
            }
            Trace.WriteLine("returning false");
            return false;
        }

        public bool HasNoteAtTime(float time) {
            Trace.WriteLine("Detecting if there is a note at: " + time  + "Rail spans from:"  + startTime + " to " + endTime);
            
            float timeRangeDuplicatesStart = time - Track.MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = time + Track.MIN_TIME_OVERLAY_CHECK;

            List<float> times = notesByTime.Keys.ToList();
            Trace.WriteLine("Rail already has notes at: " + times);

            times = times.Where(testedTIme => testedTIme >= timeRangeDuplicatesStart
                    && testedTIme <= timeRangeDuplicatesEnd).ToList();

            Trace.WriteLine("Returning note count of: " + times.Count);
            return times.Count > 0;
        }

        public void MoveNoteAtTimeToPosition(float time, float x, float y) {
            Trace.WriteLine("Asked to move note at: " + time + " to x: " + x + " y:" + y);
            // need to get the closest forward match
            float timeRangeDuplicatesStart = time - Track.MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = time + Track.MIN_TIME_OVERLAY_CHECK;

            List<float> times = notesByTime.Keys.ToList();
            times.Sort();

            times = times.Where(testedTIme => testedTIme >= timeRangeDuplicatesStart
                    && testedTIme <= timeRangeDuplicatesEnd).ToList();
            times.Sort();

            Trace.WriteLine("Rail has notes at: " + times);
            // we act on the first relevant note
            if(times.Count == 0) {
                Trace.WriteLine("No matching notes to move, returning");
                return;
            }

            EditorNote foundNote = notesByTime[times[0]].thisNote;
            foundNote.Position[0] = x;
            foundNote.Position[1] = y;

            Trace.WriteLine("Moving note with id: " + foundNote.noteId + "positioned at x:" + foundNote.Position[0] + " y:" + foundNote.Position[1] + " z:" + foundNote.Position[2]);

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
            Trace.WriteLine("Called to remove the note with id: " + id);
            if(!notesByID.ContainsKey(id)) {
                Trace.WriteLine("Note not found, exiting");
                return;
            }
            Trace.WriteLine("Note found: " + notesByID[id].thisNote.noteId + "positioned at x:" + notesByID[id].thisNote.Position[0] + " y:" + notesByID[id].thisNote.Position[1] + " z:" + notesByID[id].thisNote.Position[2]);

            // three distinct cases here, removing sole leader, breaker and removing simple note
            // first removes the rail altogether
            // second needs to set a new breaker as the previous note (merging two rails should be done by replacing the breaker with a note)
            // third just moves pointers around

            if(notesByID.Count == 1) {
                // we are removing the last note of the rail
                Trace.WriteLine("Last note detected, setting deletion schedule");
                scheduleForDeletion = true;
                return;
            }

            RailNoteWrapper removedNote = notesByID[id];
            EditorNote note = removedNote.thisNote;

            if(IsBreakerNote(note.UsageType)) 
                RemoveBreakerNote(removedNote);
            else
                RemoveSimpleNote(removedNote);

            Trace.WriteLine("Removing note from rails dictionaries");
            notesByID.Remove(id);
            notesByTime.Remove(removedNote.thisNote.TimePoint);

            RecalcDuration();
            
            DestroyNoteObjectAndRemoveItFromTheRail(id);
            ReinstantiateRail(this);
            Trace.WriteLine("Exiting RemoveNote");
            return;
        }


        void RemoveSimpleNote(RailNoteWrapper wrapper) {
            Trace.WriteLine("Called to remove simple note.");
            EditorNote note = wrapper.thisNote;
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);

            // by this point we've established it's not the breaker so we only need 
            // to concern ourselves if we're removing the leader or not
            // need to check that this note isn't the leader and assign a new one
            if(leader.thisNote.noteId == note.noteId) {
                Trace.WriteLine("Removing leader note.");
                RailNoteWrapper oldNextNote = leader.nextNote;
                Trace.WriteLine("New leader with id: " + leader.thisNote.noteId + "positioned at x:" + leader.thisNote.Position[0] + " y:" + leader.thisNote.Position[1] + " z:" + leader.thisNote.Position[2]);
                leader = oldNextNote;
            } else {
                Trace.WriteLine("Removing simple note.");
                RailNoteWrapper oldPreviousNote = wrapper.previousNote;
                RailNoteWrapper oldNextNote = wrapper.nextNote;

                oldPreviousNote.nextNote = oldNextNote;
                Trace.WriteLine("Linking  notes: ");
                Trace.WriteLine("Note 1: " + oldPreviousNote.thisNote.noteId + "positioned at x:" + oldPreviousNote.thisNote.Position[0] + " y:" + oldPreviousNote.thisNote.Position[1] + " z:" + oldPreviousNote.thisNote.Position[2]);
                Trace.WriteLine("Note 2: " + oldNextNote.thisNote.noteId + "positioned at x:" + oldNextNote.thisNote.Position[0] + " y:" + oldNextNote.thisNote.Position[1] + " z:" + oldNextNote.thisNote.Position[2]);

                // we can be removing the last section of unterminated rail
                // in this case oldNextNote will be null
                if(oldNextNote != null) {
                    oldNextNote.previousNote = oldPreviousNote;
                } else {
                    Trace.WriteLine("Removed the last section of unterminated rail.");
                }
            }
        }

        void RemoveBreakerNote(RailNoteWrapper wrapper) {
            Trace.WriteLine("Called to remove breaker note.");
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
            Trace.WriteLine("Called to add a note: " + note.noteId);
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

            Trace.WriteLine("Called to add a BREAKER note positioned at x:" + wrapper.thisNote.Position[0] + " y:" + wrapper.thisNote.Position[1] + " z:" + wrapper.thisNote.Position[2]);

            EditorNote note = wrapper.thisNote;
            Rail potentialNewRail = null;

            breaker = wrapper;

            

            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            if(previous != null)
                Trace.WriteLine("Note just before is at:" + previous.thisNote.Position[0] + " y:" + previous.thisNote.Position[1] + " z:" + previous.thisNote.Position[2]);
            else
                Trace.WriteLine("Failed to find note just before");

            // breaker becomes the first note 
            // two possibilities > first note of an existing rail and first note of a new rail
            // first shouldn't be happening because request like that won't pass the external function
            // still, better to handle this and just exit
            if(previous == null ) {

                // first note of an existing rail 
                // returning, this is bs
                if(notesByID.Count > 0)
                    return;
                Trace.WriteLine("Initiating the rail with a breaker note.");
                // rail that starts with the breaker is theoretically possible
                leader = wrapper;
            }
            // adding non first note
            // since it's a breaker there are two possible cases
            // we're splitting an existing rail
            // or we're adding a breaker past an already existing breaker

            // if we're breaking an existing rail
            if(previous.nextNote != null ) {
                Trace.WriteLine("Breaking the existing rail.");

                RailNoteWrapper previousLeftPoint = previous;
                RailNoteWrapper previousRightPoint = previous.nextNote;

                previous.nextNote = wrapper;
                wrapper.nextNote = null;
                wrapper.previousNote = previousLeftPoint;

                
                potentialNewRail = ConvertTheTailIntoNewRail(previousRightPoint);
                if(potentialNewRail != null)
                    Trace.WriteLine("The tail of this rail has created a new rail with id:" + potentialNewRail.railId);
            }
            // adding a breaker past the breaker
            // need to extend and set all params for a new breaker
            else {
                //RailNoteWrapper originalNextNote = previous.nextNote;
                Trace.WriteLine("Extending past an already existing breaker");
                previous.thisNote.UsageType = EditorNote.NoteUsageType.Line;
                previous.nextNote = wrapper;
                wrapper.nextNote = null;
            }

            ReinstantiateRail(potentialNewRail);

        }

        public RailNoteWrapper GetPreviousNote(float time) {
            Trace.WriteLine("Looking for a note just before: " + time);
            List<float> keys = notesByTime.Keys.ToList();
            keys.Reverse();
            Trace.WriteLine("Will look among: " + keys);
            float first = keys.FirstOrDefault(x => x < time);
            Trace.WriteLine("Found time: " + first);
            if(EqualityComparer<float>.Default.Equals(first, default(float))) {
                Trace.WriteLine("Returning null");
                return null;
            }
            Trace.WriteLine("Returning value");
            return notesByTime[first];
        }

        EditorNote.NoteHandType GetNoteHandType() {
            return noteType;
        }

        Rail ConvertTheTailIntoNewRail(RailNoteWrapper note) {
            return null;
        }
        public static void Print2DArray<T>(T[,] matrix) {
            for(int i = 0; i < matrix.GetLength(0); i++) {
                for(int j = 0; j < matrix.GetLength(1); j++) {
                    Trace.Write(matrix[i, j] + "\t");
                }
                Trace.WriteLine("");
            }
        }
        public static void ReinstantiateRail(Rail rail) {
            Trace.WriteLine("Called to reinstantiate the rail");
            if(rail == null) {
                Trace.WriteLine("Can't instantiate the null rail. Returning");
                return;
            }
            // need to somehow indicate that rail is too shor or too long, maybe with a color
            if(rail.duration < Track.MIN_LINE_DURATION ||
                rail.duration > Track.MAX_LINE_DURATION) {
                Trace.WriteLine("Rail duration is at: " + rail.duration + " Min duration is:" + Track.MIN_LINE_DURATION + "Max Duration is: "  + Track.MAX_LINE_DURATION);
                return;
            }

            if(!Track.Instance.AddTimeToCurrentTrack(rail.startTime)) {
                Trace.WriteLine("Failed to add the time to track. Returning");
                return;
            }

            // longNote.segments is basically a temprorary prototype GameObjects that need to be used
            // to initialize the actual Note object with Note.segments
            // since I am doing my own housekeeping in the Rail, I can skip LongNote.segments altogether
            // and go straight to rendered note setup
            //if(rail.waveCustom != null) {
            //    GameObject.DestroyImmediate(rail.waveCustom);
            //    rail.waveCustom = null;
            //}


            // will render it myself?
            //if(rail.linkedObject == null) {
            //    rail.linkedObject = Track.Instance.AddNoteGameObjectToScene(rail.leader.thisNote);
            //}

            EditorNote leaderNote = rail.leader.thisNote;
            leaderNote.Segments = new float[rail.notesByID.Count, 3];

            Trace.WriteLine("Will instantiate the rail for note:" + leaderNote.noteId + " x:" + leaderNote.Position[0] + " y:" + leaderNote.Position[1] + " z:" + leaderNote.Position[2]);

            //float[,] testedArray = new float[rail.notesByID.Count, 3];

            List<float> keys = rail.notesByTime.Keys.ToList();
            keys.Sort();
            int i = 0;
            try {
                for(i = 0; i < keys.Count; i++) {
                    float key = keys[i];
                    RailNoteWrapper note = rail.notesByTime[key];

                    leaderNote.Segments[i, 0] = note.thisNote.Position[0];
                    leaderNote.Segments[i, 1] = note.thisNote.Position[1];
                    leaderNote.Segments[i, 2] = note.thisNote.Position[2];
                }
            } catch {
                Trace.WriteLine("!!!!!!!!!!!!!!!!!CRASH CRASH CRASH!!!!!!!!!!!!!!!!!!!");
                Trace.WriteLine("CRASHED into catch segment at i: " + i);
            }

            Trace.WriteLine("Written segments are:");
            Print2DArray(leaderNote.Segments);


            
            // need to reset leader's object to reset its component then reinstantiate it
            rail.DestroyNoteObjectAndRemoveItFromTheRail(rail.leader.thisNote.noteId);
            rail.InstantiateNoteObject(rail.leader);
            GameObject leaderObject = rail.leader.thisNoteObject;

            Game_LineWaveCustom waveCustom = leaderObject.GetComponentInChildren<Game_LineWaveCustom>();
            if(waveCustom != null) {
                Trace.WriteLine("Rendering line");
                waveCustom.targetOptional = leaderNote.Segments;
                waveCustom.RenderLine(true);
            } else {
                Trace.WriteLine("Failed to get component to render line");
            }
        }

        public static void Destroy(Rail rail) {
            Trace.WriteLine("Called to destroy the rail: " + rail.railId);
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
            Trace.WriteLine("Recalcualting rail duration for rail: " + this.railId);
            List<float> keys = notesByTime.Keys.ToList();
            if(keys.Count == 0) {
                Trace.WriteLine("Rail is empty, nothing to calculate");
                return;
            }

            keys.Sort();
            Trace.WriteLine("Rail contains times: " + keys);
            var lastTime = keys[keys.Count - 1];
            var firstTime = keys[0];
            if(keys.Count == 0)
                duration = 0;
            else
                duration = lastTime - firstTime;
            startTime = keys[0];
            endTime = keys[keys.Count - 1];

            Trace.WriteLine("New data: starttime:" + startTime + " endtime:" + endTime + " duration:" + duration);
        }

        void InstantiateNoteObject(RailNoteWrapper wrapper) {
            Trace.WriteLine("Called to instantiate the note: " + wrapper.thisNote.noteId);
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
            Trace.WriteLine("Attempting to find and destroy the old note object: " + id);
            if(!noteObjects.ContainsKey(id)) {
                Trace.WriteLine("Not found. Exiting.");
                return;
            }
            Trace.WriteLine("Destroying the object and removing it from noteObjects");
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