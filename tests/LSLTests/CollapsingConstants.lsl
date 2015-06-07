default
{
	state_entry()
	{
		string s;
		s = ((string)<1,2,3>) + ((string)<1,2,3,4>) + ((string)5) + ((string)5.) + "string";
	}
}
