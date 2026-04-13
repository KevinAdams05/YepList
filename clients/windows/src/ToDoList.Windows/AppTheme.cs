using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Krypton.Toolkit;
using Microsoft.Win32;

namespace ToDoList.Windows
{
    public static class AppTheme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        public static bool IsDark { get; }

        // Sidebar
        public static Color SidebarBg { get; }
        public static Color SidebarSelectedBg { get; }
        public static Color SidebarSelectedText { get; }
        public static Color SidebarHoverBg { get; }

        // Header / Status / Quick-add
        public static Color HeaderBg { get; }
        public static Color ContentBg { get; }
        public static Color BorderColor { get; }

        // Text
        public static Color TitleColor { get; }
        public static Color SubtextColor { get; }
        public static Color CompletedTextColor { get; }

        // Task cards
        public static Color CardBg { get; }
        public static Color CardBorderColor { get; }
        public static Color CardHoverColor { get; }
        public static Color CardSelectedColor { get; }
        public static Color CardSelectedBorderColor { get; }

        // FlatButton
        public static Color ButtonNormalBg { get; }
        public static Color ButtonHoverBg { get; }
        public static Color ButtonPressedBg { get; }
        public static Color ButtonForeColor { get; }

        // AccentButton
        public static Color AccentBg { get; } = Color.FromArgb(53, 122, 232);
        public static Color AccentHoverBg { get; } = Color.FromArgb(40, 100, 200);
        public static Color AccentPressedBg { get; } = Color.FromArgb(30, 80, 170);

        // Krypton input controls
        public static Color InputBg { get; }
        public static Color InputBorderColor { get; }

        // Checkbox
        public static Color CheckboxBorderColor { get; }
        public static Color CheckboxCheckedBg { get; } = Color.FromArgb(53, 122, 232);
        public static Color CheckboxHoverBorder { get; } = Color.FromArgb(53, 122, 232);

        // Due date colors (same in both themes)
        public static Color OverdueColor { get; } = Color.FromArgb(211, 47, 47);
        public static Color DueTodayColor { get; } = Color.FromArgb(245, 124, 0);

        static AppTheme()
        {
            IsDark = DetectDarkMode();

            if (IsDark)
            {
                SidebarBg = Color.FromArgb(32, 32, 32);
                SidebarSelectedBg = Color.FromArgb(40, 52, 74);
                SidebarSelectedText = Color.FromArgb(100, 160, 255);
                SidebarHoverBg = Color.FromArgb(45, 45, 45);

                HeaderBg = Color.FromArgb(38, 38, 38);
                ContentBg = Color.FromArgb(25, 25, 25);
                BorderColor = Color.FromArgb(55, 55, 55);

                TitleColor = Color.FromArgb(230, 230, 230);
                SubtextColor = Color.FromArgb(150, 150, 150);
                CompletedTextColor = Color.FromArgb(90, 90, 90);

                CardBg = Color.FromArgb(38, 38, 38);
                CardBorderColor = Color.FromArgb(55, 55, 55);
                CardHoverColor = Color.FromArgb(48, 48, 48);
                CardSelectedColor = Color.FromArgb(35, 45, 65);
                CardSelectedBorderColor = Color.FromArgb(66, 133, 244);

                ButtonNormalBg = Color.FromArgb(50, 50, 50);
                ButtonHoverBg = Color.FromArgb(65, 65, 65);
                ButtonPressedBg = Color.FromArgb(75, 75, 75);
                ButtonForeColor = Color.FromArgb(210, 210, 210);

                CheckboxBorderColor = Color.FromArgb(120, 120, 120);

                InputBg = Color.FromArgb(45, 45, 45);
                InputBorderColor = Color.FromArgb(70, 70, 70);
            }
            else
            {
                SidebarBg = Color.FromArgb(247, 247, 247);
                SidebarSelectedBg = Color.FromArgb(232, 240, 254);
                SidebarSelectedText = Color.FromArgb(53, 122, 232);
                SidebarHoverBg = Color.FromArgb(237, 237, 237);

                HeaderBg = Color.FromArgb(250, 250, 250);
                ContentBg = Color.White;
                BorderColor = Color.FromArgb(222, 222, 222);

                TitleColor = Color.FromArgb(32, 32, 32);
                SubtextColor = Color.FromArgb(120, 120, 120);
                CompletedTextColor = Color.FromArgb(158, 158, 158);

                CardBg = Color.White;
                CardBorderColor = Color.FromArgb(228, 228, 228);
                CardHoverColor = Color.FromArgb(245, 245, 245);
                CardSelectedColor = Color.FromArgb(232, 240, 254);
                CardSelectedBorderColor = Color.FromArgb(66, 133, 244);

                ButtonNormalBg = Color.FromArgb(240, 240, 240);
                ButtonHoverBg = Color.FromArgb(225, 225, 225);
                ButtonPressedBg = Color.FromArgb(210, 210, 210);
                ButtonForeColor = Color.FromArgb(60, 60, 60);

                CheckboxBorderColor = Color.FromArgb(180, 180, 180);

                InputBg = Color.White;
                InputBorderColor = Color.FromArgb(200, 200, 200);
            }
        }

