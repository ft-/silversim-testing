default
{
	state_entry()
	{
		string s;
		s = osGetScriptEngineName();
		s = osGetSimulatorVersion();
		integer i;
		i = osGetSimulatorMemory();
		key k;
		k = osGetMapTexture();
		k = osGetRegionMapTexture("Region");
		vector v;
		v = osGetRegionSize();
		list l;
		l = osGetRegionStats();
		s = osLoadedCreationDate();
		s = osLoadedCreationTime();

		k = osLoadedCreationID();
		s = osGetPhysicsEngineType();
	}
}