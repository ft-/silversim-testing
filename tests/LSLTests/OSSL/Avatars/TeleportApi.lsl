default
{
	state_entry()
	{
		osTeleportAgent(NULL_KEY, 10, 10, <1,1,1>,<1,1,1>);
		osTeleportAgent(NULL_KEY, "Destination", <1,1,1>,<1,1,1>);
		osTeleportAgent(NULL_KEY, <1,1,1>,<1,1,1>);
		osTeleportOwner(10, 10, <1,1,1>,<1,1,1>);
		osTeleportOwner("Destination", <1,1,1>,<1,1,1>);
		osTeleportOwner(<1,1,1>,<1,1,1>);
	}
}