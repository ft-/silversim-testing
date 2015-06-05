default
{
	state_entry()
	{
		list a;
		list b = [1,2,3];
		list c = [4, 5, 6];
		a = (a = []) + b + c;
	}
}
