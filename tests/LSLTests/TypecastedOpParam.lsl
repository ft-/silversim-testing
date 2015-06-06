 
integer floatEqual(float actual, float expected)
{
    float error = llFabs(expected - actual);
    float epsilon = 0.001;
    if(error > epsilon)
    {
        llSay(0,"Float equality delta " + (string)error);
        return FALSE;
    }
    return TRUE;
}

default
{
	state_entry()
	{
	}
}