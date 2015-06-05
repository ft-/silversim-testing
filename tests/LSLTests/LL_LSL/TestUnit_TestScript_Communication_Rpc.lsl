///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////       
///////
///////  This is the test script for the XML-RPC communication function 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//Communication_Rpc      .1 -> modified from TestUnit_TestScript base to test RPC functions  7.6.2007
 
 
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
 
integer debug = 2;                                                // level of debug message
integer debugChannel = DEBUG_CHANNEL;                             // output channel for debug messages
 
key HTTP_CONTROL;                                                 // HTTP Handler 
string HTTP_URL = "http://www.i3dnow.com/LSLTest_Rpc.php";        // url for the RPC test
 
string http_body;                                                 // temp storage for the http response
 
key gChannel;                                                     // XML-RPC channel
list remoteInfo;                                                  // a list of the parameters of the remote_data listener
 
string stringMessage = "Myvoiceismypassport";                     // string message used to test the XML-RPC
integer intMessage = 42;                                          // integer message used to test the XML-RPC
 
 
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
     // this will raise remote_data event REMOTE_DATA_CHANNEL when created
     llOpenRemoteDataChannel(); // create an XML-RPC channel
 
 
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
        // format example -> remoteInfo = [ type, channel, message_id, sender, ival, sval ];
        reportString += "type: " + llList2String( remoteInfo, 0) + "\n";
        reportString += "channel: " + llList2String( remoteInfo, 1)+ "\n";
        reportString += "message_id: " + llList2String( remoteInfo, 2)+ "\n";
        reportString += "sender: " + llList2String( remoteInfo, 3)+ "\n";
        reportString += "ival: " + llList2String( remoteInfo, 4)+ "\n";
        reportString += "sval: " + llList2String( remoteInfo, 5)+ "\n";
 
    }
 
    //QUITE - shortest level of reporting
    if( reportType == "QUIET" )
    {
        reportString += "ival: " + llList2String( remoteInfo, 4)+ "\n";
        reportString += "sval: " + llList2String( remoteInfo, 5)+ "\n";  
    }
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {        
        reportString += "http_body: " + http_body + "\n";
 
        reportString += "type: " + llList2String( remoteInfo, 0) + "\n";
        reportString += "channel: " + llList2String( remoteInfo, 1) + "\n";
        reportString += "message_id: " + llList2String( remoteInfo, 2) + "\n";
        reportString += "sender: " + llList2String( remoteInfo, 3) + "\n";
        reportString += "ival_rpc: " + llList2String( remoteInfo, 4) + "\n";
        reportString += "ival: " + (string)intMessage + "\n";
        reportString += "sval_rpc: " + llList2String( remoteInfo, 5) + "\n";
        reportString += "sval: " + stringMessage + "\n";
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
 
   llSetText( "Communication_Rpc" , <255,255,255>, 1);
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
//  Http Response of default state                   //
///////////////////////////////////////////////////////
http_response(key id, integer status, list metadata, string body)
    {
        if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":FUNCTION: DEFAULT HTTP");}
        if(debug > 0){llSay(debugChannel, body);}
 
        //if it was a call to the url
        if(id == HTTP_CONTROL)
        {    
            http_body = body; 
        }
 
    } // end http_response
 
///////////////////////////////////////////////////////
//  Remote Data of default state                     //
///////////////////////////////////////////////////////
    remote_data(integer type, key channel, key message_id, string sender, integer ival, string sval)
    {
        //if it is the channel creation
        if (type == REMOTE_DATA_CHANNEL)
        { 
            // channel created
            gChannel = channel;
 
            //initiate a XML-RPC call through the test url
            HTTP_CONTROL = llHTTPRequest( HTTP_URL + "?channel=" + (string)channel 
                                                   + "&stringMessage=" + stringMessage 
                                                   + "&intMessage=" + (string)intMessage  
                                                   , [HTTP_METHOD,"POST"],"");
        }
 
        // if it is data coming in from the test url 
        if (type == REMOTE_DATA_REQUEST)
        { 
            //set pass indicator
            string passIndication = "PASS";
 
            remoteInfo = [ type, channel, message_id, sender, ival, sval ];
 
            //if the incoming value does not match the requested string            
            if(sval != stringMessage )
            {
                if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":svalFAIL");}
                passIndication = "FAIL";
            }
 
            //if the incoming value does not match the requested integer
            if(ival != intMessage)
            {
                if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":intFAIL");}
                passIndication = "FAIL";
            }
 
            //One of the following messages needs to be sent at the end of this function
            //each time that it is run
            //
            // llMessageLinked(LINK_SET, passFailChannel, "PASS", NULL_KEY);
            // llMessageLinked(LINK_SET, passFailChannel, "FAIL", NULL_KEY);
            llMessageLinked(LINK_SET, passFailChannel, passIndication, NULL_KEY);
 
        } 
 
 
    } // end of remote data
 
 
 
} // end default