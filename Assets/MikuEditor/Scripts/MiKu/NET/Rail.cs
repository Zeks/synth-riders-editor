using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiKu.NET.Charting;
using ThirdParty.Custom;
using UnityEngine;
using System.Diagnostics;

namespace MiKu.NET {
    public enum RailClickType {
        NoNote = 0,
        NoteAtTime = 1,
        NoteAtTimeAndPlace= 2,
    }

    public class RailClickWrapper {
        public RailClickWrapper(Rail rail, RailClickType type) {
            this.rail = rail;
            this.clickedPointType = type;
        }
        public Rail rail;
        public RailClickType clickedPointType;
    }

    public class RailNoteWrapper {
        public RailNoteWrapper(EditorNote note) {
            thisNote = note;
        }

        public RailNoteWrapper() {
        }

        public void AssignPreviousNote(RailNoteWrapper note) {
            if(note == null)
                Trace.WriteLine("Assigning null previous note");
            prevNote = note;
        }

        public RailNoteWrapper GetPreviousNote() {
            return prevNote;
        }

        public EditorNote thisNote;
        public GameObject thisNoteObject;
        private RailNoteWrapper prevNote;
        public RailNoteWrapper nextNote;

    }

    public class Rail {
        public Rail() {
            railId = railCounter++;
            Trace.WriteLine("Created new rail with ID: " + railId);
            notesByID = new Dictionary<int, RailNoteWrapper>();
            notesByTime = new SortedDictionary<float, RailNoteWrapper>();
            noteObjects = new Dictionary<int, GameObject>();
        }

        ~Rail() {
            if(leader != null) {

                DestroyNoteObjectAndRemoveItFromTheRail(leader.thisNote.noteId);
            }
            Trace.WriteLine("DESTRUCTOR called for rail with ID: " + railId);
            IdDictionaries.RemoveRail(railId);
        }

        public void Log() {
            Trace.WriteLine("Rail ID: " + railId + "start:"  + startTime + " end:" + endTime +  " duration:" + duration +  "type: " + noteType);
        }

