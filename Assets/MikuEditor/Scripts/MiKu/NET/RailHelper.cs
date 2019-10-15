using MiKu.NET.Charting;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using ThirdParty.Custom;
using UnityEngine;
using Shogoki.Utils;

namespace MiKu.NET {
    public static class RailHelper {
        //public static FloatEqualityComparer floatComparator = new FloatEqualityComparer();
        public enum RailRangeBehaviour {
            Skip = 0,
            Allow = 1,
        }
        public enum RailFetchBehaviour {
            All = 0,
            HasPointsAtCurrentTime = 1,
            HasEdgesAtCurrentTime = 2,
            StartsAtCurrentTime = 3,
        }

        public static void Print2DArray<T>(T[,] matrix) {
            for(int i = 0; i < matrix.GetLength(0); i++) {
                for(int j = 0; j < matrix.GetLength(1); j++) {
                    Trace.Write(matrix[i, j] + "\t");
                }
                Trace.WriteLine("");
            }
        }

        public static void ReinstantiateRailSegmentObjects(Rail rail) {
            List<TimeWrapper> times = rail.notesByTime.Keys.ToList();
            if(times.Count == 1)
                return;
            for(int i = 1; i < times.Count; i++) {
                rail.InstantiateNoteObject(rail.notesByTime[times[i]]);
            }
        }

        public static void ReinstantiateRail(Rail rail, bool addSfx = true) {
            if(rail == null) {
                Trace.WriteLine("Can't instantiate the null rail. Returning");

                return;
            }
            Trace.WriteLine("Called to reinstantiate the rail: " + rail.railId);
            // need to somehow indicate that rail is too shor or too long, maybe with a color
            if(rail.duration < Track.MIN_LINE_DURATION ||
                rail.duration > Track.MAX_LINE_DURATION) {

                if(rail.Leader != null) {

                    rail.DestroyNoteObjectAndRemoveItFromTheRail(rail.Leader.thisNote.noteId);
                    rail.InstantiateNoteObject(rail.Leader);
                }
                Trace.WriteLine("Rail duration is at: " + rail.duration + " Min duration is:" + Track.MIN_LINE_DURATION + "Max Duration is: "  + Track.MAX_LINE_DURATION);
                return;
            }

            //if(!Track.Instance.AddTimeToCurrentTrack(rail.startTime)) {
            //    Trace.WriteLine("Failed to add the time to track. Returning");
            //    return;
            //}


            EditorNote leaderNote = rail.Leader.thisNote;
            leaderNote.Segments = new float[rail.notesByID.Count, 3];

            Trace.WriteLine("Will instantiate the rail: " + rail.railId +  " for note:" + leaderNote.noteId + " x:" + leaderNote.Position[0] + " y:" + leaderNote.Position[1] + " z:" + leaderNote.Position[2]);

            //float[,] testedArray = new float[rail.notesByID.Count, 3];

            List<TimeWrapper> keys = rail.notesByTime.Keys.ToList();
            keys.Sort();
            int i = 0;
            try {
                for(i = 0; i < keys.Count; i++) {
                    TimeWrapper key = keys[i];
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
            rail.DestroyNoteObjectAndRemoveItFromTheRail(rail.Leader.thisNote.noteId);
            rail.InstantiateNoteObject(rail.Leader);
            GameObject leaderObject = rail.Leader.thisNoteObject;

            Game_LineWaveCustom waveCustom = leaderObject.GetComponentInChildren<Game_LineWaveCustom>();
            if(waveCustom != null) {
                Trace.WriteLine("Rendering line");
                waveCustom.targetOptional = leaderNote.Segments;
                waveCustom.RenderLine(true);
                if(addSfx)
                    Track.AddTimeToSFXList(rail.startTime);
            } else {
                Trace.WriteLine("Failed to get component to render line");
            }
        }

        public static void CleanupRailObjects(Rail rail) {
            List<int> ids = rail.noteObjects.Keys.ToList();
            foreach(int id in ids.OrEmptyIfNull())
                GameObject.DestroyImmediate(rail.noteObjects[id]);
            ids = rail.notesByID.Keys.ToList();
            rail.noteObjects.Clear();
            foreach(int id in ids.OrEmptyIfNull()) {
                rail.notesByID[id].thisNoteObject = null;
            }
        }

        public static void Destroy(Rail rail) {
            //Trace.WriteLine("Called to destroy the rail: " + rail.railId);
            //GameObject.DestroyImmediate(rail.waveCustom);
        }


        public static Rail GetNextRail(int thisRailId, TimeWrapper time, EditorNote.NoteHandType handType = EditorNote.NoteHandType.NoHand, EditorNote.NoteUsageType usageType = EditorNote.NoteUsageType.Line) {

            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();
            if(rails == null)
                return null;

            if(handType != EditorNote.NoteHandType.NoHand)
                rails = rails.Where(rail => rail.noteType == handType).ToList();

            rails.Sort((rail1, rail2) => rail1.startTime.CompareTo(rail2.startTime));
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                if(rail.scheduleForDeletion)
                    continue;
                if(thisRailId != rail.railId && rail.startTime >= time)
                    return rail;
            }

            return null;
        }


        public static Rail GetPreviousRail(int thisRailId, TimeWrapper time, EditorNote.NoteHandType handType = EditorNote.NoteHandType.NoHand, EditorNote.NoteUsageType usageType = EditorNote.NoteUsageType.Line) {

            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();
            if(rails == null)
                return null;

            if(handType != EditorNote.NoteHandType.NoHand)
                rails = rails.Where(rail => rail.noteType == handType).ToList();

            rails.Sort((rail1, rail2) => rail2.startTime.CompareTo(rail1.startTime));
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                if(rail.scheduleForDeletion)
                    continue;
                if(thisRailId != rail.railId && rail.startTime <= time)
                    return rail;
            }

            return null;
        }


