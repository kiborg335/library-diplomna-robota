using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibraryApp.Forms
{
    public partial class ReaderForm : Form
    {
        private TextBox txtContent;
        private Button btnClose;
        private Button btnFontUp;
        private Button btnFontDown;
        private Label lblFontSize;
        private int _fontSize = 12;

        public ReaderForm(string title, string text)
        {
            InitializeComponent();
            this.Text = "Читання: " + title;
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            SetupUI(text);
        }

        private void SetupUI(string text)
        {
            var controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Top;
            controlPanel.Height = 40;
            controlPanel.BackColor = Color.LightGray;

            btnFontUp = new Button();
            btnFontUp.Text = "A+";
            btnFontUp.Location = new Point(10, 8);
            btnFontUp.Width = 40;
            btnFontUp.Height = 25;
            btnFontUp.Click += (s, e) => ChangeFontSize(2);

            btnFontDown = new Button();
            btnFontDown.Text = "A-";
            btnFontDown.Location = new Point(55, 8);
            btnFontDown.Width = 40;
            btnFontDown.Height = 25;
            btnFontDown.Click += (s, e) => ChangeFontSize(-2);

            lblFontSize = new Label();
            lblFontSize.Text = "Шрифт: " + _fontSize;
            lblFontSize.Location = new Point(105, 12);
            lblFontSize.Width = 80;

            btnClose = new Button();
            btnClose.Text = "Закрити";
            btnClose.Location = new Point(200, 8);
            btnClose.Width = 100;
            btnClose.Height = 25;
            btnClose.Click += (s, e) => this.Close();

            controlPanel.Controls.AddRange(new Control[] {
                btnFontUp, btnFontDown, lblFontSize, btnClose
            });

            txtContent = new TextBox();
            txtContent.Dock = DockStyle.Fill;
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Vertical;
            txtContent.Font = new Font("Segoe UI", _fontSize);
            txtContent.ReadOnly = true;
            txtContent.BackColor = Color.White;
            txtContent.Text = text;
            txtContent.WordWrap = true;

            this.Controls.Add(txtContent);
            this.Controls.Add(controlPanel);
            controlPanel.BringToFront();
        }

        private void ChangeFontSize(int delta)
        {
            _fontSize = Math.Max(8, Math.Min(24, _fontSize + delta));
            txtContent.Font = new Font("Segoe UI", _fontSize);
            lblFontSize.Text = "Шрифт: " + _fontSize;
        }
    }
}