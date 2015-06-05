///////////////////////////////////////////////////////////////////////////////////
///////
///////
///////
///////            TestUnit_TestScript
///////             
///////       
///////
///////  This is the test script that should include the code you want to test. 
///////      
///////              
//////////////////////////////////////////////////////////////////////////////////////    
 
//TestUnit_TestScript    .1 -> initial framework  6.23.2007
//TestUnit_TestScript    .2 -> tested with minor bug fixes  7.2.2007
 
 
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
 
integer toAllChannel = -255;           // general channel - linked message
integer passFailChannel = -355;        // test scripts channel for communicating pass/fail - linked message
 
integer debug = 0;                     // level of debug message
integer debugChannel = DEBUG_CHANNEL;  // output channel for debug messages
 
integer gTestsPassed = 0;
integer gTestsFailed = 0;
 
 
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
 
///////////////////////////////////////////////////////////////////////////////////////////////////
// supporting functions
testPassed(string description, string actual, string expected)
{
    ++gTestsPassed;
    //llSay(0, description);
}
 
testFailed(string description, string actual, string expected)
{
    ++gTestsFailed;
    llSay(0, "FAILED!: " + description + " (" + actual + " expected " + expected + ")");
}
 
ensureTrue(string description, integer actual)
{
    if(actual)
    {
        testPassed(description, (string) actual, (string) TRUE);
    }
    else
    {
        testFailed(description, (string) actual, (string) TRUE);
    }
}
 
ensureFalse(string description, integer actual)
{
    if(actual)
    {
        testFailed(description, (string) actual, (string) FALSE);
    }
    else
    {
        testPassed(description, (string) actual, (string) FALSE);
    }
}
 
