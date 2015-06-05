default
{
	state_entry()
	{
		osMakeNotecard("notecard", "notecard");
		osMakeNotecard("notecard", ["notecard"]);
		string nc;
		nc = osGetNotecard("notecard");
		nc = osGetNotecardLine("notecard", 0);
		integer i;
		i = osGetNumberOfNotecardLines("notecard");
	}
}