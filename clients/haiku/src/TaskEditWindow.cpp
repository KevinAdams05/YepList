/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "TaskEditWindow.h"

#include <Alert.h>
#include <Button.h>
#include <LayoutBuilder.h>
#include <MenuField.h>
#include <MenuItem.h>
#include <PopUpMenu.h>
#include <ScrollView.h>
#include <TextControl.h>
#include <TextView.h>

#include "ApiClient.h"
#include "MainWindow.h"


static const uint32 kMsgSave			= 'save';
static const uint32 kMsgCancel			= 'cncl';
static const uint32 kMsgCategoryPicked	= 'cpik';


TaskEditWindow::TaskEditWindow(BMessenger target)
	:
	BWindow(BRect(0, 0, 420, 340), "New Task",
		B_FLOATING_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE),
	fTarget(target),
	fListId(-1),
	fItemId(-1),
	fSortOrder(0),
	fIsCompleted(false),
	fTitleField(NULL),
	fNotesView(NULL),
	fCategoryField(NULL),
	fCategoryMenu(NULL),
	fDueDateField(NULL),
	fSaveButton(NULL),
	fCancelButton(NULL)
{
	fTitleField = new BTextControl("title", "Title:", "",
		NULL);

	fNotesView = new BTextView("notes");
	fNotesView->SetStylable(false);
	fNotesView->MakeEditable(true);
	fNotesView->SetWordWrap(true);
	BScrollView* notesScroll = new BScrollView("notes_scroll",
		fNotesView, 0, false, true);
	notesScroll->SetExplicitMinSize(BSize(B_SIZE_UNSET, 80));

	fCategoryMenu = new BPopUpMenu("(None)");
	fCategoryField = new BMenuField("category", "Category:",
		fCategoryMenu);

	fDueDateField = new BTextControl("due_date", "Due Date:",
		"", NULL);
	fDueDateField->SetToolTip("YYYY-MM-DD format (optional)");

	fSaveButton = new BButton("save", "Save",
		new BMessage(kMsgSave));
	fSaveButton->MakeDefault(true);
	fCancelButton = new BButton("cancel", "Cancel",
		new BMessage(kMsgCancel));

	BLayoutBuilder::Group<>(this, B_VERTICAL)
		.SetInsets(B_USE_WINDOW_SPACING)
		.AddGrid(B_USE_DEFAULT_SPACING, B_USE_HALF_ITEM_SPACING)
			.Add(fTitleField->CreateLabelLayoutItem(), 0, 0)
			.Add(fTitleField->CreateTextViewLayoutItem(), 1, 0)
			.Add(fCategoryField->CreateLabelLayoutItem(), 0, 1)
			.Add(fCategoryField->CreateMenuBarLayoutItem(), 1, 1)
			.Add(fDueDateField->CreateLabelLayoutItem(), 0, 2)
			.Add(fDueDateField->CreateTextViewLayoutItem(), 1, 2)
		.End()
		.Add(notesScroll, 1.0f)
		.AddGroup(B_HORIZONTAL)
			.AddGlue()
			.Add(fCancelButton)
			.Add(fSaveButton)
		.End()
	;

	CenterOnScreen();
}


void
TaskEditWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgSave:
			_SaveTask();
			break;

		case kMsgCancel:
			PostMessage(B_QUIT_REQUESTED);
			break;

		case kMsgCategoryPicked:
			// Category selection is handled by the BMenuField
			break;

		default:
			BWindow::MessageReceived(message);
			break;
	}
}


void
TaskEditWindow::SetListId(int64 listId)
{
	fListId = listId;
}


void
TaskEditWindow::SetCategories(const std::vector<Category>& categories)
{
	fCategories = categories;

	// Rebuild category menu
	while (fCategoryMenu->CountItems() > 0)
		delete fCategoryMenu->RemoveItem(static_cast<int32>(0));

	// Add "None" option
	BMessage* noneMsg = new BMessage(kMsgCategoryPicked);
	noneMsg->AddInt64("category_id", 0);
	BMenuItem* noneItem = new BMenuItem("(None)", noneMsg);
	noneItem->SetMarked(true);
	fCategoryMenu->AddItem(noneItem);

	fCategoryMenu->AddSeparatorItem();

	for (const Category& cat : fCategories) {
		BMessage* catMsg = new BMessage(kMsgCategoryPicked);
		catMsg->AddInt64("category_id", cat.categoryId);
		BMenuItem* item = new BMenuItem(cat.name.String(), catMsg);
		fCategoryMenu->AddItem(item);
	}
}


void
TaskEditWindow::SetItem(const TodoItem& item)
{
	fItemId = item.itemId;
	fSortOrder = item.sortOrder;
	fIsCompleted = item.isCompleted;
	SetTitle("Edit Task");

	fTitleField->SetText(item.title.String());
	fNotesView->SetText(item.notes.String());
	fDueDateField->SetText(item.dueDate.String());

	// Select the matching category in the menu
	if (item.categoryId > 0) {
		for (int32 i = 0; i < fCategoryMenu->CountItems(); i++) {
			BMenuItem* menuItem = fCategoryMenu->ItemAt(i);
			BMessage* msg = menuItem->Message();
			if (msg == NULL)
				continue;
			int64 catId;
			if (msg->FindInt64("category_id", &catId) == B_OK
				&& catId == item.categoryId) {
				menuItem->SetMarked(true);
				break;
			}
		}
	}
}


void
TaskEditWindow::_SaveTask()
{
	const char* title = fTitleField->Text();
	if (title == NULL || title[0] == '\0') {
		BAlert* alert = new BAlert("Error",
			"Title is required.",
			"OK", NULL, NULL, B_WIDTH_AS_USUAL, B_WARNING_ALERT);
		alert->Go();
		return;
	}

	// Get selected category ID
	int64 categoryId = 0;
	BMenuItem* marked = fCategoryMenu->FindMarked();
	if (marked != NULL && marked->Message() != NULL) {
		marked->Message()->FindInt64("category_id", &categoryId);
	}

	const char* notes = fNotesView->Text();
	const char* dueDate = fDueDateField->Text();

	// Get the MainWindow to access its ApiClient
	MainWindow* mainWindow = dynamic_cast<MainWindow*>(
		fTarget.Target(NULL));
	if (mainWindow == NULL) {
		PostMessage(B_QUIT_REQUESTED);
		return;
	}

	ApiClient* api = mainWindow->GetApiClient();

	if (fItemId > 0) {
		// Update existing item
		api->UpdateItem(fItemId, title, notes, categoryId,
			fListId, fIsCompleted, dueDate, fSortOrder,
			fTarget);
	} else {
		// Create new item
		api->CreateItem(fListId, title, notes, categoryId,
			dueDate, fSortOrder, fTarget);
	}

	// Notify main window that we saved
	BMessage savedMsg(kMsgTaskSaved);
	fTarget.SendMessage(&savedMsg);

	PostMessage(B_QUIT_REQUESTED);
}
