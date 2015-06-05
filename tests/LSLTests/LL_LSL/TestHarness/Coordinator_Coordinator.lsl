///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            Coordinator_Coordinator
///////             
///////       
///////
///////  This is the main script of the coordinator. It manages the states of the
///////  coordinator, and communicates with both the controller and test units. 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//Coordinator_Coordinator    .1 -> initial framework  7.01.2007
//Coordinator_Coordinator    .2 -> bug fixing and testing  7.8.2007
//Coordinator_Coordinator    .3 -> added reportTimeoutLength  7.11.2007
 
 
////////////////////////////////////////////////////////////////////////////////////////
//  General Specification for Coordinator from https://wiki.secondlife.com/wiki/LSLTest
////////////////////////////////////////////////////////////////////////////////////////
 
//    *  There should be no need to link any new objects, including test units, to the coordinator.
//    * There is no need for secure communication between test objects and the coordinator.
//    * The coordinator does not need to be a single lsl script.
//    * The coordinator will announce the ability for nearby test units to register.
//    * Once a test session has begun, the coordinator will not accept any more registrations.
//    * The coordinator can start a group of tests and collect results.
//    * The coordinator can start a single test unit and collect results.
//    * The coordinator can start all known tests.
//    * The coordinator will have a variable test broadcast channel.
//    * The coordinator will accept commands from its controller and broadcast appropriate commands to all, groups, or individual test units.
//    * Collect reported results from registered test units.
//          o All pass/fail messages by group and test unit.
//          o All late registrations. 
//    * It should be possible for an agent or another script to control the coordinator.
//    * Every test session will record a start time, test location,
//    * The collection of all tests which failed the last test session is an ad-hoc test group known only to the controller. For example we run all tests in 'quiet mode' and then all failures are treated as a new group which allows 'verbose mode' testing individually or all at once.
//    * If results are filling memory, the coordinator should output information prematurely, but needs to keep statistical totals as well as the known failed group.
//    * Every time summary or transient results are output, the session id, and current time are included in the output.
//    * The coordinator must know by the end of a test session and be able to report on:
//          o Which test units passed
//          o Which test units failed
//          o Which test units failed to report before timeout. 
//    * At the end of every test session it is possible to get a summary from the coordinator.
//    * The summary reports:
//          o Number of test units in the last session
//          o Number of passed test units.
//          o Number of failed test units.
//          o Number of test units which failed to report before timeout. 
 
 
 
