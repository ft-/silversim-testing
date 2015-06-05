///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            Controller_Controller
///////             
///////       
///////
///////  This is the main script of the controller. It manages communication with
///////  the coordinator, controlling the user interface. 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//Controller_Controller    .1 -> initial framework  7.01.2007
 
 
////////////////////////////////////////////////////////////////////////////////////////
//  General Specification for Controller from https://wiki.secondlife.com/wiki/LSLTest
////////////////////////////////////////////////////////////////////////////////////////
 
 //   *  The controller must be separate from the coordinator -- not the same script, nor required to be in the same linked set.
 //   * The controller does not need to be a single lsl script.
 //   * The controller must provide avatar controller interface.
 //         o An avatar can control the tests through a series of dialog menus or prompted typed commands.
 //         o The controller should activate when an avatar touches it.
 //         o There should be a shiny red button inviting the touch. :) 
 //   * The controller knows enough of the coordinator protocol to initiate a set of tests.
 //   * The controller must be able to initiate:
 //         o A single test unit
 //         o A test group
 //         o All tests 
 //   * The controller can assume that only one coordinator is available to run tests
 //   * The controller should allow specification of test results endpoint:
 //         o llSay on channel 0
 //         o email to a specified address
 //         o http 
 
 
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
//////// OUTPUT ///////////
//   
//   Reset - sends message to test units calling for a reset of test units
//   format example -> ALL::Reset
//
//   ActivateRegistration - initate the registration process
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
//////////////////////////////////////////////
//        LINK MESSAGE commands
//////////////////////////////////////////////
//
//  link message commands will be sent out and received on the toAllChannel
//
//////// INPUT ///////////
//
// 
//
//////// OUTPUT ///////////
//
// Reset - resets this script
// format example -> Reset
//
// RegistrationComplete - notification that the registration process is complete
// format example -> RegistrationComplete
//
// TestComplete - notification that the test phase is complete
// format example -> TestComplete
//
// ReportComplete - notification that the report phase is complete 
// format example -> ReportComplete
// 
// 
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
 
 
 
// Global Variables
 
integer toAllChannel = -255;                    // general channel - linked message
 
integer broadcastChannel = 0;                   // report broadcast channel - chat
integer controlChannel = 1234;                  // command communication channel - chat
 
integer controlChannelListen;                   // handler for the listener event
 
integer debug = 0;                              // level of debug message
integer debugChannel = DEBUG_CHANNEL;           // output channel for debug messages
 
string testSelected = "ALL";                    // specifies what units to test. ALL, a specific unitName, or a group
 
string reportType = "NORMAL";                   // determines length and content of report type
                                                // NORMAL - failures and summary information
                                                // QUITE - summary information only
                                                // VERBOSE - everything
 
