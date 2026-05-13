/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef LIST_ITEM_H
#define LIST_ITEM_H


#include <ListItem.h>
#include <String.h>


class ListItem : public BListItem {
public:
								ListItem(int64 listId,
									const char* name,
									bool isDefault);

	virtual void				DrawItem(BView* owner, BRect frame,
									bool complete);
	virtual void				Update(BView* owner, const BFont* font);

			int64				ListId() const { return fListId; }
			const BString&		Name() const { return fName; }
			bool				IsDefault() const { return fIsDefault; }
			void				SetDefault(bool isDefault);

private:
			int64				fListId;
			BString				fName;
			bool				fIsDefault;
			float				fBaselineOffset;
};


#endif	// LIST_ITEM_H
