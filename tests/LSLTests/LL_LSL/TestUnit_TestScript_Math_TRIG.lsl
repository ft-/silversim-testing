///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////            Math_TRIG
///////
///////  This is the test script for the trigonometry math functions.  
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//Math_TRIG              .1 -> modified from TestUnit_TestScript base to test trig math functions  7.3.2007
 
 
//////////////////////////////////////////////////////////////////////////////////////
//
//                  Command Protocol
//
//////////////////////////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////
//        CHAT commands
//////////////////////////////////////////////
//
//  Chat commands will be on the specified broadcastChannel
//
//////// OUTPUT ///////////
//
// AddUnitReport - send Report update to Coordinator on the chat broadcastChannel
// format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
//
//////////////////////////////////////////////
//        LINK MESSAGE commands
//////////////////////////////////////////////
//
//  link message commands will be sent out on the toAllChannel, and recieved on the passFailChannel
//
//////// INPUT ///////////
//
//  RunTest - activation command to start test
//  format example -> RunTest
//
//  Report - channel and report type
//  format example -> Report::controlChannel::0::reportType::NORMAL
//
//  Reset - rest the scripts 
//  format example -> Reset
//
//////// OUTPUT ///////////
//
//  passFail - status of test sent on passFailChannel
//  format example -> PASS
//
//////////////////////////////////////////////////////////////////////////////////////////
 
 
// Global Variables
 
integer toAllChannel = -255;           // general channel - linked message
integer passFailChannel = -355;        // test scripts channel for cummunicating pass/fail - linked message
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
 
