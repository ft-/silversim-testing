implicit()
{
	vector v;
	key k;
	string s;
	list l;
	rotation r;
	integer i;
	float f;
	quaternion q;
	
	f = i;
	k = s;
	s = k;
	l = v;
	l = k;
	l = s;
	l = r;
	l = i;
	l = f;
	l = q;
	
	v = v;
	k = k;
	s = s;
	l = l;
	r = r;
	i = i;
	f = f;
	q = q;
	
	q = r;
	r = q;
}

explicit()
{
	vector v;
	key k;
	string s;
	list l;
	rotation r;
	integer i;
	float f;
	/* yes, there is another type */
	quaternion q;
	
	i = (integer)f;
	i = (integer)s;
	i = (integer)i;
	
	f = (float)i;
	f = (float)f;
	f = (float)s;
	
	s = (string)i;
	s = (string)f;
	s = (string)s;
	s = (string)k;
	s = (string)l;
	s = (string)v;
	s = (string)r;
	s = (string)q;
	
	k = (key)s;
	k = (key)k;
	
	l = (list)i;
	l = (list)f;
	l = (list)s;
	l = (list)k;
	l = (list)l;
	l = (list)v;
	l = (list)r;
	l = (list)q;
	
	v = (vector)s;
	v = (vector)v;
	
	r = (rotation)s;
	r = (rotation)r;
	r = (rotation)q;
	
	/* yes, there is another type */
	q = (quaternion)s;
	q = (quaternion)r;
	q = (quaternion)r;
}

default
{
	state_entry()
	{
	}
}