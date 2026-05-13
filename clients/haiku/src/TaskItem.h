/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef TASK_ITEM_H
#define TASK_ITEM_H


#include <ListItem.h>
#include <String.h>


class TaskItem : public BListItem {
public:
								TaskItem(int64 itemId,
									const char* title,
									bool isCompleted,
									const char* categoryName,
									const char* categoryColor,
									const char* dueDate);

	virtual void				DrawItem(BView* owner, BRect frame,
									bool complete);
	virtual void				Update(BView* owner, const BFont* font);

			int64				ItemId() const { return fItemId; }
			const BString&		Title() const { return fTitle; }
			bool				IsCompleted() const { return fIsCompleted; }
			void				SetCompleted(bool completed);
			const BString&		CategoryName() const { return fCategoryName; }
			const BString&		DueDate() const { return fDueDate; }

private:
			int64				fItemId;
			BString				fTitle;
			bool				fIsCompleted;
			BString				fCategoryName;
			BString				fCategoryColor;
			BString				fDueDate;
			float				fBaselineOffset;
			float				fStrikeOffset;
};


#endif	// TASK_ITEM_H
