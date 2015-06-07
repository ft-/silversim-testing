default
{
	state_entry()
	{
		string reportString;
		string llAcosPASSstring;
		string llAsinPASSstring;
		string llAtan2PASSstring;
		string llCosPASSstring;
		string llSinPASSstring;
		string llTanPASSstring;
		
             reportString = "///////////////////////////////////////////////////////////////////////////////////////////" + "\n"+
             	"// Function: float llAcos( float val );" + "\n" +
				"// Returns a float that is the arccosine in radians of val" + "\n" +
				"// • float     val     –     val must fall in the range [-1.0, 1.0]. (-1.0 <= val <= 1.0)     " + "\n" +
				"///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAcosPASSstring + "\n\n" +
 
				"///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llAsin( float val );" + "\n" +
				"// Returns a float that is the arcsine in radians of val" + "\n" +
				"// • float     val     –     must fall in the range [-1.0, 1.0]. (-1.0 <= val <= 1.0)     " + "\n" +
				"////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAsinPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llAtan2( float y, float x );" + "\n" +
				"// Returns a float that is the arctangent2 of y, x." + "\n" +
				"// • float     y             " + "\n" +
				"// • float     x             " + "\n" +
				"// Similar to the arctangent(y/x) except it utilizes the signs of x & y to " + "\n" +
				"// determine the quadrant. Returns zero if x and y are zero. " + "\n" +
				"////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAtan2PASSstring + "\n\n" +
 
				"//////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llCos( float theta );" + "\n" +
				"// Returns a float that is the cosine of theta." + "\n" +
				"// • float     theta     –     angle expressed in radians.     " + "\n" +
				"/////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llCosPASSstring + "\n\n" +
 
 
				"///////////////////////////////////////////////////////////////////////////////////" + "\n" +    
				"// Function: float llSin( float theta );" + "\n" +
				"// Returns a float that is the sine of theta." + "\n" +
				"// • float     theta     –     angle expressed in radians.     " + "\n" +
				"//////////////////////////////////////////////////////////////////////////////////" + "\n" +   
				"PASS/FAIL -> " + llSinPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llTan( float theta );" + "\n" +
				"// Returns a float that is the tangent of theta." + "\n" +
				"// • float     theta     –     angle expressed in radians." + "\n" +
				"/////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llTanPASSstring + "\n\n";
	}
}