//////////////////////////////////////////////////////////////////////////////////////
//
//                  Command Protocol
//
//////////////////////////////////////////////////////////////////////////////////////
//
//   All commmands, input,output,chat, or linked message will be :: separated 
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
//   Registration - response from testUnits to RegisterUnit command
//   format example -> Registration::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
//
//   UpdateUnitStatus - response from testUnits to request to send out unit status information
//   format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
//
//   ActivateRegistration - initiate the registration process
//   format example -> ActivateRegistration
//
//   SetTestSelected - specify test to be run. ALL, a specific unitName, or a groupName            
//   format example -> SetTestSelected::ALL
//
//   SetControlChannel - channel for chat communication among elements in the system 
//   format example -> SetControlChannel::-1234
//
//   SetBroadcastChannel - chat channel to output reports on 
//   format example -> SetBroadcastChannel::0
//
//   SetRegTimeoutLength - registration time limit
//   format example -> SetRegTimeoutLength::10
//
//   SetTestTimeoutLength - test time limit
//   format example -> SetTestTimeoutLength::10
//
//   SetReportTimeoutLength - report time limit
//   format example -> SetReportTimeoutLength::10
//
//   ActivateTest - command to begin testing process
//   format example -> ActivateTest
//
//   SetReportType - specify report type. NORMAL, QUITE, VERBOSE, STATS
//   format example -> SetReportType::NORMAL
//
//   SetReportMethod - CHAT, EMAIL, HTTP
//   format example -> SetReportMethod::CHAT::channel::0
//                  -> SetReportMethod::EMAIL::address::you@lindenlabs.com
//                  -> SetReportMethod::HTTP::url::www.yoururl.com
//
//   ActivateReport - initiate the report process
//   format example -> ActivateReport
//
//////// OUTPUT ///////////
//
//   Reset - sends message to test units calling for a reset of test units
//   format example -> ALL::Reset
//
//   RegisterUnit - sends out chat on controlChannel requesting unit registration information
//   format example -> ALL::RegisterUnit
//
//   RunTest - message to specified test units starting tests in test units
//   format example -> ALL::RunTest
//
//   Report - initiating request for full report from test units
//   format example -> ALL::Report
//
//   UpdateUnitStatus - chat message to initiate status update from test units
//   format example -> ALL::UpdateUnitStatus
//
//   SetControlChannel - changes controlChannel to value given
//   format example -> ALL::SetControlChannel::-1234
//
//   RegistrationComplete - notification that the registration process is complete
//   format example -> RegistrationComplete
//
//   TestComplete - notification that the test phase is complete
//   format example -> TestComplete
//
//   ReportComplete - notification that the report phase is complete 
//   format example -> ReportComplete
//
//
//////////////////////////////////////////////
//        LINK MESSAGE commands
//////////////////////////////////////////////
//
//  link message commands will be sent out and received on the toAllChannel
//
//////// INPUT ///////////
//
// ReportRequest - request from Coordinator_TestUnitReports to generate a chat request for a test unit report
// format example -> ReportRequest::unitName
//
// ReportComplete - notification from Coordinator_TestUnitReports script that reporting is done
// format example -> ReportComplete
//
//////// OUTPUT ///////////
//
// Reset - resets this script
// format example -> Reset
//
// ClearAll - empties all lists 
// format example -> ClearAll
//
// AddUnitToList - provides unit information of newly registered unit to Coordinator_TestUnits 
// format example -> AddUnitToList::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
//
// UpdateUnitStatus - provides unit status information to Coordinator_TestUnits
// format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
//
// ReportUnitStats - initiates a ReportStats output from Coordinator_TestUnits
// format example -> ReportUnitStats
//
// RequestUnitCount - request for number of units registered from Coordinator_TestUnits
// format example -> RequestUnitCount
//
// OutputReports - generates report dump for whatever collection specified
// format example -> OutputReports::testSelected::ALL::controlChannel::-1234::broadcastChannel::1
//
// SetReportMethod - provides report output parameter to Coordinator_TestUnitsReports
// format example -> SetReportMethod::CHAT::channel::0
//                -> SetReportMethod::EMAIL::address::you@lindenlabs.com
//                -> SetReportMethod::HTTP::url::www.yoururl.com
//
// SetReportType - provides type of Report desired to Coordinator_TestUnitsReports
// format example -> SetReportType::NORMAL
//
// SetTestSelected - provides type of test selected to Coordinator_TestUnitsReports
// format example -> SetTestSelected::ALL
//
//   SetReportTimeoutLength - report time limit
//   format example -> SetReportTimeoutLength::10
//
// AddUnitReport -  update to Coordinator_TestUnitsReports
// format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
// 
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
 
 
 
// Global Variables
 
integer toAllChannel = -255;           // general channel - linked message
 
integer broadcastChannel = 0;          // report broadcast channel - chat
integer controlChannel = 1234;        // command communication channel - chat
 
integer controlChannelListen;          // handler for the listener event
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
integer notecardLines;                 //
key notecardRequestKey;                // notecard globals 
key notecardLineRequest;               // notecard stores UnitName and GroupName
integer currentNoteLine;               //
 
string testSelected = "ALL";           // specifies what units to test. ALL, a specific unitName, or a group
 
string reportType = "NORMAL";          // determines length and content of report type
                                       // NORMAL - failures and summary information
                                       // QUITE - summary information only
                                       // VERBOSE - everything
 
string reportMethod = "CHAT";
 
integer startTime = 0;                  // time that the tests were run, with llGetUnixTime()
integer regTimeoutLength = 0;           // time in seconds to allow for registration
integer testTimeoutLength = 0;          // time in seconds to allow for testing  
integer reportTimeoutLength = 0;        // report in seconds to allow for reporting  
 
