/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "AboutWindow.h"

#include <Application.h>
#include <File.h>
#include <FindDirectory.h>
#include <Font.h>
#include <InterfaceDefs.h>
#include <LayoutBuilder.h>
#include <Path.h>
#include <Roster.h>
#include <ScrollView.h>
#include <Size.h>
#include <String.h>
#include <StringView.h>
#include <TabView.h>
#include <TextView.h>

#include <cstdio>
#include <vector>


namespace {

// A styled run of text: [offset, offset+length) rendered bold, monospace,
// and/or scaled relative to the base font size.
struct MarkdownSpan {
	int32	offset;
	int32	length;
	float	sizeScale;
	bool	bold;
	bool	code;
};


void
AppendRun(BString& out, std::vector<MarkdownSpan>& spans,
	const BString& text, float sizeScale, bool bold, bool code)
{
	if (text.Length() == 0)
		return;
	MarkdownSpan span = { out.Length(), text.Length(), sizeScale, bold, code };
	out.Append(text);
	spans.push_back(span);
}


// Append `text`, rendering **bold** segments in bold and `code` segments in a
// monospace font. Everything else is plain body text.
void
AppendInline(BString& out, std::vector<MarkdownSpan>& spans,
	const BString& text)
{
	int32 i = 0;
	int32 len = text.Length();
	while (i < len) {
		int32 boldPos = text.FindFirst("**", i);
		int32 codePos = text.FindFirst("`", i);

		// Pick whichever marker comes first.
		int32 marker = -1;
		bool isBold = false;
		if (boldPos >= 0 && (codePos < 0 || boldPos <= codePos)) {
			marker = boldPos;
			isBold = true;
		} else if (codePos >= 0) {
			marker = codePos;
			isBold = false;
		}

		if (marker < 0) {
			BString rest;
			text.CopyInto(rest, i, len - i);
			AppendRun(out, spans, rest, 1.0f, false, false);
			break;
		}

		if (marker > i) {
			BString plain;
			text.CopyInto(plain, i, marker - i);
			AppendRun(out, spans, plain, 1.0f, false, false);
		}

		if (isBold) {
			int32 end = text.FindFirst("**", marker + 2);
			if (end < 0) {
				BString rest;
				text.CopyInto(rest, marker, len - marker);
				AppendRun(out, spans, rest, 1.0f, false, false);
				break;
			}
			BString boldText;
			text.CopyInto(boldText, marker + 2, end - marker - 2);
			AppendRun(out, spans, boldText, 1.0f, true, false);
			i = end + 2;
		} else {
			int32 end = text.FindFirst("`", marker + 1);
			if (end < 0) {
				BString rest;
				text.CopyInto(rest, marker, len - marker);
				AppendRun(out, spans, rest, 1.0f, false, false);
				break;
			}
			BString codeText;
			text.CopyInto(codeText, marker + 1, end - marker - 1);
			AppendRun(out, spans, codeText, 1.0f, false, true);
			i = end + 1;
		}
	}
}


// Renders a subset of Markdown (# / ## / ### headings, "- " bullets, and
// **bold** inline) into a BTextView, matching the Linux and Windows clients.
// The text view must be stylable.
void
RenderMarkdown(BTextView* view, const BString& markdown)
{
	BString out;
	std::vector<MarkdownSpan> spans;

	int32 total = markdown.Length();
	int32 start = 0;
	while (start <= total) {
		int32 nl = markdown.FindFirst('\n', start);
		int32 lineEnd = (nl < 0) ? total : nl;

		BString line;
		markdown.CopyInto(line, start, lineEnd - start);
		if (line.EndsWith("\r"))
			line.Truncate(line.Length() - 1);

		if (line.StartsWith("### ")) {
			BString t;
			line.CopyInto(t, 4, line.Length() - 4);
			AppendRun(out, spans, t, 1.1f, true, false);
		} else if (line.StartsWith("## ")) {
			BString t;
			line.CopyInto(t, 3, line.Length() - 3);
			AppendRun(out, spans, t, 1.3f, true, false);
		} else if (line.StartsWith("# ")) {
			BString t;
			line.CopyInto(t, 2, line.Length() - 2);
			AppendRun(out, spans, t, 1.5f, true, false);
		} else if (line.StartsWith("- ")) {
			BString content;
			line.CopyInto(content, 2, line.Length() - 2);
			AppendRun(out, spans, "  \xE2\x80\xA2 ", 1.0f, false, false);
			AppendInline(out, spans, content);
		} else if (line == ">" || line.StartsWith("> ")) {
			// Blockquote — drop the "> " marker and render the remainder as
			// ordinary body text (still honouring inline bold/code).
			BString content;
			if (line.Length() > 2)
				line.CopyInto(content, 2, line.Length() - 2);
			AppendInline(out, spans, content);
		} else {
			AppendInline(out, spans, line);
		}

		out.Append("\n");

		if (nl < 0)
			break;
		start = nl + 1;
	}

	view->SetText(out.String());

	rgb_color textColor = ui_color(B_DOCUMENT_TEXT_COLOR);
	float baseSize = be_plain_font->Size();
	for (const MarkdownSpan& span : spans) {
		const BFont* base = be_plain_font;
		if (span.code)
			base = be_fixed_font;
		else if (span.bold)
			base = be_bold_font;
		BFont font(base);
		font.SetSize(baseSize * span.sizeScale);
		view->SetFontAndColor(span.offset, span.offset + span.length,
			&font, B_FONT_ALL, &textColor);
	}
}

}	// namespace


