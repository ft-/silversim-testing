
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