string emailAddress;                   // an email address for report output if EMAIL is the selected report method
string httpUrl;                        // an website url for report output if HTTP is the selected report method          
 
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ParseCommand
//////////
//////////      Input:      string message - command to be parsed
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function calls various other functions or sets globals
//////////                  depending on message string. Allows external command calls
//////////                  from chat controlChannel and linked messages
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
 
        llSay(0, "Coordinator Reset"); 
 
        //reset this script as well 
        llResetScript();      
 
    }
 
    //SetBroadcastChannel()
    //format example -> SetBroadcastChannel::1
    else if(llSubStringIndex(message, "SetBroadcastChannel::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global broadcastChannel variable
        broadcastChannel = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "broadcastChannel now set to: " + (string)broadcastChannel);
 
        //relay information to Coordinator_TestUnitsReports script
        llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);
    }  
 
    //SetControlChannel()
    //format example -> SetControlChannel::1
    else if(llSubStringIndex(message, "SetControlChannel::") != -1)
    {
        //notify specified test units of change
        llSay(controlChannel, testSelected + "::" + message);
 
        //parse value from string by deleting message up to index of ::
        //and set global controlChannel variable
        controlChannel = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "controlChannel now set to: " + (string)controlChannel);
 
        //update listener
        ActivateCoordinator();
 
    }
 
    //SetRegTimeoutLength()
    //format example -> SetRegTimeoutLength::10
    else if(llSubStringIndex(message, "SetRegTimeoutLength::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global regTimeoutLength variable
        regTimeoutLength = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "regTimeoutLength now set to: " + (string)regTimeoutLength);
    }
 
     //SetReportTimeoutLength()
    //format example -> SetReportTimeoutLength::10
    else if(llSubStringIndex(message, "SetReportTimeoutLength::") != -1)
    {
        //parse value from string by deleting message up to index of ::
        //and set global reportTimeoutLength variable
        reportTimeoutLength = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "reportTimeoutLength now set to: " + (string)reportTimeoutLength);
 
 
        //relay information to Coordinator_TestUnitsReports script
        llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);
    }
 
    //SetTestTimeoutLength()
    //format example -> SetTestTimeoutLength::10
    else if(llSubStringIndex(message, "SetTestTimeoutLength::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global testTimeoutLength variable
        testTimeoutLength = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "testTimeoutLength now set to: " + (string)testTimeoutLength);
 
    }
 
    //SetTestSelected()         
    //format example -> SetTestSelected::ALL
    else if(llSubStringIndex(message, "SetTestSelected::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global testSelected variable
        testSelected = llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "testSelected now set to: " + (string)testSelected);
    }
 
    //SetReportType()
    //format example -> SetReportType::NORMAL
    else if(llSubStringIndex(message, "SetReportType::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global reportType variable
        reportType = llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //notify specified test units of change
        llSay(controlChannel, testSelected + "::" + message);
 
        //provide feedback on the change
        llSay( 0, "reportType now set to: " + (string)reportType);
 
 
        //relay information to Coordinator_TestUnitsReports script
        llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);
    }
 
    //SetReportMethod()
    //format example -> SetReportMethod::CHAT::channel::0
    else if(llSubStringIndex(message, "SetReportMethod::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global reportMethod variable
        reportMethod = llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
 
        //provide feedback on the change
        llSay( 0, "reportMethod now set to: " + (string)reportMethod);
 
        //relay information to Coordinator_TestUnitsReports script
        llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);  
 
        if(llSubStringIndex( message, "CHAT") != -1)
        {
            //dump report method parameters into usable list
            list methodParameters = llParseString2List( reportMethod, ["::"], [""]);
 
               //pull channel from parameters list
            integer channel = (integer)llList2String( methodParameters, llListFindList( methodParameters, ["channel"]) + 1);
 
        }      
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
    notecardRequestKey = llGetNumberOfNotecardLines("Coordinator_nc");
}
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ActivateCoordinator
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
ActivateCoordinator()
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
 
    //from Controller
    if(channel == controlChannel)
    {
        if( message == "ActivateRegistration" )
        {
                         ////////////////////
                        //  State Change  //
                        //////////////////// 
           state REGISTRATION;    
        }
        else
        {
           //not a state specific command, so send to general command parse
           ParseCommand ( message );
        }
    }
 
} //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of default state                    //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            //not a state specific command, so send to general command parse
            //ParseCommand( message );
 
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
            currentNoteLine = 1;
            notecardLineRequest = llGetNotecardLine("Coordinator_nc", currentNoteLine);
        } //end line number request
 
        if(queryid == notecardLineRequest) //reading a line from the notecard
        {
            // if the string "BroadcastChannel:" exists in the data string, parse the unitName from between the []
            if(llSubStringIndex(data,"BroadcastChannel:") > -1)
            {
                broadcastChannel = (integer)llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);                
            }
            // if the string "ControlChannel:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"ControlChannel:") > -1)
            {
                controlChannel = (integer)llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            }  
            // if the string "RegistrationTimeout:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"RegistrationTimeout:") > -1)
            {
                regTimeoutLength = (integer)llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            }    
            // if the string "TestTimeout:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"TestTimeout:") > -1)
            {
                testTimeoutLength = (integer)llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            } 
            // if the string "ReportTimeout:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"ReportTimeout:") > -1)
            {
                reportTimeoutLength = (integer)llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
                //send it on to Coordinator_TestUnitsReports
                llMessageLinked(LINK_SET, toAllChannel, "SetReportTimeoutLength::" + (string)reportTimeoutLength, NULL_KEY);
            }       
            // if the string "Email:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"Email:") > -1)
            {
                emailAddress = llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            }    
            // if the string "Http:" exists in the data string, parse the groupName from between the []
            else if(llSubStringIndex(data,"Http:") > -1)
            {
                httpUrl = llGetSubString(data, llSubStringIndex(data,"[") + 1,llSubStringIndex(data,"]") - 1);
            }              
 
            //if additional lines on the notecard
            if(currentNoteLine < notecardLines - 1)
            {
                currentNoteLine += 1;
                //initiate another line request from the dataserver
                notecardLineRequest = llGetNotecardLine("Coordinator_nc", currentNoteLine);
            }else
            {
                // Done setting up, turn on listener
                ActivateCoordinator();
            }
 
        } //end line request
    } //end data server
 
 
 
 
} // end default
 
 
///////////////////////////////////////////////////////////////////////////////////////
//STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE//
///////////////////////////////////////////////////////////////////////////////////////
//                                                                                   //
//                                                                                   //
//                          REGISTRATION STATE                                       //
//                                                                                   //
//                                                                                   //
///////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
state REGISTRATION
{
///////////////////////////////////////////////////////
//  State Entry of REGISTRATION state                //
///////////////////////////////////////////////////////
   state_entry()
    {
        //throw timer event for max registration time limit
        //simple implementation, will be at least regTimeoutLength
        //becuase of the potential for time dilation 
        llSetTimerEvent( regTimeoutLength );
 
        //setup listener for this state
        ActivateCoordinator();
 
        //clear lists
        llMessageLinked(LINK_SET, toAllChannel, "ClearAll", NULL_KEY);
 
        //send out registration request to testUnits
        llSay( controlChannel, "ALL::RegisterUnit");
 
    }
////////////////////////////////////////////////////////
//  On Rez of REGISTRATION state                      //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
                ////////////////////
                //  State Change  //
                //////////////////// 
           state default;    
    }
 
