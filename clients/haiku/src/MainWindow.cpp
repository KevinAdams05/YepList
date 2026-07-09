/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "MainWindow.h"

#include <Alert.h>
#include <Application.h>
#include <LayoutBuilder.h>
#include <Menu.h>
#include <MenuBar.h>
#include <MenuItem.h>
#include <MessageRunner.h>
#include <Screen.h>
#include <Size.h>
#include <StringView.h>
#include <TimeFormat.h>

#include <cstdio>

#include "CategoryWindow.h"
#include "ListEditWindow.h"
#include "ListItem.h"
#include "ListSidebar.h"
#include "SettingsWindow.h"
#include "TaskEditWindow.h"
#include "TaskItem.h"
#include "TaskListView.h"


static const bigtime_t kSyncInterval = 30 * 1000000LL;	// 30 seconds


MainWindow::MainWindow()
	:
	BWindow(BRect(100, 100, 1100, 740), "YepList",
		B_DOCUMENT_WINDOW,
		B_ASYNCHRONOUS_CONTROLS),
	fMenuBar(NULL),
	fListSidebar(NULL),
	fTaskListView(NULL),
	fStatusBar(NULL),
	fSyncRunner(NULL),
	fSelectedListId(-1),
	fInitialLoadDone(false)
{
	fSettings = Settings::Load();
	fApiClient.SetServerUrl(fSettings.ServerUrl());

	_BuildMenu();
	_BuildLayout();

	// Restore the last-used window frame if it was saved and still lands
	// on-screen; otherwise fall back to centering.
	BScreen screen(this);
	if (fSettings.HasWindowFrame()
		&& screen.Frame().Intersects(fSettings.WindowFrame())) {
		BRect frame = fSettings.WindowFrame();
		MoveTo(frame.LeftTop());
		ResizeTo(frame.Width(), frame.Height());
	} else {
		CenterOnScreen();
	}

	// Kick off initial data load
	_DoFullRefresh();
	_StartSyncTimer();
}


MainWindow::~MainWindow()
{
	delete fSyncRunner;
}


