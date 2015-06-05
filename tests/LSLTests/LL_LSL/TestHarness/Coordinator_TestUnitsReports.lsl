///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            Coordinator_TestUnitsReports
///////             
///////       
///////
///////  This is the coordinator script that maintains the record of testUnit reports, 
///////  and manages the collection and output of those reports. It should be   
///////  included in the coordinator.    
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//Coordinator_TestUnitReports    .1 -> initial framework  6.28.2007
//Coordinator_TestUnitReports    .2 -> bug fixing and testing  7.8.2007
//Coordinator_TestUnitReports    .3 -> added report timeout and report filter for TEST_TIMED_OUT units  7.11.2007
 
//////////////////////////////////////////////////////////////////////////////////////
//
//                  Command Protocol
//
//////////////////////////////////////////////////////////////////////////////////////
//
//   All commands will be :: separated 
//   lists in string form.
//
//////////////////////////////////////////////
//        LINK MESSAGE commands 
//////////////////////////////////////////////
//
//  link message commands will be received and sent on the toAllChannel
//
//////// INPUT ///////////
//
// Reset - resets this script
// format example -> Reset
//
// ClearAll - empties all lists 
// format example -> ClearAll
//
// OutputReports - generates report dump for whatever collection specified
// format example -> OutputReports::testSelected::ALL
//
// SetReportMethod - provides report output parameter
// format example -> SetReportMethod::CHAT::channel::0
//                -> SetReportMethod::EMAIL::address::you@lindenlabs.com
//                -> SetReportMethod::HTTP::url::www.yoururl.com
//
// SetReportType - provides type of Report desired
// format example -> SetReportType::NORMAL
//
// SetTestSelected - provides type of test selected
// format example -> SetTestSelected::ALL
//
// SetUnitCount - provides number of registered units
// format example -> SetUnitCount::1
//
//   SetReportTimeoutLength - report time limit
//   format example -> SetReportTimeoutLength::10
//
// AddUnitReport -  update to Coordinator on the chat broadcastChannel
// format example -> AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
// 
//////// OUTPUT ///////////
//
// ReportUnitStats - sends out request for unit stats
// format example -> ReportUnitStats
//
// RequestUnitCount - sends out request for number of units registered
// format example -> RequestUnitCount
//
// ReportRequest - sends out request to main coordinator script for reports from testUnits
// format example -> ReportRequest::unitName
//
// ReportComplete - sends out notification to main coordinator script that reporting is done
// format example -> ReportComplete
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
 
 
 
// Global Variables
 
integer toAllChannel = -255;                    // general channel - linked message
 
integer debug = 0;                              // level of debug message
integer debugChannel = DEBUG_CHANNEL;           // output channel for debug messages
 
list unitKeys = [];                             // object keys list specified test units
list unitReports = [];                          // list of test unit reports logged in
 
string reportType = "NORMAL";                   // determines length and content of report type
                                                // NORMAL - failures and summary information
                                                // QUITE - summary information only
                                                // VERBOSE - everything
 
string reportMethod = "CHAT::channel::0";       // determines output method of report
                                                // CHAT::channel::0
                                                // EMAIL::address::you@lindenlabs.com
                                                // HTTP::url::www.yoururl.com
integer broadcastChannel;
string testSelected = "ALL";                    // selected test -> ALL, unitName, or groupName
 
key httpManager;                                // handler for the HTTP event
 
integer unitCount = 0;                          // number of registered units
integer reportCount = 0;                        // number of units that reported
 
integer minMemory;                              // minimum free memory allowed before early report dump
 
list unitStatsSum;                              //the four element list is a running count of the status types
                                                //each stored at a specific index
                                                //LATE_REGISTRATION - 0 
                                                //TEST_TIMED_OUT - 1
                                                //PASS - 2
                                                //FAIL - 3
 
