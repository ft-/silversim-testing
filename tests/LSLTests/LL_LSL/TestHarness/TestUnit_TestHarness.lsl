///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestHarness
///////             
///////       
///////
///////  This is the interface script that talks to the Coordinator. It should be included
///////  in each TestUnit. It communicates with the TestScripts via linked message. 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestHarness    .1 -> initial framework  6.23.2007
//TestUnit_TestHarness    .2 -> testing and minor bugfixes  7.2.2007
 
 
//////////////////////////////////////////////////////////////////////////////////////
//  General Specification for TestUnit from https://wiki.secondlife.com/wiki/LSLTest
//////////////////////////////////////////////////////////////////////////////////////
 
//Each test unit will have a small amount of boiler plate code to talk to the controller and understand the communication enough to change to another channel, start tests, and report test results.
//[edit] Requirements
 
//    * All test units reset to a default test broadcast channel of 0.
//    * All test units will be in a test group and the unit knows the group name.
//    * All test units have a name.
//    * Register to a coordinator on announcement of registration.
//    * Change communication channel on request of the coordinator
//    * Run its tests on the request of the coordinator
//    * Report test results with multiple levels of output
//          o verbose: everything
//          o normal: failures and summary information
//          o quiet: summary information only 
 
 
//////////////////////////////////////////////////////////////////////////////////////
//
//                  Command Protocol
//
//////////////////////////////////////////////////////////////////////////////////////
//
//   All commands, input,output,chat, or linked message will be :: separated 
//   lists in string form.
//
//////////////////////////////////////////////
//        CHAT commands
//////////////////////////////////////////////
//
//  Chat commands will be on the specified controlChannel
//
//////// INPUT ///////////
//
//    The first element of a chat command can be only one of three things. 
//   
//   ALL - A general indicator for all testUnits to process the command
//   unitName - The specific name of this unit
//   groupName - The specific name of the group this unit belongs to
//
//   This first element of the chat input is parsed, and the remainder of the command
//   is sent to the parseCommand function. The follow are the current input commands   
//
////
//   Reset - sends link message toAllChannel calling for reset, and then resets this script
//   format example -> ALL::Reset
//
//   RegisterUnit - sends out chat on controlChannel unit registration information
//   format example -> ALL::RegisterUnit
//
//   RunTest - clears passFail and sends out linked message to TestScript 
//   format example -> ALL::RunTest
//
//   Report - sends out linked message to TestScript with broadcastChannel information
//   format example -> ALL::Report
//
//   SetReportType - sends out linked message to TestScript with Report Level change
//   format example -> ALL::SetReportType::NORMAL
//
//   UpdateUnitStatus - sends out unit status information on chat controlChannel 
//   format example -> ALL::UpdateUnitStatus
//
//   SetControlChannel - changes controlChannel to value provided
//   format example -> ALL::SetControlChannel::-1234
//
//
//////// OUTPUT ///////////
//   
// Registration - Response to RegisterUnit command, send registration information to Coordinator
// format example -> Registration::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
//
// UpdateUnitStatus - Response to request to send out unit status information
// format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
//
//
//
//////////////////////////////////////////////
//        LINK MESSAGE commands
//////////////////////////////////////////////
//
//  link message commands will be sent out on the toAllChannel, and recieved on the passFailChannel
//
//////// INPUT ///////////
//
//  passFail - status of test sent by TestScript
//  format example -> PASS
//
//////// OUTPUT ///////////
//
//  RunTest - activation command to start test
//  format example -> RunTest
//
//  Reset - calls for script resets on toAllChannel
//  format example -> Reset
//
//  Report - sends channel and report type to TestScript
//  format example -> Report::controlChannel::0::reportType::NORMAL
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
 
 
 
// Global Variables
 
integer toAllChannel = -255;           // general channel - linked message
integer passFailChannel = -355;        // test scripts channel for communicating pass/fail - linked message
 
integer controlChannel = 1234;            // command communication channel - chat
 
integer controlChannelListen;          // handler for the listener event
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
integer notecardLines;                 //
key notecardRequestKey;                // notecard globals 
key notecardLineRequest;               // notecard stores UnitName and GroupName
integer currentNoteLine;               //
 
string groupName = "UNKNOWN";          // name of TestUnit group, used by Coordinator 
                                       // to select multiple units simultaneously 
 
string unitName = "UNKNOWN";           // name of this TestUnit, used by Coordinator 
                                       // to select this unit specifically 
 
string reportType = "NORMAL";          // determines length and content of report type
                                       // NORMAL - failures and summary information
                                       // QUITE - summary information only
                                       // VERBOSE - everything
 
