﻿using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.HoLLy.Cytus.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using OpenTK;

namespace osu.Game.Rulesets.HoLLy.Cytus.Beatmaps
{
    internal class CytusBeatmapConverter : BeatmapConverter<CytusHitObject>
    {
        protected override IEnumerable<Type> ValidConversionTypes => new[] {typeof(IHasXPosition)};

        public CytusBeatmapConverter(IBeatmap beatmap) : base(beatmap) { }

        protected override IEnumerable<CytusHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap)
        {
            if (!(original is IHasXPosition))
                throw new Exception($"This hitobject of type {original.GetType().Name} is not a {nameof(IHasXPosition)}!");

            double time = original.StartTime;
            float x = ((IHasXPosition)original).X;
            float y = beatmap.GetScanPosition(original.StartTime, Constants.BeatsPerScan);

            // we have to determine if this is a slider or normal hitobject
            if (original is IHasCurve ihc) {
                CytusSliderTick lastTick;
                double endTime = ihc.EndTime;
                var tp = beatmap.ControlPointInfo.TimingPointAt(time);
                double tickInterval = tp.BeatLength / (int)tp.TimeSignature * 2;
                SliderCurve curve = ihc.Curve ?? new SliderCurve {
                    ControlPoints = ihc.ControlPoints, 
                    CurveType = ihc.CurveType, 
                    Distance = ihc.Distance, 
                    Offset = Vector2.Zero
                };
                
                var end = lastTick = new CytusSliderEnd(endTime, x + curve.PositionAt(1).X, beatmap.GetScanPosition(endTime, Constants.BeatsPerScan));

                var ticks = new List<CytusSliderTick>();
                for (double i = endTime - tickInterval; i >= time; i -= tickInterval)
                    ticks.Add(lastTick = new CytusSliderTick(i, x + curve.PositionAt((i - time) / (endTime - time)).X, beatmap.GetScanPosition(i, Constants.BeatsPerScan), lastTick));

                var start = new CytusSliderHead(original.StartTime, x, y, lastTick);
                
                yield return start;
                foreach (var tick in ticks)
                    yield return tick;
                yield return end;
            } else {
                // This is a normal note
                yield return new CytusNote(time, x, y) { 
                    Samples = original.Samples, 
                    SampleControlPoint = original.SampleControlPoint 
                };
            }
        }
    }
}
