/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef TASK_EDIT_WINDOW_H
#define TASK_EDIT_WINDOW_H


#include <Messenger.h>
#include <Window.h>

#include <vector>

#include "Models.h"


class BButton;
class BMenuField;
class BPopUpMenu;
class BTextControl;
class BTextView;


class TaskEditWindow : public BWindow {
public:
								TaskEditWindow(BMessenger target);

	virtual void				MessageReceived(BMessage* message);

			void				SetListId(int64 listId);
			void				SetCategories(
									const std::vector<Category>&
										categories);
			void				SetItem(const TodoItem& item);

private:
			void				_SaveTask();

			BMessenger			fTarget;
			int64				fListId;
			int64				fItemId;		// -1 for new task
			int32				fSortOrder;
			bool				fIsCompleted;

			BTextControl*		fTitleField;
			BTextView*			fNotesView;
			BMenuField*			fCategoryField;
			BPopUpMenu*			fCategoryMenu;
			BTextControl*		fDueDateField;
			BButton*			fSaveButton;
			BButton*			fCancelButton;

			std::vector<Category>	fCategories;
};


#endif	// TASK_EDIT_WINDOW_H
