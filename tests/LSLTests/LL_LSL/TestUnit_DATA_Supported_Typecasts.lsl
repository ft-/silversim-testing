///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////            DATA_Supported Typecasts
///////
///////  This is the test script for the data types.  
///////
///////  Created by Vektor Linden         
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//Supported Typecasts    .3 -> Formal creation of script 2/09/10
 
 
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
 
 
integer IntVar = 5;                    // These are global pass/fail
rotation RotVar = <90.0, 90.0, 90.0, 1.0>;                    // indicators for the various
float FloatVar = 2.0;                   // Data Types that are 
vector VectorVar = <90.0, 90.0, 90.0>;                     // being tested. These variables 
key KeyVar = "07be0b18-230b-81c4-51a7-f4a3be2aae0b";                     // are used in the Run Test and
string StringVar = "This is the typecasting test string.";                     // Report Functions of this script. 
 
// Variables for holding typecasts
integer int2int;
float int2float;                   
string int2string; 
 
rotation rot2rot;
string rot2string;
 
integer float2int;
float float2float;                   
string float2string; 
 
vector Vec2vec;
string Vec2string;
 
key key2key;
string key2string;
 
integer string2int;
rotation string2rot;                   
float string2float;
vector string2vec;
key string2key;                   
string string2string; 
 
// PASS BITS
integer int2int_PASS;                    
integer int2float_PASS;                    
integer int2string_PASS;
 
integer rot2rot_PASS;             
integer rot2string_PASS;             
 
integer float2int_PASS;     
integer float2float_PASS;
integer float2string_PASS; 
 
integer Vec2vec_PASS; 
integer Vec2string_PASS;
 
integer key2key_PASS; 
integer key2string_PASS;
 
