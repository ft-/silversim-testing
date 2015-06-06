default
{
	state_entry()
	{
		osAgentSaveAppearance(NULL_KEY, "appearance");
		osOwnerSaveAppearance("appearance");
	}
}