///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////            MATH_BinaryOperators
///////
///////  This is the test script for the data types.  
///////
///////  Created by Vektor Linden    
///////               
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//BinaryOperators        .3 -> Formal creation of script 2/18/10
 
 
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
 
 
integer ArithmeticAddition_PASS;                    // These are global pass/fail
integer ArithmeticSubtraction_PASS;                    // indicators for the various
integer ArithmeticMultiplication_PASS;                   // Data Types that are 
integer ArithmeticDivision_PASS;                     // being tested. These variables 
integer ArithmeticModulo_PASS;                     // are used in the Run Test and
integer ArithmeticGreaterThan_PASS;                     // Report Functions of this script. 
integer ArithmeticLessThan_PASS;
integer ArithmeticGreaterThanOrEqualTo_PASS; 
integer ArithmeticLessThanOrEqualTo_PASS; 
integer LogicalInequality_PASS; 
integer LogicalEquality_PASS; 
integer LogicalAND_PASS; 
integer LogicalOR_PASS; 
integer BitwiseAND_PASS; 
integer BitwiseOR_PASS; 
integer BitwiseLeftShift_PASS; 
integer BitwiseRightShift_PASS;
integer BitwiseExclusiveOR_PASS; 
 
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
//////////      Input:      no input paramaters
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
        // initialize PASS variable        
        ArithmeticAddition_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (1 + 1 == 2)
        {
            ArithmeticAddition_PASS = 1;
        }
 
        // initialize PASS variable        
        ArithmeticSubtraction_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (1 - 1 == 0)
        {
            ArithmeticSubtraction_PASS = 1;
        }
 
        // initialize PASS variable        
        ArithmeticMultiplication_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (2 * 2 == 4)
        {
            ArithmeticMultiplication_PASS = 1;
        }
 
        // initialize PASS variable        
        ArithmeticDivision_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (4 / 2 == 2)
        {
            ArithmeticDivision_PASS = 1;
        }
 
        // initialize PASS variable        
        ArithmeticModulo_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (5 % 2 == 1)
        {
            ArithmeticModulo_PASS = 1;
        }
 
        // initialize PASS variable
        ArithmeticGreaterThan_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (2 > 1)
        {
            ArithmeticGreaterThan_PASS = 1;
        }
 
        // initialize PASS variable
        ArithmeticLessThan_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (1 < 2)
        {
            ArithmeticLessThan_PASS = 1;
        }
 
        // initialize PASS variable        
        ArithmeticGreaterThanOrEqualTo_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (2 >= 1)
        {
            if (1 >= 1)
            {
                ArithmeticGreaterThanOrEqualTo_PASS = 1;
            }
        }
 
        // initialize PASS variable        
        ArithmeticLessThanOrEqualTo_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (1 <= 2)
        {
            if (1 <= 1)
            {
                ArithmeticLessThanOrEqualTo_PASS = 1;
            }
        }
 
        // initialize PASS variable        
        LogicalInequality_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (1 != 2)
        {
            LogicalInequality_PASS = 1;
        }
 
        // initialize PASS variable        
        LogicalEquality_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (1 == 1)
        {
            LogicalEquality_PASS = 1;
        }
 
        // initialize PASS variable        
        LogicalAND_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (TRUE && TRUE == TRUE)
        {
            LogicalAND_PASS = 1;
        }
 
        // initialize PASS variable
        LogicalOR_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (TRUE || TRUE == TRUE)
        {
            LogicalOR_PASS = 1;
        }
 
        // initialize PASS variable
        BitwiseAND_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if  (TRUE & TRUE == TRUE)
        {
            BitwiseAND_PASS = 1;
        }
 
        // initialize PASS variable        
        BitwiseOR_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if  (TRUE | TRUE == TRUE)
        {
            BitwiseOR_PASS = 1;
        }
 
        // initialize PASS variable        
        BitwiseLeftShift_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (2 << 1 == 4)
        {
            BitwiseLeftShift_PASS = 1;
        }
 
        // initialize PASS variable        
        BitwiseRightShift_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.        
        if (2 >> 1 == 1)
        {
            BitwiseRightShift_PASS = 1;
        }
 
        // initialize PASS variable        
        BitwiseExclusiveOR_PASS = 0;
        // Compare data against criteria in GetType function, which returns an integer corresponding to the type.
        if (0 ^ 0 == 0)
        {
            if (0 ^ 1 == 1)
            {
                if (1 ^ 0 == 1)
                {
                    if (1 ^ 1 == 0)
                    {
                        BitwiseExclusiveOR_PASS = 1;
                    }
                }
            }
        }
 
     //check to see if any failures occured. 
        integer pass = ArithmeticAddition_PASS &
                       ArithmeticSubtraction_PASS &
                       ArithmeticMultiplication_PASS &
                       ArithmeticModulo_PASS &
                       ArithmeticDivision_PASS &
                       ArithmeticGreaterThan_PASS &
                       ArithmeticLessThan_PASS &
                       ArithmeticGreaterThanOrEqualTo_PASS &
                       ArithmeticLessThanOrEqualTo_PASS &
                       LogicalInequality_PASS &
                       LogicalEquality_PASS &
                       LogicalAND_PASS &
                       LogicalOR_PASS &
                       BitwiseAND_PASS &
                       BitwiseOR_PASS &
                       BitwiseLeftShift_PASS &                       
                       BitwiseRightShift_PASS &
                       BitwiseExclusiveOR_PASS;
 
     // if all of the individual cases pass, test passes.                                
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
//////////                                         -> QUIET - summary information only
//////////                                         -> VERBOSE - everything
//////////                    
//////////      Output:     llSay on broadcastChannel 
//////////                    
//////////      Purpose:    This function is where you design the three level of reports
//////////                  avaliable upon request by the Coordinator
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
Report( integer controlChannel, string reportType )
{
    //this string will be sent out reguardless of reporting mode
    string reportString;
 
    // PASS or FAIL wording for the report
    string ArithmeticAddition_PASSstring = "FAIL";
    string ArithmeticSubtraction_PASSstring = "FAIL";
    string ArithmeticMultiplication_PASSstring = "FAIL";
    string ArithmeticDivision_PASSstring = "FAIL";
    string ArithmeticModulo_PASSstring = "FAIL";
    string ArithmeticGreaterThan_PASSstring = "FAIL";
    string ArithmeticLessThan_PASSstring = "FAIL";
    string ArithmeticGreaterThanOrEqualTo_PASSstring = "FAIL";
    string ArithmeticLessThanOrEqualTo_PASSstring = "FAIL";
    string LogicalInequality_PASSstring = "FAIL";
    string LogicalEquality_PASSstring = "FAIL";
    string LogicalAND_PASSstring = "FAIL";
    string LogicalOR_PASSstring = "FAIL";
    string BitwiseAND_PASSstring = "FAIL";  
    string BitwiseOR_PASSstring = "FAIL";
    string BitwiseLeftShift_PASSstring = "FAIL";
    string BitwiseRightShift_PASSstring = "FAIL";
    string BitwiseExclusiveOR_PASSstring = "FAIL";
 
    //translate integer conditional into text string for the report. 
    if ( ArithmeticAddition_PASS )
    {
          ArithmeticAddition_PASSstring = "PASS";
    }
    if ( ArithmeticSubtraction_PASS )
    {
          ArithmeticSubtraction_PASSstring = "PASS";
    }
    if ( ArithmeticMultiplication_PASS )
    {
          ArithmeticMultiplication_PASSstring = "PASS";
    }
    if ( ArithmeticDivision_PASS  )
    {
          ArithmeticDivision_PASSstring = "PASS";
    }
    if ( ArithmeticModulo_PASS )
    {
          ArithmeticModulo_PASSstring = "PASS";
    }
    if ( ArithmeticGreaterThan_PASS )
    {
          ArithmeticGreaterThan_PASSstring = "PASS";
    }
    if ( ArithmeticLessThan_PASS )
    {
          ArithmeticLessThan_PASSstring = "PASS";
    }   
    if ( ArithmeticGreaterThanOrEqualTo_PASS )
    {
          ArithmeticGreaterThanOrEqualTo_PASSstring = "PASS";
    }
    if ( ArithmeticLessThanOrEqualTo_PASS )
    {
          ArithmeticLessThanOrEqualTo_PASSstring = "PASS";
    }
    if ( LogicalInequality_PASS )
    {
          LogicalInequality_PASSstring = "PASS";
    }
    if ( LogicalEquality_PASS  )
    {
          LogicalEquality_PASSstring = "PASS";
    }
    if ( LogicalAND_PASS )
    {
          LogicalAND_PASSstring = "PASS";
    }
    if ( LogicalOR_PASS )
    {
          LogicalOR_PASSstring = "PASS";
    }
    if ( BitwiseAND_PASS )
    {
          BitwiseAND_PASSstring = "PASS";
    }   
    if ( BitwiseOR_PASS )
    {
          BitwiseOR_PASSstring = "PASS";
    }
    if ( BitwiseLeftShift_PASS )
    {
          BitwiseLeftShift_PASSstring = "PASS";
    }
    if ( BitwiseRightShift_PASS )
    {
          BitwiseRightShift_PASSstring = "PASS";
    }
    if ( BitwiseExclusiveOR_PASS  )
    {
          BitwiseExclusiveOR_PASSstring = "PASS";
    }
 
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
      reportString = "Type: ArithmeticAddition -> " 
                                                + ArithmeticAddition_PASSstring + "\n"
                   + "Type: ArithmeticSubtraction -> " 
                                                + ArithmeticSubtraction_PASSstring + "\n"
                   + "Type: ArithmeticMultiplication -> " 
                                                + ArithmeticMultiplication_PASSstring + "\n"
                   + "Type: ArithmeticDivision -> " 
                                                + ArithmeticDivision_PASSstring + "\n"
                   + "Type: ArithmeticModulo -> "
                                                + ArithmeticModulo_PASSstring + "\n"
                   + "Type: ArithmeticGreaterThan ->" 
                                                + ArithmeticGreaterThan_PASSstring + "\n"
                   + "Type: ArithmeticLessThan -> " 
                                                + ArithmeticLessThan_PASSstring + "\n"
                   + "Type: ArithmeticGreaterThanOrEqualTo -> " 
                                                + ArithmeticGreaterThanOrEqualTo_PASSstring + "\n"
                   + "Type: ArithmeticLessThanOrEqualTo -> " 
                                                + ArithmeticLessThanOrEqualTo_PASSstring + "\n"
                   + "Type: LogicalInequality -> "
                                                + LogicalInequality_PASSstring + "\n"
                   + "Type: LogicalEquality ->" 
                                                + LogicalEquality_PASSstring + "\n"
                   + "Type: LogicalAND -> " 
                                                + LogicalAND_PASSstring + "\n"
                   + "Type: LogicalOR -> " 
                                                + LogicalOR_PASSstring + "\n"
                   + "Type: BitwiseAND -> " 
                                                + BitwiseAND_PASSstring + "\n"
                   + "Type: BitwiseOR -> "
                                                + BitwiseOR_PASSstring + "\n"                                                                 + "Type: BitwiseLeftShift -> "
                                                + BitwiseLeftShift_PASSstring + "\n"                                           
                   + "Type: BitwiseRightShift -> "
                                                + BitwiseRightShift_PASSstring + "\n"
                   + "Type: BitwiseExclusiveOR ->" 
                                                + BitwiseExclusiveOR_PASSstring + "\n";
 
    } // end normal   
 
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
             reportString = "///////////////////////////////////////////////////////////////////////////////////////////" + "\n"+
                 "// Type: ArithmeticAddition" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • ArithmeticAddition = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticAddition_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticSubtraction" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • ArithmeticSubtraction = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticSubtraction_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticMultiplication" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • ArithmeticMultiplication = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticMultiplication_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticDivision" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • ArithmeticDivision = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticDivision_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////" + "\n" +    
                "// Type: ArithmeticModulo" + "\n" +
                "// Returns an integer representing the vector data type constant" + "\n" +
                "// • ArithmeticModulo = 5" + "\n" +
                "//////////////////////////////////////////////////////////////////////////////////" + "\n" +   
                "PASS/FAIL -> " + ArithmeticModulo_PASSstring + "\n\n" +
 
                 "// Type: ArithmeticGreaterThan" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • ArithmeticGreaterThan = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticGreaterThan_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticLessThan" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • ArithmeticLessThan = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticLessThan_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticGreaterThanOrEqualTo" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • ArithmeticGreaterThanOrEqualTo = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticGreaterThanOrEqualTo_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: ArithmeticLessThanOrEqualTo" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • ArithmeticLessThanOrEqualTo = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + ArithmeticLessThanOrEqualTo_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                 "// Type: LogicalInequality" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • LogicalInequality = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + LogicalInequality_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: LogicalEquality" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • LogicalEquality = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + LogicalEquality_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: LogicalAND" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • LogicalAND = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + LogicalAND_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: LogicalOR" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • LogicalOR = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + LogicalOR_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: BitwiseAND" + "\n" +
                "// Returns an integer representing the invalid data type constant" + "\n" +
                "// • BitwiseAND = 0" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + BitwiseAND_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: BitwiseOR" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • BitwiseOR = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + BitwiseOR_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: BitwiseLeftShift" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • BitwiseLeftShift = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + BitwiseLeftShift_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: BitwiseRightShift" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • BitwiseRightShift = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + BitwiseRightShift_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: BitwiseExclusiveOR" + "\n" +
                "// Returns an integer representing the invalid data type constant" + "\n" +
                "// • BitwiseExclusiveOR = 0" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + BitwiseExclusiveOR_PASSstring + "\n\n";                
 
 
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
//////////      Input:      no input paramaters
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
    llSetText( "MATH_BinaryOperators", <255,255,255>, 1);
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