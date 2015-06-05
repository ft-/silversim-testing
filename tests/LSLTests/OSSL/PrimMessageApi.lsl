default
{
	state_entry()
	{
		osMessageObject(NULL_KEY, "Hello");
		osMessageAttachments(NULL_KEY, "Hello", [ATTACH_HEAD], OS_ATTACH_MSG_OBJECT_CREATOR);
	}
}