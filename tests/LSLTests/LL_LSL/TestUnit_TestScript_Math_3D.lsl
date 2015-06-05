///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////            Math_3D
///////
///////  This is the test script for the 3D math functions.  
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//Math_3D                .1 -> modified from TestUnit_TestScript base to test 3D math functions  7.3.2007
 
 
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
//  link message commands will be sent out on the toAllChannel, and received on the passFailChannel
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
integer passFailChannel = -355;        // test scripts channel for communicating pass/fail - linked message
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
 
integer llAngleBetweenPASS;            // 
integer llAxes2RotPASS;                //  
integer llAxisAngle2RotPASS;           //  These are global pass/fail
integer llEuler2RotPASS;               //  indicators for the various
integer llRot2EulerPASS;               //  Math 3D functions that are
integer llRotBetweenPASS;              //  being tested. These variables
integer llVecDistPASS;                 //  are used in the Run Test 
integer llVecMagPASS;                  //  and Report Functions of this 
integer llVecNormPASS;                 //  script.
 
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
 
     /////////////////////////////////////////////////////////////////
     // Function: float llAngleBetween( rotation a, rotation b ); 
     // Returns a float that is the angle between rotation a and b.
     // • rotation     a     –     start rotation     
     // • rotation     b     –     end rotation     
     /////////////////////////////////////////////////////////////////
    //initialize a pass variable to TRUE 
    llAngleBetweenPASS = 0;
 
     //compare two rotations with no angle in between
     if( (string)0.0 == (string)llAngleBetween( <0.0, 0.0, 0.0, 1.0>, <0.0, 0.0, 0.0, 1.0> ) &
     		(string)2.094395 == (string)llAngleBetween( <1.0, 1.0, 1.0, 1.0>, <0.0, 0.0, 0.0, 1.0> ) &
     		(string)2.094395 == (string)llAngleBetween( <0.0, 0.0, 0.0, 1.0>, <1.0, 1.0, 1.0, 1.0> ) )
     {
         llAngleBetweenPASS = 1;    
     }
 
     /////////////////////////////////////////////////////////////////////////
     // Function: rotation llAxes2Rot( vector fwd, vector left, vector up );
     // Returns a rotation that is defined by the 3 coordinate axes
     // • vector     fwd             
     // • vector     left             
     // • vector     up
     /////////////////////////////////////////////////////////////////////////
 
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
 
     /////////////////////////////////////////////////////////////////////////
     // Function: rotation llAxisAngle2Rot( vector axis, float angle );
     // Returns a rotation that is a generated angle about axis
     // • vector     axis             
     // • float     angle     –     expressed in radians.     
     /////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llAxisAngle2RotPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)<0.00000, 0.00000, 0.00000, 1.00000> == (string)llAxisAngle2Rot( < 0.0, 0.0, 0.0>, 0.0 ) &
     		(string)<0.84147, 0.00000, 0.00000, 0.54030> ==  (string)llAxisAngle2Rot( < 1.0, 0.0, 0.0>, 2.0 ) &
     		(string)<0.27680, 0.27680, 0.27680, 0.87758> == (string)llAxisAngle2Rot( < 1.0, 1.0, 1.0>, 1.0 ) &
     		(string)<0.00000, 0.00000, 0.47943, 0.87758> == (string)llAxisAngle2Rot( < 0.0, 0.0, 1.0>, 1.0 ) )
     {
         llAxisAngle2RotPASS = 1; 
     }
 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
     //////////////////////////////////////////////////////////////////////////////
     // Function: rotation llEuler2Rot( vector v );
     // Returns a rotation representation of Euler Angles v.
     // • vector     v         
     //////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llEuler2RotPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)<0.00000, 0.00000, 0.00000, 1.00000> == (string)llEuler2Rot( < 0.0, 0.0, 0.0> ) &
     		(string)<0.47943, 0.00000, 0.00000, 0.87758> ==  (string)llEuler2Rot( < 1.0, 0.0, 0.0> ) &
     		(string)<0.57094, 0.16752, 0.57094, 0.56568> == (string)llEuler2Rot( < 1.0, 1.0, 1.0> ) &
     		(string)<0.00000, 0.00000, 0.47943, 0.87758> == (string)llEuler2Rot( < 0.0, 0.0, 1.0> ) )
     {
         llEuler2RotPASS = 1; 
     }
 
       ///////////////////////////////////////////////////////////////////////////////////    
       // Function: vector llRot2Euler( rotation quat );
       // Returns a vector that is the Euler representation (roll, pitch, yaw) of quat.
       // • rotation     quat     –     Any valid rotation     
       ///////////////////////////////////////////////////////////////////////////////////    
 
    //initialize a pass variable to TRUE 
    llRot2EulerPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)<0.00000, 0.00000, 0.00000> == (string)llRot2Euler( <0.00000, 0.00000, 0.00000, 1.00000> ) &
     		(string)<1.57574, 0.11067, -0.08922> ==  (string)llRot2Euler( <1.00000, 0.10000, 0.01100, 1.00000> ) &
     		(string)<-1.10715, 0.72973, 2.03444> == (string)llRot2Euler( <0.00000, 1.00000, 1.00000, 1.00000> ) &
     		(string)<0.00000, 1.57080, 1.57080> == (string)llRot2Euler( <1.00000, 1.00000, 1.00000, 1.00000> ) )
     {
         llRot2EulerPASS = 1; 
     }     
 
 
     /////////////////////////////////////////////////////////////////////////////////
     // Function: rotation llRotBetween( vector start, vector end );
     // Returns a rotation that is the rotation between start to end
     // • vector     start             
     // • vector     end
     /////////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llRotBetweenPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)<0.00000, 0.00000, 0.00000, 1.00000> == (string)llRotBetween( < 0.0, 0.0, 0.0>, < 0.0, 0.0, 0.0> ) &
			(string)<0.00000, 0.03531, -0.70622, 0.70711> ==  (string)llRotBetween( < -10.0, 0.0, 0.0>, < 0.0, 10.0, 0.5> ) &
			(string)<0.62796, 0.62796, 0.00000, 0.45970> == (string)llRotBetween( < 10.0, -10.0, 10.0>, < 0.0, 0.0, -1.0> ) &
			(string)<0.00000, -0.99969, 0.00000, 0.02498> == (string)llRotBetween( < 0.0, 0.0, -10.0>, < 0.5, 0.0, 10.0> ) )
     {
         llRotBetweenPASS = 1; 
     }     
 
     /////////////////////////////////////////////////////////////////////////////////////////////////////
     // Function: float llVecDist( vector vec_a, vector vec_b );
     // Returns a float that is the distance between vec_a and vec_b (llVecMag(vec_a - vec_b)).
     // • vector     vec_a     –     Any valid vector     
     // • vector     vec_b     –     Any valid vector
     //////////////////////////////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llVecDistPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)0.0 == (string)llVecDist( < 0.0, 0.0, 0.0>, < 0.0, 0.0, 0.0> ) &
     		(string)1.0 ==  (string)llVecDist( < 1.0, 0.0, 0.0>, < 0.0, 0.0, 0.0> ) &
     		(string)1.732051 == (string)llVecDist( < 1.0, 1.0, 1.0>, < 0.0, 0.0, 0.0> ) &
     		(string)1.0 == (string)llVecDist( < 0.0, 0.0, -1.0>, < 0.0, 0.0, 0.0> ) )
     {
         llVecDistPASS = 1; 
     }     
 
 
     //////////////////////////////////////////////////////////////////////////////////////////////////////
     // Function: float llVecMag( vector vec );
     // Returns a float that is the magnitude of the vector (the distance from vec to <0.0, 0.0, 0.0>).
     // • vector     vec             
     //////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
    //initialize a pass variable to TRUE 
    llVecMagPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)0.0 == (string)llVecMag( < 0.0, 0.0, 0.0>) &
     		(string)1.0 ==  (string)llVecMag( < 1.0, 0.0, 0.0>) &
     		(string)1.732051 == (string)llVecMag( < 1.0, 1.0, 1.0> ) &
     		(string)1.414214 == (string)llVecMag( < 1.0, 0.0, -1.0> ) )
     {
         llVecMagPASS = 1; 
     }     
 
 
     ///////////////////////////////////////////////////////////////////////////////
     // Function: vector llVecNorm( vector vec );
     // Returns a vector that is the normal of the vector (vec / llVecMag(vec)).
     // • vector     vec     –     Any valid vector     
     ///////////////////////////////////////////////////////////////////////////////
 
    //initialize a pass variable to TRUE 
    llVecNormPASS = 0;
 
     //test four sets of configurations to hard-coded values
     if( (string)< 0.0, 0.0, 0.0> == (string)llVecNorm( < 0.0, 0.0, 0.0>) &
     		(string)< 1.0, 0.0, 0.0> ==  (string)llVecNorm( < 1.0, 0.0, 0.0>) &
     		(string)<0.57735, 0.57735, 0.57735> == (string)llVecNorm( < 1.0, 1.0, 1.0> ) &
     		(string)<0.70711, 0.00000, -0.70711> == (string)llVecNorm( < 1.0, 0.0, -1.0> ) )
     {
         llVecNormPASS = 1; 
     }         
 
 
     //multiple all of the individual pass variables together to check for any failures.
     integer pass = llAngleBetweenPASS *
     llAxes2RotPASS *
     llAxisAngle2RotPASS *
     llEuler2RotPASS *
     llRot2EulerPASS *
     llRotBetweenPASS *
     llVecDistPASS *
     llVecMagPASS *
     llVecNormPASS;
 
 
 
     // if all of the individual 
     if( pass == 1)
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
 
    // Initialize with FAIL
    string llAngleBetweenPASSstring = "FAIL";            
    string llAxes2RotPASSstring = "FAIL";
    string llAxisAngle2RotPASSstring = "FAIL";
    string llEuler2RotPASSstring = "FAIL";
    string llRot2EulerPASSstring = "FAIL";
    string llRotBetweenPASSstring = "FAIL";
    string llVecDistPASSstring = "FAIL";
    string llVecMagPASSstring = "FAIL";
    string llVecNormPASSstring = "FAIL";
 
    //translate integer conditional into text string for the report. 
    if ( llAngleBetweenPASS )
    {
          llAngleBetweenPASSstring = "PASS";
    }
    if ( llAxes2RotPASS )
    {
          llAxes2RotPASSstring = "PASS";
    }
    if ( llAxisAngle2RotPASS )
    {
        llAxisAngle2RotPASSstring = "PASS";
    }
    if ( llEuler2RotPASS)
    {
          llEuler2RotPASSstring = "PASS";
    }
    if ( llRot2EulerPASS )
    {
          llRot2EulerPASSstring = "PASS";
    }
    if ( llRotBetweenPASS )
    {
          llRotBetweenPASSstring = "PASS";
    }
    if ( llVecDistPASS )
    {
          llVecDistPASSstring = "PASS";
    }
    if ( llVecMagPASS )
    {
          llVecMagPASSstring = "PASS";
    }
    if ( llVecNormPASS )
    {
          llVecNormPASSstring = "PASS";
    }
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
      reportString = "Function: float llAngleBetween( rotation a, rotation b ) -> " 
                                                + llAngleBetweenPASSstring + "\n"
                   + "Function: rotation llAxes2Rot( vector fwd, vector left, vector up) ->" 
                                                + llAxes2RotPASSstring + "\n"
                   + "Function: rotation llAxisAngle2Rot( vector axis, float angle ) -> " 
                                                + llAxisAngle2RotPASSstring + "\n"
                   + "Function: rotation llEuler2Rot( vector v ) -> " 
                                                + llEuler2RotPASSstring + "\n"
                   + "Function: vector llRot2Euler( rotation quat ) -> " 
                                                + llRot2EulerPASSstring + "\n"
                   + "Function: rotation llRotBetween( vector start, vector end ) -> "
                                                + llRotBetweenPASSstring + "\n"
                   + "Function: float llVecDist( vector vec_a, vector vec_b ) -> " 
                                                + llVecDistPASSstring + "\n"
                   + "Function: float llVecMag( vector vec ) -> " 
                                                + llVecMagPASSstring + "\n"
                   + "Function: vector llVecNorm( vector vec ) -> " 
                                                + llVecNormPASSstring + "\n";
 
    } // end normal  
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
        reportString = "/////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llAngleBetween( rotation a, rotation b ) + " + "\n" +
				"// Returns a float that is the angle between rotation a and b." + "\n" +
				"// • rotation     a     –     start rotation     " + "\n" +
				"// • rotation     b     –     end rotation     " + "\n" +
				"/////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAngleBetweenPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: rotation llAxes2Rot( vector fwd, vector left, vector up ) +" + "\n" +
				"// Returns a rotation that is defined by the 3 coordinate axes" + "\n" +
				"// • vector     fwd             " + "\n" +
				"// • vector     left             " + "\n" +
				"// • vector     up" + "\n" +
				"/////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAxes2RotPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: rotation llAxisAngle2Rot( vector axis, float angle );" + "\n" +
				"// Returns a rotation that is a generated angle about axis" + "\n" +
				"// • vector     axis             " + "\n" +
				"// • float     angle     –     expressed in radians.     " + "\n" +
				"/////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llAxisAngle2RotPASSstring + "\n\n" +
 
				"//////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: rotation llEuler2Rot( vector v );" + "\n" +
				"// Returns a rotation representation of Euler Angles v." + "\n" +
				"// • vector     v         " + "\n" +
				"//////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llEuler2RotPASSstring + "\n\n" +
 
				"///////////////////////////////////////////////////////////////////////////////////  " + "\n" +  
				"// Function: vector llRot2Euler( rotation quat );" + "\n" +
				"// Returns a vector that is the Euler representation (roll, pitch, yaw) of quat." + "\n" +
				"// • rotation     quat     –     Any valid rotation     " + "\n" +
				"///////////////////////////////////////////////////////////////////////////////////  " + "\n" +  
				"PASS/FAIL -> " + llRot2EulerPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: rotation llRotBetween( vector start, vector end );" + "\n" +
				"// Returns a rotation that is the rotation between start to end" + "\n" +
				"// • vector     start             " + "\n" +
				"// • vector     end" + "\n" +
				"/////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llRotBetweenPASSstring + "\n\n" +
 
				"/////////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llVecDist( vector vec_a, vector vec_b );" + "\n" +
				"// Returns a float that is the distance between vec_a and vec_b (llVecMag(vec_a - vec_b))." + "\n" +
				"// • vector     vec_a     –     Any valid vector     " + "\n" +
				"// • vector     vec_b     –     Any valid vector" + "\n" +
				"//////////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llVecDistPASSstring + "\n\n" +
 
				"///////////////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: float llVecMag( vector vec );" + "\n" +
				"// Returns a float that is the magnitude of the vector (the distance from vec to <0.0, 0.0, 0.0>)." + "\n" +
				"// • vector     vec             " + "\n" +
				"///////////////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llVecMagPASSstring + "\n\n" +
 
				"///////////////////////////////////////////////////////////////////////////////" + "\n" +
				"// Function: vector llVecNorm( vector vec );" + "\n" +
				"// Returns a vector that is the normal of the vector (vec / llVecMag(vec))." + "\n" +
				"// • vector     vec     –     Any valid vector     " + "\n" +
				"///////////////////////////////////////////////////////////////////////////////" + "\n" +
				"PASS/FAIL -> " + llVecNormPASSstring + "\n\n";
 
  }// end verbose
 
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
   //why
   llSetText( "Math 3D", <255,255,255>, 1);
 
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