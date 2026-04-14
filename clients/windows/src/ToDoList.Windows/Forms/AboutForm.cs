using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ToDoList.Windows.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponents();
            AppTheme.StyleForm(this);
        }

        private void InitializeComponents()
        {
            Text = "About YepList";
            Size = new Size(520, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.ContentBg;

            // Logo
            PictureBox logo = new PictureBox
            {
                Location = new Point(160, 20),
                Size = new Size(200, 60),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            string logoPath = Path.Combine(AppContext.BaseDirectory, AppTheme.LogoFileName);
            if (File.Exists(logoPath))
            {
                logo.Image = Image.FromFile(logoPath);
            }

            // Version
            string version = System.Reflection.Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString(3) ?? "0.0.0";
            Label lblVersion = new Label
            {
                Text = $"Version {version}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.SubtextColor,
                Location = new Point(0, 88),
                Size = new Size(520, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Author
            Label lblAuthor = new Label
            {
                Text = "by Kevin Adams",
                Font = new Font("Segoe UI", 10f),
                ForeColor = AppTheme.SubtextColor,
                Location = new Point(0, 112),
                Size = new Size(520, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // License
            Label lblLicense = new Label
            {
                Text = "Licensed under the MIT License",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AppTheme.TitleColor,
                Location = new Point(0, 144),
                Size = new Size(520, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Tab control
            TabControl tabs = new TabControl
            {
                Location = new Point(16, 180),
                Size = new Size(472, 250),
                Font = new Font("Segoe UI", 9.5f)
            };

            tabs.TabPages.Add(CreateLibrariesTab());
            tabs.TabPages.Add(CreateChangelogTab());

            StyleTabControl(tabs);

            // Close button
            Controls.AccentButton btnClose = new Controls.AccentButton
            {
                Text = "Close",
                Width = 100,
                Location = new Point(210, 440)
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; };

            Controls.AddRange(new Control[]
            {
                logo, lblVersion, lblAuthor, lblLicense, tabs, btnClose
            });
        }

        private TabPage CreateLibrariesTab()
        {
            TabPage page = new TabPage("Libraries")
            {
                BackColor = AppTheme.ContentBg,
                Padding = new Padding(12)
            };

            Panel creditsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = AppTheme.ContentBg
            };

            int cy = 0;
            AddCreditRow(creditsPanel, ref cy, ".NET Runtime", "MIT", "Microsoft");
            AddCreditRow(creditsPanel, ref cy, "Windows Forms", "MIT", "Microsoft");
            AddCreditRow(creditsPanel, ref cy, "Krypton Toolkit", "BSD 3-Clause", "Krypton Suite");
            AddCreditRow(creditsPanel, ref cy, "System.Text.Json", "MIT", "Microsoft");

            page.Controls.Add(creditsPanel);
            return page;
        }

        private TabPage CreateChangelogTab()
        {
            TabPage page = new TabPage("Changelog")
            {
                BackColor = AppTheme.ContentBg,
                Padding = new Padding(8)
            };

            RichTextBox rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                ForeColor = AppTheme.TitleColor,
                BackColor = AppTheme.ContentBg,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            string changelogPath = Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md");
            if (File.Exists(changelogPath))
            {
                MarkdownRenderer.Render(rtb, File.ReadAllText(changelogPath), AppTheme.TitleColor, AppTheme.AccentBg);
            }
            else
            {
                rtb.Text = "Changelog not found.";
            }

            page.Controls.Add(rtb);
            return page;
        }

        private static void StyleTabControl(TabControl tabs)
        {
            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = AppTheme.ContentBg;
                page.ForeColor = AppTheme.TitleColor;
            }
        }

        private static void AddCreditRow(Panel parent, ref int y, string library, string license, string author)
        {
            Label lblLib = new Label
            {
                Text = library,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.TitleColor,
                Location = new Point(0, y),
                Size = new Size(180, 22),
                BackColor = Color.Transparent
            };

            Label lblLicense = new Label
            {
                Text = license,
                Font = new Font("Segoe UI", 9f),
                ForeColor = AppTheme.SubtextColor,
                Location = new Point(180, y),
                Size = new Size(100, 22),
                BackColor = Color.Transparent
            };

            Label lblAuthor = new Label
            {
                Text = author,
                Font = new Font("Segoe UI", 9f),
                ForeColor = AppTheme.SubtextColor,
                Location = new Point(280, y),
                Size = new Size(120, 22),
                BackColor = Color.Transparent
            };

            parent.Controls.AddRange(new Control[] { lblLib, lblLicense, lblAuthor });
            y += 28;
        }
    }
}