integer reportTimeoutLength;                    // report in seconds to allow for reporting
integer reportTimerInterupt = 0;                // indication if reportTimer has expired
 
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
    if(debug > 0 )llSay(debugChannel, llGetScriptName()+ "->ParseCommand: " + message  + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>");
 
    //reset all scripts 
    if(message == "reset")
    {
        //reset this script 
        llResetScript();                   
    }
 
    //ClearAll()
    else if(message == "ClearAll")
    {
        //reset the lists to an empty state
        unitKeys = [];                    
        unitReports = [];                   
 
    }
 
    // SetReportMethod()
    // format example -> SetReportMethod::CHAT::channel::0
    else if(llSubStringIndex(message, "SetReportMethod::") != -1)
    {
        //store the parameters from the message string in the reportMethod after removing the command 
        reportMethod = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
 
        if(llSubStringIndex( message, "CHAT") != -1)
        {
            //dump report method parameters into usable list
            list methodParameters = llParseString2List( reportMethod, ["::"], [""]);
 
               //pull channel from parameters list
            integer channel = (integer)llList2String( methodParameters, llListFindList( methodParameters, ["channel"]) + 1);
 
        }
 
    }
 
    //SetReportTimeoutLength()
    //format example -> SetReportTimeoutLength::10
    else if(llSubStringIndex(message, "SetReportTimeoutLength::") != -1)
    {
        //parse value from string by deleting message upto index of ::
        //and set global reportTimeoutLength variable
        reportTimeoutLength = (integer)llDeleteSubString( message, 0, llSubStringIndex(message, "::") + 1);
    }
 
    // SetReportType()
    // format example -> SetReportType::NORMAL
    else if(llSubStringIndex(message, "SetReportType::") != -1)
    {
        //store the parameters from the message string in the reportType after removing the command 
        reportType = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
    }
 
    // SetBroadcastChannel()
    // format example -> SetBroadcastChannel::0
    else if(llSubStringIndex(message, "SetBroadcastChannel::") != -1)
    {
        //store the parameters from the message string in the broadcastChannel after removing the command 
        broadcastChannel = (integer)llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
    }
 
    // SetTestSelected()
    // format example -> SetTestSelected::ALL
    else if(llSubStringIndex(message, "SetTestSelected::") != -1)
    {
        //store the parameters from the message string in the reportType after removing the command 
        testSelected = llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
    }
 
    // SetUnitCount()
    // format example -> SetUnitCount::1
    else if(llSubStringIndex(message, "SetUnitCount::") != -1)
    {
        //store the parameters from the message string in the reportType after removing the command 
        unitCount = (integer)llDeleteSubString( message, 0 , llSubStringIndex( message, "::") + 1 );
    }
 
} //end ParseCommand
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ProcessStats
//////////
//////////      Input:      string message - stat to be processed 
//////////                    
//////////      Output:     no return value
//////////                    
//////////      Purpose:    This function is initiated by the status info from the Coordinator_TestUnits
//////////                  script. It determines if the stat is in the current test group
//////////                  and processes accordingly. 
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
ProcessStats(string message)
{
   if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->ProcessStats: " + message  + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>"); 
 
  //format example -> ReportStats::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1::unitStatus::PASS
 
  //decrement unit count to indicate stat was recieved
  unitCount--;
 
  // dump string parameters into usable list
  list unitParameters = llParseString2List( message, ["::"], [""]);
 
  //pull name, group and status from unitParameters
  string name = llList2String( unitParameters, llListFindList( unitParameters, ["unitName"]) + 1);
  string group = llList2String( unitParameters, llListFindList( unitParameters, ["groupName"]) + 1);
  string status = llList2String( unitParameters, llListFindList( unitParameters, ["unitStatus"]) + 1);
 
 
  // if testSelected is ALL or matches the unitName or groupName
  // AND unit status does not indicate late registration
  if(
//      (     
            testSelected == "ALL" 
         || testSelected == name
         || testSelected == group 
//       )
//         && 
//       (
//         status != "LATE_REGISTRATION"
//       )
     )
     {
         string uKey = llList2String( unitParameters, llListFindList( unitParameters, ["unitKey"]) + 1);
 
         //if the unit was tested, but the status is still REGISTERED
         if(status == "REGISTERED")
         {
           status = "TEST_TIMED_OUT";    
         }
 
         //if the unit registered on time and finished the test on time, expect a further report  
         if( (status != "LATE_REGISTRATION") && (status != "TEST_TIMED_OUT") )
         {
            //add unitKey to unitKeys list
            unitKeys = (unitKeys = []) + unitKeys + uKey;
 
             uKey;
            //create an entry on the unit reports list for this unit
            unitReports = (unitReports = []) + unitReports + ["Key: " + uKey + "\n" +
                                                             "Name: " + name + "\n" +
                                                             "Group: " + group + "\n" +
                                                             "Status: " + status  ];     
         }
 
         //the four element list is a running count of the status types
         //each stored at a specific index
         //LATE_REGISTRATION - 0 
         //TEST_TIMED_OUT - 1
         //PASS - 2
         //FAIL - 3
         if(status == "LATE_REGISTRATION")
         {
             //replace the indexed position with the currently stored value + 1
             unitStatsSum = llListReplaceList( unitStatsSum, [llList2Integer( unitStatsSum, 0 ) + 1] , 0, 0 );
         }
         else if(status == "TEST_TIMED_OUT")
         {
             //replace the indexed position with the currently stored value + 1
             unitStatsSum = llListReplaceList( unitStatsSum, [llList2Integer( unitStatsSum, 1 ) + 1] , 1, 1 );
         }
         else if(status == "PASS")
         {
             //replace the indexed position with the currently stored value + 1
             unitStatsSum = llListReplaceList( unitStatsSum, [llList2Integer( unitStatsSum, 2 ) + 1] , 2, 2 );
         }
         else if(status == "FAIL")
         {
             //replace the indexed position with the currently stored value + 1
             unitStatsSum = llListReplaceList( unitStatsSum, [llList2Integer( unitStatsSum, 3 ) + 1] , 3, 3 );
         }
 
 
 
         //initiate a request from Coordinator_Coordinator for detailed report from the specific testUnit
         llMessageLinked(LINK_SET, toAllChannel, "ReportRequest::" + name, NULL_KEY);
     }
 
} // end ProcessStats
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   ProcessReports
//////////
//////////      Input:      string message - report to be processed 
//////////                    
//////////      Output:     0 - indicate process is not done
//////////                  1 - indicates the process is done
//////////                    
//////////      Purpose:    This function is initiated by a report from a testUnit sent via
//////////                  the Coordinator_Coordinator script. It processes the report according
//////////                  to globally specified ReportMethod and memory management  
//////////                    
//////////      Issues:     llEscapeURL is limited to 255 characters, which is a significant limit
//////////                  on sending HTTP reports out. 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
integer ProcessReports(string message)
{
 
    if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->ProcessReports: " + message + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>"); 
 
  //AddUnitReport::unitKey::00000-0000-0000-00000::Report::Successful Completion of Test
 
  //increment reportCount to indicate another report was received
  reportCount++;
 
  // dump command parameters into usable list
  list unitParameters = llParseString2List( message, ["::"], [""]);
  //dump report method parameters into usable list
  list methodParameters = llParseString2List( reportMethod, ["::"], [""]);
 
  //pull unit key from parameters list
  list uKey = llList2List( unitParameters, llListFindList( unitParameters, ["unitKey"]) + 1, llListFindList( unitParameters, ["unitKey"]) + 1);
  //find the index of key on the unitKey list
  integer index = llListFindList( unitKeys, uKey );
  //pull the report from the parameters list
  string report = llList2String( unitParameters, llListFindList( unitParameters, ["Report"]) + 1);
 
  //if CHAT is selected reporting method, and the timer is not expired
  if( ( llSubStringIndex( reportMethod, "CHAT" ) != -1) &&  reportTimerInterupt != 1)
  {
        //pull channel from parameters list
      //integer channel = (integer)llList2String( methodParameters, llListFindList( methodParameters, ["channel"]) + 1);
 
     //for QUIET, leave out everything but the summary
     if( reportType != "QUIET")
     {
         //dump status header already stored in unitReports and new report info
         llSay( broadcastChannel , "***************" +  "\n" + llList2String( unitReports, index ) + "\n" + report + "\n" + "***************" );
     }
 
      //if we have seen all the reports
      if(reportCount == llGetListLength(unitKeys) )
      {
 
            //output the summary stats
           //the four element list is a running count of the status types
         //each stored at a specific index
         //LATE_REGISTRATION - 0 
         //TEST_TIMED_OUT - 1
         //PASS - 2
         //FAIL - 3
            llSay( broadcastChannel, "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY" + "\n" +
                             "LATE_REGISTRATION ->  " + llList2String( unitStatsSum, 0) + "\n" +
                             "TEST_TIMED_OUT ->  " + llList2String( unitStatsSum, 1) + "\n" +
                             "PASS ->  " + llList2String( unitStatsSum, 2) + "\n" +
                             "FAIL ->  " + llList2String( unitStatsSum, 3) + "\n" +
                             "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY");
          //notify coordinator that report process is complete
          llMessageLinked(LINK_SET, toAllChannel, "ReportComplete", NULL_KEY);
 
          //shut timer down
          llSetTimerEvent(0);
 
          //return a process complete response
          return 1;
      }
  }
  //otherwise going to be EMAIL or HTTP that will be sent all at once, if the timer has not expired
  else if (reportTimerInterupt != 1)
  {
      //augment current entry with additional information
       unitReports = llListReplaceList( unitReports, [llList2String( unitReports, index ) + "\n" + report] , index, index );
  }
 
  //if units have all reported in and reports are all logged OR if memory is exceeded OR if timer has expired
  if( 
       ( 
          ( unitCount == 0 ) && ( reportCount == llGetListLength(unitKeys) )
       )
          || ( MemoryCheck() ) 
          || ( reportTimerInterupt == 1 )
    )
   {
       if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->right after mem check: " +  "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>"); 
       //if email is selected method, and the timer has not expired    
       if( ( llSubStringIndex( reportMethod, "EMAIL" ) != -1 ) && (reportTimerInterupt != 1) )
       {
             string address = llList2String( methodParameters, llListFindList( methodParameters, ["address"]) + 1);
             string subject = "Unit Test Report: " + llGetTimestamp() 
                              + " at " + llGetRegionName() + ": " + (string)llGetPos();
 
            if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->after address and subject: " +  "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>"); 
 
 
             //for QUIET, leave out everything but the summary
             if( reportType != "QUIET")
             {
                 llEmail( address, subject, llDumpList2String( unitReports, "\n ****************************** \n") + 
                     "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY" + "\n" +
                     "LATE_REGISTRATION ->  " + llList2String( unitStatsSum, 0) + "\n" +
                     "TEST_TIMED_OUT ->  " + llList2String( unitStatsSum, 1) + "\n" +
                     "PASS ->  " + llList2String( unitStatsSum, 2) + "\n" +
                     "FAIL ->  " + llList2String( unitStatsSum, 3) + "\n" +
                     "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY");
 
 
             }
 
             // QUIET, just the summary 
             else
             {
                  llEmail( address, subject,"SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY" + "\n" +
                     "LATE_REGISTRATION ->  " + llList2String( unitStatsSum, 0) + "\n" +
                     "TEST_TIMED_OUT ->  " + llList2String( unitStatsSum, 1) + "\n" +
                     "PASS ->  " + llList2String( unitStatsSum, 2) + "\n" +
                     "FAIL ->  " + llList2String( unitStatsSum, 3) + "\n" +
                     "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY");
            }     
 
 
          llSay(0, "EMAIL Report Sent");
 
       }
       //if http is selected method, and the timer has not expired 
       else if( (llSubStringIndex( reportMethod, "HTTP" ) != -1) && (reportTimerInterupt != 1) ) 
       {
            //pull url from parameters
            string url = llList2String( methodParameters, llListFindList( methodParameters, ["url"]) + 1);
 
              string HttpBody;
 
             //for QUIET, leave out everything but the summary
             if( reportType != "QUIET")
             {
                 //add individual unit reports
                 HttpBody = (HttpBody = "") + HttpBody + llDumpList2String( unitReports, "\n ****************************** \n");
             }
 
             //add summary to the end
             HttpBody = (HttpBody = "") + HttpBody + "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY" + "\n" +
                     "LATE_REGISTRATION ->  " + llList2String( unitStatsSum, 0) + "\n" +
                     "TEST_TIMED_OUT ->  " + llList2String( unitStatsSum, 1) + "\n" +
                     "PASS ->  " + llList2String( unitStatsSum, 2) + "\n" +
                     "FAIL ->  " + llList2String( unitStatsSum, 3) + "\n" +
                     "SUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARYSUMMARY";
 
            httpManager = llHTTPRequest( url + "?report=" + llEscapeURL(HttpBody), [HTTP_METHOD,"POST"],"");   
            llSay(0, "HTTP Report Sent");
 
       }
 
       //notify coordinator that report process is complete
       llMessageLinked(LINK_SET, toAllChannel, "ReportComplete", NULL_KEY);
 
       //shut timer down
       llSetTimerEvent(0);
 
       //return to default
       return 1;
 
   } //end of if
 
  //if still processing
  return 0;
 
} // end ProcessReports
 
