using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ToDoList.Windows.Forms
{
    public class AboutForm : Form
    {
        private static readonly Color SubtextColor = Color.FromArgb(120, 120, 120);

        public AboutForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "About YepList";
            Size = new Size(480, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // Logo
            PictureBox logo = new PictureBox
            {
                Location = new Point(140, 28),
                Size = new Size(200, 60),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            string logoFile = IsWindowsDarkMode() ? "logo-dark.png" : "logo-light.png";
            string logoPath = Path.Combine(AppContext.BaseDirectory, logoFile);
            if (File.Exists(logoPath))
            {
                logo.Image = Image.FromFile(logoPath);
            }

            // Version
            Label lblVersion = new Label
            {
                Text = "Version 1.0.0",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = SubtextColor,
                Location = new Point(0, 100),
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Author
            Label lblAuthor = new Label
            {
                Text = "by Kevin Adams",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(0, 126),
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // License
            Label lblLicense = new Label
            {
                Text = "Licensed under the MIT License",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(0, 164),
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Separator
            Panel separator = new Panel
            {
                Location = new Point(40, 200),
                Size = new Size(400, 1),
                BackColor = Color.FromArgb(222, 222, 222)
            };

            // Credits header
            Label lblCredits = new Label
            {
                Text = "Third-Party Libraries",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(40, 216),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Credits as clean rows
            Panel creditsPanel = new Panel
            {
                Location = new Point(40, 248),
                Size = new Size(400, 180),
                BackColor = Color.Transparent
            };

            int cy = 0;
            AddCreditRow(creditsPanel, ref cy, ".NET Runtime", "MIT", "Microsoft");
            AddCreditRow(creditsPanel, ref cy, "Windows Forms", "MIT", "Microsoft");
            AddCreditRow(creditsPanel, ref cy, "Krypton Toolkit", "BSD 3-Clause", "Krypton Suite");
            AddCreditRow(creditsPanel, ref cy, "System.Text.Json", "MIT", "Microsoft");

            // Close button
            Controls.AccentButton btnClose = new Controls.AccentButton
            {
                Text = "Close",
                Width = 100,
                Location = new Point(190, 450)
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; };

            Controls.AddRange(new Control[]
            {
                logo, lblVersion, lblAuthor, lblLicense,
                separator, lblCredits, creditsPanel, btnClose
            });
        }

        private static void AddCreditRow(Panel parent, ref int y, string library, string license, string author)
        {
            Label lblLib = new Label
            {
                Text = library,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(0, y),
                Size = new Size(180, 22),
                BackColor = Color.Transparent
            };

            Label lblLicense = new Label
            {
                Text = license,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(180, y),
                Size = new Size(100, 22),
                BackColor = Color.Transparent
            };

            Label lblAuthor = new Label
            {
                Text = author,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(280, y),
                Size = new Size(120, 22),
                BackColor = Color.Transparent
            };

            parent.Controls.AddRange(new Control[] { lblLib, lblLicense, lblAuthor });
            y += 28;
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
