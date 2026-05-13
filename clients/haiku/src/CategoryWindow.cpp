/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "CategoryWindow.h"

#include <Alert.h>
#include <Button.h>
#include <LayoutBuilder.h>
#include <ListView.h>
#include <ScrollView.h>
#include <StringItem.h>

#include <cstdio>

#include "ApiClient.h"
#include "CategoryEditWindow.h"
#include "MainWindow.h"


static const uint32 kMsgAdd			= 'cadd';
static const uint32 kMsgEdit		= 'cedt';
static const uint32 kMsgDelete		= 'cdel';
static const uint32 kMsgCatSaved	= 'csvd';


// Custom list item that draws a color swatch + name
class CategoryListItem : public BStringItem {
public:
	CategoryListItem(int64 categoryId, const char* name,
		const char* color)
		:
		BStringItem(name),
		fCategoryId(categoryId),
		fColor(color)
	{
	}

	virtual void DrawItem(BView* owner, BRect frame, bool complete)
	{
		rgb_color bgColor;
		if (IsSelected()) {
			bgColor = ui_color(B_LIST_SELECTED_BACKGROUND_COLOR);
		} else {
			bgColor = ui_color(B_LIST_BACKGROUND_COLOR);
		}

		owner->SetHighColor(bgColor);
		owner->FillRect(frame);

		// Draw color swatch
		float swatchSize = frame.Height() - 6.0f;
		BRect swatchRect(frame.left + 4.0f,
			frame.top + 3.0f,
			frame.left + 4.0f + swatchSize,
			frame.top + 3.0f + swatchSize);

		if (fColor.Length() == 7 && fColor[0] == '#') {
			int r, g, b;
			if (sscanf(fColor.String() + 1, "%02x%02x%02x",
				&r, &g, &b) == 3) {
				rgb_color swatchColor = {
					static_cast<uint8>(r),
					static_cast<uint8>(g),
					static_cast<uint8>(b),
					255
				};
				owner->SetHighColor(swatchColor);
				owner->FillRect(swatchRect);
			}
		}

		// Draw border around swatch
		owner->SetHighColor(tint_color(bgColor, B_DARKEN_2_TINT));
		owner->StrokeRect(swatchRect);

		// Draw name
		if (IsSelected()) {
			owner->SetHighColor(
				ui_color(B_LIST_SELECTED_ITEM_TEXT_COLOR));
		} else {
			owner->SetHighColor(ui_color(B_LIST_ITEM_TEXT_COLOR));
		}

		BFont font;
		owner->GetFont(&font);
		font_height fh;
		font.GetHeight(&fh);
		float baseline = frame.top + (frame.Height()
			+ fh.ascent - fh.descent) / 2.0f;

		owner->DrawString(Text(),
			BPoint(swatchRect.right + 8.0f, baseline));
	}

	int64 CategoryId() const { return fCategoryId; }
	const BString& Color() const { return fColor; }

private:
	int64	fCategoryId;
	BString	fColor;
};


CategoryWindow::CategoryWindow(BMessenger target)
	:
	BWindow(BRect(0, 0, 400, 300), "Manage Categories",
		B_FLOATING_WINDOW,
		B_AUTO_UPDATE_SIZE_LIMITS | B_CLOSE_ON_ESCAPE
		| B_NOT_ZOOMABLE | B_NOT_MINIMIZABLE),
	fTarget(target),
	fListView(NULL),
	fAddButton(NULL),
	fEditButton(NULL),
	fDeleteButton(NULL)
{
	fListView = new BListView("category_list",
		B_SINGLE_SELECTION_LIST);
	BScrollView* scrollView = new BScrollView("cat_scroll",
		fListView, 0, false, true);

	fAddButton = new BButton("add", "Add" B_UTF8_ELLIPSIS,
		new BMessage(kMsgAdd));
	fEditButton = new BButton("edit", "Edit" B_UTF8_ELLIPSIS,
		new BMessage(kMsgEdit));
	fDeleteButton = new BButton("delete", "Delete",
		new BMessage(kMsgDelete));

	BLayoutBuilder::Group<>(this, B_VERTICAL)
		.SetInsets(B_USE_WINDOW_SPACING)
		.Add(scrollView, 1.0f)
		.AddGroup(B_HORIZONTAL)
			.Add(fAddButton)
			.Add(fEditButton)
			.Add(fDeleteButton)
			.AddGlue()
		.End()
	;

	CenterOnScreen();

	// Load categories from MainWindow
	MainWindow* mainWindow = dynamic_cast<MainWindow*>(
		fTarget.Target(NULL));
	if (mainWindow != NULL) {
		fCategories = mainWindow->Categories();
		_RefreshList();
	}
}


