<?php
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
//
//                                 XML-RPC 
//
//
//                      script for XML-RPC test
//
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
 
/////////////////////////////////////////////////
//
//         Post To Host
//
//    INPUT: $host, $path, $data_to_send
//   OUTPUT:
//  GLOBALS:
//   LOCALS: $fp
///////////////////////////////////////////////////
  function postToHost($host, $path, $data_to_send) {
    $fp = fsockopen($host, 80);
    fputs($fp, "POST $path HTTP/1.1\r\n");
    fputs($fp, "Host: $host\r\n");
    fputs($fp, "Content-type: application/x-www-form-urlencoded\r\n");
    fputs($fp, "Content-length: ". strlen($data_to_send) ."\r\n");
    fputs($fp, "Connection: close\r\n\r\n");
    fputs($fp, $data_to_send);
//    while(!feof($fp)) {
//        $res .= fgets($fp, 128);
//    }
    fclose($fp);
//    return substr($res, strpos($res, "\r\n\r\n"));;
  }
/////////////////////////////////////////////////
//
//         Parse Response
//
//    INPUT: $response
//   OUTPUT:
//  GLOBALS:
//   LOCALS: $result
///////////////////////////////////////////////////
  function parseResponse($response) {
    $result = array();
    if (preg_match_all('#<name>(.+)</name><value><(string|int)>(.*)</\2></value>#U', $response, $regs, PREG_SET_ORDER)) {
      foreach($regs as $key=>$val) {
        $result[$val[1]] = $val[3];
      }
    }
    return $result;
  }
/////////////////////////////////////////////////
//
//         Send request
//
//    INPUT: $channel, $intValue, $stringValue
//   OUTPUT: 
//  GLOBALS:
//   LOCALS: $int, $string, $data
///////////////////////////////////////////////////
  function sendRequest($channel, $intValue, $stringValue) {
    $channel = htmlspecialchars($channel);
    $int = (int)$intValue;
    $string = htmlspecialchars($stringValue);
 
    $data = '<?xml version="1.0"?>';
    $data .= '<methodCall>';
    $data .= '<methodName>llRemoteData</methodName>';
    $data .= '<params><param><value><struct>';
	$data .= '<member><name>Channel</name><value><string>'.$channel.'</string></value></member>';
    $data .= '<member><name>IntValue</name><value><int>'.$int.'</int></value></member>';
    $data .= '<member><name>StringValue</name><value><string>'.$string.'</string></value></member>';
    $data .= '</struct></value></param></params></methodCall>';
 
    postToHost("xmlrpc.secondlife.com","/cgi-bin/xmlrpc.cgi", $data);
  }
/////////////////////////////////////////////////////////////////////////////////////
//
//                         Main
//
//////////////////////////////////////////////////////////////////////////////////////
 
  $channel = $_GET["channel"];
  $stringMessage = $_GET["stringMessage"];
  $intMessage = $_GET["intMessage"];
  sendRequest($channel,$intMessage,$stringMessage);
 
  echo "channel:" . $channel . ", stringMessage: " . $stringMessage . ", intMessage: " . $intMessage ;
?>