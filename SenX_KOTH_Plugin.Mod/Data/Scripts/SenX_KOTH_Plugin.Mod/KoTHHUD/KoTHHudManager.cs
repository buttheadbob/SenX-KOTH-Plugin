using System.Collections.Generic;
using System.Linq;
using System.Text;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.Game.Entities;
using SenX_KOTH_Plugin.Messages;
using VRageMath;

namespace SenX_KOTH_Plugin.Mod
{
    public class KoTHHudManager
    {
        private readonly KoTHWindow _window;

        public KoTHHudManager()
        {
            _window = new KoTHWindow(HudMain.HighDpiRoot);
        }

        public void Init()
        {
        }

        public void ShowZone(QuestUpdateMessage msg)
        {
            _window.HeaderText = new RichText(new StringBuilder("[KoTH] " + msg.ZoneName));
            _window.SetLines(msg);
            _window.Visible = true;
        }

        public void Hide()
        {
            _window.Visible = false;
        }

        private sealed class KoTHWindow : WindowBase
        {
            private readonly DoubleLabelBox _line1;
            private readonly DoubleLabelBox _line2;
            private readonly DoubleLabelBox _line3;

            public KoTHWindow(HudParentBase parent) : base(parent)
            {
                HeaderText = new RichText(new StringBuilder("[KoTH]"));
                HeaderBuilder.SetFormatting(new GlyphFormat(Color.White, TextAlignment.Center, 0.95f));
                BodyColor = new Color(10, 10, 15, 200);
                BorderColor = new Color(50, 55, 60);
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Top | ParentAlignments.Inner;
                Offset = new Vector2(20, -20);
                Size = new Vector2(320f, 175f);
                MinimumSize = new Vector2(100f, 60f);
                CanDrag = false;
                AllowResizing = false;
                Visible = false;

                _line1 = DblLbl();
                _line2 = DblLbl();
                _line3 = DblLbl();

                new HudChain(true, body)
                {
                    ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner,
                    DimAlignment = DimAlignments.Width,
                    SizingMode = HudChainSizingModes.FitMembersOffAxis,
                    Spacing = 8f,
                    CollectionContainer = { { _line1, 0f }, { _line2, 0f }, { _line3, 0f } }
                };
            }

            private static DoubleLabelBox DblLbl()
            {
                return new DoubleLabelBox
                {
                    Height = 28f,
                    Visible = false,
                    AutoResize = false,
                    Color = new Color(0, 0, 0, 0),
                    TextPadding = new Vector2(12, 4),
                };
            } 

            public void SetLines(QuestUpdateMessage msg)
            {
                var lines = BuildLines(msg);
                SetLine(_line1, lines, 0);
                SetLine(_line2, lines, 1);
                SetLine(_line3, lines, 2);
            }

            private static List<LineData> BuildLines(QuestUpdateMessage msg)
            {
                var result = new List<LineData>();

                if (msg.EvictionPhase >= 1 && msg.EvictionPhase <= 2)
                {
                    result.Add(new LineData("Eviction in", "[" + msg.EvictionTimeRemaining + "s]",
                        Color.OrangeRed, Color.Yellow));
                }
                else if (msg.EvictionPhase == 3)
                {
                    result.Add(new LineData("Eviction Active", "[" + msg.EvictionTimeRemaining + "s]",
                        Color.Red, Color.Yellow));
                }
                else if (msg.Lines != null && msg.Lines.Count > 0)
                {
                    foreach (var line in msg.Lines)
                    {
                        var data = ParseLine(line);
                        if (data.Label != null)
                            result.Add(data);
                    }
                }

                return result;
            }

            private static LineData ParseLine(string text)
            {
                if (text.StartsWith("Neutral"))
                    return new LineData("Neutral", null, Color.White, Color.White);

                if (text.StartsWith("Capturing"))
                    return SplitAtBracket(text, new Color(120, 255, 120), Color.Yellow);

                if (text.StartsWith("Contested"))
                    return SplitAtBracket(text, Color.Red, Color.Yellow);

                if (text.StartsWith("Decay"))
                    return SplitAtBracket(text, new Color(255, 180, 60), Color.Yellow);

                if (text.StartsWith("Held by"))
                    return SplitAtBracket(text, new Color(255, 220, 60), Color.Yellow);

                if (text.StartsWith("Points Earned"))
                    return SplitAtBracket(text, new Color(140, 220, 255), Color.Yellow);

                if (text.StartsWith("Time Remaining"))
                    return SplitAtBracket(text, new Color(140, 220, 255), Color.Yellow);

                if (text.StartsWith("Enemies Inside") || text.StartsWith("Enemies Outside"))
                    return SplitAtBracket(text, Color.Red, Color.Yellow);

                return new LineData(text, null, Color.White, Color.White);
            }

            private static LineData SplitAtBracket(string text, Color labelColor, Color valueColor)
            {
                int bracket = text.IndexOf('[');
                if (bracket > 0)
                    return new LineData(text.Substring(0, bracket).TrimEnd(), text.Substring(bracket),
                        labelColor, valueColor);
                return new LineData(text, null, labelColor, valueColor);
            }

            private static void SetLine(DoubleLabelBox box, List<LineData> lines, int index)
            {
                if (index < lines.Count && lines[index] != null)
                {
                    var data = lines[index];
                    box.LeftTextBuilder.SetFormatting(new GlyphFormat(data.LabelColor, TextAlignment.Left, 0.95f));
                    box.LeftText = new RichText(new StringBuilder(data.Label));

                    if (data.Value != null)
                    {
                        box.RightTextBuilder.SetFormatting(new GlyphFormat(data.ValueColor, TextAlignment.Right, 0.95f));
                        box.RightText = new RichText(new StringBuilder(data.Value));
                        box.RightTextBuilder.SetFormatting(new GlyphFormat(data.ValueColor, TextAlignment.Right, 0.95f));
                    }
                    else
                    {
                        box.RightText = new RichText(new StringBuilder());
                    }

                    box.Visible = true;
                }
                else
                {
                    box.Visible = false;
                }
            }

            private sealed class LineData
            {
                public readonly string Label;
                public readonly string Value;
                public readonly Color LabelColor;
                public readonly Color ValueColor;

                public LineData(string label, string value, Color labelColor, Color valueColor)
                {
                    Label = label;
                    Value = value;
                    LabelColor = labelColor;
                    ValueColor = valueColor;
                }
            }
        }
    }
}
