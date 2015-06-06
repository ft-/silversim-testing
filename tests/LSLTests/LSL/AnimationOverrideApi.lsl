default
{
	state_entry()
	{
		string s;
		s = llGetAnimationOverride("Crouching");
		llResetAnimationOverride("Crouching");
		llSetAnimationOverride("Crouching", "crouch2");
	}
}
