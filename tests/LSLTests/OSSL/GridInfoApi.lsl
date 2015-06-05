default
{
	state_entry()
	{
		string s;
		s = osGetGridName();
		s = osGetGridNick();
		s = osGetGridLoginURI();
		s = osGetGridHomeURI();
		s = osGetGridGatekeeperURI();
		s = osGetGridCustom("welcome");
	}
}