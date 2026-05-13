/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef CATEGORY_EDIT_WINDOW_H
#define CATEGORY_EDIT_WINDOW_H


#include <Messenger.h>
#include <Window.h>


class BButton;
class BTextControl;


class CategoryEditWindow : public BWindow {
public:
								CategoryEditWindow(BMessenger target);

	virtual void				MessageReceived(BMessage* message);

			void				SetExistingCategory(int64 categoryId,
									const char* name,
									const char* color);

private:
			void				_SaveCategory();

			BMessenger			fTarget;
			int64				fCategoryId;	// -1 for new

			BTextControl*		fNameField;
			BTextControl*		fColorField;
			BButton*			fOkButton;
			BButton*			fCancelButton;
};


#endif	// CATEGORY_EDIT_WINDOW_H
