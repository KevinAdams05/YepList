/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef LIST_EDIT_WINDOW_H
#define LIST_EDIT_WINDOW_H


#include <Messenger.h>
#include <Window.h>


class BButton;
class BTextControl;


class ListEditWindow : public BWindow {
public:
								ListEditWindow(BMessenger target);

	virtual void				MessageReceived(BMessage* message);

			void				SetExistingList(int64 listId,
									const char* name);

private:
			void				_SaveList();

			BMessenger			fTarget;
			int64				fListId;		// -1 for new list

			BTextControl*		fNameField;
			BButton*			fOkButton;
			BButton*			fCancelButton;
};


#endif	// LIST_EDIT_WINDOW_H
