default
{
	state_entry()
	{
		list a;
		list b;
		integer i;
		
		i = [1,2] == [3, 4];
		i = [1,2] != [3, 4];
		i = a == [3, 4];
		i = a != [3,4];
		i = [1,2] == b;
		i = [1,2] != b;
		/* tests for not using constants */
		i = a == b;
		i = a != b;
	}
}