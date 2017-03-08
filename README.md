# ADBSrv
Amazon Dash Button Service

A neat little Windows-Service to detect the button-pushed-event from an AmazonDashButton

Parts of the code are from user "Youresam" https://github.com/youresam/Dash-Button.

The Service can be configured like this:

There is one config-file:

1) config.xml

Inside this config-file you setup the buttons and other configurations.
(Just have a look into the config.xml inside the project)

The important part is actually the "buttons" tag.

(Here is an example of on button inside the "buttons" tag, but since it is not correctly displayed, you can switch to "RAW" to see the xml-part)

<button4>
  <name>AnyButtonName</name>
   <mac>FF:FF:FF:FF:FF:FF</mac>
   <dllPath>AnyPath</dllPath>
   <className>TestClass</className>
   <methodName>HelloWorld</methodName>
   <overloadValue>0</overloadValue>
</button4>

Parameter explanation:

name: This is just for debugging, so you can put in anything you like (I did out in the brand of the Dashbutton)
mac: In this tag goes the MAC-address of the button (if you do not know how to find the MAC of your Dashbutton just ask Google :) )
dllPath: This is the path to the DLL that should be triggered if you press the button
className: Put in the name of the class your method you want to call is in
methodName: The method which should be called by the button
overloadValue: If your method requires parameters, you need to put them in here
              (NOTE: at the moment it only supports a string but this might be changed later on)