        public bool TimeInInterval(float time) {
            Trace.WriteLine("Detecting if time: " + time + " is within " + startTime + " and " + endTime);
            if(time >= startTime && time <= endTime) {
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

        public EditorNote GetNoteAtPosition(float time) {
            Trace.WriteLine("Asked for note at time: " + time);
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
                return null;
            }

            EditorNote foundNote = notesByTime[times[0]].thisNote;
            return foundNote;
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
            Vector2 foundNotePosition = new Vector2(foundNote.Position[0], foundNote.Position[1]);
            Vector2 clickedPosition = new Vector2(x, y);
            float distance = Vector2.Distance(foundNotePosition, clickedPosition);
            // we're in the move  branch
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

        public void RemoveNote(int id, bool recalcOnRemove = true) {
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
                DestroyNoteObjectAndRemoveItFromTheRail(id);
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

            if(recalcOnRemove) { 
                RecalcDuration();

                DestroyNoteObjectAndRemoveItFromTheRail(id);
                ReinstantiateRail(this);
                Trace.WriteLine("Exiting RemoveNote");
            }
            return;
        }

        public void DestroyLeader() {
            if(leader != null)
                DestroyNoteObjectAndRemoveItFromTheRail(leader.thisNote.noteId);
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
                RailNoteWrapper oldPreviousNote = wrapper.GetPreviousNote(); 
                RailNoteWrapper oldNextNote = wrapper.nextNote;

                oldPreviousNote.nextNote = oldNextNote;
                Trace.WriteLine("Linking  notes: ");
                Trace.WriteLine("Note 1: " + oldPreviousNote.thisNote.noteId + "positioned at x:" + oldPreviousNote.thisNote.Position[0] + " y:" + oldPreviousNote.thisNote.Position[1] + " z:" + oldPreviousNote.thisNote.Position[2]);
                if(oldNextNote != null)
                    Trace.WriteLine("Note 2: " + oldNextNote.thisNote.noteId + "positioned at x:" + oldNextNote.thisNote.Position[0] + " y:" + oldNextNote.thisNote.Position[1] + " z:" + oldNextNote.thisNote.Position[2]);

                // we can be removing the last section of unterminated rail
                // in this case oldNextNote will be null
                if(oldNextNote != null) {
                    oldNextNote.AssignPreviousNote(oldPreviousNote);
                } else {
                    Trace.WriteLine("Removed the last section of unterminated rail.");
                }
            }
        }

        void RemoveBreakerNote(RailNoteWrapper wrapper) {
            Trace.WriteLine("Called to remove breaker note.");
            EditorNote note = wrapper.thisNote;

            if(wrapper == breakerTail) {
                RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
                previous.nextNote = null;
                breakerTail = null;
            }

            if(wrapper == breakerHead) {
                breakerHead = null;
                if(wrapper.nextNote != null) {
                    leader= wrapper.nextNote;
                    wrapper.nextNote.AssignPreviousNote(null);
                }
            }
        }

        public void AddNote(EditorNote note, bool silent = false) {
            Trace.WriteLine("////NOTE ADD/////" + note.noteId);
            // extending past the railbraker needs to make a new note railbreaker
            RailNoteWrapper wrapper = new RailNoteWrapper(note);
            bool added = true;
            if(IsBreakerNote(note.UsageType)) { 
                added = AddBreakerNote(wrapper);
            } else
                AddSimpleNote(wrapper);

            if(!added)
                return;

            notesByID[note.noteId] = wrapper;
            notesByTime[note.TimePoint] = wrapper;

            InstantiateNoteObject(wrapper);


            if(silent)
                return;

            RecalcDuration();
            
            ReinstantiateRail(this);
        }

        void AddSimpleNote(RailNoteWrapper wrapper) {

            EditorNote note = wrapper.thisNote;
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);

            // adding new leader
            if(previous == null) {
                RailNoteWrapper previousLeader = leader;

                // leader is keeping the displayed notes
                // therefore it needs to go or it will not be cleared in time
                if(previousLeader != null) { 
                    DestroyLeader();
                    
                }

                leader = wrapper;
                if(previousLeader != null) {
                    wrapper.nextNote = previousLeader;
                    previousLeader.AssignPreviousNote(wrapper);
                    InstantiateNoteObject(previousLeader);
                }
                    
            }
            // adding new tail 
            else if(previous.nextNote == null) {
                RailNoteWrapper previousLeftPoint = previous;

                previous.nextNote = wrapper;
                wrapper.nextNote = null;
                wrapper.AssignPreviousNote(previousLeftPoint);
            }
            // adding new note in the middle
            else {
                RailNoteWrapper originalNextNote = previous.nextNote;

                previous.nextNote = wrapper;
                wrapper.nextNote = originalNextNote;
                wrapper.AssignPreviousNote(previous);
            }
        }
        public bool WillBeNewHead(float time) {
            if(notesByTime.Count == 0)
                return true;
            if(time < notesByTime.Keys.ToList()[0]) {
                return true;
            }
            return false;
        }

        //need to handle special case of adding a breaker to an already broken rail
        bool AddBreakerNote(RailNoteWrapper wrapper) {
            if(HasNoteAtTime(wrapper.thisNote.TimePoint)) {
                // for simplicity we just flip an existing note to a breaker type
                // otherwise it becomes very counterintuitive
                EditorNote foundNote = GetNoteAtPosition(wrapper.thisNote.TimePoint);
                FlipNoteTypeToBreaker(foundNote.noteId);
                return false;
            }
        

            Trace.WriteLine("Called to add a BREAKER note positioned at x:" + wrapper.thisNote.Position[0] + " y:" + wrapper.thisNote.Position[1] + " z:" + wrapper.thisNote.Position[2]);

            EditorNote note = wrapper.thisNote;
            Rail potentialNewRail = null;
            if(WillBeNewHead(wrapper.thisNote.TimePoint))
                breakerHead = wrapper;
            else
                breakerTail = wrapper;

            

            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            if(previous != null)
                Trace.WriteLine("Note just before is at:" + previous.thisNote.Position[0] + " y:" + previous.thisNote.Position[1] + " z:" + previous.thisNote.Position[2]);
            else
                Trace.WriteLine("Failed to find note just before");

            // breaker becomes the first note 
            // two possibilities > first note of an existing rail and first note of a new rail
            // first shouldn't be happening because request like that won't pass the external function
            // still, better to handle this and just exit
            if(previous == null) {

                // first note of an existing rail 
                // returning, this is bs
                if(notesByID.Count > 0)
                    return true;
                Trace.WriteLine("Initiating the rail with a breaker note.");
                // rail that starts with the breaker is theoretically possible
                leader = wrapper;
                //note.HandType == EditorNote.NoteUsageType.Line; // adding breaker as a first note doesn't make sense
            } else {
                // adding non first note
                // since it's a breaker there are two possible cases
                // we're splitting an existing rail
                // or we're adding a breaker past an already existing breaker

                // if we're breaking an existing rail
                if(previous.nextNote != null) {
                    Trace.WriteLine("Breaking the existing rail.");
                    DestroyLeader();


                    RailNoteWrapper previousLeftPoint = previous;
                    RailNoteWrapper previousRightPoint = previous.nextNote;

                    previous.nextNote = wrapper;
                    wrapper.nextNote = null;
                    wrapper.AssignPreviousNote(previousLeftPoint);

                    // need to remove notes while they are still reachable
                    
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
                    wrapper.AssignPreviousNote(previous);
                }
            }
            if(potentialNewRail != null)
                ReinstantiateRail(potentialNewRail);
            return true;
        }

