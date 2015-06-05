///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////       
///////
///////  This is the test script for the notecard data server function 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//DataServer_Notecard      .1 -> modified from TestUnit_TestScript base to test notecard functions  7.6.2007
 
 
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
 
integer toAllChannel = -255;                                      // general channel - linked message
integer passFailChannel = -355;                                   // test scripts channel for communicating pass/fail - linked message
 
integer debug = 0;                                                // level of debug message
integer debugChannel = DEBUG_CHANNEL;                             // output channel for debug messages
 
string lineOne = "DataServerNotecardTest_nc";                     // string message used to test the notecard function
string lineTwo = "The is the second line of the notecard";        // string message used to test the notecard function
string lineThree = "This must be the last line of the notecard";  // string message used to test the notecard function
 
string lineOne_nc;                                                // string message used to test the notecard function
string lineTwo_nc;                                                // string message used to test the notecard function
string lineThree_nc;                                              // string message used to test the notecard function
 
integer notecardLines;                                            //
key notecardRequestKey;                                           // notecard globals 
key notecardLineRequest;                                          // 
integer currentNoteLine;                                          //
 
string passIndication;                                            // status variable for the pass/fail of the test
 
 
 
 
 
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
    if(message == "reset")
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
        //initiate data server call to begin reading notecard
        //the first line of the notecard is also the name of the notecard
        notecardRequestKey = llGetNumberOfNotecardLines(lineOne);
 
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
Report( integer broadcastChannel, string reportType )
{
    string reportString;
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
        reportString += "notecardLines: " + (string)notecardLines + "\n";
        reportString += "notecardRequestKey: " + (string)notecardRequestKey + "\n";
        reportString += "notecardLineRequest: " + (string)notecardLineRequest + "\n";
        reportString += "currentNoteLine: " + (string)currentNoteLine + "\n";
 
        reportString += "lineOne_nc: " + lineOne_nc + "\n";
        reportString += "lineTwo_nc: " + lineTwo_nc + "\n";
        reportString += "lineThree_nc: " + lineThree_nc + "\n";
 
    }
 
    //QUIET - shortest level of reporting
    if( reportType == "QUIET" )
    {
        reportString += "lineOne_nc: " + lineOne_nc + "\n";
        reportString += "lineTwo_nc: " + lineTwo_nc + "\n";
        reportString += "lineThree_nc: " + lineThree_nc + "\n";  
    }
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {        
        reportString += "lineOne: " + lineOne + "\n";
        reportString += "lineOne_nc: " + lineOne_nc + "\n";
        reportString += "lineTwo: " + lineTwo + "\n";
        reportString += "lineTwo_nc: " + lineTwo_nc + "\n";
        reportString += "lineThree: " + lineThree + "\n";
        reportString += "lineThree_nc: " + lineThree_nc + "\n";
        reportString += "notecardLines: " + (string)notecardLines + "\n";
        reportString += "notecardRequestKey: " + (string)notecardRequestKey + "\n";
        reportString += "notecardLineRequest: " + (string)notecardLineRequest + "\n";
        reportString += "currentNoteLine: " + (string)currentNoteLine + "\n";
    }
 
    //AddUnitReport()
    //send to Coordinator on the broadcastChannel the selected report
    //format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
    llSay( broadcastChannel, "AddUnitReport::unitKey::" + (string)llGetKey() + "::Report::" + reportString);
 
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
 
   llSetText( "DataServer_Notecard", <255,255,255>, 1);
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
 
 
///////////////////////////////////////////////////////
//  data server of default                           //
///////////////////////////////////////////////////////   
    dataserver(key queryid, string data)
    {
        if(debug > 1)llSay(debugChannel, llGetScriptName()+ "->DataServer: " + data);
 
        if(queryid == notecardRequestKey) // line number request
        {
            //set the number of lines in the notecard
            notecardLines = (integer)data;
            //move the pointer to zero
            currentNoteLine = 0;
            //the name of the notecard is stored in the lineOne string variable
            notecardLineRequest = llGetNotecardLine(lineOne, currentNoteLine);
 
            //since linenumber is the first
            //set pass
            passIndication = "PASS";
 
        } //end line number request
 
        if(queryid == notecardLineRequest) //reading a line from the notecard
        {
            if( currentNoteLine == 0 )
            {
                if( data != lineOne)
                {
                    passIndication = "FAIL";    
                }
                lineOne_nc = data;
            }
            else if ( currentNoteLine == 1 )
            {
                if( data != lineTwo)
                {
                    passIndication = "FAIL";    
                }
                lineTwo_nc = data;
            }
            else if ( currentNoteLine == 2 )
            {
                if( data != lineThree)
                {
                    passIndication = "FAIL";    
                }
                lineThree_nc = data;
            }
 
 
            //if additional lines on the notecard
            if(currentNoteLine < notecardLines - 1)
            {
                currentNoteLine += 1;
                //initiate another line request from the dataserver
                notecardLineRequest = llGetNotecardLine(lineOne, currentNoteLine);
            }
            else
            {
                // Done testing
 
                //One of the following messages needs to be sent at the end of this function
                //each time that it is run
                //
                // llMessageLinked(LINK_SET, passFailChannel, "PASS", NULL_KEY);
                // llMessageLinked(LINK_SET, passFailChannel, "FAIL", NULL_KEY);
                llMessageLinked(LINK_SET, passFailChannel, passIndication, NULL_KEY);
 
            }
 
        } //end line request
    } //end data server
 
 
 
} // end default