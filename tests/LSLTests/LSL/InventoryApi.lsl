default
{
	state_entry()
	{
		llAllowInventoryDrop(TRUE);
		key k;
		k = llGetInventoryCreator("inventory");
		k = llGetInventoryKey("inventory");
		string s;
		s = llGetInventoryName(INVENTORY_NONE, 0);
		s = llGetInventoryName(INVENTORY_ALL, 0);
		s = llGetInventoryName(INVENTORY_TEXTURE, 0);
		s = llGetInventoryName(INVENTORY_SOUND, 0);
		s = llGetInventoryName(INVENTORY_LANDMARK, 0);
		s = llGetInventoryName(INVENTORY_CLOTHING, 0);
		s = llGetInventoryName(INVENTORY_OBJECT, 0);
		s = llGetInventoryName(INVENTORY_NOTECARD, 0);
		s = llGetInventoryName(INVENTORY_SCRIPT, 0);
		s = llGetInventoryName(INVENTORY_BODYPART, 0);
		s = llGetInventoryName(INVENTORY_ANIMATION, 0);
		s = llGetInventoryName(INVENTORY_GESTURE, 0);
		integer i;
		i = llGetInventoryNumber(INVENTORY_ALL);
		i = llGetInventoryPermMask("item", MASK_BASE);
		i = llGetInventoryPermMask("item", MASK_OWNER);
		i = llGetInventoryPermMask("item", MASK_GROUP);
		i = llGetInventoryPermMask("item", MASK_EVERYONE);
		i = llGetInventoryPermMask("item", MASK_NEXT);
		i = PERM_ALL | PERM_COPY | PERM_MODIFY | PERM_MOVE | PERM_TRANSFER;
		i = llGetInventoryType("item");
		s = llGetScriptName();
		i = llGetScriptState("item");
		llGiveInventory(NULL_KEY, "item");
		llGiveInventoryList(NULL_KEY, "MyFolder", ["item"]);
		llRemoteLoadScriptPin(NULL_KEY, "item", 1234, TRUE, 1234);
		llRemoveInventory("item");
		llRequestInventoryData("item");
		llResetOtherScript("item");
		llRezAtRoot("item", ZERO_VECTOR, ZERO_VECTOR, ZERO_ROTATION, 1234);
		llRezObject("item", ZERO_VECTOR, ZERO_VECTOR, ZERO_ROTATION, 1234);
		llSetInventoryPermMask("item", MASK_OWNER, PERM_MODIFY);
		llSetScriptState("item", FALSE);
	}
}