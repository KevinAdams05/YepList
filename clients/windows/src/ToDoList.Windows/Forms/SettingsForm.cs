using System;
using System.Drawing;
using System.Windows.Forms;

namespace ToDoList.Windows.Forms
{
    public class SettingsForm : Form
    {
        private TextBox txtServerUrl;

        public string ServerUrl => txtServerUrl.Text.Trim();

        public SettingsForm(AppSettings settings)
        {
            Text = "Settings";
            Size = new Size(460, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.ContentBg;

            Label lblUrl = new Label
            {
                Text = "Server URL",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AppTheme.TitleColor,
                Location = new Point(24, 24),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            txtServerUrl = new TextBox
            {
                Text = settings.ServerUrl,
                Font = new Font("Segoe UI", 10f),
                ForeColor = AppTheme.TitleColor,
                BackColor = AppTheme.InputBg,
                Location = new Point(24, 52),
                Size = new Size(396, 30),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblHint = new Label
            {
                Text = "Changes take effect after restarting the app.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AppTheme.SubtextColor,
                Location = new Point(24, 86),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Controls.AccentButton btnSave = new Controls.AccentButton
            {
                Text = "Save",
                Width = 100,
                Location = new Point(220, 130)
            };
            btnSave.Click += (s, e) => { DialogResult = DialogResult.OK; };

            Controls.FlatButton btnCancel = new Controls.FlatButton
            {
                Text = "Cancel",
                Width = 100,
                Location = new Point(330, 130)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };

            Controls.AddRange(new Control[] { lblUrl, txtServerUrl, lblHint, btnSave, btnCancel });

            AppTheme.StyleForm(this);
        }
    }
}