void
MainWindow::MessageReceived(BMessage* message)
{
	switch (message->what) {
		// Menu invokes B_ABOUT_REQUESTED on the window by default;
		// forward to the application so YepListApp::AboutRequested
		// can show the AboutWindow.
		case B_ABOUT_REQUESTED:
			be_app->PostMessage(B_ABOUT_REQUESTED);
			break;

		// Sync timer
		case kMsgSync:
			_DoSync();
			break;

		// User actions
		case kMsgNewTask:
			_OnNewTask();
			break;

		case kMsgEditTask:
			_OnEditTask();
			break;

		case kMsgDeleteTask:
			_OnDeleteTask();
			break;

		case kMsgToggleComplete:
		{
			int64 itemId;
			bool completed;
			if (message->FindInt64("item_id", &itemId) == B_OK
				&& message->FindBool("completed", &completed) == B_OK) {
				fApiClient.ToggleComplete(itemId, completed,
					BMessenger(this));
			}
			break;
		}

		case kMsgQuickAdd:
		{
			const char* title;
			if (message->FindString("title", &title) == B_OK)
				_OnQuickAdd(title);
			break;
		}

		case kMsgNewList:
			_OnNewList();
			break;

		case kMsgRenameList:
		{
			int64 listId;
			if (message->FindInt64("list_id", &listId) == B_OK)
				_OnRenameList(listId);
			break;
		}

		case kMsgDeleteList:
		{
			int64 listId;
			if (message->FindInt64("list_id", &listId) == B_OK)
				_OnDeleteList(listId);
			break;
		}

		case kMsgSetDefaultList:
		{
			int64 listId;
			if (message->FindInt64("list_id", &listId) == B_OK)
				_OnSetDefaultList(listId);
			break;
		}

		case kMsgListSelected:
		{
			int64 listId;
			if (message->FindInt64("list_id", &listId) == B_OK)
				_OnListSelected(listId);
			break;
		}

		case kMsgManageCategories:
		{
			CategoryWindow* window = new CategoryWindow(
				BMessenger(this));
			window->Show();
			break;
		}

		case kMsgShowSettings:
			_OnShowSettings();
			break;

		// Dialog results
		case kMsgTaskSaved:
		{
			// Task was created or updated — refresh
			_LoadItemsForSelectedList();
			break;
		}

		case kMsgListSaved:
		{
			// List was created or renamed — refresh
			fApiClient.GetLists(BMessenger(this));
			break;
		}

		case kMsgCategoriesDone:
			fApiClient.GetCategories(BMessenger(this));
			break;

		case kMsgSettingsSaved:
		{
			const char* url;
			if (message->FindString("server_url", &url) == B_OK) {
				fSettings.SetServerUrl(BString(url));
				fSettings.Save();
				fApiClient.SetServerUrl(fSettings.ServerUrl());
				fLastSyncTime = "";
				_DoFullRefresh();
			}
			break;
		}

		// API responses
		case kMsgGetListsDone:
			_HandleGetListsDone(message);
			break;

		case kMsgGetCategoriesDone:
			_HandleGetCategoriesDone(message);
			break;

		case kMsgGetItemsDone:
			_HandleGetItemsDone(message);
			break;

		case kMsgSyncDone:
			_HandleSyncDone(message);
			break;

		case kMsgCreateListDone:
			_HandleCreateListDone(message);
			break;

		case kMsgDeleteListDone:
			_HandleDeleteListDone(message);
			break;

		case kMsgCreateItemDone:
			_HandleCreateItemDone(message);
			break;

		case kMsgDeleteItemDone:
			_HandleDeleteItemDone(message);
			break;

		case kMsgToggleCompleteDone:
			_HandleToggleDone(message);
			break;

		case kMsgUpdateListDone:
		case kMsgUpdateItemDone:
		case kMsgUpdateCategoryDone:
		case kMsgCreateCategoryDone:
		case kMsgDeleteCategoryDone:
		case kMsgReorderItemsDone:
			// Generic refresh after successful mutation
		{
			bool success;
			if (message->FindBool("success", &success) == B_OK
				&& success) {
				_DoSync();
			}
			break;
		}

		default:
			BWindow::MessageReceived(message);
			break;
	}
}


bool
MainWindow::QuitRequested()
{
	fSettings.SetWindowFrame(Frame());
	fSettings.Save();
	be_app->PostMessage(B_QUIT_REQUESTED);
	return true;
}


void
MainWindow::_BuildMenu()
{
	fMenuBar = new BMenuBar("menubar");

	// Program menu
	BMenu* programMenu = new BMenu("Program");
	programMenu->AddItem(new BMenuItem("About YepList" B_UTF8_ELLIPSIS,
		new BMessage(B_ABOUT_REQUESTED)));
	programMenu->AddItem(new BMenuItem("Settings" B_UTF8_ELLIPSIS,
		new BMessage(kMsgShowSettings), ','));
	programMenu->AddSeparatorItem();
	programMenu->AddItem(new BMenuItem("Quit",
		new BMessage(B_QUIT_REQUESTED), 'Q'));
	fMenuBar->AddItem(programMenu);

	// Edit menu
	BMenu* editMenu = new BMenu("Edit");
	editMenu->AddItem(new BMenuItem("New Task" B_UTF8_ELLIPSIS,
		new BMessage(kMsgNewTask), 'N'));
	editMenu->AddItem(new BMenuItem("Edit Task" B_UTF8_ELLIPSIS,
		new BMessage(kMsgEditTask), 'E'));
	editMenu->AddItem(new BMenuItem("Delete Task",
		new BMessage(kMsgDeleteTask), B_DELETE, 0));
	editMenu->AddSeparatorItem();
	editMenu->AddItem(new BMenuItem("New List" B_UTF8_ELLIPSIS,
		new BMessage(kMsgNewList)));
	editMenu->AddItem(new BMenuItem("Manage Categories" B_UTF8_ELLIPSIS,
		new BMessage(kMsgManageCategories)));
	fMenuBar->AddItem(editMenu);
}