///////////////////////////////////////////////////////
//  Listen of REGISTRATION state                     //
///////////////////////////////////////////////////////
    listen(integer channel, string name, key id, string message)
    {
 
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":listen:" + message);}
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":channel:" + (string)channel);}
 
    //from Controller
    if(channel == controlChannel)
    {
        //   Registration()
        //   format example -> Registration::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
        if ( llSubStringIndex( message, "Registration::" ) != -1 )
        {
            // AddUnitToList()
            // format example -> AddUnitToList::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
            //first remove "Regsitration::", then add "AddUnitReport::", the link message to Coordinator_TestUnits
            llMessageLinked(LINK_SET, toAllChannel, "AddUnitToList::" 
                                                    + llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1)
                                                    , NULL_KEY);
 
        }
        else
        {
           //not a state specific command, so send to general command parse
           //ParseCommand ( message );
        }
    }
 
} //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of REGISTRATION state               //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            //not a state specific command, so send to general command parse
            ParseCommand( message );
 
        }
 
    } //end of link message
 
///////////////////////////////////////////////////////
//  timer of REGISTRATION state                      //
///////////////////////////////////////////////////////   
    timer()
    {
        //broadcast registration is complete
        llSay( controlChannel, "RegistrationComplete");
 
        //move to SETUP state
                        ////////////////////
                        //  State Change  //
                        //////////////////// 
               state SETUP;
 
    } //end of timer
 
} // end REGISTRATION
 