        public static Rail CreateNewRailFromBeginEnd(TimeWrapper begin, TimeWrapper end,
            Vector3 posBegin, Vector3 posEnd,
            EditorNote.NoteHandType handType,
            EditorNote.NoteUsageType note1UsageType, EditorNote.NoteUsageType note2UsageType) {
            Trace.WriteLine("Creating a note to add to some rail");
            EditorNote firstNote = new EditorNote(begin.FloatValue, posBegin, handType, note1UsageType);
            firstNote.Log();

            EditorNote secondNote = new EditorNote(end.FloatValue, posEnd, handType, note2UsageType);
            secondNote.Log();

            Rail rail = new Rail();
            rail.noteType = handType;
            rail.AddNote(firstNote);
            rail.AddNote(secondNote);
            rail.Log();
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Add(rail);
            return rail;
        }


        public enum RailExtensionPolicy {
            NoInterruptions = 0,
            AllowNotesOfSameColor = 1,
            AllowNotesOfAnyColor = 2
        }

        // attempts to find and extend some rail's head and returns the extended rail or null
        public static Rail AttemptExtendHead(TimeWrapper time, Vector3 position, List<Rail> rails, RailExtensionPolicy extensionPolicy = RailExtensionPolicy.NoInterruptions) {

            Trace.WriteLine("Attempting to find a rail's head that can be extended with this note");
            // first we need to find the closest rail that has unbroken head to the right of current time
            rails.Sort((x, y) => x.startTime.CompareTo(y.startTime));
            bool found = false;
            Rail foundRail = null;
            foreach(Rail testedRail in rails.OrEmptyIfNull()) {
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
            bool hasHindrances = Track.HasRailInterruptionsBetween(foundRail.railId, foundRail.railId, time, foundRail.startTime, foundRail.noteType, extensionPolicy);
            if(hasHindrances) {
                Trace.WriteLine("Potential candidate has something between it and the extension time.");
                return null;
            }

            TimeWrapper beginOfConjoinedRailTime = default(TimeWrapper), endOfConjoinedRailTime = default(TimeWrapper);
            Vector3 beginOfConjoinedRailPos = default(Vector3), endOfConjoinedRailPos = default(Vector3);

            bool extensionBreaksTimeLimit = (foundRail.duration + (foundRail.startTime - time)) > Track.MAX_LINE_DURATION;
            TimeWrapper gapSize = foundRail.startTime - time;
            EditorNote noteForRail = new EditorNote(time.FloatValue, position, foundRail.noteType, EditorNote.NoteUsageType.Line);
            noteForRail.Log();

            if(!extensionBreaksTimeLimit) {

                foundRail.AddNote(noteForRail);
                return foundRail;
            }

            // will only attempt to extend if the gap itself is less than max time
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

            beginOfConjoinedRailTime = time;
            endOfConjoinedRailTime = foundRail.startTime;
            EditorNote refNote = foundRail.Leader.thisNote;
            beginOfConjoinedRailPos = position;
            endOfConjoinedRailPos = new Vector3(refNote.Position[0], refNote.Position[1], refNote.Position[2]); ;
            Rail result = CreateNewRailFromBeginEnd(beginOfConjoinedRailTime, endOfConjoinedRailTime,
                beginOfConjoinedRailPos, endOfConjoinedRailPos,
                foundRail.noteType,
                EditorNote.NoteUsageType.Line, EditorNote.NoteUsageType.Breaker);
            return result;
        }


        // attempts to find and extend some rail's head and returns the extended rail or null
        public static Rail AttemptExtendTail(TimeWrapper time, Vector3 position, List<Rail> rails, RailExtensionPolicy extensionPolicy = RailExtensionPolicy.NoInterruptions) {

            Trace.WriteLine("Attempting to find a rail's tail that can be extended with this note");
            // first we need to find the closest rail that has unbroken head to the right of current time
            rails.Sort((x, y) => y.startTime.CompareTo(x.startTime));
            bool found = false;
            Rail foundRail = null;
            foreach(Rail testedRail in rails.OrEmptyIfNull()) {
                if(testedRail.endTime < time && testedRail.breakerTail == null && testedRail.scheduleForDeletion != true) {
                    found = true;
                    foundRail = testedRail;
                    break;
                }
            }
            if(!found)
                return null;
            // this rail must no have a breaker tail
            if(foundRail.breakerTail != null) {
                Trace.WriteLine("Potential candidate has a breaker head. Returning");
                return null;
            }

            // now we need to check for hindrances to extension
            bool hasHindrances = Track.HasRailInterruptionsBetween(foundRail.railId, foundRail.railId, foundRail.endTime, time, foundRail.noteType, extensionPolicy);
            if(hasHindrances) {
                Trace.WriteLine("Potential candidate has something between it and the extension time.");
                return null;
            }

            TimeWrapper beginOfConjoinedRailTime = default(TimeWrapper), endOfConjoinedRailTime = default(TimeWrapper);
            Vector3 beginOfConjoinedRailPos = default(Vector3), endOfConjoinedRailPos = default(Vector3);

            bool extensionBreaksTimeLimit = (foundRail.duration + (time - foundRail.endTime)) > Track.MAX_LINE_DURATION;
            TimeWrapper gapSize = time - foundRail.endTime;
            EditorNote noteForRail = new EditorNote(time.FloatValue, position, foundRail.noteType, EditorNote.NoteUsageType.Line);
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
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Add(rail);
            return rail;
        }




        public static void DestroyRail(Rail rail) {
            TimeWrapper oldTime = rail.startTime;
            Trace.WriteLine("Deleting the rail: " + rail.railId);
            foreach(int noteObjectKey in rail.noteObjects.Keys.ToList().OrEmptyIfNull()) {
                rail.DestroyNoteObjectAndRemoveItFromTheRail(noteObjectKey);
            }
            rail.scheduleForDeletion = true;
            List<Rail> tempRailList = Track.s_instance.GetCurrentRailListByDifficulty();
            tempRailList.Remove(rail);
            var set = Track.CollectOccupiedTimes();
            if(!set.Contains(oldTime)) {
                Track.RemoveTimeFromSFXList(oldTime);
            }
        }

        public static void AddIntoDictionary(Dictionary<EditorNote.NoteHandType, List<RailClickWrapper>> dict, EditorNote.NoteHandType noteType, RailClickWrapper entity) {
            if(!dict.ContainsKey(noteType))
                dict.Add(noteType, new List<RailClickWrapper>());

            dict[noteType].Add(entity);
        }

        public static bool CanPlaceSelectedRailTypeHere(TimeWrapper time, Vector2 clickedPoint, EditorNote.NoteHandType handType) {
            
            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();

            bool isInvalidPlacementByRail = false;

            List<Rail> matches = new List<Rail>();
            foreach(Rail testedRail in rails.OrEmptyIfNull()) {
                if(testedRail.scheduleForDeletion)
                    continue;

                if(testedRail.startTime > time) {
                    Trace.WriteLine("DISCARDING Rail: " + testedRail.railId + " starts at: " + testedRail.startTime + " which is too late");
                    continue;
                }

                if(testedRail.endTime < time) {
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
            foreach(Rail rail in matches.OrEmptyIfNull()) {
                EditorNote note = rail.GetNoteAtPosition(time);
                if(note == null) {
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoNote));
                    continue;
                }
                float distance = Vector2.Distance(new Vector2(note.Position[0], note.Position[1]), clickedPoint);
                if(distance == 0) {
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoteAtTimeAndPlace));
                } else
                    AddIntoDictionary(railClicks, rail.noteType, new RailClickWrapper(rail, RailClickType.NoteAtTime));
            }

