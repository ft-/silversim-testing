default
{
	state_entry()
	{
		string s;
		s = llGetAnimation(NULL_KEY);
		list l;
		l = llGetAnimationList(NULL_KEY);
		llStartAnimation("anim");
		llStopAnimation("anim");
	}
}