//////////////////////////////////////////////////////////////////////////////////////////////////
//////////
//////////      Function:   MemoryCheck
//////////
//////////      Input:      no input parameter
//////////                    
//////////      Output:     integer 0 - PASS
//////////                  integer 1 - FAIL
//////////                    
//////////      Purpose:    This function manages the memory check to see if the reports should
//////////                  start outputting before they are all collected. This only applies 
//////////                  EMAIL and HTTP because CHAT outputs automatically.  
//////////                    
//////////      Issues:        no known issues 
//////////                    
//////////                    
/////////////////////////////////////////////////////////////////////////////////////////////////
integer MemoryCheck( )
{
    //llSay(0, "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>");
 
 
    if( llSubStringIndex( reportMethod, "CHAT") != -1 )
    {
        minMemory = 1000;
    }
    else
    {
        minMemory = 3000;
    }
 
    //if free memory is lower then acceptable threshold
    if ( llGetFreeMemory() < minMemory )
    {
        llSay(0, "MEM FAIL");
        //return fail
        return 1;
    }
 
    //return pass
    return 0;
    llSay(0, "MEM PASS");
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
//Initialize()
//{
//    if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->Init: " + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>");
 
//}
 
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
        if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->Init: " + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>");
 
        //Initialize();
    }
////////////////////////////////////////////////////////
//  On Rez of default state                           //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
        //Initialize();
    }
 
 
