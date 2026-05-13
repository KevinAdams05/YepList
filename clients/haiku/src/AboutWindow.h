/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef ABOUT_WINDOW_H
#define ABOUT_WINDOW_H


#include <Window.h>


class BTabView;


class AboutWindow : public BWindow {
public:
								AboutWindow();

	virtual void				MessageReceived(BMessage* message);

private:
			BView*				_CreateAboutTab();
			BView*				_CreateLibrariesTab();
			BView*				_CreateChangelogTab();
			BString				_LoadChangelog();

			BTabView*			fTabView;
};


#endif	// ABOUT_WINDOW_H