string passFail = "FAIL";              // current status of test script
                                       // FAIL - default until successful completion of test
                                       // PASS - successful test completed
 
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ParseCommand
//////////
//////////      Input:      string message - command to be parsed
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function calls various other functions or sets globals
//////////                    depending on message string. Allows external command calls
//////////                  from chat controlChannel
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
        //broadcast to other scripts reset command
        llMessageLinked(LINK_SET, toAllChannel, "reset", NULL_KEY); 
        //reset this script as well 
        llResetScript();                   
    }
 
    //RegisterUnit()
    else if(message == "RegisterUnit")
    {
        //Example output on controlChannel -> Registration::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
        llSay(controlChannel, "Registration::unitKey::" + (string)llGetKey() 
                                         + "::unitName::" + unitName
                                         + "::groupName::" + groupName);    
 
        llMessageLinked(LINK_SET, toAllChannel, "Registered", NULL_KEY);
 
    }
 
    //RunTest()
    else if(message == "RunTest")
    {
        //send link message on general channel initiating Test Scripts
        llMessageLinked(LINK_SET, toAllChannel, "RunTest", NULL_KEY);
    }
 
    //Report()
    else if(message == "Report")
    {
        //send link message on general channel initiating report function from Test Scripts
        //Example output on toAllChannel channel -> Report::controlChannel::0::reportType::NORMAL
        llMessageLinked(LINK_SET, toAllChannel, "Report::controlChannel::" + (string)controlChannel
                                             + "::reportType::" + reportType, NULL_KEY);
    }
    else if(llSubStringIndex(message, "SetReportType::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global reportType variable
        reportType = llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
    }    
    // UpdateUnitStatus()
    else if(message == "UpdateUnitStatus")
    {
        //send chat message on control channel updating Coordinator with unit test status
        //Example output on control channel -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
        llSay(controlChannel, "UpdateUnitStatus::unitKey::" + (string)llGetKey() + "::unitStatus::" + passFail );        
    }   
 
    //SetControlChannel()
    //example SetControlChannel::1
    else if(llSubStringIndex(message, "SetControlChannel::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global controlChannel variable
        controlChannel = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
        ActivateUnit();
    }
 
} //end ParseCommand
 
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
    //initiate data server call to begin reading notecard
    notecardRequestKey = llGetNumberOfNotecardLines("TestUnit_nc");
}
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ActivateUnit
//////////
//////////      Input:      no input parameters
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function activates any variables or functions necessary to get
//////////                  us running
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
ActivateUnit()
{
    //remove possible listeners
    llListenRemove(controlChannel);
    //create new listeners
    controlChannelListen = llListen(controlChannel,"",NULL_KEY,"");
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
//  Listen of default state                          //
///////////////////////////////////////////////////////
    listen(integer channel, string name, key id, string message)
    {
 
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":listen:" + message);}
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":channel:" + (string)channel);}
 
    //from Coordinator
    if(channel == controlChannel)
    {
        //parse :: separated list
        list commandCheck = llParseString2List( message, ["::"], [""] );
 
        //expect ALL, groupName, or unitName before first ::
        //Example ALL::RegisterUnit
        if( ( llList2String( commandCheck, 0 ) == "ALL" )
         || ( llList2String( commandCheck, 0 ) == groupName )
         || ( llList2String( commandCheck, 0 ) == unitName ) )
         {
             //send message to ParseCommand
                         //in string format
                                             // leave out variable before first ::
                                                                         //and send rest of message to last list element
                                             // put it back together with a :: separators
            ParseCommand(llDumpList2String( 
                                            llList2List(commandCheck, 1, 
                                                                         llGetListLength(commandCheck) - 1 ) 
                                           , "::" )
                         ); //end parseCommand
         } // end || if   
    } //end channel if
 
} //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of default state                    //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == passFailChannel)
        {
            //update the passFail status with incoming message
            passFail = message;
            //notify Coordinator of new status for this unit
            ParseCommand("UpdateUnitStatus");
 
        }
 
    } //end of link message
 
///////////////////////////////////////////////////////
//  data server of default                           //
///////////////////////////////////////////////////////   
    dataserver(key queryid, string data)
    {
        if(queryid == notecardRequestKey) // line number request
        {
            notecardLines = (integer)data;
            currentNoteLine = 0;
            notecardLineRequest = llGetNotecardLine("TestUnit_nc", currentNoteLine);
        } //end line number request
 
        if(queryid == notecardLineRequest) //reading a line from the notecard
        {
            // if the string "UnitName:" exists in the data string, parse the unitName from between the []
            if(llSubStringIndex(data,"UnitName:") > -1)
            {
                unitName = llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);                
            }
            // if the string "GroupName:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"GroupName:") > -1)
            {
                groupName = llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            }            
 
            //if additional lines on the notecard
            if(currentNoteLine < notecardLines - 1)
            {
                currentNoteLine += 1;
                //initiate another line request from the dataserver
                notecardLineRequest = llGetNotecardLine("TestUnit_nc", currentNoteLine);
            }else
            {
                // Done setting up, turn on listener
                ActivateUnit();
            }
 
        } //end line request
    } //end data server
 
 
 
 
} // end default