///////////////////////////////////////////////////////
//  Link Message of default state                    //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
           // OutputReports()
           // format example -> OutputReports::testSelected::ALL
           if(llSubStringIndex(message, "OutputReports::") != -1)
           {
              // dump command parameters into usable list
              list unitParameters = llParseString2List( message, ["::"], [""]);
 
              //find testSelected indicator, and pull value from unitParameters list
              testSelected = llList2String( unitParameters, llListFindList( unitParameters, ["testSelected"]) + 1);
 
              ////////////////////
              //  State Change  //
              ////////////////////
              state REPORT;
           }
 
            ParseCommand( message );
 
        }
 
    } //end of link message
 
///////////////////////////////////////////////////////
//  Http Response of REPORT state                    //
///////////////////////////////////////////////////////
 http_response(key id, integer status, list metadata, string body)
    {
        if(debug > 10){llSay(debugChannel, llGetScriptName()+ ":FUNCTION: HTTP");}
        if(debug > 10){llSay(debugChannel, body);}
 
 
        if(id == httpManager)
        {
            llSay( 0, body );
        }
 
    } 
 
 
} // end default
 
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
//  State Entry of REPORT state                     //
///////////////////////////////////////////////////////
   state_entry()
    {
        if(debug > 0)llSay(debugChannel, llGetScriptName()+ "->Report State Entry: " + "<<<<<<<<<<<<<<<<<<<<  " + (string)llGetFreeMemory() + "     >>>>>>>>>>>>>>>>>>>>");
 
        //clear counters
        unitCount = 0;                         
        reportCount = 0;
 
        //clear lists
        unitKeys = [];                         
        unitReports = [];
        unitStatsSum = [ 0, 0, 0, 0];
 
        if( llSubStringIndex( reportMethod , "CHAT") != -1)
        {
 
             //if reportMethod is CHAT we need to create the output report header here
             llSay( broadcastChannel , "************************************************************" );
             llSay( broadcastChannel , "Time: " + llGetTimestamp() );
             llSay( broadcastChannel , "Location: " + llGetRegionName() + " " + (string)llGetPos() );
        }
 
        //broadcast to other TestUnits script a request for stats
        llMessageLinked(LINK_SET, toAllChannel, "ReportUnitStats", NULL_KEY);
 
        llSay(0, "reportTimeout: " + (string)reportTimeoutLength );
        //need a timer in case not all expected units respond
        llSetTimerEvent( reportTimeoutLength );
        //clear timer Interupt
        reportTimerInterupt = 0;
 
    }
 