///////////////////////////////////////////////////////////////////////////////////////
//STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE//
///////////////////////////////////////////////////////////////////////////////////////
//                                                                                   //
//                                                                                   //
//                          SETUP STATE                                              //
//                                                                                   //
//                                                                                   //
///////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
state SETUP
{
///////////////////////////////////////////////////////
//  State Entry of SETUP state                       //
///////////////////////////////////////////////////////
   state_entry()
    {
 
        //   SetControlChannel()
        //   format example -> ALL::SetControlChannel::-1234
        llSay( controlChannel , "ALL::SetcontrolChannel::" + (string)controlChannel );
        ActivateCoordinator();
        //   SetReportType()
        //   format example -> SetReportType::NORMAL
        llSay( controlChannel, "SetReportType::" + reportType );
        llMessageLinked(LINK_SET, toAllChannel, "SetReportType::" + reportType, NULL_KEY);
        //set report time out
        llMessageLinked(LINK_SET, toAllChannel, "SetReportTimeoutLength::" + (string)reportTimeoutLength, NULL_KEY);
 
        //   SetReportMethod - CHAT, EMAIL, HTTP
        //   format example -> SetReportMethod::CHAT::channel::0
        //                  -> SetReportMethod::EMAIL::address::you@lindenlabs.com
        //                  -> SetReportMethod::HTTP::url::www.yoururl.com
        if(reportMethod == "CHAT")
        {
            //send to both testUnits via chat, and Coordinator_TestUnitsReports via linked message 
            llSay( controlChannel, "SetReportMethod::CHAT::channel::" + (string)broadcastChannel );
            llMessageLinked(LINK_SET, toAllChannel, "SetReportMethod::CHAT::channel::" + (string)broadcastChannel, NULL_KEY);
        }
        else if( reportMethod == "EMAIL")
        {
            //send to both testUnits via chat, and Coordinator_TestUnitsReports via linked message 
            llSay( controlChannel, "SetReportMethod::EMAIL::address::" + emailAddress );
            llMessageLinked(LINK_SET, toAllChannel, "SetReportMethod::EMAIL::address::" + emailAddress, NULL_KEY);
        }
        else if( reportMethod == "HTTP")
        {
            //look for the "http://" in the url
            if( llSubStringIndex( llToLower( httpUrl ), "http://") != -1)
            {
               //send to both testUnits via chat, and Coordinator_TestUnitsReports via linked message 
               llSay( controlChannel, "SetReportMethod::HTTP::url::" + httpUrl );
               llMessageLinked(LINK_SET, toAllChannel, "SetReportMethod::HTTP::url::" + httpUrl, NULL_KEY);
            }
            else
            {
               //send to both testUnits via chat, and Coordinator_TestUnitsReports via linked message 
               llSay( controlChannel, "SetReportMethod::HTTP::url::http://" + httpUrl );
               llMessageLinked(LINK_SET, toAllChannel, "SetReportMethod::HTTP::url::http://" + httpUrl, NULL_KEY);
            }
        }
 
 
        // initiate output of registered units
        // ReportUnitStats()
        // format example -> ReportUnitStats
        llMessageLinked(LINK_SET, toAllChannel, "ReportUnitStats", NULL_KEY);
 
    } // end state entry
 
////////////////////////////////////////////////////////
//  On Rez of SETUP state                             //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
                ////////////////////
                //  State Change  //
                //////////////////// 
           state default;    
    }
 