integer llAcosPASS;                    // These are global pass/fail
integer llAsinPASS;                    // indicators for the various
integer llAtan2PASS;                   // Math trig functions that are 
integer llCosPASS;                     // being tested. These variables 
integer llSinPASS;                     // are used in the Run Test and
integer llTanPASS;                     // Report Functions of this script. 
 
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ParseCommand
//////////
//////////      Input:      string message - command to be parsed
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function calls various other functions or sets globals
//////////                    depending on message string. Allows external command calls.
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
ParseCommand(string message)
{
    if(debug > 1)llSay(debugChannel, llGetScriptName()+ "->ParseCommand: " + message);
 
    //reset all scripts 
    if(message == "Reset")
    {
        //reset this script 
        llResetScript();                   
    }
 
    //RunTest()
    else if(message == "RunTest")
    {
        RunTest();
    }
 
    //Report()
    //Example format -> Report::broadcastChannel::0::reportType::NORMAL
    else if( llSubStringIndex(message, "Report") != -1 )
    {
        //parse the string command into a list
        list reportParameters = llParseString2List( message, ["::"], [""] );
 
        //find the broadcastChannel label and increment by one
        integer tempIndex = llListFindList( reportParameters, ["controlChannel"] ) + 1;
        //pull the broadcastChannel from the list with the index just calculated
        integer controlChannel = llList2Integer( reportParameters , tempIndex);
 
        //find the reportType label and increment by one
        tempIndex = llListFindList( reportParameters, ["reportType"] ) + 1;
        //pull the reportType from the list with the index just calculated
        string reportType = llList2String( reportParameters , tempIndex);
 
        //call the Report function with new parameters
        Report( controlChannel, reportType );
    }
 
 
 
} //end ParseCommand
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   RunTest
//////////
//////////      Input:      no input parameters
//////////                    
//////////      Output:     link message on passFailChannel test status
//////////                    
//////////      Purpose:    This function is where you put the scripts that you want to test
//////////                  with this unit.
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
RunTest()
{
 
     ///////////////////////////////////////////////////////////////////////////////////////////
     // Function: float llAcos( float val );
     // Returns a float that is the arccosine in radians of val
     // • float     val     –     val must fall in the range [-1.0, 1.0]. (-1.0 <= val <= 1.0)     
     ///////////////////////////////////////////////////////////////////////////////////////////
 
     //initialize a pass variable to TRUE 
     llAcosPASS = 0;
 
     //compare known cosine of some angles
     if( (string)3.141593 == (string)llAcos( -1.0 ) &
     		(string)1.570796 == (string)llAcos( 0.0 ) &
     		(string)0.0 == (string)llAcos( 1.0 ) )
     {
         llAcosPASS = 1;    
     }
 
     ///////////////////////////////////////////////////////////////////////////////////////
     // Function: float llAsin( float val );
     // Returns a float that is the arcsine in radians of val
     // • float     val     –     must fall in the range [-1.0, 1.0]. (-1.0 <= val <= 1.0)     
     ////////////////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llAsinPASS = 0;
 
     //test four sets of vector configurations to hard-coded values
     if( (string)-1.570796 == (string)llAsin( -1.0) &
     		(string)0.0 == (string)llAsin( 0.0 ) &
     		(string)1.570796 == (string)llAsin( 1.0 ) )
     {
         llAsinPASS = 1; 
     }
 
 
     /////////////////////////////////////////////////////////////////////////
     // Function: float llAtan2( float y, float x );
     // Returns a float that is the arctangent2 of y, x.
     // • float     y             
     // • float     x             
     // Similar to the arctangent(y/x) except it utilizes the signs of x & y to 
     // determine the quadrant. Returns zero if x and y are zero. 
     ////////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llAtan2PASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)0.0 == (string)llAtan2( 0.0, 0.0) &
     		(string)0.0 ==  (string)llAtan2( 0.0, 1.0 ) &
     		(string)0.785398 == (string)llAtan2( 1.0, 1.0 ) )
     {
         llAtan2PASS = 1; 
     }
 
     //////////////////////////////////////////////////////////////////////////////
     // Function: float llCos( float theta );
     // Returns a float that is the cosine of theta.
     // • float     theta     –     angle expressed in radians.     
     /////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llCosPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)1.0 == (string)llCos( 0.0 ) &
     		(string)0.540302 ==  (string)llCos( 1.0 ) &
     		(string)0.540302 == (string)llCos( -1.0 ) )
     {
         llCosPASS = 1; 
     }
 
       ///////////////////////////////////////////////////////////////////////////////////    
       // Function: float llSin( float theta );
       // Returns a float that is the sine of theta.
       // • float     theta     –     angle expressed in radians.     
       //////////////////////////////////////////////////////////////////////////////////   
 
    //initialize a pass variable to TRUE 
    llSinPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)0.0 == (string)llSin( 0.0 ) &
     		(string)0.841471 ==  (string)llSin( 1.0 ) &
     		(string)-0.841471 == (string)llSin( -1.0 ) )
     {
         llSinPASS = 1; 
     }   
 
 
     /////////////////////////////////////////////////////////////////////////////////
     // Function: float llTan( float theta );
     // Returns a float that is the tangent of theta.
     // • float     theta     –     angle expressed in radians.
     /////////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llTanPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)0.0 == (string)llTan( 0.0 ) &
     		(string)1.557408 ==  (string)llTan( 1.0 ) &
     		(string)-1.557408 == (string)llTan( -1.0 ) )
     {
         llTanPASS = 1; 
     }
 
     //check to see if any failures occurred. 
     integer pass = llAcosPASS &
                    llAsinPASS &
                    llAtan2PASS &
                    llCosPASS &
                    llSinPASS &
                    llTanPASS;
 
     // if all of the individual 
     if( pass )
     {
       llMessageLinked(LINK_SET, passFailChannel, "PASS", NULL_KEY);
     }
     else
     {
       llMessageLinked(LINK_SET, passFailChannel, "FAIL", NULL_KEY);
     }
}
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   Report
//////////
//////////      Input:      broadcastChannel - chat channel to send report
//////////                  reportType - determines length and content of report type
//////////                                         -> NORMAL - failures and summary information
//////////                                         -> QUITE - summary information only
//////////                                         -> VERBOSE - everything
//////////                    
//////////      Output:     llSay on broadcastChannel 
//////////                    
//////////      Purpose:    This function is where you design the three level of reports
//////////                  available upon request by the Coordinator
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
Report( integer controlChannel, string reportType )
{
    //this string will be sent out regardless of reporting mode
    string reportString;
 
    // PASS or FAIL wording for the report
    string llAcosPASSstring = "FAIL";
    string llAsinPASSstring = "FAIL";
    string llAtan2PASSstring = "FAIL";
    string llCosPASSstring = "FAIL";
    string llSinPASSstring = "FAIL";
    string llTanPASSstring = "FAIL";
 
 
 
    //translate integer conditional into text string for the report. 
    if ( llAcosPASS )
    {
          llAcosPASSstring = "PASS";
    }
    if ( llAsinPASS )
    {
          llAsinPASSstring = "PASS";
    }
    if ( llAtan2PASS )
    {
          llAtan2PASSstring = "PASS";
    }
    if ( llCosPASS  )
    {
          llCosPASSstring = "PASS";
    }
    if ( llSinPASS )
    {
          llSinPASSstring = "PASS";
    }
    if ( llTanPASS )
    {
          llTanPASSstring = "PASS";
    }
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
      reportString = "Function: float llAcos( float val ) -> " 
                                                + llAcosPASSstring + "\n"
                   + "Function: float llAsin( float val ) ->" 
                                                + llAsinPASSstring + "\n"
                   + "Function: float llAtan2( float y, float x ) -> " 
                                                + llAtan2PASSstring + "\n"
                   + "Function: float llCos( float theta ) -> " 
                                                + llCosPASSstring + "\n"
                   + "Function: float llSin( float theta ) -> " 
                                                + llSinPASSstring + "\n"
                   + "Function: float llTan( float theta ) -> "
                                                + llTanPASSstring + "\n";
 
    } // end normal   
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
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
 
    } // end verbose
 
    //AddUnitReport()
    //send to Coordinator on the broadcastChannel the selected report
    //format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
    llSay( controlChannel, "AddUnitReport::unitKey::" + (string)llGetKey() + "::Report::" + reportString);
 
}
 
 
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   Initialize
//////////
//////////      Input:      no input parameters
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function initializes any variables or functions necessary
//////////                  to get us started
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
Initialize()
{
    llSetText( "Math Trig", <255,255,255>, 1);
}
 
 
///////////////////////////////////////////////////////////////////////////////////////
//STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE//
///////////////////////////////////////////////////////////////////////////////////////
//                                                                                   //
//                                                                                   //
//                          DEFAULT STATE                                            //
//                                                                                   //
//                                                                                   //
///////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
default
{
///////////////////////////////////////////////////////
//  State Entry of default state                     //
///////////////////////////////////////////////////////
   state_entry()
    {
        Initialize();
    }
////////////////////////////////////////////////////////
//  On Rez of default state                           //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
        Initialize();
    }
 
 
 
///////////////////////////////////////////////////////
//  Link Message of default state                    //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            //treat as command input
            ParseCommand(message);
        }
 
    } //end of link message
 
 
} // end default