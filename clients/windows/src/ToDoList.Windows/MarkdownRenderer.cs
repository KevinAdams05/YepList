using System.Drawing;
using System.Windows.Forms;

namespace ToDoList.Windows
{
    /// <summary>
    /// Lightweight markdown renderer for RichTextBox.
    /// Supports: # h1, ## h2, ### h3, - bullets, **bold** inline.
    /// </summary>
    public static class MarkdownRenderer
    {
        public static void Render(RichTextBox rtb, string markdown, Color textColor, Color headingColor)
        {
            Font normalFont = rtb.Font;
            Font h1Font = new Font(normalFont.FontFamily, 14f, FontStyle.Bold);
            Font h2Font = new Font(normalFont.FontFamily, 12f, FontStyle.Bold);
            Font h3Font = new Font(normalFont.FontFamily, 10f, FontStyle.Bold);
            Font boldFont = new Font(normalFont.FontFamily, normalFont.Size, FontStyle.Bold);

            foreach (string rawLine in markdown.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');

                // Check longest prefix first
                if (line.StartsWith("### "))
                {
                    AppendLine(rtb, line.Substring(4), h3Font, textColor);
                }
                else if (line.StartsWith("## "))
                {
                    AppendLine(rtb, line.Substring(3), h2Font, headingColor);
                }
                else if (line.StartsWith("# "))
                {
                    AppendLine(rtb, line.Substring(2), h1Font, headingColor);
                }
                else if (line.StartsWith("- "))
                {
                    AppendBulletLine(rtb, line.Substring(2), normalFont, boldFont, textColor);
                }
                else
                {
                    AppendLine(rtb, line, normalFont, textColor);
                }
            }
        }

        private static void AppendLine(RichTextBox rtb, string text, Font font, Color color)
        {
            int start = rtb.TextLength;
            rtb.AppendText(text + "\n");
            rtb.Select(start, text.Length);
            rtb.SelectionFont = font;
            rtb.SelectionColor = color;
            rtb.SelectionLength = 0;
        }

        private static void AppendBulletLine(RichTextBox rtb, string text, Font normalFont, Font boldFont, Color color)
        {
            AppendStyled(rtb, "  \u2022 ", normalFont, color);

            int i = 0;
            while (i < text.Length)
            {
                int boldStart = text.IndexOf("**", i);
                if (boldStart == -1)
                {
                    AppendStyled(rtb, text.Substring(i), normalFont, color);
                    break;
                }

                if (boldStart > i)
                {
                    AppendStyled(rtb, text.Substring(i, boldStart - i), normalFont, color);
                }

                int boldEnd = text.IndexOf("**", boldStart + 2);
                if (boldEnd == -1)
                {
                    AppendStyled(rtb, text.Substring(boldStart), normalFont, color);
                    break;
                }

                string boldText = text.Substring(boldStart + 2, boldEnd - boldStart - 2);
                AppendStyled(rtb, boldText, boldFont, color);

                i = boldEnd + 2;
            }

            rtb.AppendText("\n");
        }

        private static void AppendStyled(RichTextBox rtb, string text, Font font, Color color)
        {
            int start = rtb.TextLength;
            rtb.AppendText(text);
            rtb.Select(start, text.Length);
            rtb.SelectionFont = font;
            rtb.SelectionColor = color;
        }
    }
}