void
MainWindow::_BuildLayout()
{
	fListSidebar = new ListSidebar();
	fTaskListView = new TaskListView();
	fStatusBar = new BStringView("statusbar", "Connecting...");
	fStatusBar->SetAlignment(B_ALIGN_LEFT);

	// Sidebar takes ~25% of the width and the task list ~75%, matching the
	// Linux client's NavigationSplitView. The sidebar is clamped to a usable
	// range (180–280px, like libadwaita's defaults) so it never dominates on
	// narrow windows nor sprawls on wide ones.
	fListSidebar->SetExplicitMinSize(BSize(180, B_SIZE_UNSET));
	fListSidebar->SetExplicitMaxSize(BSize(280, B_SIZE_UNLIMITED));

	BLayoutBuilder::Group<>(this, B_VERTICAL, 0.0f)
		.Add(fMenuBar)
		.AddGroup(B_HORIZONTAL, 0.0f)
			.Add(fListSidebar, 1.0f)
			.Add(fTaskListView, 3.0f)
		.End()
		.AddGroup(B_HORIZONTAL, 0.0f)
			.Add(fStatusBar)
			.SetInsets(B_USE_SMALL_SPACING)
		.End()
	;

	SetSizeLimits(500, 4000, 300, 3000);
}


void
MainWindow::_StartSyncTimer()
{
	BMessage syncMessage(kMsgSync);
	fSyncRunner = new BMessageRunner(BMessenger(this),
		&syncMessage, kSyncInterval, -1);
}


// -- Data operations --

void
MainWindow::_DoFullRefresh()
{
	_UpdateStatusBar("Syncing...");
	fApiClient.GetLists(BMessenger(this));
	fApiClient.GetCategories(BMessenger(this));
}


void
MainWindow::_DoSync()
{
	fApiClient.Sync(fLastSyncTime.String(), BMessenger(this));
}


void
MainWindow::_RefreshSidebar()
{
	fListSidebar->UpdateLists(fLists, fSettings.DefaultListId());
}


void
MainWindow::_RefreshTaskList()
{
	fTaskListView->UpdateItems(fItems, fCategories);

	// Update header with selected list name
	for (const TodoList& list : fLists) {
		if (list.listId == fSelectedListId) {
			fTaskListView->SetHeaderText(list.name.String());
			return;
		}
	}
	fTaskListView->SetHeaderText("Select a list");
}


void
MainWindow::_LoadItemsForSelectedList()
{
	if (fSelectedListId > 0) {
		fApiClient.GetItemsByList(fSelectedListId,
			BMessenger(this));
	}
}


// -- Handle API responses --

void
MainWindow::_HandleGetListsDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success) {
		_UpdateStatusBar("Failed to load lists");
		return;
	}

	BMessage data;
	if (message->FindMessage("data", &data) != B_OK)
		return;

	// Response is an array — parse numbered keys
	fLists.clear();
	for (int32 i = 0; ; i++) {
		char key[16];
		snprintf(key, sizeof(key), "%" B_PRId32, i);
		BMessage itemMsg;
		if (data.FindMessage(key, &itemMsg) != B_OK)
			break;
		fLists.push_back(TodoList::FromJson(itemMsg));
	}

	_RefreshSidebar();

	// Auto-select default list or first list on initial load
	if (!fInitialLoadDone && !fLists.empty()) {
		int64 defaultId = fSettings.DefaultListId();
		bool found = false;
		for (const TodoList& list : fLists) {
			if (list.listId == defaultId) {
				found = true;
				break;
			}
		}
		if (!found)
			defaultId = fLists[0].listId;

		_OnListSelected(defaultId);
		fListSidebar->SelectList(defaultId);
		fInitialLoadDone = true;
	}

	_UpdateStatusBar("Connected");
}


void
MainWindow::_HandleGetCategoriesDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;

	BMessage data;
	if (message->FindMessage("data", &data) != B_OK)
		return;

	fCategories.clear();
	for (int32 i = 0; ; i++) {
		char key[16];
		snprintf(key, sizeof(key), "%" B_PRId32, i);
		BMessage itemMsg;
		if (data.FindMessage(key, &itemMsg) != B_OK)
			break;
		fCategories.push_back(Category::FromJson(itemMsg));
	}
}