ensureIntegerEqual(string description, integer actual, integer expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
ensureFloatEqual(string description, float actual, float expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
ensureStringEqual(string description, string actual, string expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
ensureVectorEqual(string description, vector actual, vector expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
ensureRotationEqual(string description, rotation actual, rotation expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
ensureListEqual(string description, list actual, list expected)
{
    if(actual == expected)
    {
        testPassed(description, (string) actual, (string) expected);
    }
    else
    {
        testFailed(description, (string) actual, (string) expected);
    }
}
 
integer gInteger = 5;
float gFloat = 1.5;
string gString = "foo";
vector gVector = <1, 2, 3>;
rotation gRot = <1, 2, 3, 4>;
 
integer testReturn()
{
    return 1;
}
 
integer testParameters(integer param)
{
    param = param + 1;
    return param;
}
 
integer testRecursion(integer param)
{
    if(param <= 0)
    {
        return 0;
    }
    else
    {
        return testRecursion(param - 1);
    }
}
 
 
 
 
 
 
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
 
    // truth
    ensureIntegerEqual("TRUE", TRUE, TRUE);
    ensureIntegerEqual("FALSE", FALSE, FALSE);
 
    // equality
    ensureIntegerEqual("(TRUE == TRUE)", (TRUE == TRUE), TRUE);
    ensureIntegerEqual("(TRUE == FALSE)", (TRUE == FALSE), FALSE);
    ensureIntegerEqual("(TRUE == FALSE)", (TRUE == FALSE), FALSE);
    ensureIntegerEqual("(FALSE == FALSE)", (FALSE == FALSE), TRUE);
 
    // inequality
    ensureIntegerEqual("(TRUE != TRUE)", (TRUE != TRUE), FALSE);
    ensureIntegerEqual("(TRUE != FALSE)", (TRUE != FALSE), TRUE);
    ensureIntegerEqual("(TRUE != FALSE)", (TRUE != FALSE), TRUE);
    ensureIntegerEqual("(FALSE != FALSE)", (FALSE != FALSE), FALSE);
 
    // and
    ensureIntegerEqual("(TRUE && TRUE)", (TRUE && TRUE), TRUE);
    ensureIntegerEqual("(TRUE && FALSE)", (TRUE && FALSE), FALSE);
    ensureIntegerEqual("(FALSE && TRUE)", (FALSE && TRUE), FALSE);
    ensureIntegerEqual("(FALSE && FALSE)", (FALSE && FALSE), FALSE);
 
    // or
    ensureIntegerEqual("(TRUE || TRUE)", (TRUE || TRUE), TRUE);
    ensureIntegerEqual("(TRUE || FALSE)", (TRUE || FALSE), TRUE);
    ensureIntegerEqual("(FALSE || TRUE)", (FALSE || TRUE), TRUE);
    ensureIntegerEqual("(FALSE || FALSE)", (FALSE || FALSE), FALSE);
 
    // not
    ensureIntegerEqual("(! TRUE)", (! TRUE), FALSE);
    ensureIntegerEqual("(! FALSE)", (! FALSE), TRUE);
 
    // greater than
    ensureIntegerEqual("(1 > 0)", (1 > 0), TRUE);
    ensureIntegerEqual("(0 > 1)", (0 > 1), FALSE);
    ensureIntegerEqual("(1 > 1)", (1 > 1), FALSE);
 
    // less than
    ensureIntegerEqual("(0 < 1)", (0 < 1), TRUE);
    ensureIntegerEqual("(1 < 0)", (1 < 0), FALSE);
    ensureIntegerEqual("(1 < 1)", (1 < 1), FALSE);
 
    // greater than or equal
    ensureIntegerEqual("(1 >= 0)", (1 >= 0), TRUE);
    ensureIntegerEqual("(0 >= 1)", (0 >= 1), FALSE);
    ensureIntegerEqual("(1 >= 1)", (1 >= 1), TRUE);
 
    // tess than or equal
    ensureIntegerEqual("(0 <= 1)", (0 <= 1), TRUE);
    ensureIntegerEqual("(1 <= 0)", (1 <= 0), FALSE);
    ensureIntegerEqual("(1 <= 1)", (1 <= 1), TRUE);
 
    // bitwise and
    ensureIntegerEqual("(10 & 25)", (10 & 25), 8);
 
    // bitwise or
    ensureIntegerEqual("(10 | 25)", (10 | 25), 27);
 
    // bitwise not
    ensureIntegerEqual("~10", ~10, -11);
 
    // xor
    ensureIntegerEqual("(10 ^ 25)", (10 ^ 25), 19);
 
    // right shift
    ensureIntegerEqual("(523 >> 2)", (523 >> 2), 130);
 
    // left shift
    ensureIntegerEqual("(523 << 2)", (523 << 2), 2092);
 
    // addition
    ensureIntegerEqual("(1 + 1)", (1 + 1), 2);
    ensureFloatEqual("(1 + 1.1)", (1 + 1.1), 2.1);
    ensureFloatEqual("(1.1 + 1)", (1.1 + 1), 2.1);
    ensureFloatEqual("(1.1 + 1.1)", (1.1 + 1.1), 2.2);
    ensureStringEqual("\"foo\" + \"bar\"", "foo" + "bar", "foobar");
    ensureVectorEqual("(<1.1, 2.2, 3.3> + <4.4, 5.5, 6.6>)", (<1.1, 2.2, 3.3> + <4.4, 5.5, 6.6>), <5.5, 7.7, 9.9>);
    ensureRotationEqual("(<1.1, 2.2, 3.3, 4.4> + <4.4, 5.5, 6.6, 3.3>)", (<1.1, 2.2, 3.3, 4.4> + <4.4, 5.5, 6.6, 3.3>), <5.5, 7.7, 9.9, 7.7>);
    ensureListEqual("([1] + 2)", ([1] + 2), [1,2]);
    ensureListEqual("([] + 1.5)", ([] + 1.5), [1.5]);
    ensureListEqual("([\"foo\"] + \"bar\")", (["foo"] + "bar"), ["foo", "bar"]);
    ensureListEqual("([] + <1,2,3>)", ([] + <1,2,3>), [<1,2,3>]);
    ensureListEqual("([] + <1,2,3,4>)", ([] + <1,2,3,4>), [<1,2,3,4>]);
 
    // subtraction
    ensureIntegerEqual("(1 - 1)", (1 - 1), 0);
    ensureFloatEqual("(1 - 0.5)", (1 - 0.5), 0.5);
    ensureFloatEqual("(1.5 - 1)", (1.5 - 1), 0.5);
    ensureFloatEqual("(2.2 - 1.1)", (2.2 - 1.1), 1.1);
    ensureVectorEqual("(<1.5, 2.5, 3.5> - <4.5, 5.5, 6.5>)", (<1.5, 2.5, 3.5> - <4.5, 5.5, 6.5>), <-3.0, -3.0, -3.0>);
    ensureRotationEqual("(<1.5, 2.5, 3.5, 4.5> - <4.5, 5.5, 6.5, 7.5>)", (<1.5, 2.5, 3.5, 4.5> - <4.5, 5.5, 6.5, 7.5>), <-3.0, -3.0, -3.0, -3.0>);
 
    // multiplication
    ensureIntegerEqual("(2 * 3)", (2 * 3), 6);
    ensureFloatEqual("(2 * 3.5)", (2 * 3.5), 7.0);
    ensureFloatEqual("(2.5 * 3)", (2.5 * 3), 7.5);
    ensureFloatEqual("(2.5 * 3.5)", (2.5 * 3.5), 8.75);
    ensureVectorEqual("(<1.1, 2.2, 3.3> * 2)", (<1.1, 2.2, 3.3> * 2), <2.2, 4.4, 6.6>);
    ensureVectorEqual("(<2.2, 4.4, 6.6> * 2.0)", (<2.2, 4.4, 6.6> * 2.0), <4.4, 8.8, 13.2>);
    ensureFloatEqual("(<1.0, 2.0, 3.0> * <4.0, 5.0, 6.0>)", (<1.0, 2.0, 3.0> * <4.0, 5.0, 6.0>), 32.0);
 
    // division
    ensureIntegerEqual("(2 / 2)", (2 / 2), 1);
    ensureFloatEqual("(2.2 / 2)", (2.2 / 2), 1.1);
    ensureFloatEqual("(3 / 1.5)", (3 / 1.5), 2.0);
    ensureFloatEqual("(2.2 / 2.0)", (2.2 / 2.0), 1.1);
    ensureVectorEqual("(<1.0, 2.0, 3.0> / 2)", (<1.0, 2.0, 3.0> / 2), <0.5, 1.0, 1.5>);
    ensureVectorEqual("(<3.0, 6.0, 9.0> / 1.5)", (<3.0, 6.0, 9.0> / 1.5), <2.0, 4.0, 6.0>);
 
    // modulo
    ensureIntegerEqual("(3 % 1)", (3 % 1), 0);
    ensureVectorEqual("(<1.0, 2.0, 3.0> % <4.0, 5.0, 6.0>)", (<1.0, 2.0, 3.0> % <4.0, 5.0, 6.0>), <-3.0, 6.0, -3.0>);
 
    // assignment
    integer i = 1;
    ensureIntegerEqual("i = 1;", i, 1);
 
    // addition assignment
    i = 1;
    i += 1;
    ensureIntegerEqual("i = 1; i += 1;", i, 2);
 
    // subtraction assignment
    i = 1;
    i -= 1;
    ensureIntegerEqual("i = 1; i -= 1;", i, 0);
 
    // multiplication assignment
    i = 2;
    i *= 2;
    ensureIntegerEqual("i = 2; i *= 2;", i, 4);
 
    // division assignment
    i = 2;
    i /= 2;
    ensureIntegerEqual("i = 2; i /= 2;", i, 1);
 
    // modulo assignment
    i = 3;
    i %= 1;
    ensureIntegerEqual("i = 3; i %= 1;", i, 0);
 
    // post increment.
    i = 1;
    ensureIntegerEqual("i = 1; (i == 2) && (i++ == 1)", (i == 2) && (i++ == 1), TRUE);
 
    // pre increment.
    i = 1;
    ensureIntegerEqual("i = 1; (i == 2) && (++i == 2)", (i == 2) && (++i == 2), TRUE);
 
    // post decrement.
    i = 1;
    ensureIntegerEqual("i = 1; (i == 0) && (i-- == 1)", (i == 0) && (i-- == 1), TRUE);
 
    // pre decrement.
    i = 1;
    ensureIntegerEqual("i = 1; (i == 0) && (--i == 0)", (i == 0) && (--i == 0), TRUE);
 
    // casting
    ensureFloatEqual("((float)2)", ((float)2), 2.0);
    ensureStringEqual("((string)2)", ((string)2), "2");
    ensureIntegerEqual("((integer) 1.5)", ((integer) 1.5), 1);
    ensureStringEqual("((string) 1.5)", ((string) 1.5), "1.500000");
    ensureIntegerEqual("((integer) \"0xF\")", ((integer) "0xF"), 15);
    ensureIntegerEqual("((integer) \"2\")", ((integer) "2"), 2);
    ensureFloatEqual("((float) \"1.5\")", ((float) "1.5"), 1.5);
    ensureVectorEqual("((vector) \"<1,2,3>\")", ((vector) "<1,2,3>"), <1,2,3>);
    ensureRotationEqual("((quaternion) \"<1,2,3,4>\")", ((quaternion) "<1,2,3,4>"), <1,2,3,4>);
    ensureStringEqual("((string) <1,2,3>)", ((string) <1,2,3>), "<1.00000, 2.00000, 3.00000>");
    ensureStringEqual("((string) <1,2,3,4>)", ((string) <1,2,3,4>), "<1.00000, 2.00000, 3.00000, 4.00000>");
    ensureStringEqual("((string) [1,2.5,<1,2,3>])", ((string) [1,2.5,<1,2,3>]), "12.500000<1.000000, 2.000000, 3.000000>");
 
    // while
    i = 0;
    while(i < 10) ++i;
    ensureIntegerEqual("i = 0; while(i < 10) ++i", i, 10);
 
    // do while
    i = 0;
    do {++i;} while(i < 10);
    ensureIntegerEqual("i = 0; do {++i;} while(i < 10);", i, 10);
 
    // for
    for(i = 0; i < 10; ++i);
    ensureIntegerEqual("for(i = 0; i < 10; ++i);", i, 10);
 
    // jump
    i = 1;
    jump SkipAssign;
    i = 2;
    @SkipAssign;
    ensureIntegerEqual("i = 1; jump SkipAssign; i = 2; @SkipAssign;", i, 1);
 
    // return
    ensureIntegerEqual("testReturn()", testReturn(), 1);
 
    // parameters
    ensureIntegerEqual("testParameters(1)", testParameters(1), 2);
 
    // variable parameters
    i = 1;
    ensureIntegerEqual("i = 1; testParameters(i)", testParameters(i), 2);
 
    // recursion
    ensureIntegerEqual("testRecursion(10)", testRecursion(10), 0);
 
    // globals
    ensureIntegerEqual("gInteger", gInteger, 5);
    ensureFloatEqual("gFloat", gFloat, 1.5);
    ensureStringEqual("gString", gString, "foo");
    ensureVectorEqual("gVector", gVector, <1, 2, 3>);
    ensureRotationEqual("gRot", gRot, <1, 2, 3, 4>);
 
    // global assignment
    gInteger = 1;
    ensureIntegerEqual("gInteger = 1", gInteger, 1);
 
    gFloat = 0.5;
    ensureFloatEqual("gFloat = 0.5", gFloat, 0.5);
 
    gString = "bar";
    ensureStringEqual("gString = \"bar\"", gString, "bar");
 
    gVector = <3,3,3>;
    ensureVectorEqual("gVector = <3,3,3>", gVector, <3,3,3>);
 
    gRot = <3,3,3,3>;
    ensureRotationEqual("gRot = <3,3,3,3>", gRot, <3,3,3,3>);
 
    // vector accessor
    vector v;
    v.x = 3;
    ensureFloatEqual("v.x", v.x, 3);
 
    // rotation accessor
    rotation q;
    q.s = 5;
    ensureFloatEqual("q.s", q.s, 5);
 
    // global vector accessor
    gVector.y = 17.5;
    ensureFloatEqual("gVector.y = 17.5", gVector.y, 17.5);
 
    // global rotation accessor
    gRot.z = 19.5;
    ensureFloatEqual("gRot.z = 19.5", gRot.z, 19.5);
 
    // list equality
    list l = (list) 5;
    list l2 = (list) 5;
    ensureListEqual("list l = (list) 5; list l2 = (list) 5", l, l2);
    ensureListEqual("list l = (list) 5", l, [5]);
    ensureListEqual("[1.5, 6, <1,2,3>, <1,2,3,4>]", [1.5, 6, <1,2,3>, <1,2,3,4>], [1.5, 6, <1,2,3>, <1,2,3,4>]);
 
    if (gTestsFailed > 0) {
        llMessageLinked(LINK_SET, passFailChannel, "FAIL", NULL_KEY);  
    } else {
        llMessageLinked(LINK_SET, passFailChannel, "PASS", NULL_KEY);
    }    
 
    // reset globals  
    gInteger = 5;
    gFloat = 1.5;
    gString = "foo";
    gVector = <1, 2, 3>;
    gRot = <1, 2, 3, 4>;
    gTestsPassed = 0;
    gTestsFailed = 0;
 
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
        reportString = "";  //add what you would like to report here !!!!!!
    }
 
    //QUITE - shortest level of reporting
    if( reportType == "QUIET" )
    {
        reportString = "";  //add what you would like to report here !!!!!!
    }
 
    //VERBOSE - highest level of reporting
    if( reportType == "VERBOSE" )
    {
        reportString = "";  //add what you would like to report here !!!!!!
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