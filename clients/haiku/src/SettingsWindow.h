/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef SETTINGS_WINDOW_H
#define SETTINGS_WINDOW_H


#include <Messenger.h>
#include <Window.h>


class BButton;
class BTextControl;


class SettingsWindow : public BWindow {
public:
								SettingsWindow(BMessenger target);

	virtual void				MessageReceived(BMessage* message);

			void				SetServerUrl(const char* url);

private:
			BMessenger			fTarget;

			BTextControl*		fUrlField;
			BButton*			fApplyButton;
			BButton*			fCancelButton;
};


#endif	// SETTINGS_WINDOW_H