            // now that we have collected this information we can decide if we can allow click here
            {
                foreach(KeyValuePair<EditorNote.NoteHandType, List<RailClickWrapper>> kvp in railClicks.OrEmptyIfNull()) {
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
            Dictionary<TimeWrapper, List<EditorNote>> workingTrack = Track.s_instance.GetCurrentTrackDifficulty();
            // we just need this exact point
            if(workingTrack.ContainsKey(time)) {
                List<EditorNote> notes = workingTrack[time];
                if(notes != null) {
                    foreach(EditorNote testedNote in notes.OrEmptyIfNull()) {
                        if(testedNote.HandType == handType || Track.IsOppositeNoteType(testedNote.HandType, handType)) {
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

        public static Rail CreateRailFromSegments(float bpm, TimeWrapper startTime, EditorNote note) {
            float[,] segments = note.Segments;
            Rail newRail = new Rail();
            newRail.noteType = note.HandType;
            note.UsageType = EditorNote.NoteUsageType.Breaker;
            newRail.AddNote(note, true);

            for(int i = 0; i < segments.GetLength(0); i++) {
                EditorNote railNote = null;
                // we need to create a separate note unless it's the first note of the sequence

                TimeWrapper ms = Track.UnitToMS(segments[i, 2]);
                ms = ChartConverter.UpdateTimeToBPM(ms, bpm);
                railNote = new EditorNote(ms.FloatValue, new Vector3(segments[i, 0], segments[i, 1], segments[i, 2]), note.HandType, EditorNote.NoteUsageType.Line);

                RailNoteWrapper wrapper = new RailNoteWrapper(railNote);
                newRail.notesByID.Add(railNote.noteId, wrapper);
                newRail.notesByTime.Add(railNote.TimePoint, wrapper);
            }
            if(newRail.notesByID.Count == 0)
                return null;

            newRail.Leader = newRail.notesByTime.First().Value;
            newRail.RecalcDuration();
            List<TimeWrapper> directOrder = newRail.notesByTime.Keys.ToList();
            List<TimeWrapper> reverseOrder = newRail.notesByTime.Keys.ToList();
            reverseOrder.Reverse();
            RailNoteWrapper previousNote = null;
            foreach(TimeWrapper time in directOrder.OrEmptyIfNull()) {
                newRail.notesByTime[time].AssignPreviousNote(previousNote);
                previousNote = newRail.notesByTime[time];
            }
            RailNoteWrapper nextNote = null;
            foreach(TimeWrapper time in reverseOrder.OrEmptyIfNull()) {
                newRail.notesByTime[time].nextNote = nextNote;
                nextNote = newRail.notesByTime[time];
            }

            newRail.notesByTime.First().Value.thisNote.UsageType = EditorNote.NoteUsageType.Breaker;
            newRail.notesByTime.Last().Value.thisNote.UsageType = EditorNote.NoteUsageType.Breaker;
            if(newRail.Leader != newRail.GetLastNote())
                newRail.breakerTail = newRail.GetLastNote();

            return newRail;
        }


        public static Rail CloneRail(Rail rail, TimeWrapper start, TimeWrapper end, RailRangeBehaviour copyType) {
            if(rail == null)
                return null;


            bool skipWholeRail = false;
            List<RailNoteWrapper> newList = new List<RailNoteWrapper>();
            foreach(RailNoteWrapper note in rail.notesByTime.Values.OrEmptyIfNull()) {
                RailNoteWrapper nextNote = note.nextNote;
                if(note.thisNote.TimePoint >= start && note.thisNote.TimePoint <= end) {
                    newList.Add(new RailNoteWrapper(note.thisNote.Clone()));
                    continue;
                }
                if(note.thisNote.TimePoint < start && note.nextNote != null && note.nextNote.thisNote.TimePoint >= start) {
                    if(copyType == RailRangeBehaviour.Allow) {
                        newList.Add(new RailNoteWrapper(note.thisNote.Clone()));
                        continue;
                    }
                }
                if(note.thisNote.TimePoint > end && note.GetPreviousNote() != null && note.GetPreviousNote().thisNote.TimePoint <= end) {
                    if(copyType == RailRangeBehaviour.Allow) {
                        newList.Add(new RailNoteWrapper(note.thisNote.Clone()));
                        continue;
                    }
                }
                skipWholeRail = true;
                break;
            }
            if(skipWholeRail)
                return null;

            RailNoteWrapper previousNote = null;
            foreach(RailNoteWrapper note in newList.OrEmptyIfNull()) {
                note.AssignPreviousNote(previousNote);
                if(previousNote != null)
                    previousNote.nextNote = note;
                previousNote = note;
            }

            if(newList.Count == 0)
                return null;

            Rail newRail = new Rail();
            newRail.noteType = rail.noteType;
            newRail.Log();

            foreach(RailNoteWrapper note in newList.OrEmptyIfNull()) {
                newRail.notesByTime.Add(note.thisNote.TimePoint, note);
                newRail.notesByID.Add(note.thisNote.noteId, note);
            }
            newRail.Leader = newRail.notesByTime.First().Value;
            if(newRail.notesByTime.Last().Value.thisNote.UsageType == EditorNote.NoteUsageType.Breaker)
                newRail.breakerTail = newRail.notesByTime.Last().Value;

            if(newRail.notesByTime.First().Value.thisNote.UsageType == EditorNote.NoteUsageType.Breaker)
                newRail.breakerHead = newRail.notesByTime.First().Value;

            newRail.RecalcDuration();
            return newRail;
        }

        public static List<Rail> GetCopyOfRailsInRange(List<Rail> rails, TimeWrapper rangeStart, TimeWrapper rangeEnd, RailRangeBehaviour copyType) {
            List<Rail> copies = new List<Rail>();
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                Rail copy = CloneRail(rail, rangeStart, rangeEnd, copyType);
                if(copy != null)
                    copies.Add(copy);
            }
            if(copies.Count == 0)
                return copies;

            return copies;
        }

        public static List<Rail> GetListOfRailsInRange(List<Rail> rails, TimeWrapper rangeStart, TimeWrapper rangeEnd, RailRangeBehaviour rangeFetchType, RailFetchBehaviour pointFetchType = RailFetchBehaviour.All) {
            Trace.WriteLine("Trying to find a rail with start time: " + rangeStart.FloatValue + "end time: " + rangeEnd.FloatValue);
            List<Rail> fetchedRails = new List<Rail>();
            bool pointFetch = rangeStart == rangeEnd;
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                Trace.WriteLine("Trying rail: " + rail.railId + "start time: " + rail.startTime.FloatValue + "end time: " + rail.endTime.FloatValue);
                if(rail.startTime >= rangeStart && rail.endTime <= rangeEnd) {
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasPointsAtCurrentTime && !rail.HasNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasEdgesAtCurrentTime && !rail.HasEdgeNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.StartsAtCurrentTime && rail.Leader.thisNote.TimePoint != rangeStart)
                        continue;
                    fetchedRails.Add(rail);
                    continue;
                }
                if(rail.startTime >= rangeStart && rail.startTime <= rangeEnd && rail.endTime > rangeEnd && rangeFetchType == RailRangeBehaviour.Allow) {
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasPointsAtCurrentTime && !rail.HasNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasEdgesAtCurrentTime && !rail.HasEdgeNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.StartsAtCurrentTime && rail.Leader.thisNote.TimePoint != rangeStart)
                        continue;
                    fetchedRails.Add(rail);
                    continue;
                }
                if(rail.startTime < rangeStart && rail.endTime >= rangeStart && rail.endTime <= rangeEnd && rangeFetchType == RailRangeBehaviour.Allow) {
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasPointsAtCurrentTime && !rail.HasNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasEdgesAtCurrentTime && !rail.HasEdgeNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.StartsAtCurrentTime && rail.Leader.thisNote.TimePoint != rangeStart)
                        continue;
                    fetchedRails.Add(rail);
                    continue;
                }
                if(rail.startTime < rangeStart && rail.endTime >= rangeEnd && rangeFetchType == RailRangeBehaviour.Allow) {
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasPointsAtCurrentTime && !rail.HasNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.HasEdgesAtCurrentTime && !rail.HasEdgeNoteAtTime(rangeStart))
                        continue;
                    if(pointFetch && pointFetchType == RailFetchBehaviour.StartsAtCurrentTime && rail.Leader.thisNote.TimePoint != rangeStart)
                        continue;
                    fetchedRails.Add(rail);
                    continue;
                }
            }
            if(fetchedRails.Count == 0)
                return null;
            return fetchedRails;
        }

