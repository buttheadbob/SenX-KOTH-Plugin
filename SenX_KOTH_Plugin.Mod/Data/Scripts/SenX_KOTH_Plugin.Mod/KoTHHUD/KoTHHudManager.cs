using System.Collections.Generic;
using System.Text;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
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
            _window.SetLines(msg.Lines);
            _window.Visible = true;
        }

        public void Hide()
        {
            _window.Visible = false;
        }

        private sealed class KoTHWindow : WindowBase
        {
            private readonly Label _line1;
            private readonly Label _line2;
            private readonly Label _line3;

            public KoTHWindow(HudParentBase parent) : base(parent)
            {
                HeaderText = new RichText(new StringBuilder("[KoTH]"));
                BodyColor = new Color(10, 10, 15, 200);
                BorderColor = new Color(50, 55, 60);
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Top;
                Offset = new Vector2(20, 20);
                CanDrag = false;
                AllowResizing = false;
                Visible = false;

                _line1 = Lbl(new Color(220, 240, 255), 0.9f);
                _line2 = Lbl(new Color(200, 200, 200), 0.85f);
                _line3 = Lbl(new Color(200, 200, 200), 0.85f);

                new HudChain(true, body)
                {
                    ParentAlignment = ParentAlignments.Top | ParentAlignments.Left | ParentAlignments.Inner,
                    DimAlignment = DimAlignments.Width,
                    SizingMode = HudChainSizingModes.FitMembersOffAxis,
                    Spacing = 4f,
                    CollectionContainer = { { _line1, 0f }, { _line2, 0f }, { _line3, 0f } }
                };
            }

            private static Label Lbl(Color c, float s)
            {
                return new Label
                {
                    Format = new GlyphFormat(c, TextAlignment.Left, s),
                    AutoResize = true,
                    Visible = false,
                };
            }

            public void SetLines(List<string> lines)
            {
                SetLine(_line1, lines, 0);
                SetLine(_line2, lines, 1);
                SetLine(_line3, lines, 2);
            }

            private static void SetLine(Label label, List<string> lines, int index)
            {
                if (lines != null && index < lines.Count)
                {
                    label.Text = new RichText(new StringBuilder(lines[index]));
                    label.Visible = true;
                }
                else
                {
                    label.Visible = false;
                }
            }
        }
    }
}
