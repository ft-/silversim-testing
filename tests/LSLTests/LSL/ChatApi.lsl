default
{
	state_entry()
	{
		llWhisper(PUBLIC_CHANNEL, "Hello");
		llSay(DEBUG_CHANNEL, "Hello");
		llShout(PUBLIC_CHANNEL, "Hello");
		llRegionSay(DEBUG_CHANNEL, "Hello");
		llRegionSayTo(NULL_KEY, PUBLIC_CHANNEL, "Hello");
	}
}