        private static bool DetectDarkMode()
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

        /// <summary>
        /// Re-reads the current Windows theme setting. Compare with IsDark to detect changes.
        /// </summary>
        public static bool IsCurrentlyDark() => DetectDarkMode();

        public static string LogoFileName => IsDark ? "logo-dark.png" : "logo-light.png";

        /// <summary>
        /// Applies dark title bar and Krypton control styling to a form.
        /// Call at the end of the form's constructor, after InitializeComponents.
        /// </summary>
        public static void StyleForm(Form form)
        {
            if (!IsDark)
            {
                return;
            }

            // Dark title bar via DWM
            int value = 1;
            DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int));

            // Recursively style Krypton controls
            StyleControls(form.Controls);
        }

        private static void StyleControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case KryptonTextBox ktb:
                        ktb.StateCommon.Back.Color1 = InputBg;
                        ktb.StateCommon.Content.Color1 = TitleColor;
                        ktb.StateCommon.Border.Color1 = InputBorderColor;
                        ktb.StateCommon.Border.Color2 = InputBorderColor;
                        foreach (Control child in ktb.Controls)
                        {
                            child.BackColor = InputBg;
                            child.ForeColor = TitleColor;
                        }
                        break;

                    case KryptonRichTextBox krtb:
                        krtb.StateCommon.Back.Color1 = InputBg;
                        krtb.StateCommon.Content.Color1 = TitleColor;
                        krtb.StateCommon.Border.Color1 = InputBorderColor;
                        krtb.StateCommon.Border.Color2 = InputBorderColor;
                        foreach (Control child in krtb.Controls)
                        {
                            child.BackColor = InputBg;
                            child.ForeColor = TitleColor;
                        }
                        break;

                    case KryptonComboBox kcb:
                        kcb.StateCommon.ComboBox.Back.Color1 = InputBg;
                        kcb.StateCommon.ComboBox.Content.Color1 = TitleColor;
                        kcb.StateCommon.ComboBox.Border.Color1 = InputBorderColor;
                        kcb.StateCommon.ComboBox.Border.Color2 = InputBorderColor;
                        kcb.StateCommon.Item.Back.Color1 = InputBg;
                        kcb.StateCommon.Item.Content.ShortText.Color1 = TitleColor;
                        foreach (Control child in kcb.Controls)
                        {
                            child.BackColor = InputBg;
                            child.ForeColor = TitleColor;
                        }
                        break;

                    case KryptonListBox klb:
                        klb.BackColor = InputBg;
                        klb.ForeColor = TitleColor;
                        klb.StateCommon.Item.Back.Color1 = InputBg;
                        klb.StateCommon.Item.Back.Color2 = InputBg;
                        klb.StateCommon.Item.Content.ShortText.Color1 = TitleColor;
                        klb.StateCommon.Border.Color1 = InputBorderColor;
                        klb.StateCommon.Border.Color2 = InputBorderColor;
                        // Force the inner ListBox control to use dark colors
                        foreach (Control child in klb.Controls)
                        {
                            child.BackColor = InputBg;
                            child.ForeColor = TitleColor;
                        }
                        break;

                    case KryptonCheckBox kcbox:
                        kcbox.StateCommon.ShortText.Color1 = TitleColor;
                        break;

                    case KryptonDateTimePicker kdtp:
                        kdtp.StateCommon.Back.Color1 = InputBg;
                        kdtp.StateCommon.Content.Color1 = TitleColor;
                        kdtp.StateCommon.Border.Color1 = InputBorderColor;
                        kdtp.StateCommon.Border.Color2 = InputBorderColor;
                        break;

                    case KryptonLabel kl:
                        kl.StateCommon.ShortText.Color1 = TitleColor;
                        break;

                    case KryptonDataGridView kdgv:
                        kdgv.StateCommon.Background.Color1 = InputBg;
                        kdgv.StateCommon.DataCell.Back.Color1 = InputBg;
                        kdgv.StateCommon.DataCell.Content.Color1 = TitleColor;
                        kdgv.StateCommon.HeaderColumn.Back.Color1 = HeaderBg;
                        kdgv.StateCommon.HeaderColumn.Content.Color1 = TitleColor;
                        kdgv.BackgroundColor = InputBg;
                        kdgv.GridColor = InputBorderColor;
                        kdgv.DefaultCellStyle.BackColor = InputBg;
                        kdgv.DefaultCellStyle.ForeColor = TitleColor;
                        kdgv.DefaultCellStyle.SelectionBackColor = CardSelectedColor;
                        kdgv.DefaultCellStyle.SelectionForeColor = TitleColor;
                        kdgv.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
                        kdgv.ColumnHeadersDefaultCellStyle.ForeColor = TitleColor;
                        kdgv.EnableHeadersVisualStyles = false;
                        break;
                }

                if (control.HasChildren)
                {
                    StyleControls(control.Controls);
                }
            }
        }
    }
}
