/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef TASK_LIST_VIEW_H
#define TASK_LIST_VIEW_H


#include <View.h>

#include <vector>

#include "Models.h"


class BButton;
class BListView;
class BStringView;
class BTextControl;


class TaskListView : public BView {
public:
								TaskListView();

	virtual void				AttachedToWindow();
	virtual void				MessageReceived(BMessage* message);

			void				UpdateItems(
									const std::vector<TodoItem>& items,
									const std::vector<Category>&
										categories);
			void				SetHeaderText(const char* text);
			int32				SelectedIndex() const;
			int64				SelectedItemId() const;

private:
			BStringView*		fHeaderView;
			BButton*			fAddTaskButton;
			BListView*			fListView;
			BTextControl*		fQuickAddField;
};


#endif	// TASK_LIST_VIEW_H
