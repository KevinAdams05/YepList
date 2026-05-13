/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "ListEditWindow.h"

#include <Alert.h>
#include <Button.h>
#include <LayoutBuilder.h>
#include <TextControl.h>

#include "ApiClient.h"
#include "MainWindow.h"


static const uint32 kMsgOk		= 'okok';
static const uint32 kMsgCancel	= 'cncl';


ListEditWindow::ListEditWindow(BMessenger target)
	:
	BWindow(BRect(0, 0, 320, 100), "New List",
		B_FLOATING_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE),
	fTarget(target),
	fListId(-1),
	fNameField(NULL),
	fOkButton(NULL),
	fCancelButton(NULL)
{
	fNameField = new BTextControl("name", "Name:", "", NULL);

	fOkButton = new BButton("ok", "OK",
		new BMessage(kMsgOk));
	fOkButton->MakeDefault(true);
	fCancelButton = new BButton("cancel", "Cancel",
		new BMessage(kMsgCancel));

	BLayoutBuilder::Group<>(this, B_VERTICAL)
		.SetInsets(B_USE_WINDOW_SPACING)
		.AddGrid(B_USE_DEFAULT_SPACING, B_USE_HALF_ITEM_SPACING)
			.Add(fNameField->CreateLabelLayoutItem(), 0, 0)
			.Add(fNameField->CreateTextViewLayoutItem(), 1, 0)
		.End()
		.AddGroup(B_HORIZONTAL)
			.AddGlue()
			.Add(fCancelButton)
			.Add(fOkButton)
		.End()
	;

	CenterOnScreen();
	fNameField->MakeFocus(true);
}


void
ListEditWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgOk:
			_SaveList();
			break;

		case kMsgCancel:
			PostMessage(B_QUIT_REQUESTED);
			break;

		default:
			BWindow::MessageReceived(message);
			break;
	}
}


void
ListEditWindow::SetExistingList(int64 listId, const char* name)
{
	fListId = listId;
	SetTitle("Rename List");
	fNameField->SetText(name);
}


void
ListEditWindow::_SaveList()
{
	const char* name = fNameField->Text();
	if (name == NULL || name[0] == '\0') {
		BAlert* alert = new BAlert("Error",
			"List name is required.",
			"OK", NULL, NULL, B_WIDTH_AS_USUAL, B_WARNING_ALERT);
		alert->Go();
		return;
	}

	MainWindow* mainWindow = dynamic_cast<MainWindow*>(
		fTarget.Target(NULL));
	if (mainWindow == NULL) {
		PostMessage(B_QUIT_REQUESTED);
		return;
	}

	ApiClient* api = mainWindow->GetApiClient();

	if (fListId > 0) {
		// Update existing list
		api->UpdateList(fListId, name, 0, fTarget);
	} else {
		// Create new list
		api->CreateList(name, 0, fTarget);
	}

	// Notify main window
	BMessage savedMsg(kMsgListSaved);
	fTarget.SendMessage(&savedMsg);

	PostMessage(B_QUIT_REQUESTED);
}
