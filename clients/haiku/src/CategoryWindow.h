/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef CATEGORY_WINDOW_H
#define CATEGORY_WINDOW_H


#include <Messenger.h>
#include <Window.h>

#include <vector>

#include "Models.h"


class BButton;
class BListView;


class CategoryWindow : public BWindow {
public:
								CategoryWindow(BMessenger target);

	virtual void				MessageReceived(BMessage* message);
	virtual bool				QuitRequested();

			void				SetCategories(
									const std::vector<Category>&
										categories);

private:
			void				_RefreshList();
			void				_OnAdd();
			void				_OnEdit();
			void				_OnDelete();

			BMessenger			fTarget;
			BListView*			fListView;
			BButton*			fAddButton;
			BButton*			fEditButton;
			BButton*			fDeleteButton;

			std::vector<Category>	fCategories;
};


#endif	// CATEGORY_WINDOW_H
