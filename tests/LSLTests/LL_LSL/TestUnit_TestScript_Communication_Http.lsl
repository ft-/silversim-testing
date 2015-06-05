///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////       
///////
///////  This is the test script for the HTTP communication function 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
//Communication_Http     .1 -> modified from TestUnit_TestScript base to test http functions  7.6.2007
 
 
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
 
integer toAllChannel = -255;                                      // general channel - linked message
integer passFailChannel = -355;                                   // test scripts channel for communicating pass/fail - linked message
 
integer debug = 0;                                                // level of debug message
integer debugChannel = DEBUG_CHANNEL;                             // output channel for debug messages
 
key HTTP_CONTROL;                                                 // HTTP Handler 
string HTTP_URL = "TODO: add your server url here";               // URL for the HTTP test
 
string http_body;                                                 // temp storage for the http response
 
 
 
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
     //initiate a http request
     HTTP_CONTROL = llHTTPRequest( HTTP_URL, [HTTP_METHOD,"PUT"],"");
 
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
 
 
     if(debug > 1)llSay(debugChannel, llGetScriptName()+ "->Report() type=" + reportType);
 
    //Normal - moderate level of reporting
    if( reportType == "NORMAL" )
    {
        //parse http response to useful list
        //format example -> ownername::objectname::objectkey::ownerkey::region::location
        list unitParameters = llParseString2List( http_body , ["::"], [""]); 
 
        reportString += "http_ownername: " + llList2String( unitParameters, 0) + "\n";
        reportString += "http_objectname: " + llList2String( unitParameters, 1)+ "\n";
        reportString += "http_objectkey: " + llList2String( unitParameters, 2)+ "\n";
        reportString += "http_ownerkey: " + llList2String( unitParameters, 3)+ "\n";
        reportString += "http_region: " + llList2String( unitParameters, 4)+ "\n";
        reportString += "http_location: " + llList2String( unitParameters, 5)+ "\n";
 
    }
 
    //QUITE - shortest level of reporting
    if( reportType == "QUIET" )
    {
        reportString = http_body;  
    }
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
 
        //parse http response to useful list
        //format example -> ownername::objectname::objectkey::ownerkey::region::location
        list unitParameters= llParseString2List( http_body, ["::"], [""]);
 
        reportString += "http_body: " + llDumpList2String( unitParameters , ",") + "\n";
        reportString += "http_ownername: " + llList2String( unitParameters, 0) + " --- ownername: " + llKey2Name( llGetOwner() ) + "\n";
        reportString += "http_objectname: " + llList2String( unitParameters, 1)+ " --- objectname: " + llGetObjectName()  + "\n";
        reportString += "http_objectkey: " + llList2String( unitParameters, 2)+ " --- objectkey: " + (string)llGetKey() + "\n";
        reportString += "http_ownerkey: " + llList2String( unitParameters, 3)+ " --- ownerkey: " + (string)llGetOwner() + "\n";
        reportString += "http_region: " + llList2String( unitParameters, 4)+ " --- region: " + llGetRegionName() + "\n";
        reportString += "http_location: " + llList2String( unitParameters, 5)+ " --- location: " + (string)llGetLocalPos() + "\n";
    }
 
    //AddUnitReport()
    //send to Coordinator on the broadcastChannel the selected report
    //format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
    llSay( broadcastChannel, "AddUnitReport::unitKey::" + (string)llGetKey() + "::Report::" + reportString);
 
}
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   errorcheck
//////////
//////////      Input:      the body of a http response
//////////                    
//////////      Output:     0 -> error detected
//////////                  1 -> no error in message
//////////                    
//////////      Purpose:    This function is used to test the body of a http response for a 
//////////                  error message
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
integer errorcheck(string message)
{
 if(debug > 1){llSay(debugChannel, llGetScriptName()+ ":errorcheck ");}
 
        //if the word "error" is found in the body
        if( llSubStringIndex( llToLower(message), "error" ) != -1)
        {
            llSay(0, message);
            //return an error detected reponse
            return 0;
        }
        //if the "404 not found" is present in the body
        if( llSubStringIndex( llToLower(message), "404 not found" ) != -1)
        {
            llSay(0, message);
            //return an error detected response
            return 0;
        }
 
        //if it made it past the error detection test, return a passed response 
        return 1;
}
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   HttpVerification
//////////
//////////      Input:      the body of a http response
//////////                    
//////////      Output:     sends pass/fail message out via linked message
//////////                  
//////////                    
//////////      Purpose:    This function is used to verify the body of a http response 
//////////                  
//////////                    
//////////      Issues:     format and float precision issue with the location compare 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
HttpVerification(string message)
{
 
    //expect a :: separated list of the information transmitted in the http headers
    //format example -> ownername::objectname::objectkey::ownerkey::region::location
 
    // dump string parameters into usable list
    list unitParameters= llParseString2List( message, ["::"], [""]);
 
    //initialize a status variable
    string passIndication = "PASS";
 
    //ownername
    if(  llList2String( unitParameters, 0) != llKey2Name( llGetOwner() )  )
    {
       passIndication = "FAIL";    
       if(debug > 1){llSay(debugChannel, llGetScriptName()+ ":GetOwnerFAIL ");}
    }
 
    //objectname
    if(  llList2String( unitParameters, 1) != llGetObjectName()  )
    {
       passIndication = "FAIL";    
       if(debug > 1){llSay(debugChannel, llGetScriptName()+ ":GetObjectNameFAIL ");}
    }
 
    //objectkey
    if(  llList2String( unitParameters, 2) != (string)llGetKey()  )
    {
       passIndication = "FAIL";  
       if(debug > 1){llSay(debugChannel, llGetScriptName()+ ":GetKeyFAIL ");}  
    }
 
    //ownerkey
    if(  llList2String( unitParameters, 3) != (string)llGetOwner()   )
    {
       passIndication = "FAIL";    
       if(debug > 1){llSay(debugChannel, llGetScriptName()+ ":GetOwnerFAIL ");}
    }
 
    //region
    //because the response contains the region grid numbers,
    //you can not do a direct compare
    if( llSubStringIndex( llList2String( unitParameters, 4), llGetRegionName() ) == -1)
    {
       passIndication = "FAIL";    
    }
 
    //location
//    if(  llList2String( unitParameters, 5) != (string)llGetLocalPos()  )
//    {
//       passIndication = "FAIL";    
//    }    
 
     //One of the following messages needs to be sent at the end of this function
     //each time that it is run
     //
     // llMessageLinked(LINK_SET, passFailChannel, "PASS", NULL_KEY);
     // llMessageLinked(LINK_SET, passFailChannel, "FAIL", NULL_KEY);
     llMessageLinked(LINK_SET, passFailChannel, passIndication, NULL_KEY);
 
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
 
   llSetText( "Communication_Http", <255,255,255>, 1);
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
        if(debug > 1)llSay(debugChannel, llGetScriptName()+ "->link_message: " + message);
 
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
       // if(debug > 10){llSay(debugChannel, llGetScriptName()+ ":FUNCTION: DEFAULT HTTP");}
       // if(debug > 10){llSay(debugChannel, body);}
 
        //if it was a call to setup
        if(id == HTTP_CONTROL)
        {    //if we can pass the error check
             if( errorcheck(body) )
             { 
                 //expect a :: separated list of the information transmitted in the http headers
                 //format example -> ownername::objectname::objectkey::ownerkey::region::location
                 HttpVerification( body );
 
                 http_body = body; 
             } 
        }
 
    } // end http_response
 
 
} // end default