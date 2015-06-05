///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            Coordinator_TestUnits
///////             
///////       
///////
///////  This is the coordinator script that maintains the record of testUnits. It should be 
///////  included in the coordinator.  
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//Coordinator_TestUnits    .1 -> initial framework  6.28.2007
//Coordinator_Coordinator  .2 -> bug fixing and testing  7.8.2007
 
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
// AddUnitToList - provides unit information of newly registered list 
// format example -> AddUnitToList::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
//
// UpdateUnitStatus - provides unit status information
// format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
//
// ReportUnitStats - initiates a ReportStats output 
// format example -> ReportUnitStats
//
// RequestUnitCount - request for number of units registered
// format example -> RequestUnitCount
//
//////// OUTPUT ///////////
//
//  ReportStats - sends out unit information including status
//  format example -> ReportStats::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1::status::PASS
//
// SetUnitCount - provides number of registered units
// format example -> SetUnitCount::1
/////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
 
 
 
 
// Global Variables
 
integer toAllChannel = -255;           // general channel - linked message
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
 
      // The following lists are parameters associated with registered test units.
      // A specific unit will have the same list index on all of the lists. 
 
list unitKeys = [];                    // object keys list of registered test units
list unitNames = [];                   // list of registered unit names
list unitGroups = [];                  // list of registered unit group
 
list statusOfUnits = [];               // status of the registered test unit
                                       // PASS - unit completed test process successfully
                                       // FAIL - unit did not complete the process successfully
                                       // REGISTERED - unit is now ready for test
 
 
 
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
        //reset this script 
        llResetScript();                   
    }
 
    //ClearAll()
    else if(message == "ClearAll")
    {
        //reset the lists to an empty state
        unitKeys = [];                    
        unitNames = [];                   
        unitGroups = [];                  
        statusOfUnits = [];
 
    }
 
    // AddUnitToList()
    // format example -> AddUnitToList::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1
    else if(llSubStringIndex(message, "AddUnitToList::") != -1)
    {
        //parse string command into a list of elements
        list unitParameters = llParseString2List( message, ["::"], [""] );
 
        //find first variable name of concern - unitKey
        integer index = llListFindList( unitParameters, ["unitKey"] );
        //use the position of the variable name to update unitKey list 
        unitKeys += llList2List( unitParameters, index + 1, index + 1);                    
 
        //find variable name of concern - unitName
        index = llListFindList( unitParameters, ["unitName"] );
        //use the position of the variable name to update unitName list 
        unitNames += llList2List( unitParameters, index + 1, index + 1);                    
 
        //find variable name of concern - groupName
        index = llListFindList( unitParameters, ["groupName"] );
        //use the position of the variable name to update unitName list 
        unitGroups += llList2List( unitParameters, index + 1, index + 1);                   
 
        //add first status indication for unit
        statusOfUnits += ["REGISTERED"];
    }
 
    // UpdateUnitStatus()
    // format example -> UpdateUnitStatus::unitKey::00000-0000-0000-00000::unitStatus::PASS
    else if(llSubStringIndex(message, "UpdateUnitStatus::") != -1)
    {
        //parse string command into a parameter list
        list unitStatusParameters = llParseString2List( message, ["::"], [""] );
 
        //use variable name to find first variable of concern - unitKey
        integer commandIndex = llListFindList( unitStatusParameters, ["unitKey"] );
 
        //pull the unitKey value from the parameter list
        list CurrentUnitKey = llList2List( unitStatusParameters, commandIndex + 1, commandIndex + 1 );
 
        //use the unitKey to find the index for all the lists of the test unit
        integer keyIndex = llListFindList( unitKeys, CurrentUnitKey );
 
        //find the index of the unitStatus variable
        commandIndex = llListFindList( unitStatusParameters, ["unitStatus"] );
 
        // if unit met the registration time line
        if( llList2String( statusOfUnits, keyIndex ) != "LATE_REGISTRATION" )
        {
           //update the status list with the unitStatus provided by the command string               
           statusOfUnits = llListReplaceList( statusOfUnits, llList2List( unitStatusParameters, commandIndex + 1, commandIndex + 1) , keyIndex, keyIndex);
        }
    }
 
    //  ReportUnitStats()
    //  format example -> ReportStats::unitKey::00000-0000-0000-00000::unitName::TestUnit1::groupName::Group1::unitStatus::PASS
    else if(message == "ReportUnitStats")
    {
 
        //send link message on general channel reporting unit stats
        llMessageLinked(LINK_SET, toAllChannel, "SetUnitCount::" + (string)llGetListLength( unitKeys ), NULL_KEY );
 
        //initialize counter to zero to use as list index
        integer increment = 0;
        //get the size of the lists
        integer count = llGetListLength( unitKeys );
 
        //iterate through the lists reporting the unit parameters for them all, one at a time
        while( increment < count )
        {
           //send link message on general channel reporting unit stats
           llMessageLinked(LINK_SET, toAllChannel, "ReportStats::unitKey::" + llList2String( unitKeys, increment)
                                                            + "::unitName::" + llList2String( unitNames, increment)
                                                            + "::groupName::" + llList2String( unitGroups, increment)
                                                            + "::unitStatus::" + llList2String( statusOfUnits, increment)
                                    , NULL_KEY);
          //increment index counter
          increment++;
        } // end while
    }
 
    // RequestUnitCount - request for number of units registered
    // format example -> RequestUnitCount
    else if(message == "RequestUnitCount")
    {
        //send link message on general channel reporting unit stats
        llMessageLinked(LINK_SET, toAllChannel, "SetUnitCount::" + (string)llGetListLength( unitKeys ), NULL_KEY );
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
            // 
            ParseCommand( message );
 
        }
 
    } //end of link message
 
 
} // end default