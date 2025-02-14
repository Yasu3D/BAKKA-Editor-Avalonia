﻿using Avalonia.Markup.Xaml.Templates;
using System;
using BAKKA_Editor.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BAKKA_Editor.Operations;
using DynamicData;
using System.Net;

namespace BAKKA_Editor
{
    internal class BakeHold
    {
        public static void StepSymmetric(Chart chart, Note startNote, Note endNote, float length, int positionChange,
            int sizeChange, OperationManager operationManager)
        {
            decimal interval = (decimal) (1 / (1 / length * Math.Abs(positionChange)));
            int positionStep = (Math.Abs(positionChange) > Math.Abs(sizeChange) ? 2 : 1) * Math.Sign(positionChange);
            int sizeStep = (Math.Abs(positionChange) > Math.Abs(sizeChange) ? 1 : 2) * Math.Sign(sizeChange);

            int newPosition = startNote.Position;
            int newSize = startNote.Size;

            var lastNote = startNote;
            List<Note> segmentList = new List<Note>();

            lock (chart)
            {
                for (decimal i = (decimal) startNote.Measure + interval; i < (decimal) endNote.Measure; i += interval)
                {
                    // avoid decimal/floating point errors that would
                    // otherwise cause two segments on the same beat
                    // if i is just *barely* less than endNote.Measure.
                    if (chart.GetBeatInfoFromTime((double) i) == chart.GetBeatInfoFromTime(endNote.Measure))
                        break;

                    newPosition += positionStep;
                    newSize += sizeStep;

                    var newNote = new Note()
                    {
                        BeatInfo = new((float) i),
                        NoteType = NoteType.HoldJoint,
                        Position = (newPosition + 60) % 60,
                        Size = newSize,
                        HoldChange = true,
                        PrevReferencedNote = lastNote,
                        NextReferencedNote = endNote
                    };

                    lastNote.NextReferencedNote = newNote;
                    endNote.PrevReferencedNote = newNote;

                    // this may be pure brainfuck, let me explain.
                    // give new note reference to last placed note. will be startNote if the loop just started.
                    // give new note reference to end note.
                    // give last note reference to new note. this will be overwritten by every iteration to always keep the connections correct.
                    // give end note reference to new note. same as above ^

                    lastNote = newNote;
                    segmentList.Add(newNote);

                    chart.Notes.Add(newNote);
                    chart.IsSaved = false;
                }
            }

            operationManager.Push(new BakeHoldNote(chart, startNote, endNote, segmentList));
        }

        public static void StepAsymmetric(Chart chart, Note startNote, Note endNote, float length, int positionChange,
            int sizeChange, OperationManager operationManager)
        {
            decimal interval = (decimal) (1 / (1 / length * Math.Max(Math.Abs(positionChange), Math.Abs(sizeChange))));
            int positionStep = int.Sign(positionChange);
            int sizeStep = int.Sign(sizeChange);

            int newPosition = startNote.Position;
            int newSize = startNote.Size;

            var lastNote = startNote;
            List<Note> segmentList = new List<Note>();

            lock (chart)
            {
                for (decimal i = (decimal) startNote.Measure + interval; i < (decimal) endNote.Measure; i += interval)
                {
                    if (chart.GetBeatInfoFromTime((double) i) == chart.GetBeatInfoFromTime(endNote.Measure))
                        break;

                    newPosition += positionStep;
                    newSize += sizeStep;

                    var newNote = new Note()
                    {
                        BeatInfo = new((float) i),
                        NoteType = NoteType.HoldJoint,
                        Position = (newPosition + 60) % 60,
                        Size = newSize,
                        HoldChange = true,
                        PrevReferencedNote = lastNote,
                        NextReferencedNote = endNote
                    };

                    lastNote.NextReferencedNote = newNote;
                    endNote.PrevReferencedNote = newNote;

                    lastNote = newNote;
                    segmentList.Add(newNote);

                    chart.Notes.Add(newNote);
                    chart.IsSaved = false;
                }
            }

            operationManager.Push(new BakeHoldNote(chart, startNote, endNote, segmentList));
        }

        public static void LerpRound(Chart chart, Note startNote, Note endNote, OperationManager operationManager)
        {
            decimal interval = 0.015625m;

            int newPosition = startNote.Position;
            int newSize = startNote.Size;

            var virtualPosStart0 = startNote.Position;
            var virtualPosEnd0 = endNote.Position;

            var virtualPosStart1 = startNote.Position + startNote.Size;
            var virtualPosEnd1 = endNote.Position + endNote.Size;

            var lastNote = startNote;
            List<Note> segmentList = new List<Note>();

            lock (chart)
            {
                for (decimal i = (decimal) startNote.Measure + interval; i < (decimal) endNote.Measure; i += interval)
                {
                    if (chart.GetBeatInfoFromTime((double) i) == chart.GetBeatInfoFromTime(endNote.Measure))
                        break;

                    float lerpTime = ((float) i - startNote.Measure) / (endNote.Measure - startNote.Measure);

                    newPosition = (int) MathF.Round(ShortLerp(virtualPosStart0, virtualPosEnd0, lerpTime));
                    newSize = (int) MathF.Round(ShortLerp(virtualPosStart1, virtualPosEnd1, lerpTime)) -
                              newPosition;

                    var newNote = new Note()
                    {
                        BeatInfo = new((float) i),
                        NoteType = NoteType.HoldJoint,
                        Position = (newPosition + 60) % 60,
                        Size = (newSize + 60) % 60,
                        HoldChange = false,
                        PrevReferencedNote = lastNote,
                        NextReferencedNote = endNote
                    };

                    lastNote.NextReferencedNote = newNote;
                    endNote.PrevReferencedNote = newNote;

                    lastNote = newNote;
                    segmentList.Add(newNote);

                    chart.Notes.Add(newNote);
                    chart.IsSaved = false;
                }
            }

            operationManager.Push(new BakeHoldNote(chart, startNote, endNote, segmentList));
        }

        private static float ShortLerp(int a, int b, float t)
        {
            if (int.Abs(a - b) > 30)
            {
                if (a > b)
                    a -= 60;
                else
                    b -= 60;
            }

            return (1 - t) * a + t * b;
        }
    }
}