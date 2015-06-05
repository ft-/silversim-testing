default
{
	state_entry()
	{
		list l;
		l = osGetPrimitiveParams(NULL_KEY, [PRIM_NAME]);
		l = osGetLinkPrimitiveParams(10, [PRIM_NAME]);
		osSetPrimitiveParams(NULL_KEY, [PRIM_NAME, "Hello"]);
		osSetProjectionParams(TRUE, TEXTURE_BLANK, 1f, 1f, 1f);
		osSetProjectionParams(NULL_KEY, TRUE, TEXTURE_BLANK, 1f, 1f, 1f);
		osForceCreateLink(NULL_KEY, 1);
		osForceBreakLink(1);
		osForceBreakAllLinks();
		osSetSpeed(NULL_KEY, 3f);
		osMessageObject(NULL_KEY, "Hello");
		string s;
		s = osGetInventoryDesc("Hello");
		key k;
		k = osGetRezzingObject();
		integer i;
		i = osIsUUID(NULL_KEY);
		
	}
}