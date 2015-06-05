default
{
	state_entry()
	{
		osListenRegex(10, "Hello", NULL_KEY, "Hello", OS_LISTEN_REGEX_NAME);
	}
}
