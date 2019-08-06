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
            notesByTime = new Dictionary<float, RailNoteWrapper>();
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
            RailNoteWrapper previous = GetPreviousNote(note.TimePoint);
            // by this point we've established this note isn't a sole note of a rail
            // also at this point breaker can only be the last note in the segment
            // by default removing a breaker does _not_ merge the rail to the next one
            // neither does it automatically make the previous note a breaker
            previous.nextNote = null;
            breakerTail = null;
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

        //need to handle special case of adding a breaker to an already broken rail
        bool AddBreakerNote(RailNoteWrapper wrapper) {
            if(HasNoteAtTime(wrapper.thisNote.TimePoint)) {
                // for simplicity we just flip an existing note to a breaker type
                // otherwise it becomes very counterintuitive
                EditorNote foundNote = GetNoteAtPosition(wrapper.thisNote.TimePoint);
                FlipNoteTypeToBreaker(foundNote.noteId, EditorNote.NoteUsageType.Breaker);
                return false;
            }
        

            Trace.WriteLine("Called to add a BREAKER note positioned at x:" + wrapper.thisNote.Position[0] + " y:" + wrapper.thisNote.Position[1] + " z:" + wrapper.thisNote.Position[2]);

            EditorNote note = wrapper.thisNote;
            Rail potentialNewRail = null;

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

        public void FlipNoteTypeToBreaker(int noteId, EditorNote.NoteUsageType usageType) {
            Trace.WriteLine("Flipping the note on existing rail to breaker");
            RailNoteWrapper note = notesByID[noteId];
            if(note == null)
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
            if(note == null)
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

        Rail ConvertTheTailIntoNewRail(RailNoteWrapper note) {
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
            noteGO.name = note.Id;
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

        //float CanReplaceNextNote()
        public static int railCounter = 0;

        public int railId;
        public float startTime;
        public float endTime;
        public float duration;

        public EditorNote.NoteHandType noteType;

        public bool scheduleForDeletion = false;

        public Dictionary<int, RailNoteWrapper> notesByID;
        public Dictionary<float, RailNoteWrapper> notesByTime;
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
