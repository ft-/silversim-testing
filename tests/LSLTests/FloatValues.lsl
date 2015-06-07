void test(string a, float b, float c)
{
}

default
{
	state_entry()
	{
		test("1.4e-45 == (float)\"1.4e-45\"", 1.4e-45, (float)"1.4e-45");
 	}
}