        public void FlipNoteTypeToBreaker(int noteId) {
            Trace.WriteLine("Flipping the note on existing rail to breaker");
            RailNoteWrapper note = notesByID[noteId];
            if(note == null || note.thisNote.UsageType == EditorNote.NoteUsageType.Breaker)
                return;

            RailNoteWrapper previous = GetPreviousNote(note.thisNote.TimePoint);
            if(previous != null)
                Trace.WriteLine("Note just before is at:" + previous.thisNote.Position[0] + " y:" + previous.thisNote.Position[1] + " z:" + previous.thisNote.Position[2]);
            else
                Trace.WriteLine("Failed to find note just before");


            RailNoteWrapper previousLeftPoint = previous;
            RailNoteWrapper previousRightPoint = note.nextNote;

            // two cases here: we're breaking the rail OR marking a leader as a closed one
            if(note != leader) {
                // note isn't a leader, this means we are cutting off the tail
                note.nextNote = null;

                Rail potentialNewRail = null;
                potentialNewRail = ConvertTheTailIntoNewRail(previousRightPoint);
                if(potentialNewRail != null)
                    Trace.WriteLine("The tail of this rail has created a new rail with id:" + potentialNewRail.railId);

                note.thisNote.UsageType =  EditorNote.NoteUsageType.Breaker;
                breakerTail = note;
            } else {
                // note IS a leader, this means we are toggling the leader as non expandable
                breakerHead = note;
                note.thisNote.UsageType =  EditorNote.NoteUsageType.Breaker;
            }

            RecalcDuration();

            InstantiateNoteObject(note);
            ReinstantiateRail(this);
        }

        public void FlipNoteTypeToLineWithoutMerging(int noteId) {
            Trace.WriteLine("Flipping the note on existing rail to line");
            RailNoteWrapper note = notesByID[noteId];
            if(note == null || note.thisNote.UsageType == EditorNote.NoteUsageType.Line)
                return;

            note.thisNote.UsageType =  EditorNote.NoteUsageType.Line;
            if(note != leader) {
                breakerTail = null;
            } else {
                breakerHead = null;
            }

            InstantiateNoteObject(note);

            RecalcDuration();
            ReinstantiateRail(this);
        }


