default
{
	state_entry()
	{
		osAvatarPlayAnimation(NULL_KEY, "anim");
		osAvatarStopAnimation(NULL_KEY, "anim");
	}
}