///////////////////////////////////////////////////////
//  Listen of SETUP state                            //
///////////////////////////////////////////////////////
    listen(integer channel, string name, key id, string message)
    {
 
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":listen:" + message);}
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":channel:" + (string)channel);}
 
    //from Controller
    if(channel == controlChannel)
    {
        //if a registration command comes it during SETUP, then it is a late registration 
        //   Registration()
        //   format example -> Registration::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
        if ( llSubStringIndex( message, "Registration::" ) != -1 )
        {
            // AddUnitToList()
            // format example -> AddUnitToList::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
            //first remove "Registration::", then add "AddUnitToList::", then link message to Coordinator_TestUnits
            llMessageLinked(LINK_SET, toAllChannel, "AddUnitToList::" 
                                                    + llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1)
                                                    , NULL_KEY);
 
            //parse string command into a parameter list
            list unitStatusParameters = llParseString2List( message, ["::"], [""] );
 
            //use variable name to find first variable of concern - unitKey
            integer commandIndex = llListFindList( unitStatusParameters, ["unitKey"] );
 
            //use index to pull the unitKey
            string unitKeyTemp = llList2String( unitStatusParameters, commandIndex + 1);
 
               // UpdateUnitStatus()
            // format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::LATE_REGISTRATION
            llMessageLinked(LINK_SET, toAllChannel, "UpdateUnitStatus::unitKey::" 
                                                      + unitKeyTemp 
                                                      + "::unitStatus::LATE_REGISTRATION"
                                                      , NULL_KEY);
 
        } // end if Registration
 
        //Controller indicates a run test command
        else if( message == "ActivateTest" )
        {
                         ////////////////////
                        //  State Change  //
                        //////////////////// 
           state TEST;    
        }
        else
        {
           //not a state specific command, so send to general command parse
           ParseCommand ( message );
        }
    }
 
    } //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of SETUP state                      //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            //  ReportStats - sends out unit information including status
            //  format example -> ReportStats::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1::unitStatus::PASS
            if ( llSubStringIndex( message, "ReportStats") != -1) 
            {
 
                 //output on control channel 
                 llSay( controlChannel, message );
 
                  // dump string parameters into usable list
                 list unitParameters = llParseString2List( message, ["::"], [""]);
 
                 //pull name, group and status from unitParameters
                 string name = llList2String( unitParameters, llListFindList( unitParameters, ["unitName"]) + 1);
                 string group = llList2String( unitParameters, llListFindList( unitParameters, ["groupName"]) + 1);
                 string status = llList2String( unitParameters, llListFindList( unitParameters, ["unitStatus"]) + 1);
                 string uKey = llList2String( unitParameters, llListFindList( unitParameters, ["unitKey"]) + 1);
 
 
                 //output for user
                 //after a bit of formatting
                 llSay( 0 , "****************UNIT:" + uKey + "***************************");
                 llSay( 0 , "NAME: " + name);
                 llSay( 0 , "GROUP: " + group);
                 llSay( 0 , "STATUS: " + status);
                 llSay( 0 , "****************UNIT:" + uKey + "***************************");
 
 
            }
            else
            {
               //not a state specific command, so send to general command parse
               //ParseCommand( message );
            }
 
        }
 
    } //end of link message
 
 
} // end REGISTRATION
 
///////////////////////////////////////////////////////////////////////////////////////
//STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE//
///////////////////////////////////////////////////////////////////////////////////////
//                                                                                   //
//                                                                                   //
//                          TEST STATE                                               //
//                                                                                   //
//                                                                                   //
///////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
state TEST
{
///////////////////////////////////////////////////////
//  State Entry of TEST state                        //
///////////////////////////////////////////////////////
   state_entry()
    {
        //throw timer event for max registration time limit
        //simple implementation, will be at least regTimeoutLength
        //because of the potential for time dilation 
        llSetTimerEvent( testTimeoutLength );
        ActivateCoordinator();
        if( testSelected == "FAILS")
        {
 
        }
 
        else
        {
           //send out test command to selected testUnits
           llSay( controlChannel, testSelected + "::RunTest");
        }
    }
////////////////////////////////////////////////////////
//  On Rez of TEST state                              //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
                ////////////////////
                //  State Change  //
                //////////////////// 
           state default;    
    }
 
