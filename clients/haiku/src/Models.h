/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef MODELS_H
#define MODELS_H


#include <String.h>

#include <vector>


class BMessage;


struct TodoList {
	int64		listId;
	BString		name;
	int32		sortOrder;
	BString		createdDate;
	BString		modifiedDate;

	static TodoList		FromJson(const BMessage& json);
};


struct Category {
	int64		categoryId;
	BString		name;
	BString		color;
	BString		createdDate;
	BString		modifiedDate;

	static Category		FromJson(const BMessage& json);
};


struct TodoItem {
	int64		itemId;
	int64		listId;
	int64		categoryId;
	BString		title;
	BString		notes;
	bool		isCompleted;
	BString		dueDate;
	int32		sortOrder;
	BString		createdDate;
	BString		modifiedDate;

	static TodoItem		FromJson(const BMessage& json);
};


struct SyncResponse {
	BString						serverTime;
	std::vector<TodoList>		lists;
	std::vector<Category>		categories;
	std::vector<TodoItem>		items;
	std::vector<int64>			deletedListIds;
	std::vector<int64>			deletedCategoryIds;
	std::vector<int64>			deletedItemIds;

	static SyncResponse			FromJson(const BMessage& json);
};


#endif	// MODELS_H
