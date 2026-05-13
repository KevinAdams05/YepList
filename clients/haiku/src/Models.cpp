/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "Models.h"

#include <Message.h>

#include "JsonHelper.h"


TodoList
TodoList::FromJson(const BMessage& json)
{
	TodoList list;
	list.listId = JsonHelper::GetInt64(json, "listId");
	list.name = JsonHelper::GetString(json, "name");
	list.sortOrder = JsonHelper::GetInt32(json, "sortOrder");
	list.createdDate = JsonHelper::GetString(json, "createdDate");
	list.modifiedDate = JsonHelper::GetString(json, "modifiedDate");
	return list;
}


Category
Category::FromJson(const BMessage& json)
{
	Category category;
	category.categoryId = JsonHelper::GetInt64(json, "categoryId");
	category.name = JsonHelper::GetString(json, "name");
	category.color = JsonHelper::GetString(json, "color");
	category.createdDate = JsonHelper::GetString(json, "createdDate");
	category.modifiedDate = JsonHelper::GetString(json, "modifiedDate");
	return category;
}


TodoItem
TodoItem::FromJson(const BMessage& json)
{
	TodoItem item;
	item.itemId = JsonHelper::GetInt64(json, "itemId");
	item.listId = JsonHelper::GetInt64(json, "listId");
	item.categoryId = JsonHelper::GetInt64(json, "categoryId");
	item.title = JsonHelper::GetString(json, "title");
	item.notes = JsonHelper::GetString(json, "notes");
	item.isCompleted = JsonHelper::GetBool(json, "isCompleted");
	item.dueDate = JsonHelper::GetString(json, "dueDate");
	item.sortOrder = JsonHelper::GetInt32(json, "sortOrder");
	item.createdDate = JsonHelper::GetString(json, "createdDate");
	item.modifiedDate = JsonHelper::GetString(json, "modifiedDate");
	return item;
}


SyncResponse
SyncResponse::FromJson(const BMessage& json)
{
	SyncResponse response;
	response.serverTime = JsonHelper::GetString(json, "serverTime");

	std::vector<BMessage> listMsgs
		= JsonHelper::GetMessageArray(json, "lists");
	for (const BMessage& msg : listMsgs)
		response.lists.push_back(TodoList::FromJson(msg));

	std::vector<BMessage> catMsgs
		= JsonHelper::GetMessageArray(json, "categories");
	for (const BMessage& msg : catMsgs)
		response.categories.push_back(Category::FromJson(msg));

	std::vector<BMessage> itemMsgs
		= JsonHelper::GetMessageArray(json, "items");
	for (const BMessage& msg : itemMsgs)
		response.items.push_back(TodoItem::FromJson(msg));

	response.deletedListIds
		= JsonHelper::GetInt64Array(json, "deletedListIds");
	response.deletedCategoryIds
		= JsonHelper::GetInt64Array(json, "deletedCategoryIds");
	response.deletedItemIds
		= JsonHelper::GetInt64Array(json, "deletedItemIds");

	return response;
}