void
MainWindow::_HandleGetItemsDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success) {
		_UpdateStatusBar("Failed to load tasks");
		return;
	}

	BMessage data;
	if (message->FindMessage("data", &data) != B_OK)
		return;

	fItems.clear();
	for (int32 i = 0; ; i++) {
		char key[16];
		snprintf(key, sizeof(key), "%" B_PRId32, i);
		BMessage itemMsg;
		if (data.FindMessage(key, &itemMsg) != B_OK)
			break;
		fItems.push_back(TodoItem::FromJson(itemMsg));
	}

	_RefreshTaskList();
}


void
MainWindow::_HandleSyncDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success) {
		_UpdateStatusBar("Sync failed");
		return;
	}

	BMessage data;
	if (message->FindMessage("data", &data) != B_OK)
		return;

	SyncResponse sync = SyncResponse::FromJson(data);
	fLastSyncTime = sync.serverTime;

	bool listsChanged = false;
	bool categoriesChanged = false;
	bool itemsChanged = false;

	// Apply list changes
	for (const TodoList& updatedList : sync.lists) {
		bool found = false;
		for (TodoList& existing : fLists) {
			if (existing.listId == updatedList.listId) {
				existing = updatedList;
				found = true;
				break;
			}
		}
		if (!found)
			fLists.push_back(updatedList);
		listsChanged = true;
	}

	// Apply list deletions
	for (int64 id : sync.deletedListIds) {
		for (auto it = fLists.begin(); it != fLists.end(); ++it) {
			if (it->listId == id) {
				fLists.erase(it);
				listsChanged = true;
				break;
			}
		}
	}

	// Apply category changes
	for (const Category& updatedCat : sync.categories) {
		bool found = false;
		for (Category& existing : fCategories) {
			if (existing.categoryId == updatedCat.categoryId) {
				existing = updatedCat;
				found = true;
				break;
			}
		}
		if (!found)
			fCategories.push_back(updatedCat);
		categoriesChanged = true;
	}

	// Apply category deletions
	for (int64 id : sync.deletedCategoryIds) {
		for (auto it = fCategories.begin();
			it != fCategories.end(); ++it) {
			if (it->categoryId == id) {
				fCategories.erase(it);
				categoriesChanged = true;
				break;
			}
		}
	}

	// Apply item changes
	for (const TodoItem& updatedItem : sync.items) {
		if (updatedItem.listId == fSelectedListId) {
			bool found = false;
			for (TodoItem& existing : fItems) {
				if (existing.itemId == updatedItem.itemId) {
					existing = updatedItem;
					found = true;
					break;
				}
			}
			if (!found)
				fItems.push_back(updatedItem);
			itemsChanged = true;
		}
	}

	// Apply item deletions
	for (int64 id : sync.deletedItemIds) {
		for (auto it = fItems.begin(); it != fItems.end(); ++it) {
			if (it->itemId == id) {
				fItems.erase(it);
				itemsChanged = true;
				break;
			}
		}
	}

	if (listsChanged)
		_RefreshSidebar();
	if (itemsChanged || categoriesChanged)
		_RefreshTaskList();

	// Format current time for status bar
	time_t now = time(NULL);
	struct tm* localTime = localtime(&now);
	char timeStr[32];
	strftime(timeStr, sizeof(timeStr), "%H:%M:%S", localTime);
	BString statusText;
	statusText.SetToFormat("Synced at %s", timeStr);
	_UpdateStatusBar(statusText.String());
}


void
MainWindow::_HandleCreateListDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;
	fApiClient.GetLists(BMessenger(this));
}


void
MainWindow::_HandleDeleteListDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;
	fApiClient.GetLists(BMessenger(this));

	// If we deleted the selected list, clear the task view
	if (!fLists.empty() && fSelectedListId > 0) {
		bool found = false;
		for (const TodoList& list : fLists) {
			if (list.listId == fSelectedListId) {
				found = true;
				break;
			}
		}
		if (!found) {
			fItems.clear();
			_RefreshTaskList();
		}
	}
}


void
MainWindow::_HandleCreateItemDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;
	_LoadItemsForSelectedList();
}


void
MainWindow::_HandleDeleteItemDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;
	_LoadItemsForSelectedList();
}