        public RailNoteWrapper GetPreviousNote(float time) {
            Trace.WriteLine("Looking for a note just before: " + time);
            List<float> keys = notesByTime.Keys.ToList();
            keys.Sort();
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

        public RailNoteWrapper GetNoteAtThisOrFollowingTime(float time) {
            List<float> notes = notesByTime.Keys.ToList();
            notes.Sort();
            float first = notes.FirstOrDefault(x => x > time);
            if(first == default(float))
                return null;

            return notesByTime[first];
        }

        public Rail ConvertTheTailIntoNewRail(RailNoteWrapper note) {
            if(note == null)
                return null;

            RailNoteWrapper initialNote = note;

            Trace.WriteLine("!<><><><><><><><><><>RAIL CREATION<><><><><><><><><><><><><><><>!");
            Rail newRail = new Rail();
            newRail.noteType = noteType;
            IdDictionaries.AddRail(newRail);
            List<Rail> railList = Track.s_instance.GetCurrentRailListByDifficulty();
            railList.Add(newRail);

            while(note != null) {

                notesByID.Remove(note.thisNote.noteId);
                notesByTime.Remove(note.thisNote.TimePoint);
                GameObject.DestroyImmediate(noteObjects[note.thisNote.noteId]);
                noteObjects.Remove(note.thisNote.noteId);

                newRail.AddNote(note.thisNote, true);
                note = note.nextNote;
            };

            newRail.RecalcDuration();
            ReinstantiateRail(newRail);

            return newRail;
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
            if(rail == null) {
                Trace.WriteLine("Can't instantiate the null rail. Returning");

                return;
            }
            Trace.WriteLine("Called to reinstantiate the rail: " + rail.railId);
            // need to somehow indicate that rail is too shor or too long, maybe with a color
            if(rail.duration < Track.MIN_LINE_DURATION ||
                rail.duration > Track.MAX_LINE_DURATION) {

                if(rail.leader != null) {

                    rail.DestroyNoteObjectAndRemoveItFromTheRail(rail.leader.thisNote.noteId);
                    rail.InstantiateNoteObject(rail.leader);
                }
                Trace.WriteLine("Rail duration is at: " + rail.duration + " Min duration is:" + Track.MIN_LINE_DURATION + "Max Duration is: "  + Track.MAX_LINE_DURATION);
                return;
            }

            if(!Track.Instance.AddTimeToCurrentTrack(rail.startTime)) {
                Trace.WriteLine("Failed to add the time to track. Returning");
                return;
            }


            EditorNote leaderNote = rail.leader.thisNote;
            leaderNote.Segments = new float[rail.notesByID.Count, 3];

            Trace.WriteLine("Will instantiate the rail: " + rail.railId +  " for note:" + leaderNote.noteId + " x:" + leaderNote.Position[0] + " y:" + leaderNote.Position[1] + " z:" + leaderNote.Position[2]);

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

        public void RecalcDuration() {
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

            bool isSegment = wrapper == leader ? false : true;
            GameObject noteGO = GameObject.Instantiate(Track.s_instance.GetNoteMarkerByType(note.HandType, note.UsageType, isSegment));
            noteGO.transform.localPosition = new Vector3(
                                                note.Position[0],
                                                note.Position[1],
                                                note.Position[2]
                                            );
            Vector3 oldScale = noteGO.transform.localScale;
            if(isSegment)
                noteGO.transform.localScale *= 0.5f;
            
            noteGO.transform.rotation = Quaternion.identity;
            if(note.UsageType == EditorNote.NoteUsageType.Breaker) {
                noteGO.transform.Rotate(90, 0 , 0);
                noteGO.transform.localScale = oldScale*0.05f;
            }

            noteGO.transform.parent = Track.s_instance.m_NotesHolder;
            noteGO.name = note.name;
            wrapper.thisNoteObject = noteGO;
            noteObjects.Add(note.noteId, noteGO);
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

        public static Rail GetNextRail(int previousRailId, float time, EditorNote.NoteUsageType type) {

            Dictionary<int, Rail> rails = IdDictionaries.rails;
            if(rails == null)
                return null;
            List<int> railIdList = rails.Keys.ToList();
            railIdList.Sort((id1, id2) => rails[id1].startTime.CompareTo(rails[id2].startTime));
            foreach(int railId in railIdList) {
                if(rails[railId].scheduleForDeletion)
                    continue;
                if(previousRailId != railId && rails[railId].startTime >= time)
                    return rails[railId];
            }

            return null;
        }
        public RailNoteWrapper GetLastNote(){
            List<float> noteTimes = notesByTime.Keys.ToList();
            if(noteTimes == null || noteTimes.Count == 0)
                return null;

            noteTimes.Sort((x, y) => y.CompareTo(x));
            return notesByTime[noteTimes[0]];
        }
        public void Merge(Rail nextRail) {
            if(nextRail == null)
                return;
            RailNoteWrapper lastNote = GetLastNote();
            if(lastNote == null)
                return;

            lastNote.thisNote.UsageType = EditorNote.NoteUsageType.Line;
            InstantiateNoteObject(lastNote);
            // this destroys the displayed part
            nextRail.DestroyLeader();

            foreach(int noteId in nextRail.notesByID.Keys.ToList()) {
                AddNote(nextRail.notesByID[noteId].thisNote);
                nextRail.RemoveNote(noteId);
           }

            Trace.WriteLine("Deleting the rail: " + nextRail.railId);
            IdDictionaries.RemoveRail(nextRail.railId);
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Remove(nextRail);
            nextRail.DestroyLeader();

        }

        public static Rail CreateNewRailFromBeginEnd(float begin, float end, 
            Vector3 posBegin, Vector3 posEnd,
            EditorNote.NoteHandType handType,
            EditorNote.NoteUsageType note1UsageType, EditorNote.NoteUsageType note2UsageType) {
            Trace.WriteLine("Creating a note to add to some rail");
            EditorNote firstNote = new EditorNote(begin, posBegin, handType, note1UsageType);
            firstNote.Log();

            EditorNote secondNote = new EditorNote(end, posEnd, handType, note2UsageType);
            secondNote.Log();

            Rail rail = new Rail();
            rail.noteType = handType;
            rail.AddNote(firstNote);
            rail.AddNote(secondNote);
            rail.Log();
            IdDictionaries.AddRail(rail);
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Add(rail);
            return rail;
        }

        // attempts to find and extend some rail's head and returns the extended rail or null
        public static Rail AttemptExtendHead(float time, Vector3 position, List<Rail> rails) {

            Trace.WriteLine("Attempting to find a rail's head that can be extended with this note");
            // first we need to find the closest rail that has unbroken head to the right of current time
            rails.Sort((x, y) => x.startTime.CompareTo(y.startTime));
            bool found = false;
            Rail foundRail = null;
            foreach(Rail testedRail in rails) {
                if(testedRail.startTime > time && testedRail.breakerHead == null && testedRail.scheduleForDeletion != true) {
                    found = true;
                    foundRail = testedRail;
                    break;
                }
            }
            if(!found)
                return null;
            // this rail must no have a breaker head
            if(foundRail.breakerHead != null) {
                Trace.WriteLine("Potential candidate has a breaker head. Returning");
                return null;
            }

            // now we need to check for hindrances to extension
            bool hasHindrances = Track.HasRailInterruptionsBetween(foundRail.railId, foundRail.railId, time, foundRail.startTime, foundRail.noteType);
            if(hasHindrances) {
                Trace.WriteLine("Potential candidate has something between it and the extension time.");
                return null;
            }

            float beginOfConjoinedRailTime = default(float), endOfConjoinedRailTime = default(float);
            Vector3 beginOfConjoinedRailPos = default(Vector3), endOfConjoinedRailPos = default(Vector3);

            bool extensionBreaksTimeLimit = (foundRail.duration + (foundRail.startTime - time)) > Track.MAX_LINE_DURATION;
            float gapSize = foundRail.startTime - time;
            EditorNote noteForRail = new EditorNote(time, position,  foundRail.noteType, EditorNote.NoteUsageType.Line);
            noteForRail.Log();

            if(!extensionBreaksTimeLimit) {
                foundRail.AddNote(noteForRail);
                return foundRail;
            }

            // will only attempt to extend if the gap itself is less than max time
            if(extensionBreaksTimeLimit ) {
                if(gapSize > Track.MAX_LINE_DURATION) {
                    Trace.WriteLine("Cannot extend if the gap is longer than max time.");
                    return null;
                }
                if(foundRail.notesByID.Count == 1) {
                    Trace.WriteLine("Cannot extend if the initial rail has just one note.");
                    return null;
                }
            }

            beginOfConjoinedRailTime = time;
            endOfConjoinedRailTime = foundRail.startTime;
            EditorNote refNote = foundRail.leader.thisNote;
            beginOfConjoinedRailPos = position;
            endOfConjoinedRailPos = new Vector3(refNote.Position[0], refNote.Position[1], refNote.Position[2]); ;
            Rail result = CreateNewRailFromBeginEnd(beginOfConjoinedRailTime, endOfConjoinedRailTime, 
                beginOfConjoinedRailPos, endOfConjoinedRailPos,
                foundRail.noteType,
                EditorNote.NoteUsageType.Line, EditorNote.NoteUsageType.Breaker);
            return result;
        }


        // attempts to find and extend some rail's head and returns the extended rail or null
        public static Rail AttemptExtendTail(float time, Vector3 position, List<Rail> rails) {

            Trace.WriteLine("Attempting to find a rail's tail that can be extended with this note");
            // first we need to find the closest rail that has unbroken head to the right of current time
            rails.Sort((x, y) => y.startTime.CompareTo(x.startTime));
            bool found = false;
            Rail foundRail = null;
            foreach(Rail testedRail in rails) {
                if(testedRail.endTime < time && testedRail.breakerTail == null && testedRail.scheduleForDeletion != true) {
                    found = true;
                    foundRail = testedRail;
                    break;
                }
            }
            if(!found)
                return null;
            // this rail must no have a breaker head
            if(foundRail.breakerTail != null) {
                Trace.WriteLine("Potential candidate has a breaker head. Returning");
                return null;
            }

            // now we need to check for hindrances to extension
            bool hasHindrances = Track.HasRailInterruptionsBetween(foundRail.railId, foundRail.railId, foundRail.endTime, time, foundRail.noteType);
            if(hasHindrances) {
                Trace.WriteLine("Potential candidate has something between it and the extension time.");
                return null;
            }

            float beginOfConjoinedRailTime = default(float), endOfConjoinedRailTime = default(float);
            Vector3 beginOfConjoinedRailPos = default(Vector3), endOfConjoinedRailPos = default(Vector3);

            bool extensionBreaksTimeLimit = (foundRail.duration + (time - foundRail.endTime)) > Track.MAX_LINE_DURATION;
            float gapSize = time - foundRail.endTime;
            EditorNote noteForRail = new EditorNote(time, position, foundRail.noteType, EditorNote.NoteUsageType.Line);
            noteForRail.Log();

            if(!extensionBreaksTimeLimit) {
                foundRail.AddNote(noteForRail);
                return foundRail;
            }

            if(extensionBreaksTimeLimit) {
                if(gapSize > Track.MAX_LINE_DURATION) {
                    Trace.WriteLine("Cannot extend if the gap is longer than max time.");
                    return null;
                }
                if(foundRail.notesByID.Count == 1) {
                    Trace.WriteLine("Cannot extend if the initial rail has just one note.");
                    return null;
                }
            }

            beginOfConjoinedRailTime = foundRail.endTime;
            endOfConjoinedRailTime = time;

            EditorNote refNote = foundRail.GetLastNote().thisNote;
            beginOfConjoinedRailPos= new Vector3(refNote.Position[0], refNote.Position[1], refNote.Position[2]); ;
            endOfConjoinedRailPos  = position;
            
            Rail result = CreateNewRailFromBeginEnd(beginOfConjoinedRailTime, endOfConjoinedRailTime,
                beginOfConjoinedRailPos, endOfConjoinedRailPos,
                foundRail.noteType,
                EditorNote.NoteUsageType.Breaker, EditorNote.NoteUsageType.Line);
            return result;
        }


        public static Rail CreateNewRailAndAddNoteToIt(EditorNote note) {
            if(note == null)
                return null;
            Trace.WriteLine("Creating a note to add to some rail");

            Rail rail = new Rail();
            rail.noteType = note.HandType;
            rail.AddNote(note);
            rail.Log();
            IdDictionaries.AddRail(rail);
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Add(rail);
            return rail;
        }


        public int Size() {
            return notesByID.Count;
        }

        public static void DestroyRail(Rail rail) {
            Trace.WriteLine("Deleting the rail: " + rail.railId);
            foreach(int noteObjectKey in rail.noteObjects.Keys.ToList()){
                rail.DestroyNoteObjectAndRemoveItFromTheRail(noteObjectKey);
            }
            rail.scheduleForDeletion = true;
            IdDictionaries.RemoveRail(rail.railId);
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Remove(rail);
        }

        public static void AddIntoDictionary(Dictionary<EditorNote.NoteHandType, List<RailClickWrapper>> dict, EditorNote.NoteHandType noteType, RailClickWrapper entity) {
            if(!dict.ContainsKey(noteType))
                dict.Add(noteType, new List<RailClickWrapper>());

            dict[noteType].Add(entity);
        }

        public static bool CanPlaceSelectedRailTypeHere(float time, Vector2 clickedPoint, EditorNote.NoteHandType handType) {
            float timeRangeDuplicatesStart = time - Track.MIN_TIME_OVERLAY_CHECK;
            float timeRangeDuplicatesEnd = time + Track.MIN_TIME_OVERLAY_CHECK;
            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();

            bool isInvalidPlacementByRail = false;

            List<Rail> matches = new List<Rail>();
            foreach(Rail testedRail in rails.OrEmptyIfNull()) {
                if(testedRail.scheduleForDeletion)
                    continue;

                if(testedRail.startTime > timeRangeDuplicatesEnd) {
                    Trace.WriteLine("DISCARDING Rail: " + testedRail.railId + " starts at: " + testedRail.startTime + " which is too late");
                    continue;
                }

                if(testedRail.endTime < timeRangeDuplicatesStart) {
                    Trace.WriteLine("DISCARDING Rail: " + testedRail.railId + " ends at: " + testedRail.endTime + " which is too early");
                    continue;
                }

                if(testedRail.TimeInInterval(time)) {
                    Trace.WriteLine("ADDING Rail: " + testedRail.railId + " contains the current time.");
                    matches.Add(testedRail);
                }
                // needs optimization here
            }

            // for each matched rail we need to determine if we are clicking on the exact point it starts or ends or anywhere else
            // if we're clicking on that same point
            Dictionary<EditorNote.NoteHandType, List<RailClickWrapper>> railClicks = new Dictionary<EditorNote.NoteHandType, List<RailClickWrapper>>();
            foreach(Rail rail in matches) {
                EditorNote note = rail.GetNoteAtPosition(time);
                if(note == null) {
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoNote));
                    continue;
                }
                float distance = Vector2.Distance(new Vector2(note.Position[0], note.Position[1]), clickedPoint);
                if(distance == 0) {
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoteAtTimeAndPlace));
                }
                else
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoteAtTime));
            }

            // now that we have collected this information we can decide if we can allow click here
            {
                foreach(KeyValuePair<EditorNote.NoteHandType, List<RailClickWrapper>> kvp in railClicks) {
                    if(Track.IsOppositeNoteType(kvp.Key, handType)) {
                        //red for blue or blue for red are allowed
                        continue;
                    }
                    if(kvp.Value.Count == 1 && kvp.Value[0].clickedPointType == RailClickType.NoteAtTimeAndPlace) {
                        // same point as the end of the different type rail, this is allowed
                        continue;
                    }
                    if(kvp.Key == handType) {
                        // obviously click on the same rail type is allowed
                        continue;
                    }

                    //everything else is an error
                    isInvalidPlacementByRail = true;
                    break;
                }
            }
            bool isInvalidPlacementByNote = false;
            // now we need to collect information about notes
            // any opposite note type that is not a rail is disallowed
            Dictionary<float, List<EditorNote>> workingTrack = Track.s_instance.GetCurrentTrackDifficulty();
            // we just need this exact point
            if(workingTrack.ContainsKey(time)) {
                List<EditorNote> notes = workingTrack[time];
                if(notes != null) {
                    foreach(EditorNote testedNote in notes) {
                        if(testedNote.HandType == handType || Track.IsOppositeNoteType(testedNote.HandType,handType)) {
                            // same or opposite type are allowed
                            continue;
                        }
                        isInvalidPlacementByNote = true;
                        // everything else is an error
                        break;
                    }
                }
            }

            bool isAllowed = !isInvalidPlacementByRail && !isInvalidPlacementByNote;
            return isAllowed;
        }

        //float CanReplaceNextNote()
        public static int railCounter = 0;

        public int railId;
        public float startTime;
        public float endTime;
        public float duration;

        public EditorNote.NoteHandType noteType;

        public bool scheduleForDeletion = false;

        public Dictionary<int, RailNoteWrapper> notesByID;
        public SortedDictionary<float, RailNoteWrapper> notesByTime;
        // rail does its own object housekeeping
        // and its own note object instantiation / destruction
        public Dictionary<int, GameObject> noteObjects; 

        public RailNoteWrapper leader;
        public RailNoteWrapper breakerTail;
        public RailNoteWrapper breakerHead;
        //public LongNote railInstance;
        Game_LineWaveCustom waveCustom;
        
    }

}
