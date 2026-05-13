/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef LIST_SIDEBAR_H
#define LIST_SIDEBAR_H


#include <View.h>

#include <vector>

#include "Models.h"


class BBitmap;
class BButton;
class BListView;


class ListSidebar : public BView {
public:
								ListSidebar();
	virtual						~ListSidebar();

	virtual void				AttachedToWindow();
	virtual void				MessageReceived(BMessage* message);

			void				UpdateLists(
									const std::vector<TodoList>& lists,
									int64 defaultListId);
			void				SelectList(int64 listId);

private:
			void				_ShowContextMenu(BPoint where);
			BView*				_BuildHeader();
			BBitmap*			_LoadLogo();

			BListView*			fListView;
			BButton*			fNewListButton;
			BBitmap*			fLogoBitmap;
};


#endif	// LIST_SIDEBAR_H
