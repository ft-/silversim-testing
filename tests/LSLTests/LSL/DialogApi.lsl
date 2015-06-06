default
{
	state_entry()
	{
		llLoadURL(NULL_KEY, "Hello", "http://example.com/");
		llMapDestination("Destination", <128, 128, 23>, <1,1,1>);
		llSetPayPrice(10, [20, 30, 40, 50]);
		llTextBox(NULL_KEY, "Hello", 10);
		llDialog(NULL_KEY, "Hello", ["A", "B"], 10);
	}
}