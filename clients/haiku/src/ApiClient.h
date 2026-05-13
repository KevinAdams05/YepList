/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef API_CLIENT_H
#define API_CLIENT_H


#include <Messenger.h>
#include <String.h>

#include <vector>

#include "Models.h"


// Message 'what' codes for async results posted back to caller
enum {
	kMsgGetListsDone			= 'gLdn',
	kMsgCreateListDone			= 'cLdn',
	kMsgUpdateListDone			= 'uLdn',
	kMsgDeleteListDone			= 'dLdn',

	kMsgGetCategoriesDone		= 'gCdn',
	kMsgCreateCategoryDone		= 'cCdn',
	kMsgUpdateCategoryDone		= 'uCdn',
	kMsgDeleteCategoryDone		= 'dCdn',

	kMsgGetItemsDone			= 'gIdn',
	kMsgCreateItemDone			= 'cIdn',
	kMsgUpdateItemDone			= 'uIdn',
	kMsgDeleteItemDone			= 'dIdn',
	kMsgToggleCompleteDone		= 'tCdn',
	kMsgReorderItemsDone		= 'rIdn',

	kMsgSyncDone				= 'sydn',

	kMsgApiError				= 'aerr'
};


class ApiClient {
public:
								ApiClient();

			void				SetServerUrl(const BString& url);
			const BString&		ServerUrl() const { return fServerUrl; }

	// Lists — async, results posted to target
			void				GetLists(BMessenger target);
			void				CreateList(const char* name,
									int32 sortOrder, BMessenger target);
			void				UpdateList(int64 listId,
									const char* name, int32 sortOrder,
									BMessenger target);
			void				DeleteList(int64 listId,
									BMessenger target);

	// Categories — async
			void				GetCategories(BMessenger target);
			void				CreateCategory(const char* name,
									const char* color,
									BMessenger target);
			void				UpdateCategory(int64 categoryId,
									const char* name, const char* color,
									BMessenger target);
			void				DeleteCategory(int64 categoryId,
									BMessenger target);

	// Items — async
			void				GetItemsByList(int64 listId,
									BMessenger target);
			void				CreateItem(int64 listId,
									const char* title,
									const char* notes,
									int64 categoryId,
									const char* dueDate,
									int32 sortOrder,
									BMessenger target);
			void				UpdateItem(int64 itemId,
									const char* title,
									const char* notes,
									int64 categoryId,
									int64 listId,
									bool isCompleted,
									const char* dueDate,
									int32 sortOrder,
									BMessenger target);
			void				DeleteItem(int64 itemId,
									BMessenger target);
			void				ToggleComplete(int64 itemId,
									bool isCompleted,
									BMessenger target);
			void				ReorderItems(int64 listId,
									const std::vector<std::pair<int64, int32>>&
										items,
									BMessenger target);

	// Sync — async
			void				Sync(const char* since,
									BMessenger target);

private:
	// Internal struct for passing data to worker threads
	struct RequestData {
		BString			url;
		BString			method;
		BString			body;
		uint32			resultWhat;
		BMessenger		target;
	};

	static int32			_RequestThreadEntry(void* data);
	static void				_DoRequest(RequestData* data);

			BString			_BuildUrl(const char* path) const;

			BString			fServerUrl;
};


#endif	// API_CLIENT_H
