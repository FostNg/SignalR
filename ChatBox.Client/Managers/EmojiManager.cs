using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace ChatBox.Client.Managers
{
    public class EmojiManager
    {
        private readonly WrapPanel _pnlSmileys;
        private readonly WrapPanel _pnlAnimals;
        private readonly WrapPanel _pnlFood;
        private readonly WrapPanel _pnlActivities;
        private readonly WrapPanel _pnlTravel;
        private readonly WrapPanel _pnlObjects;
        private object? _txtInput;
        private object? _lblInputPlaceholder;

        public event Action<string>? OnEmojiSelected;

        public EmojiManager(
            WrapPanel pnlSmileys,
            WrapPanel pnlAnimals,
            WrapPanel pnlFood,
            WrapPanel pnlActivities,
            WrapPanel pnlTravel,
            WrapPanel pnlObjects)
        {
            _pnlSmileys = pnlSmileys;
            _pnlAnimals = pnlAnimals;
            _pnlFood = pnlFood;
            _pnlActivities = pnlActivities;
            _pnlTravel = pnlTravel;
            _pnlObjects = pnlObjects;
        }

        public void SetInputControls(object txtInput, object lblInputPlaceholder)
        {
            _txtInput = txtInput;
            _lblInputPlaceholder = lblInputPlaceholder;
        }

        public void Initialize()
        {
            AddEmojisToPanel(_pnlSmileys, Smileys);
            AddEmojisToPanel(_pnlAnimals, Animals);
            AddEmojisToPanel(_pnlFood, Food);
            AddEmojisToPanel(_pnlActivities, Activities);
            AddEmojisToPanel(_pnlTravel, Travel);
            AddEmojisToPanel(_pnlObjects, Objects);
        }

        private void AddEmojisToPanel(WrapPanel panel, string[] emojis)
        {
            panel.Children.Clear();
            foreach (var emoji in emojis)
            {
                string trimmed = emoji.Trim();
                if (trimmed.Length == 0 || trimmed.Any(c => char.IsLetterOrDigit(c))) continue;

                var btn = new Button
                {
                    Content = new Emoji.Wpf.TextBlock { Text = trimmed, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Width = 38,
                    Height = 38
                };

                btn.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                    @"<ControlTemplate TargetType='Button' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <Border Background='{TemplateBinding Background}' CornerRadius='4'>
                            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                        </Border>
                      </ControlTemplate>");

                btn.Click += (s, e) =>
                {
                    if (_txtInput == null || _lblInputPlaceholder == null) return;
                    if (_txtInput is TextBox tb) tb.Text += trimmed;
                    else if (_txtInput is Emoji.Wpf.RichTextBox rtb) rtb.Text += trimmed;
                    string cleanText = (_txtInput.ToString() ?? "").Replace("\r", "").Replace("\n", "").Trim();
                    if (_lblInputPlaceholder is TextBlock lbl)
                        lbl.Visibility = string.IsNullOrEmpty(cleanText) ? Visibility.Visible : Visibility.Collapsed;
                    OnEmojiSelected?.Invoke(trimmed);
                };
                panel.Children.Add(btn);
            }
        }

        private static readonly string[] Smileys = {
            "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇",
            "🙂", "🙃", "😉", "😌", "😍", "🥰", "😘", "😗", "😙", "😚",
            "😋", "😛", "😝", "😜", "🤪", "🤨", "🧐", "🤓", "😎", "🥸",
            "🤩", "🥳", "😏", "😒", "😞", "😔", "😟", "😕", "🙁", "☹️",
            "😣", "😖", "😫", "😩", "🥺", "😢", "😭", "😤", "😠", "😡",
            "🤬", "🤯", "😳", "🥵", "🥶", "😱", "😨", "😰", "😥", "😓",
            "🤗", "🤔", "🫣", "🤭", "🤫", "🤥", "😶", "😐", "😑", "😬",
            "🫠", "🫥", "😴", "🥱", "🤢", "🤮", "🤧", "😷", "🤒", "🤕",
            "😈", "👿", "👹", "👺", "💀", "☠️", "👻", "👽", "👾", "🤖",
            "💩", "👋", "🤚", "🖐️", "✋", "🖖", "👌", "🤌", "🤏", "✌️",
            "🤞", "🫰", "🤟", "🤘", "🤙", "👈", "👉", "👆", "🖕", "👇",
            "☝️", "👍", "👎", "✊", "👊", "🤛", "🤜", "👏", "🙌", "👐",
            "🫶", "🤲", "🤝", "🙏", "✍️", "💅", "🤳", "💪", "🧠", "🫀",
            "🫁", "🦷", "🦴", "👀", "👁️", "👅", "👄", "💋", "❤️", "🧡",
            "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "💖", "💗",
            "💓", "💞", "💕", "💟", "❣️", "💘", "💝"
        };

        private static readonly string[] Animals = {
            "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯",
            "🦁", "🐮", "🐷", "🐽", "🐸", "🐵", "🙈", "🙉", "🙊", "🐒",
            "🐔", "🐧", "🐦", "🐤", "🐣", "🐥", "🦆", "🦅", "🦉", "🦇",
            "🐺", "🐗", "🐴", "🦄", "🐝", "🪱", "🐛", "🦋", "🐌", "🐞",
            "🐜", "🪰", "🪲", "🪳", "🦂", "🕸️", "🕷️", "🐢", "🐍", "🦎",
            "🐙", "🦑", "🦞", "🦀", "🐡", "🐠", "🐟", "🐬", "🐳", "🐋",
            "🦈", "🐊", "🐅", "🐆", "🦓", "🦍", "🦧", "🐘", "🦛", "🦏",
            "🐪", "🐫", "🦒", "🦘", "🦬", "🐃", "🐂", "🐄", "🐎", "🐖",
            "🐏", "🐑", "🦙", "🐐", "🦌", "🐕", "🐩", "🐈", "🐈‍⬛", "🐇",
            "🐿️", "🦫", "🦔", "🦦", "🦥", "🦡", "🍁", "🍂", "🍃", "🍄",
            "🌸", "💮", "🪷", "🌹", "🥀", "🌺", "🌻", "🌼", "🌷", "🌱",
            "🪴", "🌲", "🌳", "🌴", "🌵", "🌾", "🌿", "🍀"
        };

        private static readonly string[] Food = {
            "🍏", "🍎", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🫐",
            "🍒", "🍑", "🥭", "🍍", "🥥", "🥝", "🍅", "🍆", "🥑", "🥦",
            "🥬", "🥒", "🌶️", "🫑", "🌽", "🥕", "🫒", "🧄", "🧅", "🥔",
            "🍠", "🥐", "🥯", "🍞", "🥖", "🥨", "🧀", "🥚", "🍳", "🧈",
            "🥞", "🧇", "🥓", "🥩", "🍗", "🍖", "🌭", "🍔", "🍟", "🍕",
            "🥪", "🌮", "🌯", "🍲", "🥘", "🥣", "🥗", "🍿", "🧂", "🥫",
            "🍱", "🍘", "🍙", "🍚", "🍛", "🍜", "🍝", "🍢", "🍣", "🍤",
            "🍥", "🥮", "🍡", "🥟", "🥠", "🥡", "🍦", "🍧", "🍨", "🍩",
            "🍪", "🎂", "🍰", "🧁", "🥧", "🍫", "🍬", "🍭", "🍮", "🍯",
            "🍼", "🥛", "☕", "🍵", "🍶", "🍾", "🍷", "🍸", "🍹", "🍺",
            "🍻", "🥂", "🥃"
        };

        private static readonly string[] Activities = {
            "⚽", "🏀", "🏈", "⚾", "🥎", "🎾", "🏐", "🏉", "🥏", "🎱",
            "🪀", "🏓", "🏸", "🏒", "🏑", "🥍", "🏹", "🎣", "🤿", "🥊",
            "🥋", "🎽", "🛹", "🛼", "🛷", "⛸️", "🥌", "🎿", "🏂", "🪂",
            "🏋️", "🤼", "🤸", "⛹️", "🤺", "🤾", "🏌️", "🏇", "🧘", "🏄",
            "🏊", "🤽", "🚣", "🧗", "🚴", "🚵", "🏆", "🥇", "🥈", "🥉",
            "🏅", "🎖️", "🎫", "🎟️", "🎭", "🎨", "🎬", "🎤", "🎧", "🎼",
            "🎹", "🥁", "🪘", "🎷", "🎺", "🎸", "🪕", "🎻", "🎲", "🧩",
            "🎯", "🎮", "🕹️", "🎰", "👾", "♟️", "🪁", "🏰", "🗼", "🗽",
            "⛩️", "🕋", "🕌", "🛕", "🕍", "🛰️", "🇻🇳", "🇺🇸", "🇬🇧", "🇯🇵"
        };

        private static readonly string[] Travel = {
            "🚗", "🚕", "🚙", "🚌", "🚎", "🏎️", "🚓", "🚑", "🚒", "🚐",
            "🛻", "🚚", "🚛", "🚜", "🛵", "🏍️", "🛺", "🚲", "🛴", "🚏",
            "🛤️", "⚓", "⛵", "🛶", "🚤", "🛳️", "⛴️", "🚢", "✈️", "🛩️",
            "🛫", "🛬", "🚡", "🚠", "🚟", "🚀", "🛸", "🚁", "🌍", "🌎",
            "🌏", "🌐", "🗺️", "🗾", "🧭", "🏔️", "⛰️", "🗻", "🏕️", "🏖️",
            "🏜️", "🏝️", "🏞️", "🏟️", "🏛️", "🏗️", "🧱", "🪨", "🪵", "🏠",
            "🏡", "🏢", "🏣", "🏤", "🏥", "🏦", "🏨", "🏩", "🏪", "🏫",
            "🏬", "🏭", "🏯", "💒", "🗼", "🗽", "⛩️", "🕋", "🕌", "🛕"
        };

        private static readonly string[] Objects = {
            "💡", "🔦", "🕯️", "🪔", "🔌", "🔋", "💻", "🖥️", "🖨️", "⌨️",
            "🖱️", "🖲️", "💽", "💾", "💿", "📀", "🧮", "🎥", "🎞️", "📽️",
            "📺", "📷", "📸", "📹", "📼", "🔍", "🔎", "🔬", "🔭", "📡",
            "✉️", "📩", "📨", "📧", "📥", "📤", "📦", "🏷️", "🪪", "📯",
            "📮", "🗳️", "✏️", "🖋️", "🖊️", "🖌️", "🖍️", "📝", "📁", "📂",
            "📅", "📆", "🗒️", "🗓️", "📊", "📈", "📉", "📋", "📌", "📍",
            "📎", "🖇️", "📏", "📐", "✂️", "🗃️", "🗄️", "🗑️", "🔒", "🔓",
            "🔏", "🔐", "🔑", "🗝️", "🔨", "🪓", "⛏️", "🛠️", "🗡️", "⚔️",
            "🔫", "🛡️", "🔧", "🪛", "⚙️", "🗜️", "⚖️", "🔗", "⛓️", "🔮",
            "📿", "🧿", "🔔", "🔕", "🩹", "🧬", "🌡️", "🧪", "🧫", "⭐",
            "🌟", "✨", "💥"
        };
    }
}