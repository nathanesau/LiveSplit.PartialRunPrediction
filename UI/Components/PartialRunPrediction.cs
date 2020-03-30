using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class PartialRunPrediction : IComponent
    {
        protected InfoTimeComponent InternalComponent { get; set; }
        public PartialRunPredictionSettings Settings { get; set; }
        private PartialRunPredictionFormatter Formatter { get; set; }

        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingRight => InternalComponent.PaddingRight;

        public IDictionary<string, Action> ContextMenuControls => null; 

        public PartialRunPrediction(LiveSplitState state)
        {
            Settings = new PartialRunPredictionSettings()
            {
                CurrentState = state
            };
            Formatter = new PartialRunPredictionFormatter(Settings.Accuracy);
            InternalComponent = new InfoTimeComponent(null, null, Formatter);
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        private void PrepareDraw(LiveSplitState state)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow 
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public float VerticalHeight => InternalComponent.VerticalHeight;

        public float MinimumWidth => InternalComponent.MinimumWidth;

        public float HorizontalWidth => InternalComponent.HorizontalWidth;

        public float MinimumHeight => InternalComponent.MinimumHeight;

        public string ComponentName => GetDisplayedName(Settings.Comparison);

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        protected string GetDisplayedName(string comparison)
        {
            // #1682: Currently, support "Best Segments" comparison only
            switch (comparison)
            {
                case BestSegmentsComparisonGenerator.ComparisonName:
                    return "Best Possible Time (Up To Split)";
                default:
                    return "Partial Run Prediction";
            }
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var comparison = Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
            {
                // #1682: Currently, support "Best Segments" comparison only ("Current Comparison" not supported)
                InternalComponent.InformationName = "Split #" + Settings.VisualCompareSplitNumber + " comparison not available";
                InternalComponent.TimeValue = null;
            }
            else
            {
                if (Settings.VisualCompareSplitNumber > state.Run.Count)
                {
                    InternalComponent.InformationName = "Split #" + Settings.VisualCompareSplitNumber + " not available";
                    InternalComponent.TimeValue = null;
                }
                else // get information for Settings.VisualCompareSplitNumber
                {
                    ISegment visualCompareSplitNumberSegment = state.Run[Settings.VisualCompareSplitNumber - 1];

                    if (state.CurrentSplitIndex > Settings.VisualCompareSplitNumber - 1)
                    {
                        InternalComponent.InformationName = "Time (" + visualCompareSplitNumberSegment.Name + ")";
                        InternalComponent.TimeValue = visualCompareSplitNumberSegment.SplitTime[state.CurrentTimingMethod].GetValueOrDefault();
                    }
                    else
                    {
                        InternalComponent.InformationName = "Best Possible Time (" + visualCompareSplitNumberSegment.Name + ")";

                        if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
                        {
                            TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, state.CurrentTimingMethod) ?? TimeSpan.Zero;
                            var liveDelta = state.CurrentTime[state.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][state.CurrentTimingMethod];
                            if (liveDelta > delta)
                                delta = liveDelta;
                            InternalComponent.TimeValue = delta + visualCompareSplitNumberSegment.Comparisons[comparison][state.CurrentTimingMethod];
                        }
                        else
                        {
                            TimeSpan? timeValue = visualCompareSplitNumberSegment.Comparisons[comparison][state.CurrentTimingMethod];
                            InternalComponent.TimeValue = timeValue;
                        }
                    }
                }
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        public void Dispose()
        {
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
