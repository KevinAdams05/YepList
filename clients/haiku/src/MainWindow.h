/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef MAIN_WINDOW_H
#define MAIN_WINDOW_H


#include <Window.h>

#include <vector>

#include "ApiClient.h"
#include "Models.h"
#include "Settings.h"


class BMenuBar;
class BMessageRunner;
class BSplitView;
class BStringView;

class ListSidebar;
class TaskListView;


// Message constants for MainWindow
enum {
	kMsgSync				= 'sync',
	kMsgNewTask				= 'ntsk',
	kMsgEditTask			= 'etsk',
	kMsgDeleteTask			= 'dtsk',
	kMsgToggleComplete		= 'tglc',
	kMsgQuickAdd			= 'qadd',
	kMsgNewList				= 'nlst',
	kMsgRenameList			= 'rlst',
	kMsgDeleteList			= 'dlst',
	kMsgSetDefaultList		= 'dflt',
	kMsgListSelected		= 'lsel',
	kMsgManageCategories	= 'mcat',
	kMsgShowSettings		= 'stng',

	// Dialog results
	kMsgTaskSaved			= 'tsvd',
	kMsgListSaved			= 'lsvd',
	kMsgCategoriesDone		= 'catd',
	kMsgSettingsSaved		= 'ssvd'
};


class MainWindow : public BWindow {
public:
								MainWindow();
	virtual						~MainWindow();

	virtual void				MessageReceived(BMessage* message);
	virtual bool				QuitRequested();

			ApiClient*			GetApiClient() { return &fApiClient; }
			Settings*			GetSettings() { return &fSettings; }

			const std::vector<Category>&
								Categories() const { return fCategories; }

private:
			void				_BuildMenu();
			void				_BuildLayout();
			void				_StartSyncTimer();

	// Data operations
			void				_DoFullRefresh();
			void				_DoSync();
			void				_RefreshSidebar();
			void				_RefreshTaskList();
			void				_LoadItemsForSelectedList();

	// Handle API responses
			void				_HandleGetListsDone(BMessage* message);
			void				_HandleGetCategoriesDone(BMessage* message);
			void				_HandleGetItemsDone(BMessage* message);
			void				_HandleSyncDone(BMessage* message);
			void				_HandleCreateListDone(BMessage* message);
			void				_HandleDeleteListDone(BMessage* message);
			void				_HandleCreateItemDone(BMessage* message);
			void				_HandleDeleteItemDone(BMessage* message);
			void				_HandleToggleDone(BMessage* message);

	// User actions
			void				_OnNewTask();
			void				_OnEditTask();
			void				_OnDeleteTask();
			void				_OnQuickAdd(const char* title);
			void				_OnNewList();
			void				_OnRenameList(int64 listId);
			void				_OnDeleteList(int64 listId);
			void				_OnSetDefaultList(int64 listId);
			void				_OnListSelected(int64 listId);
			void				_OnShowSettings();

			void				_UpdateStatusBar(const char* text);

	// Find helpers
			Category*			_FindCategory(int64 categoryId);
			BString				_CategoryNameForItem(
									const TodoItem& item);

	// UI components
			BMenuBar*			fMenuBar;
			BSplitView*			fSplitView;
			ListSidebar*		fListSidebar;
			TaskListView*		fTaskListView;
			BStringView*		fStatusBar;
			BMessageRunner*		fSyncRunner;

	// State
			ApiClient			fApiClient;
			Settings			fSettings;
			std::vector<TodoList>	fLists;
			std::vector<Category>	fCategories;
			std::vector<TodoItem>	fItems;
			int64				fSelectedListId;
			BString				fLastSyncTime;
			bool				fInitialLoadDone;
};


#endif	// MAIN_WINDOW_H