string reportMethod = "CHAT::channel::0";       // determines output method of report
                                                // CHAT::channel::0
                                                // EMAIL::address::you@lindenlabs.com
                                                // HTTP::url::www.yoururl.com
 
 
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
        //notify to all system objects reset command
        llSay( controlChannel , "reset" );
        llSay( controlChannel , "ALL::reset" );
        //broadcast to other scripts reset command
        llMessageLinked(LINK_ALL_OTHERS, toAllChannel, "reset", NULL_KEY); 
        //reset this script as well 
        llResetScript();                   
    }
 
     //   RegistrationComplete()
     //   format example -> RegistrationComplete
     else if(message == "RegistrationComplete")
     {
         //notify other scripts
         llMessageLinked(LINK_ALL_OTHERS, toAllChannel, "RegistrationComplete", NULL_KEY);
     }
 
     //   TestComplete()
     //   format example -> TestComplete
     else if(message == "TestComplete")
     {
         //notify other scripts
         llMessageLinked(LINK_ALL_OTHERS, toAllChannel, "TestComplete", NULL_KEY);
     }
 
     //   ReportComplete()
     //   format example -> ReportComplete
     else if(message == "ReportComplete")
     {
         //notify other scripts
         llMessageLinked(LINK_ALL_OTHERS, toAllChannel, "ReportComplete", NULL_KEY);
     }
 
     //   ActivateRegistration()
     //   format example -> ActivateRegistration
     else if(message == "ActivateRegistration")
     {
         //notify coordinator
         llSay( controlChannel, "ActivateRegistration");
     }
 
     //   SetTestSelected()
     //   format example -> SetTestSelected::ALL
     else if(llSubStringIndex(message, "SetTestSelected::ALL") != -1)
     {
         //set testSelected variable
         testSelected = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
         //notify coordinator
         llSay( controlChannel, message);
     }
     //   broadcastChannelButton()
     //   format example -> broadcastChannelButton
     else if(llSubStringIndex(message, "broadcastChannelButton") != -1)
     {
         //send out instructions
         llSay(0, "**************************************************************************************************************************");
         llSay(0, "To Select a broadcastChannel ...");
         llSay(0, "Copy from your chat history the line between the \" \" below, paste it into the chat bar, and replace the VALUE with the channel to be used.");
         llSay(0, " \"/" + (string)controlChannel + " SetBroadcastChannel::VALUE\"");
         llSay(0, "**************************************************************************************************************************");
     }
 
     //   controlChannelButton()
     //   format example -> controlChannelButton
     else if(llSubStringIndex(message, "controlChannelButton") != -1)
     {
         //send out instructions
         llSay(0, "**************************************************************************************************************************");
         llSay(0, "To Select a controlChannel ...");
         llSay(0, "Copy from your chat history the line between the \" \" below, paste it into the chat bar, and replace the VALUE with the channel to be used.");
         llSay(0, " \"/" + (string)controlChannel + " SetControlChannel::VALUE\"");
         llSay(0, "**************************************************************************************************************************");
     }
 
     //   TestSelectedButton()
     //   format example -> SetTestSelected::GROUP
     else if(llSubStringIndex(message, "TestSelectedButton::") != -1)
     {
         //send out instructions
         llSay(0, "**************************************************************************************************************************");
         llSay(0, "To Select a " + llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 ));
         llSay(0, "Copy from your chat history the line between the \" \" below, paste it into the chat bar, and replace the NAME with the  " 
                         +  llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 ) + "Name to be tested.");
         llSay(0, " \"/" + (string)controlChannel + " SetTestSelected::NAME\"");
         llSay(0, "**************************************************************************************************************************");
     }
 
     //   ReportMethodButton()
     //   format example -> ReportMethodButton::EMAIL
     else if(llSubStringIndex(message, "ReportMethodButton::") != -1)
     {
         //parse from the message the method selected
         string method = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
         //send out instructions
         llSay(0, "**************************************************************************************************************************");
         llSay(0, "To select " + method + " as the report method");
         llSay(0, "Copy from your chat history the line between the \" \" below, paste it into the chat bar, and replace ");
 
         if( method == "CHAT")
         {
           llSay( 0, "ENTER_VALUE with the broadcast channel you would like to use.");
           llSay(0, " \"/" + (string)controlChannel + " SetReportMethod::CHAT::channel::ENTER_VALUE\"");
         }
 
         else if( method == "EMAIL")
         {
           llSay( 0, "ENTER_VALUE with the email address you would like to use.");
           llSay(0, " \"/" + (string)controlChannel + " SetReportMethod::EMAIL::address::ENTER_VALUE\"");
         }   
 
         else if( method == "HTTP")
         {
           llSay( 0, "ENTER_VALUE with the url you would like to use.");
           llSay(0, " \"/" + (string)controlChannel + " SetReportMethod::HTTP::url::ENTER_VALUE\"");
         }
 
         llSay(0, "**************************************************************************************************************************");
     }
 
     //   SetControlChannel()
     //   format example -> SetControlChannel::-1234
     else if(llSubStringIndex(message, "SetControlChannel::") != -1)
     {
         //notify coordinator
         llSay( controlChannel, message);
 
         //set controlChannel variable
         controlChannel = (integer)llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
         //reset listener
         Initialize();
     }
 
     //   SetBroadcastChannel()
     //   format example -> SetBroadcastChannel::0
     else if(llSubStringIndex(message, "SetBroadcastChannel::") != -1)
     {
         //set boradcastChannel variable
         broadcastChannel = (integer)llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
         //notify coordinator
         llSay( controlChannel, message);
     }
 
     //   ActivateTest()
     //   format example -> ActivateTest
     else if(message == "ActivateTest")
     {
         //notify coordinator
         llSay( controlChannel, "ActivateTest");
     }
 
     //   SetReportType()
     //   format example -> SetReportType::NORMAL
     else if(llSubStringIndex(message, "SetReportType::") != -1)
     {
         //set reportType variable
         reportType = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
         //notify coordinator
         llSay( controlChannel, message);
     }
 
     //   SetReportMethod()
     //   format example -> SetReportMethod::CHAT::channel::0
     //                  -> SetReportMethod::EMAIL::address::you@lindenlabs.com
     //                  -> SetReportMethod::HTTP::url::www.yoururl.com
     else if(llSubStringIndex(message, "SetReportMethod::") != -1)
     {
         //set reportMethod variable
         reportMethod = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
         //notify coordinator
         llSay( controlChannel, message);
     }
 
     //   ActivateReport()
     //   format example -> ActivateReport
     else if(message == "ActivateReport")
     {
         //notify coordinator
         llSay( controlChannel, message);
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
 
    //from coordinator
    if(channel == controlChannel)
    {
 
       //if it is a status indication from the coordinator
       if( (llSubStringIndex( message, "Complete" ) != -1) || (llSubStringIndex( message, "SetControlChannel" ) != -1) )
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
            ParseCommand( message );
 
        }
 
    } //end of link message
 
 
} // end default