void
MainWindow::_HandleToggleDone(BMessage* message)
{
	bool success;
	if (message->FindBool("success", &success) != B_OK || !success)
		return;
	_LoadItemsForSelectedList();
}


// -- User actions --

void
MainWindow::_OnNewTask()
{
	if (fSelectedListId <= 0) {
		BAlert* alert = new BAlert("No List",
			"Please select a list first.",
			"OK", NULL, NULL, B_WIDTH_AS_USUAL, B_INFO_ALERT);
		alert->Go();
		return;
	}

	TaskEditWindow* window = new TaskEditWindow(BMessenger(this));
	window->SetListId(fSelectedListId);
	window->SetCategories(fCategories);
	window->Show();
}


void
MainWindow::_OnEditTask()
{
	int64 itemId = fTaskListView->SelectedItemId();
	if (itemId <= 0)
		return;

	for (const TodoItem& item : fItems) {
		if (item.itemId == itemId) {
			TaskEditWindow* window = new TaskEditWindow(
				BMessenger(this));
			window->SetListId(fSelectedListId);
			window->SetCategories(fCategories);
			window->SetItem(item);
			window->Show();
			return;
		}
	}
}


void
MainWindow::_OnDeleteTask()
{
	int64 itemId = fTaskListView->SelectedItemId();
	if (itemId <= 0)
		return;

	BAlert* alert = new BAlert("Delete Task",
		"Are you sure you want to delete this task?",
		"Cancel", "Delete", NULL, B_WIDTH_AS_USUAL, B_WARNING_ALERT);
	if (alert->Go() == 1) {
		fApiClient.DeleteItem(itemId, BMessenger(this));
	}
}


void
MainWindow::_OnQuickAdd(const char* title)
{
	if (fSelectedListId <= 0 || title == NULL || title[0] == '\0')
		return;

	int32 sortOrder = 0;
	if (!fItems.empty()) {
		for (const TodoItem& item : fItems) {
			if (item.sortOrder >= sortOrder)
				sortOrder = item.sortOrder + 1;
		}
	}

	fApiClient.CreateItem(fSelectedListId, title, "", 0, "",
		sortOrder, BMessenger(this));
}


void
MainWindow::_OnNewList()
{
	ListEditWindow* window = new ListEditWindow(BMessenger(this));
	window->Show();
}


void
MainWindow::_OnRenameList(int64 listId)
{
	for (const TodoList& list : fLists) {
		if (list.listId == listId) {
			ListEditWindow* window = new ListEditWindow(
				BMessenger(this));
			window->SetExistingList(listId, list.name.String());
			window->Show();
			return;
		}
	}
}


void
MainWindow::_OnDeleteList(int64 listId)
{
	BAlert* alert = new BAlert("Delete List",
		"Delete this list and all its tasks?",
		"Cancel", "Delete", NULL, B_WIDTH_AS_USUAL, B_WARNING_ALERT);
	if (alert->Go() == 1) {
		fApiClient.DeleteList(listId, BMessenger(this));
	}
}


void
MainWindow::_OnSetDefaultList(int64 listId)
{
	fSettings.SetDefaultListId(listId);
	fSettings.Save();
	_RefreshSidebar();
}


void
MainWindow::_OnListSelected(int64 listId)
{
	if (listId == fSelectedListId)
		return;

	fSelectedListId = listId;
	fItems.clear();
	_RefreshTaskList();
	_LoadItemsForSelectedList();
}


void
MainWindow::_OnShowSettings()
{
	SettingsWindow* window = new SettingsWindow(BMessenger(this));
	window->SetServerUrl(fSettings.ServerUrl().String());
	window->Show();
}


void
MainWindow::_UpdateStatusBar(const char* text)
{
	if (fStatusBar != NULL)
		fStatusBar->SetText(text);
}


Category*
MainWindow::_FindCategory(int64 categoryId)
{
	for (Category& cat : fCategories) {
		if (cat.categoryId == categoryId)
			return &cat;
	}
	return NULL;
}


BString
MainWindow::_CategoryNameForItem(const TodoItem& item)
{
	Category* cat = _FindCategory(item.categoryId);
	if (cat != NULL)
		return cat->name;
	return BString("");
}
