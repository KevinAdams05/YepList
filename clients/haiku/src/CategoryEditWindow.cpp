/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "CategoryEditWindow.h"

#include <Alert.h>
#include <Application.h>
#include <Button.h>
#include <LayoutBuilder.h>
#include <TextControl.h>

#include "ApiClient.h"
#include "MainWindow.h"


static const uint32 kMsgOk			= 'okok';
static const uint32 kMsgCancel		= 'cncl';
static const uint32 kMsgCatSaved	= 'csvd';


CategoryEditWindow::CategoryEditWindow(BMessenger target)
	:
	BWindow(BRect(0, 0, 320, 130), "New Category",
		B_FLOATING_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE),
	fTarget(target),
	fCategoryId(-1),
	fNameField(NULL),
	fColorField(NULL),
	fOkButton(NULL),
	fCancelButton(NULL)
{
	fNameField = new BTextControl("name", "Name:", "", NULL);
	fColorField = new BTextControl("color", "Color:", "",
		NULL);
	fColorField->SetToolTip("Hex color, e.g. #FF5733 (optional)");

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
			.Add(fColorField->CreateLabelLayoutItem(), 0, 1)
			.Add(fColorField->CreateTextViewLayoutItem(), 1, 1)
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
CategoryEditWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgOk:
			_SaveCategory();
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
CategoryEditWindow::SetExistingCategory(int64 categoryId,
	const char* name, const char* color)
{
	fCategoryId = categoryId;
	SetTitle("Edit Category");
	fNameField->SetText(name);
	fColorField->SetText(color);
}


void
CategoryEditWindow::_SaveCategory()
{
	const char* name = fNameField->Text();
	if (name == NULL || name[0] == '\0') {
		BAlert* alert = new BAlert("Error",
			"Category name is required.",
			"OK", NULL, NULL, B_WIDTH_AS_USUAL, B_WARNING_ALERT);
		alert->Go();
		return;
	}

	const char* color = fColorField->Text();

	// Find MainWindow to access the ApiClient
	MainWindow* mainWindow = NULL;
	for (int32 i = 0; i < be_app->CountWindows(); i++) {
		mainWindow = dynamic_cast<MainWindow*>(
			be_app->WindowAt(i));
		if (mainWindow != NULL)
			break;
	}

	if (mainWindow == NULL) {
		PostMessage(B_QUIT_REQUESTED);
		return;
	}

	ApiClient* api = mainWindow->GetApiClient();

	// Send API response to CategoryWindow (fTarget)
	if (fCategoryId > 0) {
		api->UpdateCategory(fCategoryId, name, color, fTarget);
	} else {
		api->CreateCategory(name, color, fTarget);
	}

	// Notify CategoryWindow that a save happened
	BMessage savedMsg(kMsgCatSaved);
	fTarget.SendMessage(&savedMsg);

	PostMessage(B_QUIT_REQUESTED);
}