integer string2int_PASS; 
integer string2rot_PASS; 
integer string2float_PASS; 
integer string2vec_PASS; 
integer string2key_PASS;
integer string2string_PASS;
 
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
        // INT IntVar
        int2int = (integer)IntVar;
        // initialize PASS variable  
        int2int_PASS = 0;
        if ((string)int2int == "5")
        {
            int2int_PASS = 1;
        }
 
        int2float = (float)IntVar ;
        // initialize PASS variable          
        int2float_PASS = 0;
        if ((string)int2float == "5.000000")
        {
            int2float_PASS = 1;
        }        
 
        int2string = (string)IntVar;
        // initialize PASS variable          
        int2string_PASS = 0;
        if ((string)int2string == "5")
        {
            int2string_PASS = 1;
        }        
 
 
        // rotation RotVar
        rot2rot = (rotation)RotVar;
        // initialize PASS variable          
        rot2rot_PASS = 0;
        if ((string)rot2rot == "<90.00000, 90.00000, 90.00000, 1.00000>")
        {
            rot2rot_PASS = 1;
        }        
 
        rot2string = (string)RotVar;
        // initialize PASS variable          
        rot2string_PASS = 0;
        if ((string)rot2string == "<90.00000, 90.00000, 90.00000, 1.00000>")
        {
            rot2string_PASS = 1;
        }
 
 
        // float FloatVar
        float2int = (integer)FloatVar;
        // initialize PASS variable          
        float2int_PASS = 0;
        if ((string)float2int == "2")
        {
            float2int_PASS = 1;
        }
 
        float2float = (float)FloatVar;
        // initialize PASS variable          
        float2float_PASS = 0;         
        if ((string)float2float == "2.000000")
        {
            float2float_PASS = 1;
        }
 
        float2string = (string)FloatVar;
        // initialize PASS variable          
        float2string_PASS = 0;
        if ((string)float2string == "2.000000")
        {
            float2string_PASS = 1;
        }
 
 
        // vector VectorVar
        Vec2vec = (vector)VectorVar;
        // initialize PASS variable          
        Vec2vec_PASS = 0;                
        if ((string)Vec2vec == "<90.00000, 90.00000, 90.00000>")
        {
            Vec2vec_PASS = 1;
        }
 
        Vec2string = (string)VectorVar;
        // initialize PASS variable          
        Vec2string_PASS = 0;
        if ((string)Vec2string == "<90.00000, 90.00000, 90.00000>")
        {
            Vec2string_PASS = 1;
        }        
 
 
        // key KeyVar
        key2key = (key)KeyVar;
        // initialize PASS variable          
        key2key_PASS = 0;
        if ((string)key2key == "07be0b18-230b-81c4-51a7-f4a3be2aae0b")
        {
            key2key_PASS = 1;
        }
 
        key2string = (string)KeyVar;
        // initialize PASS variable          
        key2string_PASS = 0;        
        if ((string)key2string == "07be0b18-230b-81c4-51a7-f4a3be2aae0b")
        {
            key2string_PASS = 1;
        }        
 
 
        // string StringVar 
        string2int = (integer)StringVar;
        // initialize PASS variable          
        string2int_PASS = 0; 
        if ((string)string2int == "0")
        {
            string2int_PASS = 1;
        }
 
        string2rot = (rotation)StringVar;
        // initialize PASS variable          
        string2rot_PASS = 0;
        if ((string)string2rot == "<0.00000, 0.00000, 0.00000, 1.00000>")
        {
            string2rot_PASS = 1;
        }
 
        string2float = (float)StringVar;
        // initialize PASS variable          
        string2float_PASS = 0;
        if ((string)string2float == "0.000000")
        {
            string2float_PASS = 1;
        }
 
        string2vec = (vector)StringVar;
        // initialize PASS variable          
        string2vec_PASS = 0;
        if ((string)string2vec == "<0.00000, 0.00000, 0.00000>")
        {
            string2vec_PASS = 1;
        }
 
        string2key = (key)StringVar;
        // initialize PASS variable          
        string2key_PASS = 0;
        if ((string)string2key == "This is the typecasting test string.")
        {
            string2key_PASS = 1;
        }
 
        string2string = (string)StringVar;
        // initialize PASS variable          
        string2string_PASS = 0;
        if ((string)string2string == "This is the typecasting test string.")
        {
            string2string_PASS = 1;
        }
 
     //check to see if any failures occured. 
        integer pass = int2int_PASS &
                       int2float_PASS &
                       int2string_PASS &
                       rot2rot_PASS &
                       rot2string_PASS &
                       float2int_PASS &
                       float2float_PASS &
                       float2string_PASS &
                       Vec2vec_PASS &
                       Vec2string_PASS &
                       key2key_PASS &
                       key2string_PASS &
                       string2int_PASS &
                       string2rot_PASS &
                       string2float_PASS &
                       string2vec_PASS &                       
                       string2key_PASS &
                       string2string_PASS;
 
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
    string int2int_PASSstring = "FAIL";
    string int2float_PASSstring = "FAIL";
    string int2string_PASSstring = "FAIL";
    string rot2rot_PASSstring = "FAIL";
    string rot2string_PASSstring = "FAIL";
    string float2int_PASSstring = "FAIL";
    string float2float_PASSstring = "FAIL";
    string float2string_PASSstring = "FAIL";
    string Vec2vec_PASSstring = "FAIL";
    string Vec2string_PASSstring = "FAIL";
    string key2key_PASSstring = "FAIL";
    string key2string_PASSstring = "FAIL";
    string string2int_PASSstring = "FAIL";
    string string2rot_PASSstring = "FAIL";  
    string string2float_PASSstring = "FAIL";
    string string2vec_PASSstring = "FAIL";
    string string2key_PASSstring = "FAIL";
    string string2string_PASSstring = "FAIL";
 
    //translate integer conditional into text string for the report. 
    if ( int2int_PASS )
    {
          int2int_PASSstring = "PASS";
    }
    if ( int2float_PASS )
    {
          int2float_PASSstring = "PASS";
    }
    if ( int2string_PASS )
    {
          int2string_PASSstring = "PASS";
    }
    if ( rot2rot_PASS  )
    {
          rot2rot_PASSstring = "PASS";
    }
    if ( rot2string_PASSstring )
    {
          rot2string_PASSstring = "PASS";
    }
    if ( float2int_PASS )
    {
          float2int_PASSstring = "PASS";
    }
    if ( float2float_PASS )
    {
          float2float_PASSstring = "PASS";
    }   
    if ( float2string_PASS )
    {
          float2string_PASSstring = "PASS";
    }
    if ( Vec2vec_PASS )
    {
          Vec2vec_PASSstring = "PASS";
    }
    if ( Vec2string_PASS )
    {
          Vec2string_PASSstring = "PASS";
    }
    if ( key2key_PASS  )
    {
          key2key_PASSstring = "PASS";
    }
    if ( key2string_PASS )
    {
          key2string_PASSstring = "PASS";
    }
    if ( string2int_PASS )
    {
          string2int_PASSstring = "PASS";
    }
    if ( string2rot_PASS )
    {
          string2rot_PASSstring = "PASS";
    }   
    if ( string2float_PASS )
    {
          string2float_PASSstring = "PASS";
    }
    if ( string2vec_PASS )
    {
          string2vec_PASSstring = "PASS";
    }
    if ( string2key_PASS )
    {
          string2key_PASSstring = "PASS";
    }
    if ( string2string_PASS  )
    {
          string2string_PASSstring = "PASS";
    }
 
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
      reportString = "Type: int2int -> " 
                                                + int2int_PASSstring + "\n"
                   + "Type: int2float -> " 
                                                + int2float_PASSstring + "\n"
                   + "Type: int2string -> " 
                                                + int2string_PASSstring + "\n"
                   + "Type: rot2rot -> " 
                                                + rot2rot_PASSstring + "\n"
                   + "Type: rot2string -> "
                                                + rot2string_PASSstring + "\n"
                   + "Type: float2int ->" 
                                                + float2int_PASSstring + "\n"
                   + "Type: float2float -> " 
                                                + float2float_PASSstring + "\n"
                   + "Type: float2string -> " 
                                                + float2string_PASSstring + "\n"
                   + "Type: Vec2vec -> " 
                                                + Vec2vec_PASSstring + "\n"
                   + "Type: Vec2string -> "
                                                + Vec2string_PASSstring + "\n"
                   + "Type: key2key ->" 
                                                + key2key_PASSstring + "\n"
                   + "Type: key2string -> " 
                                                + key2string_PASSstring + "\n"
                   + "Type: string2int -> " 
                                                + string2int_PASSstring + "\n"
                   + "Type: string2rot -> " 
                                                + string2rot_PASSstring + "\n"
                   + "Type: string2float -> "
                                                + string2float_PASSstring + "\n"                                                                 + "Type: string2vec -> "
                                                + string2vec_PASSstring + "\n"                                           
                   + "Type: string2key -> "
                                                + string2key_PASSstring + "\n"
                   + "Type: string2string ->" 
                                                + string2string_PASSstring + "\n";
 
    } // end normal    
 
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
             reportString = "///////////////////////////////////////////////////////////////////////////////////////////" + "\n"+
                 "// Type: int2int" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • int2int = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + int2int_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: int2float" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • int2float = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + int2float_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: int2string" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • int2string = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + int2string_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: rot2rot" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • rot2rot = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + rot2rot_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////" + "\n" +    
                "// Type: rot2string" + "\n" +
                "// Returns an integer representing the vector data type constant" + "\n" +
                "// • rot2string = 5" + "\n" +
                "//////////////////////////////////////////////////////////////////////////////////" + "\n" +   
                "PASS/FAIL -> " + rot2string_PASSstring + "\n\n" +
 
                 "// Type: float2int" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • float2int = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + float2int_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: float2float" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • float2float = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + float2float_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: float2string" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • float2string = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + float2string_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: Vec2vec" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • Vec2vec = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + Vec2vec_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                 "// Type: Vec2string" + "\n" +
                "// Returns an integer representing the integer type constant" + "\n" +
                "// • Vec2string = 1" + "\n" +
                "///////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + Vec2string_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: key2key" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • key2key = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + key2key_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: key2string" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • key2string = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + key2string_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2int" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • string2int = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2int_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2rot" + "\n" +
                "// Returns an integer representing the invalid data type constant" + "\n" +
                "// • string2rot = 0" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2rot_PASSstring + "\n\n" +
 
                "///////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2float" + "\n" +
                "// Returns an integer representing the float type constant" + "\n" +
                "// • string2float = 2" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2float_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2vec" + "\n" +
                "// Returns an integer representing the string data type constant" + "\n" +
                "// • string2vec = 3" + "\n" +
                "////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2vec_PASSstring + "\n\n" +
 
                "//////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2key" + "\n" +
                "// Returns an integer representing the key data type constant" + "\n" +
                "// • string2key = 4" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2key_PASSstring + "\n\n" +
 
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "// Type: string2string" + "\n" +
                "// Returns an integer representing the invalid data type constant" + "\n" +
                "// • string2string = 0" + "\n" +
                "/////////////////////////////////////////////////////////////////////////////////" + "\n" +
                "PASS/FAIL -> " + string2string_PASSstring + "\n\n";                
 
 
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
    llSetText( "DATA_Supported Typecasts", <255,255,255>, 1);
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