        public static void RemoveRailsWithinRange(List<Rail> rails, TimeWrapper rangeStart, TimeWrapper rangeEnd, RailRangeBehaviour copyType) {
            List<Rail> fetchedRails = GetListOfRailsInRange(rails, rangeStart, rangeEnd, copyType);
            if(fetchedRails == null)
                return;

            foreach(Rail rail in fetchedRails.OrEmptyIfNull()) {
                RailHelper.DestroyRail(rail);
                Track.s_instance.DecreaseTotalDisplayedNotesCount();
            }
        }
        public static void DestroyAllRailsForCurrentDifficulty() {
            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                Trace.WriteLine("Deleting the rail: " + rail.railId);
                foreach(int noteObjectKey in rail.noteObjects.Keys.ToList().OrEmptyIfNull()) {
                    rail.DestroyNoteObjectAndRemoveItFromTheRail(noteObjectKey);
                }
                rail.scheduleForDeletion = true;
            }
            Track.s_instance.ResetCurrentRailList();
        }

        public static Rail ClosestRailButNotAtThisPoint(TimeWrapper time, Vector2 point, EditorNote.NoteHandType handType = EditorNote.NoteHandType.NoHand) {
            List<Rail> railsAtCurrentTime = RailHelper.GetListOfRailsInRange(Track.s_instance.GetCurrentRailListByDifficulty(), time, time, RailHelper.RailRangeBehaviour.Allow);
            if(railsAtCurrentTime == null)
                return null;

            List<Rail> railsWithJunctionsAtThisTime = new List<Rail>();
            foreach(Rail rail in railsAtCurrentTime.OrEmptyIfNull()) {
                if(handType != rail.noteType && handType!= EditorNote.NoteHandType.NoHand)
                    continue;
                if(rail.HasNoteAtTime(time))
                    railsWithJunctionsAtThisTime.Add(rail);
            }

            bool foundRailAtExactPoint = false;

            Dictionary<TimeWrapper, Rail> dictOfDistances = new Dictionary<TimeWrapper, Rail>(new TimeWrapper());
            foreach(Rail rail in railsWithJunctionsAtThisTime.OrEmptyIfNull()) {
                EditorNote note = rail.GetNoteAtPosition(time);
                float distance = Vector2.Distance(point, new Vector2(note.Position[0], note.Position[1]));
                if(distance > 0.05) {
                    if(dictOfDistances == null)
                        dictOfDistances.Add(Vector2.Distance(point, new Vector2(note.Position[0], note.Position[1])), rail);
                    else
                        dictOfDistances[distance] = rail;
                } else {
                    foundRailAtExactPoint = true;
                }
            }
            if(foundRailAtExactPoint)
                return null;
            List<TimeWrapper> distances = dictOfDistances.Keys.ToList();
            distances.Sort();
            if(distances.Count == 0)
                return null;
            return dictOfDistances[distances[0]];
        }

