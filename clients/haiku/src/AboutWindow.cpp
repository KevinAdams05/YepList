/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "AboutWindow.h"

#include <Application.h>
#include <File.h>
#include <FindDirectory.h>
#include <LayoutBuilder.h>
#include <Path.h>
#include <Roster.h>
#include <ScrollView.h>
#include <String.h>
#include <StringView.h>
#include <TabView.h>
#include <TextView.h>

#include <cstdio>


AboutWindow::AboutWindow()
	:
	BWindow(BRect(0, 0, 460, 380), "About YepList",
		B_MODAL_WINDOW,
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
		"Version 0.5.2 (Beta)");

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
	textView->SetStylable(false);
	textView->SetWordWrap(true);

	BString changelog = _LoadChangelog();
	if (changelog.Length() == 0)
		changelog = "Changelog not available.";

	textView->SetText(changelog.String());

	BScrollView* scrollView = new BScrollView("cl_scroll",
		textView, 0, false, true);

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
