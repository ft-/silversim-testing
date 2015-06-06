
void test(string a, string b, string c)
{
}

default
{
	state_entry()
	{
		test("((string) [1,2.5,<1,2,3>])", ((string) [1,2.5,<1,2,3>]), "12.500000<1.000000, 2.000000, 3.000000>");
	}
}
