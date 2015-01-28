using System;
using System.Collections.Generic;
using System.Text;
using Mogre;

namespace MSpriteRenderer
{
    public class TextManager
    {
        OverlayContainer _OverlayPanel;
        Overlay _TextOverlay;
        private uint textCounter;

        public TextManager()
        {
            this.Initalize();
        }

        private void Initalize()
        {
            // Create the font resources 
            // add the path to your resources.cfg
            // for example:
            // Default   | ../../../Media/fonts     | ../../../Media/fonts.zip
            Load("LiberationSans-Regular.ttf", Font.Default, 10);
            Load("LiberationSans-Bold.ttf", Font.DefaultBold, 15);
            Load("LiberationSans-Italic.ttf", Font.DefaultItalic, 10);
            //Load("LiberationMono-Regular.ttf", Font.Console, 26);

            // Create the overlay panel
            _OverlayPanel = OverlayManager.Singleton.CreateOverlayElement("Panel", new Guid().ToString()) as OverlayContainer;
            _OverlayPanel.MetricsMode = GuiMetricsMode.GMM_PIXELS;
            _OverlayPanel.SetPosition(0, 0);
            _OverlayPanel.SetDimensions(300, 120);

            _TextOverlay = OverlayManager.Singleton.Create(new Guid().ToString());

            _TextOverlay.Add2D(_OverlayPanel);
        }

        private void Load(string fontFileName, string fontRef, int size)
        {
            ResourcePtr font = FontManager.Singleton.Create(fontRef, ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME);
            font.SetParameter("type", "truetype");
            font.SetParameter("source", fontFileName);
            font.SetParameter("size", size.ToString());
            font.SetParameter("resolution", "96");
            font.Load();
        }

        public void HideAllText()
        {
            _TextOverlay.Hide();
        }
        public void ShowAllText()
        {
            _TextOverlay.Show();
        }

        //returns a texthandle
        public string AddText(Text text)
        {
            _OverlayPanel.AddChild(text._TextArea);
            return text._TextArea.Name;
        }
        public void RemoveText(Text text)
        {
            _OverlayPanel.RemoveChild(text._TextArea.Name);
            OverlayManager.Singleton.DestroyOverlayElement(text._TextArea.Name);
        }
        public void RemoveText(string textAreaName)
        {
            _OverlayPanel.RemoveChild(textAreaName);
            OverlayManager.Singleton.DestroyOverlayElement(textAreaName);
        }
    }

    public class Text
    {
        public TextAreaOverlayElement _TextArea;
        public string TextName = Guid.NewGuid().ToString();

        public Text(string caption)
        {
            _TextArea = OverlayManager.Singleton.CreateOverlayElement("TextArea", TextName) as TextAreaOverlayElement;
            _TextArea.MetricsMode = GuiMetricsMode.GMM_PIXELS;
            _TextArea.SetPosition(0, 0);
            _TextArea.SetDimensions(300, 120);
            _TextArea.CharHeight = 20;
            _TextArea.FontName = Font.Default;
            _TextArea.Caption = caption;
            _TextArea.Colour = ColourValue.White;
        }

        public void Hide()
        {
            _TextArea.Hide();
        }
        public void Show()
        {
            _TextArea.Show();
        }

        public System.Drawing.Point Position
        {
            set { _TextArea.SetPosition(value.X, value.Y); }
            get { return new System.Drawing.Point((int)_TextArea._getLeft(), (int)_TextArea._getTop()); }
        }
        public string FontName
        {
            set { _TextArea.FontName = value; }
            get { return _TextArea.FontName; }
        }
        public float CharacterHeight
        {
            set { _TextArea.CharHeight = value; }
            get { return _TextArea.CharHeight; }
        }

        public string Caption
        {
            set { _TextArea.Caption = value; }
            get { return _TextArea.Caption; }
        }

        public ColourValue Color
        {
            set { _TextArea.Colour = value; }
            get { return _TextArea.Colour; }
        }
        public ColourValue ColorTop
        {
            set { _TextArea.ColourTop = value; }
            get { return _TextArea.ColourTop; }
        }
        public ColourValue ColorBottom
        {
            set { _TextArea.ColourBottom = value; }
            get { return _TextArea.ColourBottom; }
        }
    }

    public class Font
    {
        public const string Default = "FONT.DEFAULT";
        public const string DefaultBold = "FONT.DEFAULT.BOLD";
        public const string DefaultItalic = "FONT.DEFAULT.ITALIC";
        public const string Console = "FONT.CONSOLE";
    }
}