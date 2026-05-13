/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "ListSidebar.h"

#include <Button.h>
#include <LayoutBuilder.h>
#include <ListView.h>
#include <MenuItem.h>
#include <Messenger.h>
#include <PopUpMenu.h>
#include <ScrollView.h>
#include <Window.h>

#include "ListItem.h"
#include "MainWindow.h"


static const uint32 kMsgListSelChanged		= 'lsch';
static const uint32 kMsgContextRename		= 'cren';
static const uint32 kMsgContextDelete		= 'cdel';
static const uint32 kMsgContextDefault		= 'cdef';
static const uint32 kMsgRightClick			= 'rclk';


// Custom BListView that detects right-clicks and forwards them
class ContextListView : public BListView {
public:
	ContextListView(const char* name)
		:
		BListView(name, B_SINGLE_SELECTION_LIST)
	{
	}

	virtual void MouseDown(BPoint where)
	{
		BMessage* message = Window()->CurrentMessage();
		int32 buttons = 0;
		if (message != NULL)
			message->FindInt32("buttons", &buttons);

		if (buttons & B_SECONDARY_MOUSE_BUTTON) {
			// Select the item under the cursor
			int32 index = IndexOf(where);
			if (index >= 0) {
				Select(index);
				BMessage rightClick(kMsgRightClick);
				rightClick.AddPoint("screen_where",
					ConvertToScreen(where));
				BMessenger(Parent()).SendMessage(&rightClick);
			}
			return;
		}

		BListView::MouseDown(where);
	}
};


ListSidebar::ListSidebar()
	:
	BView("list_sidebar", B_WILL_DRAW),
	fListView(NULL),
	fNewListButton(NULL)
{
	fListView = new ContextListView("list_view");
	fListView->SetSelectionMessage(new BMessage(kMsgListSelChanged));
	BScrollView* scrollView = new BScrollView("list_scroll",
		fListView, 0, false, true);

	fNewListButton = new BButton("new_list", "New List",
		new BMessage(kMsgNewList));

	BLayoutBuilder::Group<>(this, B_VERTICAL, 0.0f)
		.Add(scrollView, 1.0f)
		.AddGroup(B_HORIZONTAL)
			.Add(fNewListButton)
			.SetInsets(B_USE_SMALL_SPACING)
		.End()
	;
}


void
ListSidebar::AttachedToWindow()
{
	BView::AttachedToWindow();
	fNewListButton->SetTarget(Window());
	fListView->SetTarget(this);
}


void
ListSidebar::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgListSelChanged:
		{
			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;

			ListItem* item = dynamic_cast<ListItem*>(
				fListView->ItemAt(index));
			if (item == NULL)
				break;

			BMessage listMsg(kMsgListSelected);
			listMsg.AddInt64("list_id", item->ListId());
			Window()->PostMessage(&listMsg);
			break;
		}

		case kMsgRightClick:
		{
			BPoint screenWhere;
			if (message->FindPoint("screen_where",
				&screenWhere) == B_OK) {
				_ShowContextMenu(screenWhere);
			}
			break;
		}

		case kMsgContextRename:
		{
			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;
			ListItem* item = dynamic_cast<ListItem*>(
				fListView->ItemAt(index));
			if (item == NULL)
				break;

			BMessage renameMsg(kMsgRenameList);
			renameMsg.AddInt64("list_id", item->ListId());
			Window()->PostMessage(&renameMsg);
			break;
		}

		case kMsgContextDelete:
		{
			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;
			ListItem* item = dynamic_cast<ListItem*>(
				fListView->ItemAt(index));
			if (item == NULL)
				break;

			BMessage deleteMsg(kMsgDeleteList);
			deleteMsg.AddInt64("list_id", item->ListId());
			Window()->PostMessage(&deleteMsg);
			break;
		}

		case kMsgContextDefault:
		{
			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;
			ListItem* item = dynamic_cast<ListItem*>(
				fListView->ItemAt(index));
			if (item == NULL)
				break;

			BMessage defaultMsg(kMsgSetDefaultList);
			defaultMsg.AddInt64("list_id", item->ListId());
			Window()->PostMessage(&defaultMsg);
			break;
		}

		default:
			BView::MessageReceived(message);
			break;
	}
}


void
ListSidebar::UpdateLists(const std::vector<TodoList>& lists,
	int64 defaultListId)
{
	// Remember current selection
	int64 selectedId = -1;
	int32 selectedIndex = fListView->CurrentSelection();
	if (selectedIndex >= 0) {
		ListItem* item = dynamic_cast<ListItem*>(
			fListView->ItemAt(selectedIndex));
		if (item != NULL)
			selectedId = item->ListId();
	}

	// Clear and rebuild
	fListView->MakeEmpty();

	for (const TodoList& list : lists) {
		bool isDefault = (list.listId == defaultListId);
		fListView->AddItem(new ListItem(list.listId,
			list.name.String(), isDefault));
	}

	// Restore selection
	if (selectedId >= 0) {
		SelectList(selectedId);
	}
}


void
ListSidebar::SelectList(int64 listId)
{
	for (int32 i = 0; i < fListView->CountItems(); i++) {
		ListItem* item = dynamic_cast<ListItem*>(
			fListView->ItemAt(i));
		if (item != NULL && item->ListId() == listId) {
			fListView->Select(i);
			fListView->ScrollToSelection();
			return;
		}
	}
}


void
ListSidebar::_ShowContextMenu(BPoint screenWhere)
{
	int32 index = fListView->CurrentSelection();
	if (index < 0)
		return;

	ListItem* item = dynamic_cast<ListItem*>(
		fListView->ItemAt(index));
	if (item == NULL)
		return;

	BPopUpMenu* menu = new BPopUpMenu("list_context", false, false);

	menu->AddItem(new BMenuItem("Rename" B_UTF8_ELLIPSIS,
		new BMessage(kMsgContextRename)));
	menu->AddItem(new BMenuItem("Delete" B_UTF8_ELLIPSIS,
		new BMessage(kMsgContextDelete)));
	menu->AddSeparatorItem();

	if (item->IsDefault()) {
		menu->AddItem(new BMenuItem("Clear Default",
			new BMessage(kMsgContextDefault)));
	} else {
		menu->AddItem(new BMenuItem("Set as Default",
			new BMessage(kMsgContextDefault)));
	}

	menu->SetTargetForItems(this);
	menu->Go(screenWhere, true, true, true);
}