///////////////////////////////////////////////////////
//  State Exit of REPORT state                     //
///////////////////////////////////////////////////////
   state_exit()
    {
        unitKeys = [];                         
        unitReports = [];
        unitStatsSum = [];
    }   
////////////////////////////////////////////////////////
//  On Rez of REPORT state                           //
////////////////////////////////////////////////////////    
    on_rez(integer start_param)
    {
        ////////////////////
        //  State Change  //
        ////////////////////
        state default;
    }
 
 
///////////////////////////////////////////////////////
//  Link Message of REPORT state                    //
///////////////////////////////////////////////////////   
    link_message(integer sender_number, integer number, string message, key id)
    {
        //if link message is on the correct channel
        if(number == toAllChannel)
        {
            //if message is stats from Coordinator_TestUnits script
            if( llSubStringIndex( message, "ReportStats") != -1)
            {
                ProcessStats( message );    
            }
            //if message is report from a testUnit via Coordinator_Coordinator
            if( llSubStringIndex( message, "AddUnitReport") != -1)
            {
                //if process is complete for what ever reason
                if( ProcessReports( message ) )
                {
                    ////////////////////
                    //  State Change  //
                    ////////////////////
                   state default;
                }    
            }
            //otherwise assume general command from Coordinator
            else
            {
                ParseCommand( message );
            }
        } // end if toAll
 
    } //end of link message
 
///////////////////////////////////////////////////////
//  Http Response of REPORT state                    //
///////////////////////////////////////////////////////
 http_response(key id, integer status, list metadata, string body)
    {
        if(debug > 10){llSay(debugChannel, llGetScriptName()+ ":FUNCTION: HTTP");}
        if(debug > 10){llSay(debugChannel, body);}
 
        //for testing purposes
        if(id == httpManager)
        {
            llSay( 0, body );
        }
 
    } //end http reponse
 
///////////////////////////////////////////////////////
//  timer of REPORT state                            //
///////////////////////////////////////////////////////   
    timer()
    {
        //indicate timer has expired
        reportTimerInterupt = 1;
 
        //we proceeReports in case EMAIL or HTTP is selecte
        //and if successful we return to default
        if( ProcessReports( "VOID" ) )
        {
           ////////////////////
           //  State Change  //
           ////////////////////
           state default;
         }   
 
 
 
    } //end of timer
 
 
} // end default