        public static void ShiftHorizontalPositionOFCurrentRail(TimeWrapper time, float value, EditorNote.NoteHandType handType) {
            List<Rail> railsAtCurrentTime = RailHelper.GetListOfRailsInRange(Track.s_instance.GetCurrentRailListByDifficulty(), time, time, RailHelper.RailRangeBehaviour.Allow);

            List<Rail> railsWithJunctionsAtThisTime = new List<Rail>();
            foreach(Rail rail in railsAtCurrentTime.OrEmptyIfNull()) {
                if(rail.HasNoteAtTime(time) && rail.noteType == handType) {
                    EditorNote note = rail.GetNoteAtPosition(time);
                    Vector3 newPos = NotesArea.s_instance.grid.GetNextPointOnGrid(new Vector3(note.Position[0], note.Position[1], note.Position[2]), value > 0, GridManager.GridShiftBehaviour.Horizonal);
                    float xDiff = Math.Abs(newPos.x - note.Position[0]);
                    if(value < 0)
                        xDiff*=-1;
                    rail.ShiftEveryNoteBy(new Vector2(xDiff, 0));
                    ReinstantiateRail(rail);
                    ReinstantiateRailSegmentObjects(rail);
                }
            }
        }
        public static void ShiftVerticalPositionOFCurrentRail(float time, float value, EditorNote.NoteHandType handType) {
            List<Rail> railsAtCurrentTime = RailHelper.GetListOfRailsInRange(Track.s_instance.GetCurrentRailListByDifficulty(), time, time, RailHelper.RailRangeBehaviour.Allow);

            List<Rail> railsWithJunctionsAtThisTime = new List<Rail>();
            foreach(Rail rail in railsAtCurrentTime.OrEmptyIfNull()) {
                if(rail.HasNoteAtTime(time) && rail.noteType == handType) {
                    EditorNote note = rail.GetNoteAtPosition(time);
                    Vector3 newPos = NotesArea.s_instance.grid.GetNextPointOnGrid(new Vector3(note.Position[0], note.Position[1], note.Position[2]), value > 0, GridManager.GridShiftBehaviour.Vertical);
                    float yDiff = Math.Abs(newPos.y - note.Position[1]);
                    if(value < 0)
                        yDiff*=-1;
                    rail.ShiftEveryNoteBy(new Vector2(0, yDiff));
                    ReinstantiateRail(rail);
                    ReinstantiateRailSegmentObjects(rail);
                }
            }
        }

