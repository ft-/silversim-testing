<?php
 
//pulls information from headers		
$ownername = $_SERVER['HTTP_X_SECONDLIFE_OWNER_NAME'];
$objectname = $_SERVER['HTTP_X_SECONDLIFE_OBJECT_NAME'];
$objectkey = $_SERVER['HTTP_X_SECONDLIFE_OBJECT_KEY'];
$ownerkey = $_SERVER['HTTP_X_SECONDLIFE_OWNER_KEY'];
$region = $_SERVER['HTTP_X_SECONDLIFE_REGION'];
$location = $_SERVER['HTTP_X_SECONDLIFE_LOCAL_POSITION'];
 
echo "HTTP RESPONSE        " . $_GET['report'];
 
?>