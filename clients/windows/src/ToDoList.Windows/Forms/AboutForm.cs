using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Krypton.Toolkit;
using Microsoft.Win32;

namespace ToDoList.Windows.Forms
{
    public class AboutForm : KryptonForm
    {
        public AboutForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "About YepList";
            Size = new Size(480, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Logo
            PictureBox logo = new PictureBox
            {
                Location = new Point(140, 20),
                Size = new Size(200, 60),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            string logoFile = IsWindowsDarkMode() ? "logo-dark.png" : "logo-light.png";
            string logoPath = Path.Combine(AppContext.BaseDirectory, logoFile);
            if (File.Exists(logoPath))
            {
                logo.Image = Image.FromFile(logoPath);
            }

            // App info
            KryptonLabel lblVersion = new KryptonLabel
            {
                Text = "Version 1.0.0",
                Location = new Point(0, 90),
                Size = new Size(480, 24),
                LabelStyle = LabelStyle.NormalControl
            };
            lblVersion.StateCommon.ShortText.TextH = PaletteRelativeAlign.Center;

            KryptonLabel lblAuthor = new KryptonLabel
            {
                Text = "by Kevin Adams",
                Location = new Point(0, 114),
                Size = new Size(480, 24),
                LabelStyle = LabelStyle.NormalControl
            };
            lblAuthor.StateCommon.ShortText.TextH = PaletteRelativeAlign.Center;

            // License
            KryptonLabel lblLicense = new KryptonLabel
            {
                Text = "Licensed under the MIT License",
                Location = new Point(0, 148),
                Size = new Size(480, 24),
                LabelStyle = LabelStyle.BoldControl
            };
            lblLicense.StateCommon.ShortText.TextH = PaletteRelativeAlign.Center;

            // Library credits
            KryptonLabel lblCredits = new KryptonLabel
            {
                Text = "Third-Party Libraries",
                Location = new Point(20, 185),
                AutoSize = true,
                LabelStyle = LabelStyle.BoldControl
            };

            KryptonDataGridView creditsGrid = new KryptonDataGridView
            {
                Location = new Point(20, 210),
                Size = new Size(425, 190),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars = ScrollBars.Vertical
            };

            DataGridViewTextBoxColumn colLibrary = new DataGridViewTextBoxColumn
            {
                Name = "Library",
                HeaderText = "Library",
                FillWeight = 40
            };
            DataGridViewTextBoxColumn colLicense = new DataGridViewTextBoxColumn
            {
                Name = "License",
                HeaderText = "License",
                FillWeight = 30
            };
            DataGridViewTextBoxColumn colAuthor = new DataGridViewTextBoxColumn
            {
                Name = "Author",
                HeaderText = "Author / Project",
                FillWeight = 30
            };
            creditsGrid.Columns.AddRange(new DataGridViewColumn[] { colLibrary, colLicense, colAuthor });

            creditsGrid.Rows.Add(".NET Runtime", "MIT", "Microsoft");
            creditsGrid.Rows.Add("Windows Forms", "MIT", "Microsoft");
            creditsGrid.Rows.Add("Krypton Toolkit", "BSD 3-Clause", "Krypton Suite");
            creditsGrid.Rows.Add("System.Text.Json", "MIT", "Microsoft");
            creditsGrid.ClearSelection();

            // Close button
            KryptonButton btnClose = new KryptonButton
            {
                Text = "Close",
                Location = new Point(190, 420),
                Width = 100,
                DialogResult = DialogResult.OK
            };

            AcceptButton = btnClose;
            CancelButton = btnClose;

            Controls.AddRange(new Control[]
            {
                logo, lblVersion, lblAuthor, lblLicense,
                lblCredits, creditsGrid, btnClose
            });
        }

        private static bool IsWindowsDarkMode()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                object? value = key?.GetValue("AppsUseLightTheme");
                return value is int intVal && intVal == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
