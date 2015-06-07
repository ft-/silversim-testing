default
{
	state_entry()
	{
		vector v;
		rotation r;
		
		v.x = 1;
		v.y += 2;
		v.z -= 3;
		r.x *= 1;
		r.y /= 2;
		r.z %= 3;
		r.s = 4;
	}
}