AboutWindow::AboutWindow()
	:
	// Titled window (not modal) so it gets a standard window tab in the
	// title bar, like the main window.
	BWindow(BRect(0, 0, 520, 400), "About YepList",
		B_TITLED_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE | B_NOT_RESIZABLE),
	fTabView(NULL)
{
	fTabView = new BTabView("tab_view", B_WIDTH_FROM_WIDEST);

	fTabView->AddTab(_CreateAboutTab());
	fTabView->AddTab(_CreateLibrariesTab());
	fTabView->AddTab(_CreateChangelogTab());

	BLayoutBuilder::Group<>(this, B_VERTICAL, 0.0f)
		.SetInsets(B_USE_WINDOW_SPACING)
		.Add(fTabView)
	;

	CenterOnScreen();
}


void
AboutWindow::MessageReceived(BMessage* message)
{
	BWindow::MessageReceived(message);
}


BView*
AboutWindow::_CreateAboutTab()
{
	BView* view = new BView("About", B_WILL_DRAW);
	view->SetViewUIColor(B_PANEL_BACKGROUND_COLOR);

	BStringView* nameView = new BStringView("app_name", "YepList");
	BFont titleFont(be_bold_font);
	titleFont.SetSize(titleFont.Size() * 1.5f);
	nameView->SetFont(&titleFont);

	BStringView* versionView = new BStringView("version",
		"Version 0.5.4 (Beta)");

	BStringView* descView = new BStringView("desc",
		"A cross-platform to-do list application");

	BStringView* authorView = new BStringView("author",
		"Copyright " B_UTF8_COPYRIGHT " 2026 Kevin Adams");

	BStringView* licenseView = new BStringView("license",
		"Distributed under the MIT License");

	BLayoutBuilder::Group<>(view, B_VERTICAL)
		.SetInsets(B_USE_DEFAULT_SPACING)
		.AddGlue()
		.Add(nameView)
		.Add(versionView)
		.AddStrut(B_USE_HALF_ITEM_SPACING)
		.Add(descView)
		.AddStrut(B_USE_HALF_ITEM_SPACING)
		.Add(authorView)
		.Add(licenseView)
		.AddGlue()
	;

	return view;
}


BView*
AboutWindow::_CreateLibrariesTab()
{
	BView* view = new BView("Libraries", B_WILL_DRAW);
	view->SetViewUIColor(B_PANEL_BACKGROUND_COLOR);

	BTextView* textView = new BTextView("libraries_text");
	textView->MakeEditable(false);
	textView->MakeSelectable(true);
	textView->SetStylable(false);
	textView->SetWordWrap(true);

	const char* libraries =
		"YepList uses the following Haiku system libraries:\n"
		"\n"
		"libbe - Haiku Application Kit, Interface Kit, Storage Kit\n"
		"  Core BeOS/Haiku APIs for windows, views, messaging,\n"
		"  and file system access.\n"
		"\n"
		"libshared - Haiku Shared Libraries\n"
		"  JSON parsing (BJson), string utilities.\n"
		"\n"
		"libbnetapi - Haiku Network Services\n"
		"  HTTP client (BHttpSession, BHttpRequest) for REST API\n"
		"  communication.\n"
		"\n"
		"C++ Standard Library (C++17)\n"
		"  std::vector, std::string_view, std::optional,\n"
		"  std::unique_ptr.\n"
		"\n"
		"Backend: ASP.NET Core + Dapper + MySQL\n"
		"  REST API server running on Linux.\n";

	textView->SetText(libraries);

	BScrollView* scrollView = new BScrollView("lib_scroll",
		textView, 0, false, true);
	// Keep the content wide enough that all three tab labels fit.
	scrollView->SetExplicitMinSize(BSize(480, 320));

	BLayoutBuilder::Group<>(view, B_VERTICAL, 0.0f)
		.SetInsets(B_USE_DEFAULT_SPACING)
		.Add(scrollView, 1.0f)
	;

	return view;
}


BView*
AboutWindow::_CreateChangelogTab()
{
	BView* view = new BView("Changelog", B_WILL_DRAW);
	view->SetViewUIColor(B_PANEL_BACKGROUND_COLOR);

	BTextView* textView = new BTextView("changelog_text");
	textView->MakeEditable(false);
	textView->MakeSelectable(true);
	textView->SetStylable(true);
	textView->SetWordWrap(true);

	BString changelog = _LoadChangelog();
	if (changelog.Length() == 0)
		changelog = "Changelog not available.";

	RenderMarkdown(textView, changelog);

	BScrollView* scrollView = new BScrollView("cl_scroll",
		textView, 0, false, true);
	scrollView->SetExplicitMinSize(BSize(480, 320));

	BLayoutBuilder::Group<>(view, B_VERTICAL, 0.0f)
		.SetInsets(B_USE_DEFAULT_SPACING)
		.Add(scrollView, 1.0f)
	;

	return view;
}


BString
AboutWindow::_LoadChangelog()
{
	// Try to load from app directory first
	app_info info;
	BString changelogPath;

	if (be_app->GetAppInfo(&info) == B_OK) {
		BPath appPath(&info.ref);
		BPath parentPath;
		appPath.GetParent(&parentPath);

		changelogPath.SetToFormat("%s/data/CHANGELOG.md",
			parentPath.Path());

		BFile file(changelogPath.String(), B_READ_ONLY);
		if (file.InitCheck() == B_OK) {
			off_t size;
			file.GetSize(&size);
			if (size > 0 && size < 1024 * 1024) {
				char* buffer = new char[size + 1];
				ssize_t bytesRead = file.Read(buffer, size);
				if (bytesRead > 0) {
					buffer[bytesRead] = '\0';
					BString result(buffer);
					delete[] buffer;
					return result;
				}
				delete[] buffer;
			}
		}
	}

	return BString("");
}
