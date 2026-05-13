/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "ListItem.h"

#include <Font.h>
#include <InterfaceDefs.h>
#include <View.h>


static const float kPadding = 6.0f;
static const float kStarWidth = 14.0f;


ListItem::ListItem(int64 listId, const char* name, bool isDefault)
	:
	BListItem(),
	fListId(listId),
	fName(name),
	fIsDefault(isDefault),
	fBaselineOffset(0.0f)
{
}


void
ListItem::DrawItem(BView* owner, BRect frame, bool complete)
{
	rgb_color background;
	if (IsSelected()) {
		background = ui_color(B_LIST_SELECTED_BACKGROUND_COLOR);
	} else {
		background = ui_color(B_LIST_BACKGROUND_COLOR);
	}

	owner->SetHighColor(background);
	owner->FillRect(frame);

	rgb_color textColor;
	if (IsSelected()) {
		textColor = ui_color(B_LIST_SELECTED_ITEM_TEXT_COLOR);
	} else {
		textColor = ui_color(B_LIST_ITEM_TEXT_COLOR);
	}

	float textX = frame.left + kPadding;

	// Draw star for default list
	if (fIsDefault) {
		owner->SetHighColor(ui_color(B_CONTROL_MARK_COLOR));
		owner->DrawString("\xe2\x98\x85",
			BPoint(textX, frame.top + fBaselineOffset));
		textX += kStarWidth;
	}

	// Draw list name
	owner->SetHighColor(textColor);
	owner->DrawString(fName.String(),
		BPoint(textX, frame.top + fBaselineOffset));
}


void
ListItem::Update(BView* owner, const BFont* font)
{
	BListItem::Update(owner, font);

	font_height fontHeight;
	font->GetHeight(&fontHeight);

	float itemHeight = ceilf(fontHeight.ascent + fontHeight.descent
		+ fontHeight.leading) + kPadding * 2.0f;
	SetHeight(itemHeight);
	fBaselineOffset = kPadding + fontHeight.ascent;
}


void
ListItem::SetDefault(bool isDefault)
{
	fIsDefault = isDefault;
}