        public enum RailTimeFindPolicy {
            Everything = 0,
            EdgesOnly = 1
        }
        public static List<TimeWrapper> CollectRailTimes(RailTimeFindPolicy railTimeFindPolicy = RailTimeFindPolicy.Everything) {
            List<TimeWrapper> times = new List<TimeWrapper>();
            List<Rail> rails = Track.s_instance.GetCurrentRailListByDifficulty();
            rails.Sort((rail1, rail2) => rail1.startTime.CompareTo(rail2.startTime));
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                if(railTimeFindPolicy == RailTimeFindPolicy.EdgesOnly) {
                    times.Add(rail.startTime);
                    times.Add(rail.endTime);
                } else {
                    foreach(RailNoteWrapper note in rail.notesByTime.Values.OrEmptyIfNull()) {
                        times.Add(note.thisNote.TimePoint);
                    }
                }
            }
            return times;
        }

        public static void LogRails(List<Rail> rails, string operation) {
            rails.Sort((rail1, rail2) => rail1.startTime.CompareTo(rail2.startTime));
            Trace.WriteLine("Rails before " + operation);
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                Trace.WriteLine("Rail id:" + rail.railId + " starts at: " + rail.startTime + " ends at: " + rail.endTime);
                foreach(RailNoteWrapper note in rail.notesByTime.Values.OrEmptyIfNull()) {
                    Trace.WriteLine("Rail segment point is located at:" + " x:" + note.thisNote.Position[0] + " y:" + note.thisNote.Position[1] + " z:" + note.thisNote.Position[2]+ " note type is: " + note.thisNote.UsageType);
                }
            }
        }

        public static void BreakTheRailAtCurrentTime(TimeWrapper time, List<Rail> rails, EditorNote.NoteHandType noteType, EditorNote.NoteUsageType usageType, bool isOnMirrorMode) {
            // detect a rail at the current time
            // check that it has an edge note here
            // flip its breaker state
            List<Rail> railsAtCurrentTime = RailHelper.GetListOfRailsInRange(rails, time, time, RailHelper.RailRangeBehaviour.Allow, RailHelper.RailFetchBehaviour.HasPointsAtCurrentTime);

            if(railsAtCurrentTime != null) {

                bool breakerRemoved = false;
                List<Rail> railCheck = new List<Rail>();
                // need to detect the case of conjoined rails with a breaker of the same color
                foreach (Rail rail in railsAtCurrentTime.OrEmptyIfNull()) {
                    if (rail.noteType != noteType) {
                        continue;
                    }
                    railCheck.Add(rail);
                }
                railCheck.Sort((x, y) => x.startTime.CompareTo(y.startTime));
                if (railCheck.Count == 2) {
                    RailNoteWrapper tempFirstRailNote = railCheck[0].GetNoteWrapperAtPosition(time);
                    RailNoteWrapper tempSecondRailNote = railCheck[1].GetNoteWrapperAtPosition(time);
                    if (Track.PreviousTime < Track.CurrentTime && railCheck[1].notesByID.Count > 1)
                    {
                        railCheck[1].RemoveNote(tempSecondRailNote.thisNote.noteId);
                        railCheck[1].FlipNoteTypeToLineWithoutMerging(railCheck[1].Leader.thisNote.noteId);
                        railCheck[0].Merge(railCheck[1]);
                    }
                    else if (Track.PreviousTime >= Track.CurrentTime && railCheck[0].notesByID.Count > 1){
                        RailNoteWrapper previous = tempFirstRailNote.GetPreviousNote();
                        railCheck[0].RemoveNote(tempFirstRailNote.thisNote.noteId);
                        railCheck[0].FlipNoteTypeToLineWithoutMerging(previous.thisNote.noteId);
                        railCheck[1].FlipNoteTypeToLineWithoutMerging(tempSecondRailNote.thisNote.noteId);
                        railCheck[0].Merge(railCheck[1]);
                    }
                    return;
                }

                foreach(Rail rail in railsAtCurrentTime.OrEmptyIfNull()) {
                    // skipping different colored rails
                    bool allowedOppositeColor = isOnMirrorMode && Track.IsOppositeNoteType(rail.noteType, noteType);
                    if(railsAtCurrentTime.Count > 1 && rail.noteType != noteType && !allowedOppositeColor)
                        continue;

                    EditorNote.NoteHandType adjustedNoteType = noteType;
                    if(allowedOppositeColor)
                        adjustedNoteType = Track.GetOppositeColor(noteType);
                    if(railsAtCurrentTime.Count == 1) {
                        adjustedNoteType = rail.noteType;
                    }

                    EditorNote railNote = rail.GetNoteAtPosition(time);
                    if(railNote == null)
                        continue;

                    // if the rail doesn't end there we just break it
                    if(railNote.noteId != rail.Leader.thisNote.noteId && railNote.noteId != rail.GetLastNote().thisNote.noteId) {
                        rail.FlipNoteTypeToBreaker(railNote.noteId);
                        continue;
                    }


                    // we're at the edge note and will need to flip it. 
                    // just onne last check if it's the sole not of the rail
                    if(railNote.UsageType == EditorNote.NoteUsageType.Line)
                        rail.FlipNoteTypeToBreaker(railNote.noteId);
                    else {
                        // we're removing a breaker. need to check if there's a rail next to this one that we can attach to
                        // for that we check if there are NO notes of any type other than the opposite hand until the next rail
                        TimeWrapper railEndTime = rail.endTime;

                        //todo
                        // need to differentiate between had and tail breakers here
                        // also it looks like the next rail can be picked as wrong color
                        bool mergeHappened = false;
                        if(railNote.noteId == rail.Leader.thisNote.noteId) {
                            // flipping the leader note
                            Rail previousRail = RailHelper.GetPreviousRail(rail.railId, rail.startTime, adjustedNoteType, usageType);


                            // need to make sure that merged rail doesn't exceed duration
                            if(previousRail != null) {
                                TimeWrapper railLengthAfterMerge = (previousRail.endTime - rail.startTime) + rail.duration + previousRail.duration;
                                if(railLengthAfterMerge <= Track.MAX_LINE_DURATION) {
                                    TimeWrapper previousRailEndTime = previousRail.endTime;
                                    // for railEndTime and nextRailStartTIme we check if there are ANY notes not of the opposite type
                                    if(!Track.HasRailInterruptionsBetween(rail.railId, previousRail.railId, previousRailEndTime, rail.startTime, rail.noteType)) {
                                        // no interrupting notes or rails, can link this rail and the next one
                                        if((previousRail.breakerTail == null || previousRail.notesByID.Count == 1 ) && !breakerRemoved) {
                                            Track.s_instance.DecreaseTotalDisplayedNotesCount();
                                            previousRail.Merge(rail);
                                        } else {
                                            breakerRemoved = true;
                                            rail.FlipNoteTypeToLineWithoutMerging(railNote.noteId);
                                        }
                                        continue;
                                    }
                                } else {
                                    // notify that we can't merge
                                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_MergedRailTooLong);
                                }
                            }
                        } else {
                            // flipping the tail note
                            Rail nextRail = RailHelper.GetNextRail(rail.railId, railEndTime, adjustedNoteType, usageType);
                            // need to make sure that merged rail doesn't exceed duration
                            if(nextRail != null) {
                                TimeWrapper railLengthAfterMerge = (nextRail.startTime - rail.endTime) + rail.duration + nextRail.duration;
                                if(railLengthAfterMerge <= Track.MAX_LINE_DURATION) {
                                    TimeWrapper nextRailStartTIme = nextRail.startTime;
                                    // for railEndTime and nextRailStartTIme we check if there are ANY notes not of the opposite type
                                    if(!Track.HasRailInterruptionsBetween(rail.railId, nextRail.railId, railEndTime, nextRailStartTIme, rail.noteType)) {
                                        // no interrupting notes or rails, can link this rail and the next one

                                        if((nextRail.breakerHead == null || nextRail.notesByID.Count == 1) && !breakerRemoved) {
                                            rail.Merge(nextRail);
                                            Track.s_instance.DecreaseTotalDisplayedNotesCount();
                                        } else {
                                            breakerRemoved = true;
                                            rail.FlipNoteTypeToLineWithoutMerging(railNote.noteId);
                                        }
                                        continue;
                                    }
                                } else {
                                    // notify that we can't merge
                                    Miku_DialogManager.ShowDialog(Miku_DialogManager.DialogType.Alert, StringVault.Alert_MergedRailTooLong);
                                }
                            }

                        }
                        if(!mergeHappened) {
                            rail.FlipNoteTypeToLineWithoutMerging(railNote.noteId);
                            continue;
                        }
                    }


                }
            }
        }
        public static void SanitycheckRailList(List<Rail> rails) {
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                if(rail.breakerHead == null || rail.breakerTail == null)
                    Trace.WriteLine("Rail isn't broken: " + rail.railId);
            }
        }

        public static List<Vector2> FetchRailPositionsAtTime(TimeWrapper time, List<Rail> rails) {
            List<Vector2> list = new List<Vector2>();
            foreach(Rail rail in rails.OrEmptyIfNull()) {
                EditorNote note = rail.GetNoteAtPosition(time);
                if(note != null) {
                    list.Add(new Vector2(note.Position[0], note.Position[1]));
                }
            }
            return list;
        }

    }


    
}