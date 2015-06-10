default
{
	state_entry()
	{
		integer llAxes2RotPASS;
		
		//initialize a pass variable to TRUE 
		llAxes2RotPASS = 0;

		//test four sets of vector configurations to hard-coded values
		if( (string)<1.00000, 0.00000, 0.00000, 0.00000> == (string)llAxes2Rot( <0.0, 0.0, 0.0>, <0.0, 0.0, 0.0>, <0.0, 0.0, 0.0>) &
			(string)<0.00000, -0.35355, 0.35355, 0.70711> == (string)llAxes2Rot( <1.0, 1.0, 1.0>, <0.0, 0.0, 0.0>, <0.0, 0.0, 0.0>) &
			(string)<0.35355, 0.00000, -0.35355, 0.70711> == (string)llAxes2Rot( <0.0, 0.0, 0.0>, <1.0, 1.0, 1.0>, <0.0, 0.0, 0.0>) &
			(string)<-0.35355, 0.35355, 0.00000, 0.70711> == (string)llAxes2Rot( <0.0, 0.0, 0.0>, <0.0, 0.0, 0.0>, <1.0, 1.0, 1.0>) )
		{
			llAxes2RotPASS = 1; 
		}
	}
}