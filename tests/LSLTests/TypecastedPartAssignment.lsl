default
{
	state_entry()
	{
		list unitParameters;
		string reportString;
	        reportString += "http_objectkey: " + llList2String( unitParameters, 2)+ " --- objectkey: " + (string)llGetKey() + "\n";
	}
}