void
CategoryWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgAdd:
			_OnAdd();
			break;

		case kMsgEdit:
			_OnEdit();
			break;

		case kMsgDelete:
			_OnDelete();
			break;

		case kMsgCatSaved:
		{
			// Category was created or updated by sub-dialog.
			// Re-fetch categories from the API into our own list.
			MainWindow* mainWindow = dynamic_cast<MainWindow*>(
				fTarget.Target(NULL));
			if (mainWindow != NULL) {
				mainWindow->GetApiClient()->GetCategories(
					BMessenger(this));
			}
			break;
		}

		case kMsgGetCategoriesDone:
		{
			bool success;
			if (message->FindBool("success", &success) != B_OK
				|| !success) {
				break;
			}

			BMessage data;
			if (message->FindMessage("data", &data) != B_OK)
				break;

			fCategories.clear();
			for (int32 i = 0; ; i++) {
				char key[16];
				snprintf(key, sizeof(key), "%" B_PRId32, i);
				BMessage itemMsg;
				if (data.FindMessage(key, &itemMsg) != B_OK)
					break;
				fCategories.push_back(Category::FromJson(itemMsg));
			}
			_RefreshList();
			break;
		}

		case kMsgCreateCategoryDone:
		case kMsgUpdateCategoryDone:
		{
			// API response from create/update — re-fetch the list
			bool success;
			if (message->FindBool("success", &success) == B_OK
				&& success) {
				MainWindow* mainWindow = dynamic_cast<MainWindow*>(
					fTarget.Target(NULL));
				if (mainWindow != NULL) {
					mainWindow->GetApiClient()->GetCategories(
						BMessenger(this));
				}
			}
			break;
		}

		case kMsgDeleteCategoryDone:
		{
			bool success;
			if (message->FindBool("success", &success) == B_OK
				&& success) {
				// Already removed locally in _OnDelete, just
				// notify MainWindow
				BMessage catDoneMsg(kMsgCategoriesDone);
				fTarget.SendMessage(&catDoneMsg);
			}
			break;
		}

		default:
			BWindow::MessageReceived(message);
			break;
	}
}


bool
CategoryWindow::QuitRequested()
{
	// Notify main window that categories may have changed
	BMessage doneMsg(kMsgCategoriesDone);
	fTarget.SendMessage(&doneMsg);
	return true;
}


void
CategoryWindow::SetCategories(const std::vector<Category>& categories)
{
	fCategories = categories;
	_RefreshList();
}


void
CategoryWindow::_RefreshList()
{
	fListView->MakeEmpty();
	for (const Category& cat : fCategories) {
		fListView->AddItem(new CategoryListItem(
			cat.categoryId, cat.name.String(),
			cat.color.String()));
	}
}


void
CategoryWindow::_OnAdd()
{
	CategoryEditWindow* window = new CategoryEditWindow(
		BMessenger(this));
	window->Show();
}


void
CategoryWindow::_OnEdit()
{
	int32 index = fListView->CurrentSelection();
	if (index < 0)
		return;

	CategoryListItem* item = dynamic_cast<CategoryListItem*>(
		fListView->ItemAt(index));
	if (item == NULL)
		return;

	CategoryEditWindow* window = new CategoryEditWindow(
		BMessenger(this));
	window->SetExistingCategory(item->CategoryId(),
		item->Text(), item->Color().String());
	window->Show();
}


void
CategoryWindow::_OnDelete()
{
	int32 index = fListView->CurrentSelection();
	if (index < 0)
		return;

	CategoryListItem* item = dynamic_cast<CategoryListItem*>(
		fListView->ItemAt(index));
	if (item == NULL)
		return;

	BAlert* alert = new BAlert("Delete Category",
		"Delete this category? Tasks using it will become "
		"uncategorized.",
		"Cancel", "Delete", NULL, B_WIDTH_AS_USUAL,
		B_WARNING_ALERT);
	if (alert->Go() != 1)
		return;

	MainWindow* mainWindow = dynamic_cast<MainWindow*>(
		fTarget.Target(NULL));
	if (mainWindow == NULL)
		return;

	mainWindow->GetApiClient()->DeleteCategory(
		item->CategoryId(), BMessenger(this));

	// Remove from local list immediately
	for (auto it = fCategories.begin(); it != fCategories.end();
		++it) {
		if (it->categoryId == item->CategoryId()) {
			fCategories.erase(it);
			break;
		}
	}
	_RefreshList();
}