///////////////////////////////////////////////////////
//  Listen of TEST state                             //
///////////////////////////////////////////////////////
    listen(integer channel, string name, key id, string message)
    {
 
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":listen:" + message);}
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":channel:" + (string)channel);}
 
    //from Controller
    if(channel == controlChannel)
    {
        // UpdateUnitStatus()
        // format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
        if ( llSubStringIndex( message, "UpdateUnitStatus::" ) != -1 )
        {
            //send on the Coordinator_TestUnits
            llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);
 
        }
        else
        {
           //not a state specific command, so send to general command parse
           ParseCommand ( message );
        }
    }
 
} //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of TEST state                       //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {   
            //if we are retesting failed units
            if( testSelected == "FAILS" )
            {
               //  ReportStats - sends out unit information including status
               //  format example -> ReportStats::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1::status::PASS
               if ( llSubStringIndex( message, "ReportStats") != -1) 
               {
 
                       // dump string parameters into usable list
                    list unitParameters = llParseString2List( message, ["::"], [""]);
 
                    //pull status from command string
                    string status = llList2String( unitParameters, llListFindList( unitParameters, ["unitStatus"]) + 1);
 
                    //if we find a failed unit
                    if ( status == "FAIL" )
                    {
                       //pull key from unitParameters
                       string uKey = llList2String( unitParameters, llListFindList( unitParameters, ["unitKey"]) + 1);
 
                       //call for a test run on this particular unit
                       llSay( controlChannel, uKey + "::RunTest" );
                    }
 
               }
            }
            else
            {
               //not a state specific command, so send to general command parse
               //ParseCommand( message );
            }
        }
 
    } //end of link message
 
///////////////////////////////////////////////////////
//  timer of TEST state                              //
///////////////////////////////////////////////////////   
    timer()
    {
        //broadcast test is complete
        llSay( controlChannel, "TestComplete");
        llSay( 0, "TestComplete");
 
        //move to REPORT state
                        ////////////////////
                        //  State Change  //
                        //////////////////// 
               state REPORT;
 
    } //end of timer
 
} // end TEST
 
///////////////////////////////////////////////////////////////////////////////////////
//STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE STATE//
///////////////////////////////////////////////////////////////////////////////////////
//                                                                                   //
//                                                                                   //
//                          REPORT STATE                                             //
//                                                                                   //
//                                                                                   //
///////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////
state REPORT
{
///////////////////////////////////////////////////////
//  State Entry of REPORT state                      //
///////////////////////////////////////////////////////
   state_entry()
    {
        //turn the listener back on for this state
        ActivateCoordinator();
    }
////////////////////////////////////////////////////////
//  On Rez of REPORT state                            //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
                ////////////////////
                //  State Change  //
                //////////////////// 
           state default;    
    }
 
///////////////////////////////////////////////////////
//  Listen of REPORT state                           //
///////////////////////////////////////////////////////
    listen(integer channel, string name, key id, string message)
    {
 
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":listen:" + message);}
     if(debug > 0){llSay(debugChannel, llGetScriptName()+ ":channel:" + (string)channel);}
 
    //from Controller
    if(channel == controlChannel)
    {
        //   ActivateReport()
        //   format example -> ActivateReport
        if ( message == "ActivateReport" )
        {
            //send on the Coordinator_TestUnits
            llMessageLinked(LINK_SET, toAllChannel, "OutputReports::testSelected::" + testSelected, NULL_KEY);
 
        }
        // AddUnitReport()
        // format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
        else if ( llSubStringIndex( message, "AddUnitReport" ) != -1 )
        {
            //send on the Coordinator_TestUnits
            llMessageLinked(LINK_SET, toAllChannel, message, NULL_KEY);
 
        }
        //Controller indicates a retest command
        else if( message == "ActivateTest" )
        {
                         ////////////////////
                        //  State Change  //
                        //////////////////// 
           state TEST;    
        }
        //Controller indicates a run test command
        else if( message == "ActivateRegistration" )
        {
                         ////////////////////
                        //  State Change  //
                        //////////////////// 
           state REGISTRATION;    
        }
        else
        {
           //not a state specific command, so send to general command parse
           ParseCommand ( message );
        }
    }
 
} //end of listen
 
///////////////////////////////////////////////////////
//  Link Message of REPORT state                     //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            // ReportRequest()
            // format example -> ReportRequest::unitName
            if(llSubStringIndex(message, "ReportRequest::") != -1)
            {
                  // controlChannel request to test units for a report
                  // format example -> 00000-0000-0000-00000::Report
                  llSay( controlChannel , llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1) + "::Report");
              }
              // ReportComplete()
            // format example -> ReportComplete
            else if( message == "ReportComplete" )
            {
               // notify controller
               llSay( controlChannel , "ReportComplete" );
            }
              else
              {
               //not a state specific command, so send to general command parse
               //ParseCommand( message );
              }
        }
 
    } //end of link message
 
 
} // end REPORT