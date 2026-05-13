/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "SettingsWindow.h"

#include <Alert.h>
#include <Button.h>
#include <LayoutBuilder.h>
#include <TextControl.h>

#include "MainWindow.h"


static const uint32 kMsgApply	= 'aply';
static const uint32 kMsgCancel	= 'cncl';


SettingsWindow::SettingsWindow(BMessenger target)
	:
	BWindow(BRect(0, 0, 420, 100), "Settings",
		B_FLOATING_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE),
	fTarget(target),
	fUrlField(NULL),
	fApplyButton(NULL),
	fCancelButton(NULL)
{
	fUrlField = new BTextControl("server_url", "Server URL:",
		"", NULL);

	fApplyButton = new BButton("apply", "Apply",
		new BMessage(kMsgApply));
	fApplyButton->MakeDefault(true);
	fCancelButton = new BButton("cancel", "Cancel",
		new BMessage(kMsgCancel));

	BLayoutBuilder::Group<>(this, B_VERTICAL)
		.SetInsets(B_USE_WINDOW_SPACING)
		.AddGrid(B_USE_DEFAULT_SPACING, B_USE_HALF_ITEM_SPACING)
			.Add(fUrlField->CreateLabelLayoutItem(), 0, 0)
			.Add(fUrlField->CreateTextViewLayoutItem(), 1, 0)
		.End()
		.AddGroup(B_HORIZONTAL)
			.AddGlue()
			.Add(fCancelButton)
			.Add(fApplyButton)
		.End()
	;

	CenterOnScreen();
	fUrlField->MakeFocus(true);
}


void
SettingsWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgApply:
		{
			const char* url = fUrlField->Text();
			if (url == NULL || url[0] == '\0') {
				BAlert* alert = new BAlert("Error",
					"Server URL is required.",
					"OK", NULL, NULL, B_WIDTH_AS_USUAL,
					B_WARNING_ALERT);
				alert->Go();
				return;
			}

			BMessage savedMsg(kMsgSettingsSaved);
			savedMsg.AddString("server_url", url);
			fTarget.SendMessage(&savedMsg);

			PostMessage(B_QUIT_REQUESTED);
			break;
		}

		case kMsgCancel:
			PostMessage(B_QUIT_REQUESTED);
			break;

		default:
			BWindow::MessageReceived(message);
			break;
	}
}


void
SettingsWindow::SetServerUrl(const char* url)
{
	fUrlField->SetText(url);
}
